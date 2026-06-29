using System.Text.RegularExpressions;
using VEIN_Item_And_Container_Modifier;

if (args.Length > 1)
{
    throw new InvalidOperationException("Usage: smoke-tests [ItemAndContainerModifier template folder]");
}

SmokeTests.TemplateOverride = args.Length == 1 ? Path.GetFullPath(args[0]) : null;
SmokeTests.RunAll();
Console.WriteLine("VEIN Mod Manager smoke tests passed.");

internal static class SmokeTests
{
    private static readonly Regex KeyEntryPattern = new(@"\[""(?<key>[^""]+)""\]\s*=\s*\{(?<body>.*?)(?=^\s*\},?)", RegexOptions.Multiline | RegexOptions.Singleline);
    private static readonly Regex ClassBlockPattern = new(@"classes\s*=\s*\{(?<body>.*?)(?=^\s*\},?)", RegexOptions.Multiline | RegexOptions.Singleline);
    private static readonly Regex QuotedStringPattern = new(@"""(?<value>[^""]+)""", RegexOptions.Multiline);
    private static readonly Regex CdoPathPattern = new(@"_CDOPath\s*=\s*""(?<path>[^""]+)""", RegexOptions.Multiline);

    public static string? TemplateOverride { get; set; }

    public static void RunAll()
    {
        SmokeWorkflow_LoadsAppliesBacksUpAndInstallsUiConfig();
        ServerManager_GeneratesSafeProfilesConfigsAndHelperPackage();
        ModParity_BuildsPackageAndInstallsWindowsServerMod();
        CreateBackup_ConsecutiveCallsUseUniqueFolders();
        ApplyConfig_ReplacesMalformedExistingUiConfig();
        LoadUiConfigStateFromFile_MalformedConfigThrowsInvalidDataException("local UiConfig = {}", "return UiConfig");
        LoadUiConfigStateFromFile_MalformedConfigThrowsInvalidDataException("return UiConfig", "UiConfig table");
        LoadUiConfigStateFromFile_MalformedConfigThrowsInvalidDataException("local UiConfig = { CategoryDefaults = { vehicles = { MaxWeight = \"unterminated } } }\nreturn UiConfig", "parse");
        LoadUiConfigStateFromFile_MalformedConfigThrowsInvalidDataException("local UiConfig = { CategoryDefaults = { vehicles = { MaxWeight = 999999999999999999999999999999999999999999999999999999999999 } } }\nreturn UiConfig", "parse");
        InstallUiConfig_RejectsMalformedSourceAndPreservesDestination();
        CategoryTemplates_DoNotContainDuplicateKeysOrClasses();
        CategoryTemplates_CdoPathTailsMatchKeys();
    }

