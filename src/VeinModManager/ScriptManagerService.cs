using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace VEIN_Item_And_Container_Modifier;

public static class ScriptManagerService
{
    public const string RegistryFileName = ".vein-manager-scripts.json";
    public const string AutomationFolderName = "Automation";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string GetScriptsFolder(string modFolder)
    {
        return Path.Combine(modFolder, "Scripts");
    }

    public static string GetAutomationFolder(string modFolder)
    {
        return Path.Combine(GetScriptsFolder(modFolder), AutomationFolderName);
    }

    public static string GetRegistryPath(string modFolder)
    {
        return Path.Combine(GetScriptsFolder(modFolder), RegistryFileName);
    }

    public static ManagedScriptRegistry LoadOrCreate(string modFolder)
    {
        EnsureValidModFolder(modFolder);
        var automationFolder = GetAutomationFolder(modFolder);
        Directory.CreateDirectory(automationFolder);
        var registryPath = GetRegistryPath(modFolder);
        var created = false;
        var changed = false;
        ManagedScriptRegistry registry;

        if (File.Exists(registryPath))
        {
            registry = JsonSerializer.Deserialize<ManagedScriptRegistry>(File.ReadAllText(registryPath), JsonOptions)
                ?? new ManagedScriptRegistry();
        }
        else
        {
            registry = CreateStarterRegistry(automationFolder);
            created = true;
            changed = true;
        }

        changed |= NormalizeRegistry(registry);
        changed |= SynchronizeWithFiles(registry, automationFolder);
        if (created || changed)
        {
            Save(modFolder, registry);
        }

        return registry;
    }

    public static ManagedScriptEntry CreateScript(string modFolder, string name, string runtime, string trigger)
    {
        var registry = LoadOrCreate(modFolder);
        var automationFolder = GetAutomationFolder(modFolder);
        Directory.CreateDirectory(automationFolder);

        var normalizedRuntime = NormalizeRuntime(runtime, ".lua");
        var fileName = UniqueFileName(automationFolder, SafeFileName(name), ExtensionForRuntime(normalizedRuntime));
        var fullPath = Path.Combine(automationFolder, fileName);
        File.WriteAllText(fullPath, DraftContent(name, normalizedRuntime, trigger));

        var entry = new ManagedScriptEntry
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = UniqueScriptName(registry, name),
            RelativePath = fileName,
            Runtime = normalizedRuntime,
            Trigger = string.IsNullOrWhiteSpace(trigger) ? "Manual" : trigger.Trim(),
            Description = DefaultDescription(name, normalizedRuntime, string.IsNullOrWhiteSpace(trigger) ? "Manual" : trigger.Trim()),
            Icon = DefaultIcon(name, normalizedRuntime),
            Enabled = true
        };

