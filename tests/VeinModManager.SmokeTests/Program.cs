using System.Text.RegularExpressions;
using VEIN_Item_And_Container_Modifier;
using Xunit;

public sealed class SmokeTests
{
    private static readonly Regex KeyEntryPattern = new(@"\[""(?<key>[^""]+)""\]\s*=\s*\{(?<body>.*?)(?=^\s*\},?)", RegexOptions.Multiline | RegexOptions.Singleline);
    private static readonly Regex ClassBlockPattern = new(@"classes\s*=\s*\{(?<body>.*?)(?=^\s*\},?)", RegexOptions.Multiline | RegexOptions.Singleline);
    private static readonly Regex QuotedStringPattern = new(@"""(?<value>[^""]+)""", RegexOptions.Multiline);
    private static readonly Regex CdoPathPattern = new(@"_CDOPath\s*=\s*""(?<path>[^""]+)""", RegexOptions.Multiline);

    [Fact]
    public void SmokeWorkflow_LoadsAppliesBacksUpAndInstallsUiConfig()
    {
        var template = GetTemplateFolder();
        var testRoot = CreateTempRoot();
        var modFolder = Path.Combine(testRoot, "ItemAndContainerModifier");

        try
        {
            CopyDirectory(template, modFolder);
            SeedExistingUiConfig(modFolder);

            var loaded = LuaModService.LoadUiConfigState(modFolder);
            Assert.Equal(7, loaded.CountEdits(LuaModService.LoadModData(modFolder)));
            AssertNumber(999999m, loaded.CategoryDefaults["backpacks"]["ExtraWeightCapacity"]);
            AssertNumber(999999m, loaded.CategoryDefaults["containers"]["MaxWeight"]);
            AssertNumber(999999m, loaded.ItemOverrides["backpacks"]["BP_BackpackSchool_C"]["ExtraWeightCapacity"]);
            AssertNumber(1m, loaded.ItemOverrides["backpacks"]["BP_BackpackSchool_C"]["RunSpeedMultiplier"]);
            AssertNumber(999999m, loaded.ContainerWeightOverrides["containers"]["BP_Fridge_Residential_C"]);
            AssertNumber(999999m, loaded.ContainerWeightOverrides["vehicles"]["BP_BoxTruck_C"]);

            loaded.ContainerWeightOverrides["vehicles"]["BP_Ambulance_C"] = new LuaValue(12345m);
            var backupPath = LuaModService.CreateBackup(modFolder);
            AssertFileExists(Path.Combine(backupPath, "config.lua"));
            AssertFileExists(Path.Combine(backupPath, "ui_config.lua"));
            AssertFileExists(Path.Combine(backupPath, "categories", "vehicles.lua"));

            LuaModService.ApplyConfig(modFolder, loaded);

            var reloaded = LuaModService.LoadUiConfigState(modFolder);
            Assert.Equal(8, reloaded.CountEdits(LuaModService.LoadModData(modFolder)));
            AssertNumber(999999m, reloaded.CategoryDefaults["backpacks"]["ExtraWeightCapacity"]);
            AssertNumber(999999m, reloaded.ItemOverrides["backpacks"]["BP_BackpackSchool_C"]["ExtraWeightCapacity"]);
            AssertNumber(999999m, reloaded.ContainerWeightOverrides["containers"]["BP_Fridge_Residential_C"]);
            AssertNumber(12345m, reloaded.ContainerWeightOverrides["vehicles"]["BP_Ambulance_C"]);

            var configText = File.ReadAllText(Path.Combine(modFolder, "Scripts", "config.lua"));
            Assert.Contains("UI_CONFIG_SUPPORT_BEGIN", configText, StringComparison.Ordinal);

            var emptyStateFolder = Path.Combine(testRoot, "EmptyStateMod");
            CopyDirectory(template, emptyStateFolder);
            File.Delete(Path.Combine(emptyStateFolder, "Scripts", "ui_config.lua"));
            Assert.Equal(0, LuaModService.LoadUiConfigState(emptyStateFolder).CountEdits(null));

            var installTargetFolder = Path.Combine(testRoot, "InstallTargetMod");
            CopyDirectory(template, installTargetFolder);
            var sourceConfigPath = Path.Combine(testRoot, "import-source", "ui_config.lua");
            SeedImportSourceConfig(sourceConfigPath);
            var install = LuaModService.InstallUiConfig(installTargetFolder, sourceConfigPath);
            AssertFileExists(Path.Combine(install.BackupPath, "config.lua"));
            AssertFileExists(install.InstalledPath);

            var installed = LuaModService.LoadUiConfigState(installTargetFolder);
            AssertNumber(55555m, installed.CategoryDefaults["vehicles"]["MaxWeight"]);
            AssertNumber(77777m, installed.ContainerWeightOverrides["vehicles"]["BP_Ambulance_C"]);

            var helperPackage = ServerManagerService.GenerateLinuxHelperPackage();
            AssertFileExists(helperPackage.ZipPath);
            AssertFileExists(Path.Combine(helperPackage.FolderPath, helperPackage.ScriptName));
            AssertFileContains(Path.Combine(helperPackage.FolderPath, helperPackage.ScriptName), "backup_config");
            AssertFileContains(Path.Combine(helperPackage.FolderPath, helperPackage.ScriptName), "write-config");
            AssertFileContains(Path.Combine(helperPackage.FolderPath, helperPackage.ScriptName), "restart)");

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
            AssertFileExists(linuxProfilePath);
            AssertFileDoesNotContain(linuxProfilePath, "do-not-save-this");
            AssertThrowsContains(
                () => ServerManagerService.RunLinuxHelperCommand(passwordLinuxProfile, "status"),
                "SSH key authentication is required");

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
            AssertFileDoesNotContain(windowsProfilePath, "server-pass");
            AssertFileDoesNotContain(windowsProfilePath, "rcon-pass");
            var updateStartInfo = ServerManagerService.CreateWindowsValidateOrUpdateStartInfo(windowsProfile);
            Assert.Equal(windowsProfile.SteamCmdPath, updateStartInfo.FileName);
            AssertSequenceContains(updateStartInfo.ArgumentList, new[] { "+app_update", "1857950", "validate" });
            AssertSequenceContains(updateStartInfo.ArgumentList, new[] { "+force_install_dir", serverRoot });

            var windowsConfigPath = ServerManagerService.WriteWindowsServerConfig(windowsProfile, backupBeforeSave: true);
            AssertFileContains(windowsConfigPath, "ServerName=Smoke Server");
            AssertFileContains(windowsConfigPath, "GamePort=7779");
            File.AppendAllText(windowsConfigPath, "# old config marker");
            var rewrittenWindowsConfigPath = ServerManagerService.WriteWindowsServerConfig(windowsProfile, backupBeforeSave: true);
            AssertFileExists(rewrittenWindowsConfigPath);
            Assert.True(Directory.Exists(Path.Combine(Path.GetDirectoryName(windowsConfigPath)!, "VeinManagerBackups")), "Windows config backup folder was not created.");

            var injectedProfile = windowsProfile with { ServerName = "Good Server\nInjectedSetting=true" };
            var sanitizedConfigPath = ServerManagerService.WriteWindowsServerConfig(injectedProfile, backupBeforeSave: true);
            AssertFileContains(sanitizedConfigPath, "ServerName=Good Server InjectedSetting=true");
            AssertFileDoesNotContain(sanitizedConfigPath, "\nInjectedSetting=true");

            var parityTemplateRoot = Path.GetDirectoryName(template)!;
            var paritySettings = new ModParitySettings(
                AllowExtraMods: false,
                EnforcementMode: "Log Only",
                KickMessage: "Smoke test modpack mismatch.");
            var invalidParityFolder = Path.Combine(testRoot, "RenamedButNotAMod");
            Directory.CreateDirectory(invalidParityFolder);
            var invalidParityResult = ModParityService.ValidateModFolder(invalidParityFolder);
            Assert.False(invalidParityResult.IsValid);
            AssertThrowsContains(
                () => ModParityService.BuildManifest(new[] { invalidParityFolder }, paritySettings),
                "Scripts");

            var parityPackage = ModParityService.ExportPackage(new[] { modFolder }, paritySettings, parityTemplateRoot);
            AssertFileExists(parityPackage.ZipPath);
            AssertFileContains(Path.Combine(parityPackage.FolderPath, "expected_mods.json"), "ItemAndContainerModifier");
            AssertFileContains(Path.Combine(parityPackage.FolderPath, "expected_mods.json"), "main.lua");
            AssertFileContains(Path.Combine(parityPackage.FolderPath, "expected_mods.json"), "Sha256");
            AssertFileDoesNotContain(Path.Combine(parityPackage.FolderPath, "expected_mods.json"), testRoot);
            AssertFileContains(Path.Combine(parityPackage.FolderPath, "expected_mods.lua"), "allow_extra_mods = false");
            AssertFileContains(Path.Combine(parityPackage.FolderPath, "expected_mods.lua"), "files = {");
            AssertFileContains(
                Path.Combine(parityPackage.FolderPath, "VeinManagerParityServer", "Scripts", "main.lua"),
                "VeinManagerParityServer");

            var installedParityPath = ModParityService.InstallWindowsServerMod(serverRoot, new[] { modFolder }, paritySettings, parityTemplateRoot);
            AssertFileContains(Path.Combine(installedParityPath, "Scripts", "expected_mods.lua"), "Smoke test modpack mismatch.");
        }
        finally
        {
            DeleteDirectoryIfExists(testRoot);
        }
    }