    private static void SmokeWorkflow_LoadsAppliesBacksUpAndInstallsUiConfig()
    {
        var template = GetTemplateFolder();
        var testRoot = CreateTempRoot();
        var modFolder = Path.Combine(testRoot, "ItemAndContainerModifier");

        try
        {
            CopyDirectory(template, modFolder);
            SeedExistingUiConfig(modFolder);

            var loaded = LuaModService.LoadUiConfigState(modFolder);
            AssertEqual(7, loaded.CountEdits(LuaModService.LoadModData(modFolder)), "loaded generated edit count");
            AssertNumber(999999m, loaded.CategoryDefaults["backpacks"]["ExtraWeightCapacity"], "backpack category default");
            AssertNumber(999999m, loaded.CategoryDefaults["containers"]["MaxWeight"], "container category default");
            AssertNumber(999999m, loaded.ItemOverrides["backpacks"]["BP_BackpackSchool_C"]["ExtraWeightCapacity"], "backpack item override capacity");
            AssertNumber(1m, loaded.ItemOverrides["backpacks"]["BP_BackpackSchool_C"]["RunSpeedMultiplier"], "backpack item override speed");
            AssertNumber(999999m, loaded.ContainerWeightOverrides["containers"]["BP_Fridge_Residential_C"], "fridge override");
            AssertNumber(999999m, loaded.ContainerWeightOverrides["vehicles"]["BP_BoxTruck_C"], "box truck override");

            loaded.ContainerWeightOverrides["vehicles"]["BP_Ambulance_C"] = new LuaValue(12345m);
            var backupPath = LuaModService.CreateBackup(modFolder);
            AssertFileExists(Path.Combine(backupPath, "config.lua"), "backup config.lua");
            AssertFileExists(Path.Combine(backupPath, "ui_config.lua"), "backup ui_config.lua");
            AssertFileExists(Path.Combine(backupPath, "categories", "vehicles.lua"), "backup vehicles.lua");

            LuaModService.ApplyConfig(modFolder, loaded);
            AssertFileContains(Path.Combine(testRoot, "mods.txt"), "ItemAndContainerModifier : 1", "enabled mod entry");

            var reloaded = LuaModService.LoadUiConfigState(modFolder);
            AssertEqual(8, reloaded.CountEdits(LuaModService.LoadModData(modFolder)), "reloaded generated edit count");
            AssertNumber(999999m, reloaded.CategoryDefaults["backpacks"]["ExtraWeightCapacity"], "preserved backpack category default");
            AssertNumber(999999m, reloaded.ItemOverrides["backpacks"]["BP_BackpackSchool_C"]["ExtraWeightCapacity"], "preserved backpack item override capacity");
            AssertNumber(999999m, reloaded.ContainerWeightOverrides["containers"]["BP_Fridge_Residential_C"], "preserved fridge override");
            AssertNumber(12345m, reloaded.ContainerWeightOverrides["vehicles"]["BP_Ambulance_C"], "new ambulance override");

            var configText = File.ReadAllText(Path.Combine(modFolder, "Scripts", "config.lua"));
            AssertContains(configText, "UI_CONFIG_SUPPORT_BEGIN", "config.lua patch marker");

            var emptyStateFolder = Path.Combine(testRoot, "EmptyStateMod");
            CopyDirectory(template, emptyStateFolder);
            File.Delete(Path.Combine(emptyStateFolder, "Scripts", "ui_config.lua"));
            AssertEqual(0, LuaModService.LoadUiConfigState(emptyStateFolder).CountEdits(null), "missing ui_config edit count");

            var installTargetFolder = Path.Combine(testRoot, "InstallTargetMod");
            CopyDirectory(template, installTargetFolder);
            var sourceConfigPath = Path.Combine(testRoot, "import-source", "ui_config.lua");
            SeedImportSourceConfig(sourceConfigPath);
            var install = LuaModService.InstallUiConfig(installTargetFolder, sourceConfigPath);
            AssertFileExists(Path.Combine(install.BackupPath, "config.lua"), "install backup config.lua");
            AssertFileExists(install.InstalledPath, "installed ui_config.lua");

            var installed = LuaModService.LoadUiConfigState(installTargetFolder);
            AssertNumber(55555m, installed.CategoryDefaults["vehicles"]["MaxWeight"], "installed vehicle category max weight");
            AssertNumber(77777m, installed.ContainerWeightOverrides["vehicles"]["BP_Ambulance_C"], "installed ambulance override");
        }
        finally
        {
            DeleteDirectoryIfExists(testRoot);
        }
    }

