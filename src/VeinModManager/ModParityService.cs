using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VEIN_Item_And_Container_Modifier;

public static class ModParityService
{
    private const string AppDataFolderName = "VeinModManager";
    private const string ServerTemplateName = "VeinManagerParityServer";
    private const string ClientTemplateName = "VeinManagerParityClient";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static ModParityManifest BuildManifest(IEnumerable<string> modFolders, ModParitySettings settings)
    {
        var entries = modFolders
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => BuildEntry(Path.GetFullPath(path)))
            .OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (entries.Count == 0)
        {
            throw new InvalidOperationException("Add at least one approved mod folder first.");
        }

        return new ModParityManifest(
            DateTimeOffset.UtcNow,
            settings.AllowExtraMods,
            settings.EnforcementMode,
            settings.KickMessage,
            entries);
    }

    public static ModParityPackage ExportPackage(IEnumerable<string> modFolders, ModParitySettings settings, string templateRoot)
    {
        var manifest = BuildManifest(modFolders, settings);
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppDataFolderName,
            "ModParity");
        Directory.CreateDirectory(root);

        var packageFolder = Path.Combine(root, "vein-mod-parity-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"));
        Directory.CreateDirectory(packageFolder);

        WriteManifestFiles(packageFolder, manifest);
        CopyTemplate(templateRoot, ServerTemplateName, Path.Combine(packageFolder, ServerTemplateName));
        CopyTemplate(templateRoot, ClientTemplateName, Path.Combine(packageFolder, ClientTemplateName));
        WriteManifestFiles(Path.Combine(packageFolder, ServerTemplateName, "Scripts"), manifest);
        WriteManifestFiles(Path.Combine(packageFolder, ClientTemplateName, "Scripts"), manifest);
        File.WriteAllText(Path.Combine(packageFolder, "README.txt"), BuildPackageReadme(), new UTF8Encoding(false));

        var zipPath = packageFolder + ".zip";
        if (File.Exists(zipPath)) File.Delete(zipPath);
        ZipFile.CreateFromDirectory(packageFolder, zipPath, CompressionLevel.Optimal, includeBaseDirectory: true);
        return new ModParityPackage(packageFolder, zipPath, manifest);
    }

    public static string InstallWindowsServerMod(string serverFolderPath, IEnumerable<string> modFolders, ModParitySettings settings, string templateRoot)
    {
        if (string.IsNullOrWhiteSpace(serverFolderPath) || !Directory.Exists(serverFolderPath))
        {
            throw new InvalidOperationException("Select a valid Windows server folder first.");
        }

        var manifest = BuildManifest(modFolders, settings);
        var source = GetTemplatePath(templateRoot, ServerTemplateName);
        var target = Path.Combine(serverFolderPath, "Vein", "Binaries", "Win64", "ue4ss", "Mods", ServerTemplateName);
        BackupExistingFolder(target);
        CopyDirectory(source, target);
        WriteManifestFiles(Path.Combine(target, "Scripts"), manifest);
        return target;
    }

    public static void WriteManifestFiles(string folder, ModParityManifest manifest)
    {
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "expected_mods.json"), JsonSerializer.Serialize(manifest, JsonOptions), new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(folder, "expected_mods.lua"), BuildLuaManifest(manifest), new UTF8Encoding(false));
    }

    public static ModFolderValidationResult ValidateModFolder(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder))
        {
            return new ModFolderValidationResult(false, "Select a mod folder first.");
        }

        var fullPath = Path.GetFullPath(folder);
        if (!Directory.Exists(fullPath))
        {
            return new ModFolderValidationResult(false, "Mod folder was not found: " + fullPath);
        }

        var attributes = File.GetAttributes(fullPath);
        if (attributes.HasFlag(FileAttributes.ReparsePoint))
        {
            return new ModFolderValidationResult(false, "Mod folder cannot be a junction or symlink.");
        }

        var scriptsFolder = Path.Combine(fullPath, "Scripts");
        if (!Directory.Exists(scriptsFolder))
        {
            return new ModFolderValidationResult(false, "Mod folder must contain a Scripts folder.");
        }

        var mainScript = Path.Combine(scriptsFolder, "main.lua");
        if (!File.Exists(mainScript))
        {
            return new ModFolderValidationResult(false, "Mod folder must contain Scripts\\main.lua.");
        }

        var hashableFiles = EnumerateHashableFiles(fullPath).Take(1).Any();
        if (!hashableFiles)
        {
            return new ModFolderValidationResult(false, "Mod folder does not contain any files to verify.");
        }

        return new ModFolderValidationResult(true, "Valid UE4SS mod folder.");
    }

    private static ModParityEntry BuildEntry(string folder)
    {
        var validation = ValidateModFolder(folder);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(validation.Message);
        }

        var name = new DirectoryInfo(folder).Name;
        var files = BuildFileManifest(folder);
        return new ModParityEntry(name, DetectVersion(folder), ComputeFolderHash(files), files, folder);
    }

    private static string DetectVersion(string folder)
    {
        foreach (var fileName in new[] { "version.txt", "VERSION.txt" })
        {
            var path = Path.Combine(folder, fileName);
            if (File.Exists(path))
            {
                var text = File.ReadLines(path).FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(text)) return text.Trim();
            }
        }

        return "unknown";
    }

    private static IReadOnlyList<ModParityFile> BuildFileManifest(string folder)
    {
        return EnumerateHashableFiles(folder)
            .Select(file =>
            {
                var relative = NormalizeRelativePath(Path.GetRelativePath(folder, file));
                var info = new FileInfo(file);
                return new ModParityFile(relative, ComputeFileHash(file), info.Length);
            })
            .ToList();
    }

    private static IEnumerable<string> EnumerateHashableFiles(string folder)
    {
        return Directory
            .EnumerateFiles(folder, "*", SearchOption.AllDirectories)
            .Where(ShouldHashFile)
            .OrderBy(path => NormalizeRelativePath(Path.GetRelativePath(folder, path)), StringComparer.OrdinalIgnoreCase);
    }

    private static string ComputeFolderHash(IReadOnlyList<ModParityFile> files)
    {
        using var sha = SHA256.Create();
        foreach (var file in files)
        {
            var nameBytes = Encoding.UTF8.GetBytes(file.Path);
            sha.TransformBlock(nameBytes, 0, nameBytes.Length, null, 0);
            sha.TransformBlock(new byte[] { 0 }, 0, 1, null, 0);
            var bytes = Encoding.UTF8.GetBytes(file.Sha256);
            sha.TransformBlock(bytes, 0, bytes.Length, null, 0);
            sha.TransformBlock(new byte[] { 0 }, 0, 1, null, 0);
        }

        sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        return Convert.ToHexString(sha.Hash!).ToLowerInvariant();
    }

    private static string ComputeFileHash(string file)
    {
        using var stream = File.OpenRead(file);
        using var sha = SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(stream)).ToLowerInvariant();
    }

    private static string NormalizeRelativePath(string path)
    {
        return path.Replace('\\', '/');
    }

    private static bool ShouldHashFile(string path)
    {
        var fileName = Path.GetFileName(path);
        if (fileName.Equals("expected_mods.json", StringComparison.OrdinalIgnoreCase)) return false;
        if (fileName.Equals("expected_mods.lua", StringComparison.OrdinalIgnoreCase)) return false;
        if (fileName.EndsWith(".log", StringComparison.OrdinalIgnoreCase)) return false;

        var normalized = path.Replace('\\', '/');
        return !normalized.Contains("/Backups/", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildLuaManifest(ModParityManifest manifest)
    {
        var builder = new StringBuilder();
        builder.AppendLine("-- expected_mods.lua");
        builder.AppendLine("-- Generated by Vein Mod Manager.");
        builder.AppendLine("local ExpectedMods = {");
        builder.AppendLine("    generated_at_utc = " + ToLuaString(manifest.GeneratedAtUtc.ToString("O")) + ",");
        builder.AppendLine("    allow_extra_mods = " + (manifest.AllowExtraMods ? "true" : "false") + ",");
        builder.AppendLine("    enforcement_mode = " + ToLuaString(manifest.EnforcementMode) + ",");
        builder.AppendLine("    kick_message = " + ToLuaString(manifest.KickMessage) + ",");
        builder.AppendLine("    required_mods = {");

        foreach (var entry in manifest.RequiredMods)
        {
            builder.AppendLine("        {");
            builder.AppendLine("            name = " + ToLuaString(entry.Name) + ",");
            builder.AppendLine("            version = " + ToLuaString(entry.Version) + ",");
            builder.AppendLine("            hash = " + ToLuaString(entry.Hash) + ",");
            builder.AppendLine("            files = {");
            foreach (var file in entry.Files)
            {
                builder.AppendLine("                {");
                builder.AppendLine("                    path = " + ToLuaString(file.Path) + ",");
                builder.AppendLine("                    sha256 = " + ToLuaString(file.Sha256) + ",");
                builder.AppendLine("                    size = " + file.Size.ToString(System.Globalization.CultureInfo.InvariantCulture) + ",");
                builder.AppendLine("                },");
            }
            builder.AppendLine("            },");
            builder.AppendLine("        },");
        }

        builder.AppendLine("    },");
        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine("return ExpectedMods");
        return builder.ToString();
    }

    private static string ToLuaString(string value)
    {
        return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }

    private static void CopyTemplate(string templateRoot, string name, string destination)
    {
        CopyDirectory(GetTemplatePath(templateRoot, name), destination);
    }

    private static string GetTemplatePath(string templateRoot, string name)
    {
        var path = Path.Combine(templateRoot, name);
        if (!Directory.Exists(path))
        {
            throw new DirectoryNotFoundException("Bundled parity template was not found: " + path);
        }

        return path;
    }

    private static void BackupExistingFolder(string target)
    {
        if (!Directory.Exists(target)) return;

        var backup = target.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + "_backup_"
            + DateTime.Now.ToString("yyyyMMdd-HHmmss");
        Directory.Move(target, backup);
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);

        foreach (var directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(directory.Replace(source, destination, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var target = file.Replace(source, destination, StringComparison.OrdinalIgnoreCase);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }
    }

    private static string BuildPackageReadme()
    {
        return """
VEIN Mod Parity Package

This package is generated by Vein Mod Manager.

VeinManagerParityServer is the server-side UE4SS/VeinCF mod scaffold.
VeinManagerParityClient is the client-side reporter scaffold.

The generated expected_mods.lua and expected_mods.json files contain the approved mod list.

Phase 1 generates and installs the manifest package. Runtime handshake and kick enforcement must be tested in-game before it should be used as a public enforcement claim.
""";
    }
}

public sealed record ModParitySettings(
    bool AllowExtraMods,
    string EnforcementMode,
    string KickMessage);

public sealed record ModParityManifest(
    DateTimeOffset GeneratedAtUtc,
    bool AllowExtraMods,
    string EnforcementMode,
    string KickMessage,
    IReadOnlyList<ModParityEntry> RequiredMods);

public sealed record ModParityEntry(
    string Name,
    string Version,
    string Hash,
    IReadOnlyList<ModParityFile> Files,
    [property: JsonIgnore]
    string SourcePath);

public sealed record ModParityFile(
    string Path,
    string Sha256,
    long Size);

public sealed record ModParityPackage(
    string FolderPath,
    string ZipPath,
    ModParityManifest Manifest);

public sealed record ModFolderValidationResult(
    bool IsValid,
    string Message);