        registry.Scripts.Add(entry);
        Save(modFolder, registry);
        return entry;
    }

    public static ManagedScriptEntry DuplicateScript(string modFolder, string scriptId)
    {
        var registry = LoadOrCreate(modFolder);
        var source = FindScript(registry, scriptId);
        var automationFolder = GetAutomationFolder(modFolder);
        var sourcePath = ResolveScriptPath(modFolder, source);
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException("Script file was not found.", sourcePath);
        }

        var copyName = source.Name + " Copy";
        var extension = Path.GetExtension(source.RelativePath);
        var fileName = UniqueFileName(automationFolder, SafeFileName(copyName), extension);
        var fullPath = Path.Combine(automationFolder, fileName);
        File.Copy(sourcePath, fullPath, overwrite: false);

        var duplicate = new ManagedScriptEntry
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = UniqueScriptName(registry, copyName),
            RelativePath = fileName,
            Runtime = source.Runtime,
            Trigger = source.Trigger,
            Description = source.Description,
            Icon = source.Icon,
            Enabled = source.Enabled
        };

        registry.Scripts.Add(duplicate);
        Save(modFolder, registry);
        return duplicate;
    }

    public static ManagedScriptEntry SetEnabled(string modFolder, string scriptId, bool enabled)
    {
        var registry = LoadOrCreate(modFolder);
        var script = FindScript(registry, scriptId);
        script.Enabled = enabled;
        Save(modFolder, registry);
        return script;
    }

    public static ManagedScriptRunResult RunScript(string modFolder, string scriptId, int timeoutSeconds = 20)
    {
        var registry = LoadOrCreate(modFolder);
        var script = FindScript(registry, scriptId);
        var fullPath = ResolveScriptPath(modFolder, script);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Script file was not found.", fullPath);
        }

        return script.Runtime switch
        {
            "Batch" => RunProcess(
                Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe",
                "/d /s /c \"" + fullPath + "\"",
                Path.GetDirectoryName(fullPath)!,
                timeoutSeconds,
                script.Name),
            "PowerShell" => RunProcess(
                "powershell.exe",
                "-NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"" + fullPath + "\"",
                Path.GetDirectoryName(fullPath)!,
                timeoutSeconds,
                script.Name),
            _ => new ManagedScriptRunResult(false, true, 0, "Lua scripts are opened and integrated manually.", fullPath)
        };
    }

    public static ProcessStartInfo CreateEditorStartInfo(string scriptPath)
    {
        return new ProcessStartInfo
        {
            FileName = "notepad.exe",
            Arguments = QuoteProcessArgument(scriptPath),
            UseShellExecute = false
        };
    }


    public static string ResolveScriptPath(string modFolder, ManagedScriptEntry script)
    {
        EnsureValidModFolder(modFolder);
        var automationFolder = Path.GetFullPath(GetAutomationFolder(modFolder));
        var relativePath = script.RelativePath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(automationFolder, relativePath));
        var allowedRoot = automationFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Script path must stay inside the Automation folder.");
        }

        return fullPath;
    }

    private static ManagedScriptRegistry CreateStarterRegistry(string automationFolder)
    {
        var registry = new ManagedScriptRegistry();
        registry.Scripts.Add(CreateStarterScript(
            automationFolder,
            "Scheduled Restart",
            "Batch",
            "Every 6 hours",
            "scheduled-restart",
            "Gracefully warns players, saves the world and restarts the server process on a fixed interval to clear memory.",
            "Clock"));
        registry.Scripts.Add(CreateStarterScript(
            automationFolder,
            "Discord Status Webhook",
            "Lua",
            "On player join/leave",
            "discord-status-webhook",
            "Posts live player count, server status and join notifications to a configured Discord channel.",
            "Bell"));
        registry.Scripts.Add(CreateStarterScript(
            automationFolder,
            "Nightly Backup",
            "PowerShell",
            "Daily at 04:00",
            "nightly-backup",
            "Zips the save folder and config files to a timestamped archive, keeping the last 14 days of backups.",
            "Disk"));
        return registry;
    }

    private static ManagedScriptEntry CreateStarterScript(string automationFolder, string name, string runtime, string trigger, string fileNameStem, string description, string icon)
    {
        var extension = ExtensionForRuntime(runtime);
        var fileName = fileNameStem + extension;
        var fullPath = Path.Combine(automationFolder, fileName);
        if (!File.Exists(fullPath))
        {
            File.WriteAllText(fullPath, DraftContent(name, runtime, trigger));
        }

        return new ManagedScriptEntry
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = name,
            RelativePath = fileName,
            Runtime = runtime,
            Trigger = trigger,
            Description = description,
            Icon = icon,
            Enabled = !name.Equals("Nightly Backup", StringComparison.OrdinalIgnoreCase)
        };
    }

    private static bool NormalizeRegistry(ManagedScriptRegistry registry)
    {
        var changed = false;
        foreach (var script in registry.Scripts)
        {
            if (string.IsNullOrWhiteSpace(script.Id))
            {
                script.Id = Guid.NewGuid().ToString("N");
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(script.Name))
            {
                script.Name = FriendlyNameFromPath(script.RelativePath);
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(script.RelativePath))
            {
                script.RelativePath = SafeFileName(script.Name) + ExtensionForRuntime(script.Runtime);
                changed = true;
            }

            var normalizedRuntime = NormalizeRuntime(script.Runtime, Path.GetExtension(script.RelativePath));
            if (!script.Runtime.Equals(normalizedRuntime, StringComparison.Ordinal))
            {
                script.Runtime = normalizedRuntime;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(script.Trigger))
            {
                script.Trigger = "Manual";
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(script.Description))
            {
                script.Description = DefaultDescription(script.Name, script.Runtime, script.Trigger);
                changed = true;
            }

            var defaultIcon = DefaultIcon(script.Name, script.Runtime);
            if (string.IsNullOrWhiteSpace(script.Icon))
            {
                script.Icon = defaultIcon;
                changed = true;
            }
        }

        return changed;
    }

    private static bool SynchronizeWithFiles(ManagedScriptRegistry registry, string automationFolder)
    {
        var changed = false;
        var files = Directory.EnumerateFiles(automationFolder, "*", SearchOption.AllDirectories)
            .Where(IsSupportedScriptFile)
            .Select(path => Path.GetRelativePath(automationFolder, path))
            .Select(path => path.Replace(Path.DirectorySeparatorChar, '/'))
            .ToArray();

        var missing = registry.Scripts
            .Where(script => files.All(path => !path.Equals(script.RelativePath.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase)))
            .ToArray();
        foreach (var script in missing)
        {
            registry.Scripts.Remove(script);
            changed = true;
        }

        foreach (var relativePath in files)
        {
            if (registry.Scripts.Any(script => script.RelativePath.Replace('\\', '/').Equals(relativePath, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var name = FriendlyNameFromPath(relativePath);
            var runtime = NormalizeRuntime(string.Empty, Path.GetExtension(relativePath));
            registry.Scripts.Add(new ManagedScriptEntry
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = UniqueScriptName(registry, name),
                RelativePath = relativePath,
                Runtime = runtime,
                Trigger = "Manual",
                Description = DefaultDescription(name, runtime, "Manual"),
                Icon = DefaultIcon(name, runtime),
                Enabled = true
            });
            changed = true;
        }

        return changed;
    }

    private static ManagedScriptEntry FindScript(ManagedScriptRegistry registry, string scriptId)
    {
        var script = registry.Scripts.FirstOrDefault(item => item.Id.Equals(scriptId, StringComparison.Ordinal));
        if (script == null)
        {
            throw new InvalidOperationException("Script entry was not found.");
        }

        return script;
    }

    private static ManagedScriptRunResult RunProcess(string fileName, string arguments, string workingDirectory, int timeoutSeconds, string name)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        process.Start();
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(timeoutSeconds * 1000))
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch
            {
            }

            throw new TimeoutException(name + " exceeded the " + timeoutSeconds + " second limit.");
        }

        Task.WaitAll(stdout, stderr);
        var output = (stdout.Result + Environment.NewLine + stderr.Result).Trim();
        var summary = process.ExitCode == 0
            ? name + " finished successfully."
            : name + " exited with code " + process.ExitCode.ToString() + ".";
        return new ManagedScriptRunResult(true, false, process.ExitCode, summary, output);
    }

    private static void Save(string modFolder, ManagedScriptRegistry registry)
    {
        EnsureValidModFolder(modFolder);
        Directory.CreateDirectory(GetScriptsFolder(modFolder));
        File.WriteAllText(GetRegistryPath(modFolder), JsonSerializer.Serialize(registry, JsonOptions));
    }

    private static void EnsureValidModFolder(string modFolder)
    {
        if (!LuaModService.IsValidModFolder(modFolder))
        {
            throw new InvalidOperationException("Select a valid ItemAndContainerModifier folder first.");
        }
    }

    private static bool IsSupportedScriptFile(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Equals(".lua", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".bat", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".ps1", StringComparison.OrdinalIgnoreCase);
    }

    private static string ExtensionForRuntime(string runtime) => runtime switch
    {
        "Batch" => ".bat",
        "PowerShell" => ".ps1",
        _ => ".lua"
    };

    private static string NormalizeRuntime(string runtime, string extension)
    {
        if (runtime.Equals("Batch", StringComparison.OrdinalIgnoreCase) || extension.Equals(".bat", StringComparison.OrdinalIgnoreCase)) return "Batch";
        if (runtime.Equals("PowerShell", StringComparison.OrdinalIgnoreCase) || extension.Equals(".ps1", StringComparison.OrdinalIgnoreCase)) return "PowerShell";
        return "Lua";
    }

    private static string SafeFileName(string name)
    {
        var safe = Regex.Replace(name.Trim(), @"[^A-Za-z0-9._-]+", "-").Trim('-', '.', '_');
        return string.IsNullOrWhiteSpace(safe) ? "custom-script" : safe;
    }

    private static string UniqueFileName(string folder, string stem, string extension)
    {
        var candidate = stem + extension;
        if (!File.Exists(Path.Combine(folder, candidate))) return candidate;

        var suffix = DateTime.Now.ToString("yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture);
        return stem + "-" + suffix + extension;
    }

    private static string UniqueScriptName(ManagedScriptRegistry registry, string name)
    {
        if (registry.Scripts.All(script => !script.Name.Equals(name, StringComparison.OrdinalIgnoreCase))) return name;

        for (var index = 2; ; index++)
        {
            var candidate = name + " " + index;
            if (registry.Scripts.All(script => !script.Name.Equals(candidate, StringComparison.OrdinalIgnoreCase))) return candidate;
        }
    }

    private static string FriendlyNameFromPath(string relativePath)
    {
        var name = Path.GetFileNameWithoutExtension(relativePath).Replace('-', ' ').Replace('_', ' ').Trim();
        return string.IsNullOrWhiteSpace(name)
            ? "Custom Script"
            : string.Join(" ", name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(word => char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant()));
    }

    private static string DefaultIcon(string name, string runtime)
    {
        if (name.Contains("restart", StringComparison.OrdinalIgnoreCase)) return "Clock";
        if (name.Contains("webhook", StringComparison.OrdinalIgnoreCase)) return "Bell";
        if (name.Contains("backup", StringComparison.OrdinalIgnoreCase)) return "Disk";
        return runtime == "PowerShell" ? "Disk" : "Code";
    }

    private static string DefaultDescription(string name, string runtime, string trigger)
    {
        if (name.Contains("restart", StringComparison.OrdinalIgnoreCase))
        {
            return "Gracefully warns players, saves the world and restarts the server process on a fixed interval to clear memory.";
        }

        if (name.Contains("webhook", StringComparison.OrdinalIgnoreCase))
        {
            return "Posts live player count, server status and join notifications to a configured Discord channel.";
        }

        if (name.Contains("backup", StringComparison.OrdinalIgnoreCase))
        {
            return "Zips the save folder and config files to a timestamped archive, keeping the last 14 days of backups.";
        }

        return runtime + " automation triggered by " + trigger + ".";
    }

    private static string DraftContent(string name, string runtime, string trigger)
    {
        var safeName = NormalizeDraftText(name);
        var safeTrigger = NormalizeDraftText(trigger);
        return runtime switch
        {
            "Batch" => "@echo off\r\necho(VEIN automation: " + EscapeBatchEcho(safeName) + "\r\necho(Trigger: " + EscapeBatchEcho(safeTrigger) + "\r\n",
            "PowerShell" => "Write-Host \"VEIN automation: " + EscapePowerShellString(safeName) + "\"\r\nWrite-Host \"Trigger: " + EscapePowerShellString(safeTrigger) + "\"\r\n",
            _ => "print(\"VEIN automation: " + EscapeLuaString(safeName) + "\")\nprint(\"Trigger: " + EscapeLuaString(safeTrigger) + "\")\n"
        };
    }

    private static string NormalizeDraftText(string value)
    {
        return value.Replace("\r", " ").Replace("\n", " ").Trim();
    }

    private static string EscapeBatchEcho(string value)
    {
        return value
            .Replace("^", "^^")
            .Replace("%", "%%")
            .Replace("&", "^&")
            .Replace("|", "^|")
            .Replace("<", "^<")
            .Replace(">", "^>");
    }

    private static string EscapePowerShellString(string value)
    {
        return value.Replace("`", "``").Replace("\"", "`\"");
    }

    private static string EscapeLuaString(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private static string QuoteProcessArgument(string value)
    {
        return "\"" + value.Replace("\"", "\\\"") + "\"";
    }
}

public sealed class ManagedScriptRegistry
{
    public List<ManagedScriptEntry> Scripts { get; set; } = new();
}

public sealed class ManagedScriptEntry
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public string Runtime { get; set; } = "Lua";
    public string Trigger { get; set; } = "Manual";
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = "Code";
    public bool Enabled { get; set; } = true;
}

public sealed record ManagedScriptRunResult(bool Executed, bool ManualOnly, int ExitCode, string Summary, string Output);