    private static void ServerManager_GeneratesSafeProfilesConfigsAndHelperPackage()
    {
        var testRoot = CreateTempRoot();

        try
        {
            var helperPackage = ServerManagerService.GenerateLinuxHelperPackage();
            AssertFileExists(helperPackage.ZipPath, "generated Linux helper zip");
            AssertFileExists(Path.Combine(helperPackage.FolderPath, helperPackage.ScriptName), "generated Linux helper script");
            AssertFileContains(Path.Combine(helperPackage.FolderPath, helperPackage.ScriptName), "backup_config", "Linux helper backup behavior");
            AssertFileContains(Path.Combine(helperPackage.FolderPath, helperPackage.ScriptName), "write-config", "Linux helper write behavior");
            AssertFileContains(Path.Combine(helperPackage.FolderPath, helperPackage.ScriptName), "restart)", "Linux helper fixed restart command");

            var passwordLinuxProfile = new LinuxServerProfile(
                "127.0.0.1",
                22,
                "steam",
                "Password",
                "",
                "do-not-save-this",
                "/srv/vein",
                "/srv/vein/Vein/Saved/Config/LinuxServer/Game.ini");
            var linuxProfilePath = ServerManagerService.SaveLinuxProfile(passwordLinuxProfile);
            AssertFileExists(linuxProfilePath, "Linux server profile");
            AssertFileDoesNotContain(linuxProfilePath, "do-not-save-this", "Linux profile password omission");
            AssertThrowsContains<InvalidOperationException>(
                () => ServerManagerService.RunLinuxHelperCommand(passwordLinuxProfile, "status"),
                "SSH key authentication is required",
                "password auth helper command rejection");

            var serverRoot = Path.Combine(testRoot, "WindowsServer");
            var windowsProfile = new WindowsServerProfile(
                serverRoot,
                Path.Combine(testRoot, "steamcmd.exe"),
                "Smoke Server",
                "Generated by smoke test",
                "Server",
                "server-pass",
                "/Game/Vein/Maps/ChamplainValley?listen",
                7779,
                27015,
                16,
                true,
                27020,
                "rcon-pass",
                true,
                8080,
                "123456789");
            File.WriteAllText(windowsProfile.SteamCmdPath, "fake steamcmd");
            var windowsProfilePath = ServerManagerService.SaveWindowsProfile(windowsProfile);
            AssertFileDoesNotContain(windowsProfilePath, "server-pass", "Windows profile server password omission");
            AssertFileDoesNotContain(windowsProfilePath, "rcon-pass", "Windows profile RCON password omission");
            var updateStartInfo = ServerManagerService.CreateWindowsValidateOrUpdateStartInfo(windowsProfile);
            AssertEqual(windowsProfile.SteamCmdPath, updateStartInfo.FileName, "SteamCMD executable path");
            AssertSequenceContains(updateStartInfo.ArgumentList, new[] { "+app_update", "1857950", "validate" }, "SteamCMD validate arguments");
            AssertSequenceContains(updateStartInfo.ArgumentList, new[] { "+force_install_dir", serverRoot }, "SteamCMD install directory arguments");

            var windowsConfigPath = ServerManagerService.WriteWindowsServerConfig(windowsProfile, backupBeforeSave: true);
            AssertFileContains(windowsConfigPath, "ServerName=Smoke Server", "Windows server config");
            AssertFileContains(windowsConfigPath, "GamePort=7779", "Windows server config game port");
            File.AppendAllText(windowsConfigPath, "# old config marker");
            var rewrittenWindowsConfigPath = ServerManagerService.WriteWindowsServerConfig(windowsProfile, backupBeforeSave: true);
            AssertFileExists(rewrittenWindowsConfigPath, "rewritten Windows server config");
            AssertTrue(Directory.Exists(Path.Combine(Path.GetDirectoryName(windowsConfigPath)!, "VeinManagerBackups")), "Windows config backup folder was not created.");

            var injectedProfile = windowsProfile with { ServerName = "Good Server\nInjectedSetting=true" };
            var sanitizedConfigPath = ServerManagerService.WriteWindowsServerConfig(injectedProfile, backupBeforeSave: true);
            AssertFileContains(sanitizedConfigPath, "ServerName=Good Server InjectedSetting=true", "sanitized Windows server config");
            AssertFileDoesNotContain(sanitizedConfigPath, "\nInjectedSetting=true", "Windows server config newline injection prevention");
        }
        finally
        {
            DeleteDirectoryIfExists(testRoot);
        }
    }

