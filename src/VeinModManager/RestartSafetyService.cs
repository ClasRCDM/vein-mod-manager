using System.Diagnostics;
using System.Globalization;

namespace VEIN_Item_And_Container_Modifier;

public sealed record RestartSafetySettings(
    string ServerFolderPath,
    string SaveDirectory,
    string BackupDirectory,
    string LogFilePath,
    int ShutdownBudgetSeconds,
    int ForceIdleSeconds,
    int ExtendIntervalSeconds,
    int StartupWatchSeconds,
    int CorruptionThreshold,
    bool AutoRevert,
    bool LogRotatesPerLaunch);

public sealed record SafeShutdownResult(
    DateTime ShutdownStartedUtc,
    bool ProcessExited,
    bool WasForced,
    bool SaveVerified,
    bool BackupCreated,
    string? BackupDirectory,
    string Message);

public sealed record VerifiedBackupResult(
    bool SaveVerified,
    bool BackupCreated,
    string? BackupDirectory,
    DateTime LatestSaveWriteUtc,
    string Message);

public sealed record StartupCorruptionCheckResult(
    int CorruptionCount,
    int Threshold,
    bool IsCorrupt,
    string LogFilePath);

public static class RestartSafetyService
{
    public const string CorruptionFingerprint = "Tried to load dynamic component";

    public static RestartSafetySettings CreateDefaultWindowsSettings(string serverFolderPath)
    {
        var root = string.IsNullOrWhiteSpace(serverFolderPath) ? string.Empty : Path.GetFullPath(serverFolderPath.Trim());
        var saved = string.IsNullOrWhiteSpace(root) ? string.Empty : Path.Combine(root, "Vein", "Saved");
        return new RestartSafetySettings(
            root,
            string.IsNullOrWhiteSpace(saved) ? string.Empty : Path.Combine(saved, "SaveGames"),
            string.IsNullOrWhiteSpace(saved) ? string.Empty : Path.Combine(saved, "VeinManagerLastKnownGood"),
            string.IsNullOrWhiteSpace(saved) ? string.Empty : Path.Combine(saved, "Logs", "Vein.log"),
            60,
            20,
            15,
            90,
            25,
            false,
            true);
    }

    public static long GetStartupLogOffset(RestartSafetySettings settings)
    {
        if (settings.LogRotatesPerLaunch || string.IsNullOrWhiteSpace(settings.LogFilePath) || !File.Exists(settings.LogFilePath))
        {
            return 0;
        }

        return new FileInfo(settings.LogFilePath).Length;
    }

    public static StartupCorruptionCheckResult CountStartupCorruption(RestartSafetySettings settings, long startOffset)
    {
        var count = CountFingerprint(settings.LogFilePath, startOffset);
        return new StartupCorruptionCheckResult(count, settings.CorruptionThreshold, count >= settings.CorruptionThreshold, settings.LogFilePath);
    }

    public static StartupCorruptionCheckResult WatchStartupForCorruption(RestartSafetySettings settings, long startOffset, CancellationToken cancellationToken = default)
    {
        var watchSeconds = Math.Max(0, settings.StartupWatchSeconds);
        var deadline = DateTime.UtcNow.AddSeconds(watchSeconds);
        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Thread.Sleep(TimeSpan.FromSeconds(Math.Min(1, Math.Max(0, (deadline - DateTime.UtcNow).TotalSeconds))));
        }

