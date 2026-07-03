using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace VEIN_Item_And_Container_Modifier;

public static class ServerManagerService
{
    private const string AppDataFolderName = "VeinModManager";
    private const string HelperScriptName = "vein-server-helper.sh";
    private const string VeinSteamAppId = "1857950";
    private static readonly HashSet<string> AllowedHelperCommands = new(StringComparer.Ordinal)
    {
        "status",
        "read-config",
        "write-config",
        "backup",
        "logs",
        "restart"
    };

    public static LinuxHelperPackage GenerateLinuxHelperPackage()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppDataFolderName,
            "LinuxHelper");
        Directory.CreateDirectory(root);

        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var packageFolder = Path.Combine(root, "vein-linux-helper-" + stamp);
        Directory.CreateDirectory(packageFolder);

        var helperPath = Path.Combine(packageFolder, HelperScriptName);
        File.WriteAllText(helperPath, BuildLinuxHelperScript(), new UTF8Encoding(false));

        File.WriteAllText(
            Path.Combine(packageFolder, "install.sh"),
            BuildLinuxInstallScript(),
            new UTF8Encoding(false));

        File.WriteAllText(
            Path.Combine(packageFolder, "README.txt"),
            BuildLinuxHelperReadme(),
            new UTF8Encoding(false));

        var zipPath = packageFolder + ".zip";
        if (File.Exists(zipPath)) File.Delete(zipPath);
        ZipFile.CreateFromDirectory(packageFolder, zipPath, CompressionLevel.Optimal, includeBaseDirectory: true);

        return new LinuxHelperPackage(packageFolder, zipPath, HelperScriptName);
    }

    public static string SaveLinuxProfile(LinuxServerProfile profile)
    {
        var profileFolder = GetProfileFolder();
        Directory.CreateDirectory(profileFolder);

        var path = Path.Combine(profileFolder, "linux-profile.json");
        var safeProfile = profile with { Password = string.Empty };
        var json = JsonSerializer.Serialize(safeProfile, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json, new UTF8Encoding(false));
        return path;
    }

    public static string SaveWindowsProfile(WindowsServerProfile profile)
    {
        var profileFolder = GetProfileFolder();
        Directory.CreateDirectory(profileFolder);

        var path = Path.Combine(profileFolder, "windows-profile.json");
        var safeProfile = profile with { ServerPassword = string.Empty, RconPassword = string.Empty };
        var json = JsonSerializer.Serialize(safeProfile, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json, new UTF8Encoding(false));
        return path;
    }

    public static string CreateServerConfigBackup(string configPath)
    {
        if (string.IsNullOrWhiteSpace(configPath))
        {
            throw new InvalidOperationException("Config path is required.");
        }

        if (!File.Exists(configPath))
        {
            throw new FileNotFoundException("Config file was not found.", configPath);
        }

        var backupFolder = CreateUniqueBackupFolder(configPath);
        var backupPath = Path.Combine(backupFolder, Path.GetFileName(configPath));
        File.Copy(configPath, backupPath, overwrite: false);
        return backupPath;
    }

    private static string CreateUniqueBackupFolder(string configPath)
    {
        var backupRoot = Path.Combine(Path.GetDirectoryName(configPath)!, "VeinManagerBackups");
        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");

        for (var attempt = 0; attempt < 100; attempt++)
        {
            var folderName = attempt == 0
                ? stamp
                : string.Create(CultureInfo.InvariantCulture, $"{stamp}-{attempt:00}");
            var backupFolder = Path.Combine(backupRoot, folderName);
            if (Directory.Exists(backupFolder))
            {
                continue;
            }

            Directory.CreateDirectory(backupFolder);
            return backupFolder;
        }

        throw new IOException("Unable to create a unique backup folder.");
    }

    public static string WriteWindowsServerConfig(WindowsServerProfile profile, bool backupBeforeSave)
    {
        var configPath = ResolveWindowsConfigPath(profile.ServerFolderPath);
        Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);

        if (backupBeforeSave && File.Exists(configPath))
        {
            CreateServerConfigBackup(configPath);
        }

        File.WriteAllText(configPath, BuildGameIni(profile), new UTF8Encoding(false));
        return configPath;
    }

    public static string ResolveWindowsConfigPath(string serverFolderPath)
    {
        if (string.IsNullOrWhiteSpace(serverFolderPath))
        {
            throw new InvalidOperationException("Server folder path is required.");
        }

        return Path.Combine(
            serverFolderPath,
            "Vein",
            "Saved",
            "Config",
            "WindowsServer",
            "Game.ini");
    }

    public static Process? StartWindowsServer(string serverFolderPath)
    {
        if (string.IsNullOrWhiteSpace(serverFolderPath) || !Directory.Exists(serverFolderPath))
        {
            throw new DirectoryNotFoundException("Server folder was not found: " + serverFolderPath);
        }

        var candidates = new[]
        {
            Path.Combine(serverFolderPath, "VeinServer.exe"),
            Path.Combine(serverFolderPath, "Vein", "Binaries", "Win64", "VeinServer-Win64-Shipping.exe"),
            Path.Combine(serverFolderPath, "Vein", "Binaries", "Win64", "VeinServer.exe")
        };

        var executable = candidates.FirstOrDefault(File.Exists);
        if (executable == null) return null;

        return Process.Start(new ProcessStartInfo
        {
            FileName = executable,
            WorkingDirectory = Path.GetDirectoryName(executable)!,
            UseShellExecute = true
        });
    }

    public static ProcessStartInfo CreateWindowsValidateOrUpdateStartInfo(WindowsServerProfile profile)
    {
        if (string.IsNullOrWhiteSpace(profile.SteamCmdPath) || !File.Exists(profile.SteamCmdPath))
        {
            throw new FileNotFoundException("SteamCMD was not found.", profile.SteamCmdPath);
        }

        if (string.IsNullOrWhiteSpace(profile.ServerFolderPath))
        {
            throw new InvalidOperationException("Server folder path is required.");
        }

        Directory.CreateDirectory(profile.ServerFolderPath);

        var startInfo = new ProcessStartInfo
        {
            FileName = profile.SteamCmdPath,
            UseShellExecute = true,
            WorkingDirectory = Path.GetDirectoryName(profile.SteamCmdPath) ?? profile.ServerFolderPath
        };
        startInfo.ArgumentList.Add("+force_install_dir");
        startInfo.ArgumentList.Add(profile.ServerFolderPath);
        startInfo.ArgumentList.Add("+login");
        startInfo.ArgumentList.Add("anonymous");
        startInfo.ArgumentList.Add("+app_update");
        startInfo.ArgumentList.Add(VeinSteamAppId);
        startInfo.ArgumentList.Add("validate");
        startInfo.ArgumentList.Add("+quit");
        return startInfo;
    }

    public static Process? StartWindowsValidateOrUpdate(WindowsServerProfile profile)
    {
        return Process.Start(CreateWindowsValidateOrUpdateStartInfo(profile));
    }

    public static SshConnectionResult TestSshKeyConnection(LinuxServerProfile profile)
    {
        if (!profile.AuthenticationType.Equals("SSH Key", StringComparison.OrdinalIgnoreCase))
        {
            return new SshConnectionResult(false, "Password auth is not tested by the manager because passwords are not stored or passed to SSH.");
        }

        if (string.IsNullOrWhiteSpace(profile.Host)
            || string.IsNullOrWhiteSpace(profile.Username)
            || string.IsNullOrWhiteSpace(profile.SshKeyPath))
        {
            return new SshConnectionResult(false, "Host, username, and SSH key path are required.");
        }

        if (!File.Exists(profile.SshKeyPath))
        {
            return new SshConnectionResult(false, "SSH key file was not found.");
        }

        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "ssh",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            process.StartInfo.ArgumentList.Add("-p");
            process.StartInfo.ArgumentList.Add(profile.SshPort.ToString());
            process.StartInfo.ArgumentList.Add("-i");
            process.StartInfo.ArgumentList.Add(profile.SshKeyPath);
            process.StartInfo.ArgumentList.Add("-o");
            process.StartInfo.ArgumentList.Add("BatchMode=yes");
            process.StartInfo.ArgumentList.Add("-o");
            process.StartInfo.ArgumentList.Add("ConnectTimeout=8");
            process.StartInfo.ArgumentList.Add(profile.Username + "@" + profile.Host);
            process.StartInfo.ArgumentList.Add("printf vein-manager-ok");

            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            if (!process.WaitForExit(10000))
            {
                process.Kill(entireProcessTree: true);
                return new SshConnectionResult(false, "SSH connection timed out.");
            }

            return process.ExitCode == 0 && output.Contains("vein-manager-ok", StringComparison.Ordinal)
                ? new SshConnectionResult(true, "Connected")
                : new SshConnectionResult(false, string.IsNullOrWhiteSpace(error) ? "SSH test failed." : error.Trim());
        }
        catch (Exception ex)
        {
            return new SshConnectionResult(false, ex.Message);
        }
    }

    public static LinuxHelperResult RunLinuxHelperCommand(
        LinuxServerProfile profile,
        string helperCommand,
        string? standardInput = null,
        string? commandArgument = null)
    {
        ValidateLinuxProfileForSshKey(profile);
        if (!AllowedHelperCommands.Contains(helperCommand))
        {
            throw new InvalidOperationException("Unsupported helper command.");
        }

        var remoteCommand = BuildRemoteHelperCommand(profile, helperCommand, commandArgument);
        using var process = CreateSshProcess(profile, remoteCommand, redirectInput: standardInput != null);

        process.Start();
        if (standardInput != null)
        {
            process.StandardInput.Write(standardInput);
            process.StandardInput.Close();
        }

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        if (!process.WaitForExit(15000))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException("Linux helper command timed out.");
        }

        return new LinuxHelperResult(process.ExitCode, output, error);
    }

    public static string DownloadRemoteConfig(LinuxServerProfile profile)
    {
        var result = RunLinuxHelperCommand(profile, "read-config");
        result.ThrowIfFailed("Download remote config failed.");

        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppDataFolderName,
            "RemoteConfigs");
        Directory.CreateDirectory(folder);

        var path = Path.Combine(folder, "linux-game-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".ini");
        File.WriteAllText(path, result.Output, new UTF8Encoding(false));
        return path;
    }

    public static string UploadRemoteConfig(LinuxServerProfile profile, string localConfigPath)
    {
        if (string.IsNullOrWhiteSpace(localConfigPath) || !File.Exists(localConfigPath))
        {
            throw new FileNotFoundException("Local config file was not found.", localConfigPath);
        }

        var contents = File.ReadAllText(localConfigPath, Encoding.UTF8);
        var result = RunLinuxHelperCommand(profile, "write-config", contents);
        result.ThrowIfFailed("Upload remote config failed.");
        return result.Output.Trim();
    }

    public static string BackupRemoteConfig(LinuxServerProfile profile)
    {
        var result = RunLinuxHelperCommand(profile, "backup");
        result.ThrowIfFailed("Remote backup failed.");
        return result.Output.Trim();
    }

    public static string ReadRemoteLogs(LinuxServerProfile profile)
    {
        var result = RunLinuxHelperCommand(profile, "logs", commandArgument: "200");
        result.ThrowIfFailed("Read remote logs failed.");
        return result.Output;
    }

    public static string RestartRemoteServer(LinuxServerProfile profile)
    {
        var result = RunLinuxHelperCommand(profile, "restart");
        result.ThrowIfFailed("Remote restart failed.");
        return result.Output.Trim();
    }

    private static string GetProfileFolder()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            AppDataFolderName,
            "Profiles");
    }

    private static void ValidateLinuxProfileForSshKey(LinuxServerProfile profile)
    {
        if (!profile.AuthenticationType.Equals("SSH Key", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("SSH key authentication is required for manager-run helper actions.");
        }

        if (string.IsNullOrWhiteSpace(profile.Host)
            || string.IsNullOrWhiteSpace(profile.Username)
            || string.IsNullOrWhiteSpace(profile.SshKeyPath)
            || string.IsNullOrWhiteSpace(profile.RemoteConfigPath))
        {
            throw new InvalidOperationException("Host, username, SSH key path, and remote config path are required.");
        }

        if (!File.Exists(profile.SshKeyPath))
        {
            throw new FileNotFoundException("SSH key file was not found.", profile.SshKeyPath);
        }
    }

    private static Process CreateSshProcess(LinuxServerProfile profile, string remoteCommand, bool redirectInput)
    {
        var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "ssh",
            RedirectStandardInput = redirectInput,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        process.StartInfo.ArgumentList.Add("-p");
        process.StartInfo.ArgumentList.Add(profile.SshPort.ToString());
        process.StartInfo.ArgumentList.Add("-i");
        process.StartInfo.ArgumentList.Add(profile.SshKeyPath);
        process.StartInfo.ArgumentList.Add("-o");
        process.StartInfo.ArgumentList.Add("BatchMode=yes");
        process.StartInfo.ArgumentList.Add("-o");
        process.StartInfo.ArgumentList.Add("ConnectTimeout=8");
        process.StartInfo.ArgumentList.Add(profile.Username + "@" + profile.Host);
        process.StartInfo.ArgumentList.Add(remoteCommand);
        return process;
    }

    private static string BuildRemoteHelperCommand(
        LinuxServerProfile profile,
        string helperCommand,
        string? commandArgument)
    {
        var builder = new StringBuilder();
        builder.Append("VEIN_SERVER_PATH=");
        builder.Append(ShellQuote(profile.RemoteServerPath));
        builder.Append(" VEIN_CONFIG_PATH=");
        builder.Append(ShellQuote(profile.RemoteConfigPath));
        builder.Append(" VEIN_SERVICE_NAME=");
        builder.Append(ShellQuote("vein"));
        builder.Append(" ~/.local/bin/vein-server-helper ");
        builder.Append(helperCommand);

        if (!string.IsNullOrWhiteSpace(commandArgument))
        {
            builder.Append(' ');
            builder.Append(ShellQuote(commandArgument));
        }

        return builder.ToString();
    }

    private static string ShellQuote(string value)
    {
        return "'" + value.Replace("'", "'\"'\"'", StringComparison.Ordinal) + "'";
    }

    private static string BuildGameIni(WindowsServerProfile profile)
    {
        var builder = new StringBuilder();
        builder.AppendLine("[/Script/Vein.VeinGameSession]");
        builder.AppendLine("ServerName=" + ToIniValue(profile.ServerName));
        builder.AppendLine("ServerDescription=" + ToIniValue(profile.ServerDescription));
        builder.AppendLine("SessionName=" + ToIniValue(profile.SessionName));
        if (!string.IsNullOrWhiteSpace(profile.ServerPassword)) builder.AppendLine("ServerPassword=" + ToIniValue(profile.ServerPassword));
        builder.AppendLine("MapSelection=" + ToIniValue(profile.MapSelection));
        builder.AppendLine("GamePort=" + profile.GamePort);
        builder.AppendLine("QueryPort=" + profile.QueryPort);
        builder.AppendLine("MaxPlayers=" + profile.MaxPlayers);
        builder.AppendLine("EnableRCON=" + profile.EnableRcon.ToString().ToLowerInvariant());
        builder.AppendLine("RCONPort=" + profile.RconPort);
        if (!string.IsNullOrWhiteSpace(profile.RconPassword)) builder.AppendLine("RCONPassword=" + ToIniValue(profile.RconPassword));
        builder.AppendLine("EnableHTTPAPI=" + profile.EnableHttpApi.ToString().ToLowerInvariant());
        builder.AppendLine("HTTPAPIPort=" + profile.HttpApiPort);
        builder.AppendLine("SuperAdminSteamIDs=" + ToIniValue(profile.SuperAdminSteamIds));
        return builder.ToString();
    }

    private static string ToIniValue(string value)
    {
        return value
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Trim();
    }

    private static string BuildLinuxHelperScript()
    {
        return """
#!/usr/bin/env bash
set -euo pipefail

SERVER_PATH="${VEIN_SERVER_PATH:-}"
CONFIG_PATH="${VEIN_CONFIG_PATH:-}"
SERVICE_NAME="${VEIN_SERVICE_NAME:-vein}"

fail() {
  printf 'ERROR: %s\n' "$1" >&2
  exit 1
}

require_config() {
  [ -n "$CONFIG_PATH" ] || fail "VEIN_CONFIG_PATH is required."
  [ -f "$CONFIG_PATH" ] || fail "Config file not found: $CONFIG_PATH"
}

backup_config() {
  require_config
  backup_dir="$(dirname "$CONFIG_PATH")/VeinManagerBackups/$(date +%Y%m%d-%H%M%S)"
  mkdir -p "$backup_dir"
  cp -- "$CONFIG_PATH" "$backup_dir/$(basename "$CONFIG_PATH")"
  printf '%s\n' "$backup_dir/$(basename "$CONFIG_PATH")"
}

case "${1:-}" in
  status)
    systemctl is-active --quiet "$SERVICE_NAME" && printf 'running\n' || printf 'stopped\n'
    ;;
  read-config)
    require_config
    cat -- "$CONFIG_PATH"
    ;;
  write-config)
    [ -n "$CONFIG_PATH" ] || fail "VEIN_CONFIG_PATH is required."
    mkdir -p "$(dirname "$CONFIG_PATH")"
    [ -f "$CONFIG_PATH" ] && backup_config >/dev/null
    temp_file="$(mktemp)"
    cat > "$temp_file"
    install -m 600 "$temp_file" "$CONFIG_PATH"
    rm -f "$temp_file"
    printf 'updated\n'
    ;;
  backup)
    backup_config
    ;;
  logs)
    journalctl -u "$SERVICE_NAME" -n "${2:-200}" --no-pager
    ;;
  restart)
    systemctl restart "$SERVICE_NAME"
    printf 'restarted\n'
    ;;
  *)
    printf 'Usage: %s {status|read-config|write-config|backup|logs|restart}\n' "$0" >&2
    exit 2
    ;;
esac
""";
    }

    private static string BuildLinuxInstallScript()
    {
        return """
#!/usr/bin/env bash
set -euo pipefail

install_dir="${HOME}/.local/bin"
mkdir -p "$install_dir"
cp ./vein-server-helper.sh "$install_dir/vein-server-helper"
chmod 700 "$install_dir/vein-server-helper"
printf 'Installed %s\n' "$install_dir/vein-server-helper"
""";
    }

    private static string BuildLinuxHelperReadme()
    {
        return """
VEIN Linux Server Helper

This helper is intended for a private Linux VEIN server you administer.

It does:
- read the configured server config file
- write updated config from stdin
- create a backup before every write
- return service status
- return recent service logs
- restart the server only when explicitly called

It does not:
- delete server files
- expose a public API
- store passwords
- run arbitrary commands from the manager

Expected environment variables:
- VEIN_SERVER_PATH
- VEIN_CONFIG_PATH
- VEIN_SERVICE_NAME

Install:
chmod +x install.sh
./install.sh
""";
    }
}

public sealed record WindowsServerProfile(
    string ServerFolderPath,
    string SteamCmdPath,
    string ServerName,
    string ServerDescription,
    string SessionName,
    string ServerPassword,
    string MapSelection,
    int GamePort,
    int QueryPort,
    int MaxPlayers,
    bool EnableRcon,
    int RconPort,
    string RconPassword,
    bool EnableHttpApi,
    int HttpApiPort,
    string SuperAdminSteamIds);

public sealed record LinuxServerProfile(
    string Host,
    int SshPort,
    string Username,
    string AuthenticationType,
    string SshKeyPath,
    string Password,
    string RemoteServerPath,
    string RemoteConfigPath);

public sealed record LinuxHelperPackage(string FolderPath, string ZipPath, string ScriptName);

public sealed record SshConnectionResult(bool Connected, string Message);

public sealed record LinuxHelperResult(int ExitCode, string Output, string Error)
{
    public void ThrowIfFailed(string message)
    {
        if (ExitCode == 0) return;

        throw new InvalidOperationException(string.IsNullOrWhiteSpace(Error) ? message : message + " " + Error.Trim());
    }
}