    private static void ModParity_BuildsPackageAndInstallsWindowsServerMod()
    {
        var template = GetTemplateFolder();
        var testRoot = CreateTempRoot();
        var modFolder = Path.Combine(testRoot, "ItemAndContainerModifier");
        var serverRoot = Path.Combine(testRoot, "WindowsServer");

        try
        {
            CopyDirectory(template, modFolder);
            var parityTemplateRoot = Path.GetDirectoryName(template)!;
            var paritySettings = new ModParitySettings(
                AllowExtraMods: false,
                EnforcementMode: "Log Only",
                KickMessage: "Smoke test modpack mismatch.");
            var invalidParityFolder = Path.Combine(testRoot, "RenamedButNotAMod");
            Directory.CreateDirectory(invalidParityFolder);
            var invalidParityResult = ModParityService.ValidateModFolder(invalidParityFolder);
            AssertEqual(false, invalidParityResult.IsValid, "invalid mod parity folder rejected");
            AssertThrowsContains<InvalidOperationException>(
                () => ModParityService.BuildManifest(new[] { invalidParityFolder }, paritySettings),
                "Scripts",
                "invalid mod parity manifest rejection");

            var parityPackage = ModParityService.ExportPackage(new[] { modFolder }, paritySettings, parityTemplateRoot);
            AssertFileExists(parityPackage.ZipPath, "mod parity package zip");
            AssertFileContains(Path.Combine(parityPackage.FolderPath, "expected_mods.json"), "ItemAndContainerModifier", "mod parity json manifest");
            AssertFileContains(Path.Combine(parityPackage.FolderPath, "expected_mods.json"), "main.lua", "mod parity per-file manifest");
            AssertFileContains(Path.Combine(parityPackage.FolderPath, "expected_mods.json"), "Sha256", "mod parity file hashes");
            AssertFileDoesNotContain(Path.Combine(parityPackage.FolderPath, "expected_mods.json"), testRoot, "mod parity local path omission");
            AssertFileContains(Path.Combine(parityPackage.FolderPath, "expected_mods.lua"), "allow_extra_mods = false", "mod parity lua manifest");
            AssertFileContains(Path.Combine(parityPackage.FolderPath, "expected_mods.lua"), "files = {", "mod parity lua file list");
            AssertFileContains(
                Path.Combine(parityPackage.FolderPath, "VeinManagerParityServer", "Scripts", "main.lua"),
                "VeinManagerParityServer",
                "server parity template");

            Directory.CreateDirectory(serverRoot);
            var installedParityPath = ModParityService.InstallWindowsServerMod(serverRoot, new[] { modFolder }, paritySettings, parityTemplateRoot);
            AssertFileContains(Path.Combine(installedParityPath, "Scripts", "expected_mods.lua"), "Smoke test modpack mismatch.", "installed server parity manifest");
        }
        finally
        {
            DeleteDirectoryIfExists(testRoot);
        }
    }

    private static void CreateBackup_ConsecutiveCallsUseUniqueFolders()
    {
        var template = GetTemplateFolder();
        var testRoot = CreateTempRoot();
        var modFolder = Path.Combine(testRoot, "ItemAndContainerModifier");

        try
        {
            CopyDirectory(template, modFolder);

            var firstBackup = LuaModService.CreateBackup(modFolder);
            var secondBackup = LuaModService.CreateBackup(modFolder);

            AssertNotEqual(firstBackup, secondBackup, "backup folder uniqueness");
            AssertEqual(Path.Combine(modFolder, "Backups"), Directory.GetParent(firstBackup)!.FullName, "first backup parent");
            AssertEqual(Path.Combine(modFolder, "Backups"), Directory.GetParent(secondBackup)!.FullName, "second backup parent");
            AssertTrue(Directory.Exists(firstBackup), "Missing first backup folder: " + firstBackup);
            AssertTrue(Directory.Exists(secondBackup), "Missing second backup folder: " + secondBackup);
            AssertFileExists(Path.Combine(firstBackup, "config.lua"), "first backup config.lua");
            AssertFileExists(Path.Combine(secondBackup, "config.lua"), "second backup config.lua");
        }
        finally
        {
            DeleteDirectoryIfExists(testRoot);
        }
    }