        return CountStartupCorruption(settings, startOffset);
    }

    public static SafeShutdownResult StopWindowsServerSafely(Process process, RestartSafetySettings settings, bool createBackup, CancellationToken cancellationToken = default)
    {
        var start = DateTime.UtcNow;
        var wasForced = false;
        var processExited = HasProcessExited(process);
        var lastMtime = GetMaxSaveWriteUtc(settings.SaveDirectory);
        var lastChange = start;
        var deadline = start.AddSeconds(Math.Max(1, settings.ShutdownBudgetSeconds));
        var forceIdle = TimeSpan.FromSeconds(Math.Max(1, settings.ForceIdleSeconds));
        var extend = TimeSpan.FromSeconds(Math.Max(1, settings.ExtendIntervalSeconds));

        if (!processExited)
        {
            RequestGracefulWindowsStop(process);
        }

        while (!HasProcessExited(process))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var now = DateTime.UtcNow;
            var currentMtime = GetMaxSaveWriteUtc(settings.SaveDirectory);
            if (currentMtime > lastMtime)
            {
                lastMtime = currentMtime;
                lastChange = now;
            }

            if (now > deadline)
            {
                var idle = now - lastChange;
                if (idle >= forceIdle)
                {
                    ForceKill(process);
                    wasForced = true;
                    break;
                }

                deadline = now.Add(extend);
            }

            Thread.Sleep(TimeSpan.FromSeconds(3));
        }

        processExited = HasProcessExited(process);
        var backupResult = createBackup
            ? TryCreateVerifiedBackup(settings, start)
            : new VerifiedBackupResult(GetMaxSaveWriteUtc(settings.SaveDirectory) > start, false, null, GetMaxSaveWriteUtc(settings.SaveDirectory), "Backup skipped.");
        var message = backupResult.SaveVerified
            ? backupResult.BackupCreated ? "Graceful shutdown verified and backup refreshed." : "Graceful shutdown verified without backup refresh."
            : "Shutdown completed, but no save write was verified.";
        return new SafeShutdownResult(start, processExited, wasForced, backupResult.SaveVerified, backupResult.BackupCreated, backupResult.BackupDirectory, message);
    }

    public static VerifiedBackupResult TryCreateVerifiedBackup(RestartSafetySettings settings, DateTime shutdownStartedUtc)
    {
        if (string.IsNullOrWhiteSpace(settings.SaveDirectory) || !Directory.Exists(settings.SaveDirectory))
        {
            return new VerifiedBackupResult(false, false, null, DateTime.MinValue, "Save directory was not found.");
        }

        if (string.IsNullOrWhiteSpace(settings.BackupDirectory))
        {
            throw new InvalidOperationException("Backup directory is required.");
        }

        EnsureSafeMirrorPaths(settings.SaveDirectory, settings.BackupDirectory);
        var latestSaveWrite = GetMaxSaveWriteUtc(settings.SaveDirectory);
        if (latestSaveWrite <= shutdownStartedUtc)
        {
            return new VerifiedBackupResult(false, false, null, latestSaveWrite, "No save write was detected after shutdown began.");
        }

        MirrorDirectory(settings.SaveDirectory, settings.BackupDirectory);
        return new VerifiedBackupResult(true, true, settings.BackupDirectory, latestSaveWrite, "Verified save mirrored to backup.");
    }

    public static void RestoreVerifiedBackup(RestartSafetySettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.BackupDirectory) || !Directory.Exists(settings.BackupDirectory))
        {
            throw new DirectoryNotFoundException("Verified save backup was not found: " + settings.BackupDirectory);
        }

        if (string.IsNullOrWhiteSpace(settings.SaveDirectory))
        {
            throw new InvalidOperationException("Save directory is required.");
        }

        MirrorDirectory(settings.BackupDirectory, settings.SaveDirectory);
    }

    public static void MirrorDirectory(string sourceDirectory, string destinationDirectory)
    {
        if (string.IsNullOrWhiteSpace(sourceDirectory) || !Directory.Exists(sourceDirectory))
        {
            throw new DirectoryNotFoundException("Source directory was not found: " + sourceDirectory);
        }

        if (string.IsNullOrWhiteSpace(destinationDirectory))
        {
            throw new InvalidOperationException("Destination directory is required.");
        }

        EnsureSafeMirrorPaths(sourceDirectory, destinationDirectory);
        Directory.CreateDirectory(destinationDirectory);
        var sourceRoot = Path.GetFullPath(sourceDirectory);
        var destinationRoot = Path.GetFullPath(destinationDirectory);
        var sourceDirectories = Directory.EnumerateDirectories(sourceRoot, "*", SearchOption.AllDirectories).ToArray();
        var sourceFiles = Directory.EnumerateFiles(sourceRoot, "*", SearchOption.AllDirectories).ToArray();
        var sourceRelativeDirectories = sourceDirectories.Select(path => Path.GetRelativePath(sourceRoot, path)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var sourceRelativeFiles = sourceFiles.Select(path => Path.GetRelativePath(sourceRoot, path)).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var directory in sourceDirectories)
        {
            Directory.CreateDirectory(Path.Combine(destinationRoot, Path.GetRelativePath(sourceRoot, directory)));
        }

        foreach (var file in sourceFiles)
        {
            var target = Path.Combine(destinationRoot, Path.GetRelativePath(sourceRoot, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            if (ShouldCopy(file, target))
            {
                File.Copy(file, target, overwrite: true);
                File.SetLastWriteTimeUtc(target, File.GetLastWriteTimeUtc(file));
            }
        }

        foreach (var file in Directory.EnumerateFiles(destinationRoot, "*", SearchOption.AllDirectories))
        {
            if (!sourceRelativeFiles.Contains(Path.GetRelativePath(destinationRoot, file)))
            {
                File.Delete(file);
            }
        }

        foreach (var directory in Directory.EnumerateDirectories(destinationRoot, "*", SearchOption.AllDirectories).OrderByDescending(path => path.Length))
        {
            if (!sourceRelativeDirectories.Contains(Path.GetRelativePath(destinationRoot, directory)))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    private static int CountFingerprint(string logFilePath, long startOffset)
    {
        if (string.IsNullOrWhiteSpace(logFilePath) || !File.Exists(logFilePath))
        {
            return 0;
        }

        using var stream = new FileStream(logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        if (startOffset > 0 && startOffset < stream.Length)
        {
            stream.Seek(startOffset, SeekOrigin.Begin);
        }
        else if (startOffset >= stream.Length)
        {
            return 0;
        }

        using var reader = new StreamReader(stream);
        var count = 0;
        while (reader.ReadLine() is { } line)
        {
            count += CountOccurrences(line, CorruptionFingerprint);
        }

        return count;
    }

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }

    private static DateTime GetMaxSaveWriteUtc(string saveDirectory)
    {
        if (string.IsNullOrWhiteSpace(saveDirectory) || !Directory.Exists(saveDirectory))
        {
            return DateTime.MinValue;
        }

        var latest = Directory.GetLastWriteTimeUtc(saveDirectory);
        foreach (var path in Directory.EnumerateFileSystemEntries(saveDirectory, "*", SearchOption.AllDirectories))
        {
            var writeTime = File.GetLastWriteTimeUtc(path);
            if (writeTime > latest)
            {
                latest = writeTime;
            }
        }

        return latest;
    }

    private static void RequestGracefulWindowsStop(Process process)
    {
        try
        {
            if (process.CloseMainWindow())
            {
                return;
            }
        }
        catch (InvalidOperationException)
        {
            return;
        }

        if (HasProcessExited(process))
        {
            return;
        }

        try
        {
            using var taskkill = new Process();
            taskkill.StartInfo = new ProcessStartInfo
            {
                FileName = "taskkill",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            taskkill.StartInfo.ArgumentList.Add("/PID");
            taskkill.StartInfo.ArgumentList.Add(process.Id.ToString(CultureInfo.InvariantCulture));
            taskkill.Start();
            taskkill.WaitForExit(5000);
        }
        catch (Exception)
        {
        }
    }

    private static void ForceKill(Process process)
    {
        if (HasProcessExited(process))
        {
            return;
        }

        process.Kill(entireProcessTree: true);
        process.WaitForExit(5000);
    }

    private static bool HasProcessExited(Process process)
    {
        try
        {
            return process.HasExited;
        }
        catch (InvalidOperationException)
        {
            return true;
        }
    }

    private static bool ShouldCopy(string source, string target)
    {
        if (!File.Exists(target))
        {
            return true;
        }

        var sourceInfo = new FileInfo(source);
        var targetInfo = new FileInfo(target);
        return sourceInfo.Length != targetInfo.Length || sourceInfo.LastWriteTimeUtc > targetInfo.LastWriteTimeUtc;
    }

    private static void EnsureSafeMirrorPaths(string sourceDirectory, string destinationDirectory)
    {
        var source = NormalizeDirectoryPath(sourceDirectory);
        var destination = NormalizeDirectoryPath(destinationDirectory);
        if (string.Equals(source, destination, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Source and destination directories must be different.");
        }

        if (destination.StartsWith(source, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Backup directory cannot be inside the save directory.");
        }
    }

    private static string NormalizeDirectoryPath(string path)
    {
        return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
    }
}