    [Fact]
    public void CreateBackup_ConsecutiveCallsUseUniqueFolders()
    {
        var template = GetTemplateFolder();
        var testRoot = CreateTempRoot();
        var modFolder = Path.Combine(testRoot, "ItemAndContainerModifier");

        try
        {
            CopyDirectory(template, modFolder);

            var firstBackup = LuaModService.CreateBackup(modFolder);
            var secondBackup = LuaModService.CreateBackup(modFolder);

            Assert.NotEqual(firstBackup, secondBackup);
            Assert.Equal(Path.Combine(modFolder, "Backups"), Directory.GetParent(firstBackup)!.FullName);
            Assert.Equal(Path.Combine(modFolder, "Backups"), Directory.GetParent(secondBackup)!.FullName);
            Assert.True(Directory.Exists(firstBackup), "Missing first backup folder: " + firstBackup);
            Assert.True(Directory.Exists(secondBackup), "Missing second backup folder: " + secondBackup);
            AssertFileExists(Path.Combine(firstBackup, "config.lua"));
            AssertFileExists(Path.Combine(secondBackup, "config.lua"));
        }
        finally
        {
            DeleteDirectoryIfExists(testRoot);
        }
    }

    [Fact]
    public void ApplyConfig_ReplacesMalformedExistingUiConfig()
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
            AssertNumber(12345m, loaded.CategoryDefaults["vehicles"]["MaxWeight"]);
        }
        finally
        {
            DeleteDirectoryIfExists(testRoot);
        }
    }

    [Theory]
    [InlineData("local UiConfig = {}", "return UiConfig")]
    [InlineData("return UiConfig", "UiConfig table")]
    [InlineData("local UiConfig = { CategoryDefaults = { vehicles = { MaxWeight = \"unterminated } } }\nreturn UiConfig", "parse")]
    [InlineData("local UiConfig = { CategoryDefaults = { vehicles = { MaxWeight = 999999999999999999999999999999999999999999999999999999999999 } } }\nreturn UiConfig", "parse")]
    public void LoadUiConfigStateFromFile_MalformedConfigThrowsInvalidDataException(string lua, string expectedMessage)
    {
        var testRoot = CreateTempRoot();
        var configPath = Path.Combine(testRoot, "ui_config.lua");

        try
        {
            File.WriteAllText(configPath, lua);

            var ex = Assert.Throws<InvalidDataException>(() => LuaModService.LoadUiConfigStateFromFile(configPath));
            Assert.Contains(expectedMessage, ex.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DeleteDirectoryIfExists(testRoot);
        }
    }

    [Fact]
    public void InstallUiConfig_RejectsMalformedSourceAndPreservesDestination()
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

            Assert.Throws<InvalidDataException>(() => LuaModService.InstallUiConfig(modFolder, sourceConfigPath));

            var loaded = LuaModService.LoadUiConfigState(modFolder);
            Assert.Equal(7, loaded.CountEdits(LuaModService.LoadModData(modFolder)));
            AssertNumber(999999m, loaded.CategoryDefaults["backpacks"]["ExtraWeightCapacity"]);
        }
        finally
        {
            DeleteDirectoryIfExists(testRoot);
        }
    }

    [Fact]
    public void CategoryTemplates_DoNotContainDuplicateKeysOrClasses()
    {
        var duplicates = ReadCategoryIdentifiers()
            .GroupBy(identifier => new { identifier.FileName, identifier.Kind, identifier.Value })
            .Where(group => group.Count() > 1)
            .Select(group => group.Key.FileName + " " + group.Key.Kind + " " + group.Key.Value)
            .ToArray();

        Assert.True(duplicates.Length == 0, "Duplicate category keys/classes found within a template:" + Environment.NewLine + string.Join(Environment.NewLine, duplicates));
    }

    [Fact]
    public void CategoryTemplates_CdoPathTailsMatchKeys()
    {
        var mismatches = ReadCdoPathEntries()
            .Select(entry => new { Entry = entry, Tail = GetCdoPathTail(entry.CdoPath) })
            .Where(entry => !StringComparer.Ordinal.Equals(entry.Entry.Key, entry.Tail))
            .Select(entry => entry.Entry.FileName + ": " + entry.Entry.Key + " != " + entry.Tail)
            .ToArray();

        Assert.True(mismatches.Length == 0, "_CDOPath tails do not match category keys:" + Environment.NewLine + string.Join(Environment.NewLine, mismatches));
    }

    private static string GetTemplateFolder()
    {
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

    private static void AssertFileExists(string path)
    {
        Assert.True(File.Exists(path), "Missing file: " + path);
    }

    private static void AssertFileContains(string path, string expected)
    {
        AssertFileExists(path);
        var text = File.ReadAllText(path);
        Assert.Contains(expected, text, StringComparison.Ordinal);
    }

    private static void AssertFileDoesNotContain(string path, string unexpected)
    {
        AssertFileExists(path);
        var text = File.ReadAllText(path);
        Assert.DoesNotContain(unexpected, text, StringComparison.Ordinal);
    }

    private static void AssertThrowsContains(Action action, string expectedMessagePart)
    {
        var exception = Assert.ThrowsAny<Exception>(action);
        Assert.Contains(expectedMessagePart, exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static void AssertSequenceContains(IList<string> values, string[] expected)
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

        Assert.Fail("Expected sequence: " + string.Join(" ", expected));
    }

    private static void AssertNumber(decimal expected, LuaValue actual)
    {
        var number = Assert.IsType<decimal>(actual.Value);
        Assert.Equal(expected, number);
    }

    private sealed record CategoryIdentifier(string Value, string Kind, string FileName);

    private sealed record CdoPathEntry(string Key, string CdoPath, string FileName);
}