    private static void ApplyConfig_ReplacesMalformedExistingUiConfig()
    {
        var template = GetTemplateFolder();
        var testRoot = CreateTempRoot();
        var modFolder = Path.Combine(testRoot, "ItemAndContainerModifier");

        try
        {
            CopyDirectory(template, modFolder);
            File.WriteAllText(Path.Combine(modFolder, "Scripts", "ui_config.lua"), "local UiConfig = {");

            var state = new UiConfigState();
            state.CategoryDefaults["vehicles"] = new Dictionary<string, LuaValue>(StringComparer.OrdinalIgnoreCase)
            {
                ["MaxWeight"] = new(12345m)
            };

            LuaModService.ApplyConfig(modFolder, state);

            var loaded = LuaModService.LoadUiConfigState(modFolder);
            AssertNumber(12345m, loaded.CategoryDefaults["vehicles"]["MaxWeight"], "replaced malformed ui config");
        }
        finally
        {
            DeleteDirectoryIfExists(testRoot);
        }
    }

    private static void LoadUiConfigStateFromFile_MalformedConfigThrowsInvalidDataException(string lua, string expectedMessage)
    {
        var testRoot = CreateTempRoot();
        var configPath = Path.Combine(testRoot, "ui_config.lua");

        try
        {
            File.WriteAllText(configPath, lua);

            AssertThrowsContains<InvalidDataException>(
                () => LuaModService.LoadUiConfigStateFromFile(configPath),
                expectedMessage,
                "malformed ui_config rejection");
        }
        finally
        {
            DeleteDirectoryIfExists(testRoot);
        }
    }

    private static void InstallUiConfig_RejectsMalformedSourceAndPreservesDestination()
    {
        var template = GetTemplateFolder();
        var testRoot = CreateTempRoot();
        var modFolder = Path.Combine(testRoot, "ItemAndContainerModifier");
        var sourceConfigPath = Path.Combine(testRoot, "source", "ui_config.lua");

        try
        {
            CopyDirectory(template, modFolder);
            SeedExistingUiConfig(modFolder);
            Directory.CreateDirectory(Path.GetDirectoryName(sourceConfigPath)!);
            File.WriteAllText(sourceConfigPath, "local UiConfig = {");

            AssertThrowsContains<InvalidDataException>(
                () => LuaModService.InstallUiConfig(modFolder, sourceConfigPath),
                "Invalid ui_config.lua",
                "malformed install source rejection");

            var loaded = LuaModService.LoadUiConfigState(modFolder);
            AssertEqual(7, loaded.CountEdits(LuaModService.LoadModData(modFolder)), "preserved destination edit count");
            AssertNumber(999999m, loaded.CategoryDefaults["backpacks"]["ExtraWeightCapacity"], "preserved destination backpack default");
        }
        finally
        {
            DeleteDirectoryIfExists(testRoot);
        }
    }

    private static void CategoryTemplates_DoNotContainDuplicateKeysOrClasses()
    {
        var duplicates = ReadCategoryIdentifiers()
            .GroupBy(identifier => new { identifier.FileName, identifier.Kind, identifier.Value })
            .Where(group => group.Count() > 1)
            .Select(group => group.Key.FileName + " " + group.Key.Kind + " " + group.Key.Value)
            .ToArray();

        AssertTrue(duplicates.Length == 0, "Duplicate category keys/classes found within a template:" + Environment.NewLine + string.Join(Environment.NewLine, duplicates));
    }

    private static void CategoryTemplates_CdoPathTailsMatchKeys()
    {
        var mismatches = ReadCdoPathEntries()
            .Select(entry => new { Entry = entry, Tail = GetCdoPathTail(entry.CdoPath) })
            .Where(entry => !StringComparer.Ordinal.Equals(entry.Entry.Key, entry.Tail))
            .Select(entry => entry.Entry.FileName + ": " + entry.Entry.Key + " != " + entry.Tail)
            .ToArray();

        AssertTrue(mismatches.Length == 0, "_CDOPath tails do not match category keys:" + Environment.NewLine + string.Join(Environment.NewLine, mismatches));
    }

    private static string GetTemplateFolder()
    {
        if (!string.IsNullOrWhiteSpace(TemplateOverride))
        {
            if (LuaModService.IsValidModFolder(TemplateOverride))
            {
                return TemplateOverride;
            }

            throw new DirectoryNotFoundException("Template folder is not a valid ItemAndContainerModifier mod: " + TemplateOverride);
        }

        foreach (var root in CandidateRoots())
        {
            foreach (var relativePath in new[]
            {
                Path.Combine("src", "VeinModManager", "ModTemplate", "ItemAndContainerModifier"),
                Path.Combine("ModTemplate", "ItemAndContainerModifier")
            })
            {
                var candidate = Path.Combine(root, relativePath);
                if (LuaModService.IsValidModFolder(candidate))
                {
                    return candidate;
                }
            }
        }

        throw new DirectoryNotFoundException("Could not locate ItemAndContainerModifier template folder.");
    }

    private static string GetSourceCategoryFolder()
    {
        foreach (var root in CandidateRoots())
        {
            var candidate = Path.Combine(root, "src", "VeinModManager", "ModTemplate", "ItemAndContainerModifier", "Scripts", "categories");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new DirectoryNotFoundException("Could not locate source category templates.");
    }

    private static IEnumerable<string> CandidateRoots()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            var directory = new DirectoryInfo(start);
            while (directory is not null)
            {
                if (seen.Add(directory.FullName))
                {
                    yield return directory.FullName;
                }

                directory = directory.Parent;
            }
        }
    }

    private static IReadOnlyList<CategoryIdentifier> ReadCategoryIdentifiers()
    {
        var identifiers = new List<CategoryIdentifier>();
        foreach (var path in Directory.EnumerateFiles(GetSourceCategoryFolder(), "*.lua", SearchOption.TopDirectoryOnly).OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase))
        {
            var text = StripLuaLineComments(File.ReadAllText(path));
            var fileName = Path.GetFileName(path);

            foreach (Match match in KeyEntryPattern.Matches(text))
            {
                identifiers.Add(new CategoryIdentifier(match.Groups["key"].Value, "key", fileName));
            }

            foreach (Match block in ClassBlockPattern.Matches(text))
            {
                foreach (Match match in QuotedStringPattern.Matches(block.Groups["body"].Value))
                {
                    identifiers.Add(new CategoryIdentifier(match.Groups["value"].Value, "class", fileName));
                }
            }
        }

        return identifiers;
    }

    private static IReadOnlyList<CdoPathEntry> ReadCdoPathEntries()
    {
        var entries = new List<CdoPathEntry>();
        foreach (var path in Directory.EnumerateFiles(GetSourceCategoryFolder(), "*.lua", SearchOption.TopDirectoryOnly).OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase))
        {
            var text = StripLuaLineComments(File.ReadAllText(path));
            var fileName = Path.GetFileName(path);

            foreach (Match match in KeyEntryPattern.Matches(text))
            {
                var cdoPath = CdoPathPattern.Match(match.Groups["body"].Value);
                if (cdoPath.Success)
                {
                    entries.Add(new CdoPathEntry(match.Groups["key"].Value, cdoPath.Groups["path"].Value, fileName));
                }
            }
        }

        return entries;
    }

    private static string StripLuaLineComments(string text)
    {
        var lines = text.Split('\n');
        for (var index = 0; index < lines.Length; index++)
        {
            var commentStart = lines[index].IndexOf("--", StringComparison.Ordinal);
            if (commentStart >= 0)
            {
                lines[index] = lines[index][..commentStart];
            }
        }

        return string.Join('\n', lines);
    }

    private static string GetCdoPathTail(string cdoPath)
    {
        const string marker = "Default__";
        var markerIndex = cdoPath.LastIndexOf(marker, StringComparison.Ordinal);
        if (markerIndex >= 0)
        {
            return cdoPath[(markerIndex + marker.Length)..];
        }

        var separatorIndex = cdoPath.LastIndexOfAny(new[] { '/', '\\' });
        var fileName = separatorIndex >= 0 ? cdoPath[(separatorIndex + 1)..] : cdoPath;
        var dotIndex = fileName.LastIndexOf('.');
        return dotIndex >= 0 ? fileName[(dotIndex + 1)..] : fileName;
    }

    private static void SeedExistingUiConfig(string modFolder)
    {
        var uiConfigPath = Path.Combine(modFolder, "Scripts", "ui_config.lua");
        File.WriteAllText(uiConfigPath, """
local UiConfig = {
    EnabledCategories = {
        vehicles = true,
        containers = true,
        backpacks = true,
    },

    CategoryDefaults = {
        backpacks = {
            ExtraWeightCapacity = 999999,
        },
        containers = 999999,
        vehicles = 999999,
    },

    ItemOverrides = {
        backpacks = {
            ["BP_BackpackSchool_C"] = {
                ExtraWeightCapacity = 999999,
                RunSpeedMultiplier = 1,
            },
        },
    },

    ContainerWeightOverrides = {
        containers = {
            ["BP_Fridge_Residential_C"] = 999999,
        },
        vehicles = {
            ["BP_BoxTruck_C"] = 999999,
        },
    },
}

return UiConfig
""");
    }

    private static void SeedImportSourceConfig(string uiConfigPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(uiConfigPath)!);
        File.WriteAllText(uiConfigPath, """
local UiConfig = {
    CategoryDefaults = {
        vehicles = 55555,
    },

    ContainerWeightOverrides = {
        vehicles = {
            ["BP_Ambulance_C"] = 77777,
        },
    },
}

return UiConfig
""");
    }

    private static string CreateTempRoot()
    {
        var path = Path.Combine(Path.GetTempPath(), "vein-mod-manager-smoke-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
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

    private static void DeleteDirectoryIfExists(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }

    private static void AssertFileExists(string path, string label)
    {
        if (!File.Exists(path))
        {
            throw new InvalidOperationException("Missing " + label + ": " + path);
        }
    }

    private static void AssertFileContains(string path, string expected, string label)
    {
        AssertFileExists(path, label);
        var text = File.ReadAllText(path);
        if (!text.Contains(expected, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"{label}: expected to find {expected}");
        }
    }

    private static void AssertFileDoesNotContain(string path, string unexpected, string label)
    {
        AssertFileExists(path, label);
        var text = File.ReadAllText(path);
        if (text.Contains(unexpected, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"{label}: did not expect to find {unexpected}");
        }
    }

    private static void AssertContains(string text, string expected, string label)
    {
        if (!text.Contains(expected, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"{label}: expected to find {expected}");
        }
    }

    private static void AssertThrowsContains<TException>(Action action, string expectedMessagePart, string label)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException ex) when (ex.Message.Contains(expectedMessagePart, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"{label}: expected {typeof(TException).Name} containing {expectedMessagePart}, got {ex.GetType().Name}: {ex.Message}");
        }

        throw new InvalidOperationException($"{label}: expected exception containing {expectedMessagePart}");
    }

    private static void AssertSequenceContains(IList<string> values, string[] expected, string label)
    {
        for (var start = 0; start <= values.Count - expected.Length; start++)
        {
            var matches = true;
            for (var offset = 0; offset < expected.Length; offset++)
            {
                if (!string.Equals(values[start + offset], expected[offset], StringComparison.Ordinal))
                {
                    matches = false;
                    break;
                }
            }

            if (matches) return;
        }

        throw new InvalidOperationException($"{label}: expected sequence {string.Join(" ", expected)}");
    }

    private static void AssertEqual<T>(T expected, T actual, string label)
        where T : IEquatable<T>
    {
        if (!actual.Equals(expected))
        {
            throw new InvalidOperationException($"{label}: expected {expected}, got {actual}");
        }
    }

    private static void AssertNotEqual<T>(T unexpected, T actual, string label)
        where T : IEquatable<T>
    {
        if (actual.Equals(unexpected))
        {
            throw new InvalidOperationException($"{label}: did not expect {unexpected}");
        }
    }

    private static void AssertTrue(bool condition, string label)
    {
        if (!condition)
        {
            throw new InvalidOperationException(label);
        }
    }

    private static void AssertNumber(decimal expected, LuaValue actual, string label)
    {
        if (actual.Value is not decimal number || number != expected)
        {
            throw new InvalidOperationException($"{label}: expected {expected}, got {actual.ToLua()}");
        }
    }

    private sealed record CategoryIdentifier(string Value, string Kind, string FileName);

    private sealed record CdoPathEntry(string Key, string CdoPath, string FileName);
}
