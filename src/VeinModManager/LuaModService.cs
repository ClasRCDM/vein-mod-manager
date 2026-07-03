using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace VEIN_Item_And_Container_Modifier;

public static partial class LuaModService
{
    private const string VeinSteamAppId = "1857950";
    private const string ModName = "ItemAndContainerModifier";
    private const string PatchMarker = "UI_CONFIG_SUPPORT_BEGIN";

    public static string? DetectGameFolder()
    {
        foreach (var library in DetectSteamLibraries())
        {
            var manifestPath = Path.Combine(library, "steamapps", $"appmanifest_{VeinSteamAppId}.acf");
            if (File.Exists(manifestPath))
            {
                var installDir = ReadSteamManifestValue(manifestPath, "installdir");
                if (!string.IsNullOrWhiteSpace(installDir))
                {
                    var gameFolder = Path.Combine(library, "steamapps", "common", installDir);
                    if (IsVeinGameFolder(gameFolder)) return gameFolder;
                }
            }

            var commonCandidate = Path.Combine(library, "steamapps", "common", "Vein");
            if (IsVeinGameFolder(commonCandidate)) return commonCandidate;
        }

        foreach (var candidate in CommonGameFolderCandidates())
        {
            if (IsVeinGameFolder(candidate)) return candidate;
        }

        return null;
    }

    public static string GetExpectedModFolder(string gameFolder)
    {
        return Path.Combine(gameFolder, "Vein", "Binaries", "Win64", "ue4ss", "Mods", ModName);
    }

    public static string? DetectModFolder(string gameFolder)
    {
        if (string.IsNullOrWhiteSpace(gameFolder)) return null;

        var expected = GetExpectedModFolder(gameFolder);
        if (IsValidModFolder(expected)) return expected;

        var modsFolder = Path.Combine(gameFolder, "Vein", "Binaries", "Win64", "ue4ss", "Mods");
        if (Directory.Exists(modsFolder))
        {
            foreach (var candidate in Directory.EnumerateDirectories(modsFolder, ModName, SearchOption.TopDirectoryOnly))
            {
                if (IsValidModFolder(candidate)) return candidate;
            }

            return expected;
        }

        return Directory.Exists(Path.GetDirectoryName(expected)) ? expected : null;
    }

    public static bool EnsureBundledModInstalled(string modFolder)
    {
        if (IsValidModFolder(modFolder)) return false;

        var template = GetBundledModTemplateFolder();
        if (template == null)
        {
            throw new DirectoryNotFoundException("Bundled ModTemplate\\ItemAndContainerModifier folder was not found.");
        }

        CopyDirectoryMissingOnly(template, modFolder);
        EnsureModEnabled(modFolder);
        return true;
    }

    public static bool EnsureModEnabled(string modFolder)
    {
        if (!IsValidModFolder(modFolder)) return false;

        var modsFolder = Directory.GetParent(modFolder)?.FullName;
        if (string.IsNullOrWhiteSpace(modsFolder)) return false;

        Directory.CreateDirectory(modsFolder);
        var modsFile = Path.Combine(modsFolder, "mods.txt");
        var lines = File.Exists(modsFile)
            ? File.ReadAllLines(modsFile, Encoding.UTF8).ToList()
            : new List<string>();

        var entryPattern = new Regex(@"^\s*" + Regex.Escape(ModName) + @"\s*:", RegexOptions.IgnoreCase);
        for (var i = 0; i < lines.Count; i++)
        {
            if (!entryPattern.IsMatch(lines[i])) continue;

            if (lines[i].Trim().Equals(ModName + " : 1", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            BackupModsFile(modsFile);
            lines[i] = ModName + " : 1";
            File.WriteAllLines(modsFile, lines, Encoding.UTF8);
            return true;
        }

        BackupModsFile(modsFile);
        if (lines.Count > 0 && !string.IsNullOrWhiteSpace(lines[^1]))
        {
            lines.Add(string.Empty);
        }

        lines.Add(ModName + " : 1");
        File.WriteAllLines(modsFile, lines, Encoding.UTF8);
        return true;
    }

    public static bool HasUe4ss(string gameFolder)
    {
        if (string.IsNullOrWhiteSpace(gameFolder)) return false;
        var ue4ss = Path.Combine(gameFolder, "Vein", "Binaries", "Win64", "ue4ss");
        return Directory.Exists(ue4ss) && (File.Exists(Path.Combine(ue4ss, "UE4SS.dll")) || Directory.Exists(Path.Combine(ue4ss, "Mods")));
    }

    public static bool IsValidModFolder(string modFolder)
    {
        if (string.IsNullOrWhiteSpace(modFolder) || !Directory.Exists(modFolder)) return false;
        return File.Exists(Path.Combine(modFolder, "Scripts", "main.lua"))
            && File.Exists(Path.Combine(modFolder, "Scripts", "config.lua"))
            && Directory.Exists(Path.Combine(modFolder, "Scripts", "categories"));
    }

    private static bool IsVeinGameFolder(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path)) return false;
        return File.Exists(Path.Combine(path, "Vein", "Binaries", "Win64", "Vein-Win64-Test.exe"))
            || File.Exists(Path.Combine(path, "Vein", "Binaries", "Win64", "Vein.exe"))
            || Directory.Exists(Path.Combine(path, "Vein", "Content", "Paks"));
    }

    private static HashSet<string> DetectSteamLibraries()
    {
        var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var root in CommonSteamRootCandidates())
        {
            if (TryGetFullExistingDirectory(root, out var fullRoot)) roots.Add(fullRoot);
        }

        foreach (var root in roots.ToArray())
        {
            foreach (var library in ReadSteamLibraryFolders(root))
            {
                if (TryGetFullExistingDirectory(library, out var fullLibrary)) roots.Add(fullLibrary);
            }
        }

        return roots;
    }

    private static IEnumerable<string> CommonSteamRootCandidates()
    {
        var registrySteamPath = ReadSteamInstallPath();
        if (!string.IsNullOrWhiteSpace(registrySteamPath)) yield return registrySteamPath;

        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        if (!string.IsNullOrWhiteSpace(programFilesX86)) yield return Path.Combine(programFilesX86, "Steam");

        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        if (!string.IsNullOrWhiteSpace(programFiles)) yield return Path.Combine(programFiles, "Steam");

        foreach (var drive in ReadyFixedDrives())
        {
            yield return Path.Combine(drive.RootDirectory.FullName, "SteamLibrary");
            yield return Path.Combine(drive.RootDirectory.FullName, "Steam");
        }
    }

    private static IEnumerable<string> CommonGameFolderCandidates()
    {
        yield return @"C:\Program Files (x86)\Steam\steamapps\common\Vein";
        yield return @"C:\Program Files\Steam\steamapps\common\Vein";

        foreach (var drive in ReadyFixedDrives())
        {
            yield return Path.Combine(drive.RootDirectory.FullName, "SteamLibrary", "steamapps", "common", "Vein");
            yield return Path.Combine(drive.RootDirectory.FullName, "Steam", "steamapps", "common", "Vein");
        }
    }

    private static string? ReadSteamInstallPath()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
            var steamPath = Convert.ToString(key?.GetValue("SteamPath") ?? key?.GetValue("InstallPath"));
            return string.IsNullOrWhiteSpace(steamPath) ? null : steamPath.Replace('/', Path.DirectorySeparatorChar);
        }
        catch (Exception ex) when (IsRecoverableFileSystemException(ex) || ex is System.Security.SecurityException)
        {
            return null;
        }
    }

    private static IEnumerable<string> ReadSteamLibraryFolders(string steamRoot)
    {
        var libraryFoldersPath = Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf");
        if (!File.Exists(libraryFoldersPath)) yield break;

        string text;
        try
        {
            text = File.ReadAllText(libraryFoldersPath, Encoding.UTF8);
        }
        catch (Exception ex) when (IsRecoverableFileSystemException(ex))
        {
            yield break;
        }

        foreach (Match match in SteamLibraryPathPattern.Matches(text))
        {
            var path = Regex.Unescape(match.Groups["path"].Value).Replace(@"\\", @"\");
            if (!string.IsNullOrWhiteSpace(path)) yield return path;
        }
    }

    private static string? ReadSteamManifestValue(string manifestPath, string key)
    {
        try
        {
            var match = Regex.Match(File.ReadAllText(manifestPath, Encoding.UTF8), $@"""{Regex.Escape(key)}""\s+""(?<value>[^""]+)""", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups["value"].Value : null;
        }
        catch (Exception ex) when (IsRecoverableFileSystemException(ex))
        {
            return null;
        }
    }

    private static IEnumerable<DriveInfo> ReadyFixedDrives()
    {
        DriveInfo[] drives;
        try
        {
            drives = DriveInfo.GetDrives();
        }
        catch (Exception ex) when (IsRecoverableFileSystemException(ex))
        {
            yield break;
        }

        foreach (var drive in drives)
        {
            bool ready;
            try
            {
                ready = drive.IsReady && drive.DriveType == DriveType.Fixed;
            }
            catch (Exception ex) when (IsRecoverableFileSystemException(ex))
            {
                continue;
            }

            if (ready) yield return drive;
        }
    }

    private static bool TryGetFullExistingDirectory(string path, out string fullPath)
    {
        fullPath = "";
        try
        {
            if (!Directory.Exists(path)) return false;
            fullPath = Path.GetFullPath(path);
            return true;
        }
        catch (Exception ex) when (IsRecoverableFileSystemException(ex))
        {
            return false;
        }
    }

    private static bool IsRecoverableFileSystemException(Exception ex)
    {
        return ex is IOException
            || ex is UnauthorizedAccessException
            || ex is ArgumentException
            || ex is NotSupportedException;
    }

    private static string? GetBundledModTemplateFolder()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "ModTemplate", "ItemAndContainerModifier"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "ModTemplate", "ItemAndContainerModifier"))
        };

        return candidates.FirstOrDefault(IsValidModFolder);
    }

    private static void CopyDirectoryMissingOnly(string source, string destination)
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
            if (!File.Exists(target)) File.Copy(file, target, overwrite: false);
        }
    }

    public static ModData LoadModData(string modFolder)
    {
        var data = new ModData { ModFolder = modFolder };
        var scripts = Path.Combine(modFolder, "Scripts");
        var configPath = Path.Combine(scripts, "config.lua");
        var categoriesPath = Path.Combine(scripts, "categories");

        if (File.Exists(configPath))
        {
            foreach (var pair in ParseEnabledCategories(File.ReadAllText(configPath, Encoding.UTF8)))
            {
                data.BaseEnabledCategories[pair.Key] = pair.Value;
            }
        }

        foreach (var category in CategoryNames.Ordered)
        {
            var categoryData = new CategoryData { Name = category };
            var path = Path.Combine(categoriesPath, category + ".lua");
            if (!File.Exists(path))
            {
                categoryData.ParseError = "Missing category file.";
            }
            else
            {
                try
                {
                    ParseCategoryFile(path, category, categoryData);
                }
                catch (Exception ex)
                {
                    categoryData.ParseError = ex.Message;
                }
            }
            data.Categories[category] = categoryData;
        }

        return data;
    }

    public static string CreateBackup(string modFolder)
    {
        var scripts = Path.Combine(modFolder, "Scripts");
        var backupRoot = CreateUniqueBackupFolder(modFolder);
        Directory.CreateDirectory(backupRoot);

        CopyIfExists(Path.Combine(scripts, "config.lua"), Path.Combine(backupRoot, "config.lua"));
        CopyIfExists(Path.Combine(scripts, "ui_config.lua"), Path.Combine(backupRoot, "ui_config.lua"));

        var sourceCategories = Path.Combine(scripts, "categories");
        var backupCategories = Path.Combine(backupRoot, "categories");
        if (Directory.Exists(sourceCategories))
        {
            Directory.CreateDirectory(backupCategories);
            foreach (var file in Directory.GetFiles(sourceCategories, "*.lua", SearchOption.TopDirectoryOnly))
            {
                File.Copy(file, Path.Combine(backupCategories, Path.GetFileName(file)), overwrite: true);
            }
        }

        return backupRoot;
    }

    public static void ApplyConfig(string modFolder, UiConfigState state)
    {
        var scripts = Path.Combine(modFolder, "Scripts");
        Directory.CreateDirectory(scripts);
        var uiConfigPath = Path.Combine(scripts, "ui_config.lua");
        EnsureConfigPatched(Path.Combine(scripts, "config.lua"));
        WriteUiConfigAtomic(uiConfigPath, state);
        EnsureModEnabled(modFolder);
    }

    public static ConfigInstallResult InstallUiConfig(string modFolder, string sourceUiConfigPath)
    {
        if (!IsValidModFolder(modFolder))
        {
            throw new DirectoryNotFoundException("Invalid ItemAndContainerModifier folder: " + modFolder);
        }

        if (!File.Exists(sourceUiConfigPath))
        {
            throw new FileNotFoundException("Config file does not exist.", sourceUiConfigPath);
        }

        if (!Path.GetFileName(sourceUiConfigPath).Equals("ui_config.lua", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Config file must be named ui_config.lua.");
        }

        var state = LoadUiConfigStateFromFile(sourceUiConfigPath);
        var backupPath = CreateBackup(modFolder);
        ApplyConfig(modFolder, state);
        return new ConfigInstallResult(backupPath, Path.Combine(modFolder, "Scripts", "ui_config.lua"), state);
    }

    public static UiConfigState LoadUiConfigState(string modFolder)
    {
        var path = Path.Combine(modFolder, "Scripts", "ui_config.lua");
        return LoadUiConfigStateFromFile(path);
    }

    public static UiConfigState LoadUiConfigStateFromFile(string uiConfigPath)
    {
        var state = new UiConfigState();
        if (!File.Exists(uiConfigPath)) return state;

        var root = ReadUiConfigRoot(uiConfigPath);
        ReadEnabledCategories(root, state);
        ReadCategoryDefaults(root, state);
        ReadItemOverrides(root, state);
        ReadContainerWeightOverrides(root, state);
        return state;
    }

    public static void EnsureConfigPatched(string configPath)
    {
        if (!File.Exists(configPath)) throw new FileNotFoundException("config.lua was not found.", configPath);

        var text = File.ReadAllText(configPath, Encoding.UTF8);
        if (text.Contains(PatchMarker, StringComparison.Ordinal)) return;

        var backup = CreateUniqueSiblingPath(configPath, ".codex-backup-before-ui-config-");
        File.Copy(configPath, backup, overwrite: false);

        text = ReplaceOnce(
            text,
            "-- Build ContainerWeights from a category file using the classes+overrides format.\r\n-- override > category default > not patched (omitted from ContainerWeights).\r\nlocal function buildContainerWeights(dst, categoryModule, defaultWeight)",
            UiSupportBlock + "\r\n-- Build ContainerWeights from a category file using the classes+overrides format.\r\n-- UI override > category override > UI category default > category default > not patched.\r\nlocal function buildContainerWeights(dst, categoryName, categoryModule, defaultWeight)");

        text = ReplaceOnce(
            text,
            "-- Build ContainerWeights from a category file using the classes+overrides format.\n-- override > category default > not patched (omitted from ContainerWeights).\nlocal function buildContainerWeights(dst, categoryModule, defaultWeight)",
            UiSupportBlock + "\n-- Build ContainerWeights from a category file using the classes+overrides format.\n-- UI override > category override > UI category default > category default > not patched.\nlocal function buildContainerWeights(dst, categoryName, categoryModule, defaultWeight)");

        text = ReplaceOnce(text, "    local cat = require(categoryModule)\r\n    for _, className in ipairs(cat.classes) do\r\n        local weight = cat.overrides[className]\r\n        if weight == nil then weight = defaultWeight end\r\n        if weight ~= nil then\r\n            dst[className] = weight\r\n        end\r\n    end\r\nend",
            "    local cat = require(categoryModule)\r\n    local uiDefault = get_category_default(categoryName, \"MaxWeight\")\r\n    local uiOverrides = get_container_overrides(categoryName)\r\n    for _, className in ipairs(cat.classes) do\r\n        local weight = read_max_weight(uiOverrides[className])\r\n        if weight == nil then weight = cat.overrides[className] end\r\n        if weight == nil then weight = uiDefault end\r\n        if weight == nil then weight = defaultWeight end\r\n        if weight ~= nil then\r\n            dst[className] = weight\r\n        end\r\n    end\r\nend");

        text = ReplaceOnce(text, "    local cat = require(categoryModule)\n    for _, className in ipairs(cat.classes) do\n        local weight = cat.overrides[className]\n        if weight == nil then weight = defaultWeight end\n        if weight ~= nil then\n            dst[className] = weight\n        end\n    end\nend",
            "    local cat = require(categoryModule)\n    local uiDefault = get_category_default(categoryName, \"MaxWeight\")\n    local uiOverrides = get_container_overrides(categoryName)\n    for _, className in ipairs(cat.classes) do\n        local weight = read_max_weight(uiOverrides[className])\n        if weight == nil then weight = cat.overrides[className] end\n        if weight == nil then weight = uiDefault end\n        if weight == nil then weight = defaultWeight end\n        if weight ~= nil then\n            dst[className] = weight\n        end\n    end\nend");

        text = ReplaceOnce(text, "local function applyActorDefaults(items, defaults)\r\n    if not defaults then return items end\r\n    for _, props in pairs(items) do\r\n        for key, defaultVal in pairs(defaults) do\r\n            if props[key] == nil then\r\n                props[key] = defaultVal\r\n            end\r\n        end\r\n    end\r\n    return items\r\nend",
            "local function applyActorDefaults(categoryName, items, defaults)\r\n    local uiDefaults = get_category_defaults(categoryName)\r\n    local uiOverrides = get_item_overrides(categoryName)\r\n    for className, props in pairs(items) do\r\n        apply_property_table(props, uiOverrides[className])\r\n        apply_defaults_to_nil(props, uiDefaults)\r\n        apply_defaults_to_nil(props, defaults)\r\n    end\r\n    return items\r\nend");

        text = ReplaceOnce(text, "local function applyActorDefaults(items, defaults)\n    if not defaults then return items end\n    for _, props in pairs(items) do\n        for key, defaultVal in pairs(defaults) do\n            if props[key] == nil then\n                props[key] = defaultVal\n            end\n        end\n    end\n    return items\nend",
            "local function applyActorDefaults(categoryName, items, defaults)\n    local uiDefaults = get_category_defaults(categoryName)\n    local uiOverrides = get_item_overrides(categoryName)\n    for className, props in pairs(items) do\n        apply_property_table(props, uiOverrides[className])\n        apply_defaults_to_nil(props, uiDefaults)\n        apply_defaults_to_nil(props, defaults)\n    end\n    return items\nend");

        text = ReplaceOnce(text, "local enabled = Config.EnabledCategories", "apply_ui_enabled_categories(Config.EnabledCategories)\r\n\r\nlocal enabled = Config.EnabledCategories");
        text = ReplaceOnce(text, "if enabled.vehicles   then buildContainerWeights(Config.ContainerWeights, \"categories/vehicles\",   Config.CategoryDefaults.vehicles)   end", "if enabled.vehicles   then buildContainerWeights(Config.ContainerWeights, \"vehicles\",   \"categories/vehicles\",   Config.CategoryDefaults.vehicles)   end");
        text = ReplaceOnce(text, "if enabled.containers then buildContainerWeights(Config.ContainerWeights, \"categories/containers\", Config.CategoryDefaults.containers) end", "if enabled.containers then buildContainerWeights(Config.ContainerWeights, \"containers\", \"categories/containers\", Config.CategoryDefaults.containers) end");

        foreach (var category in CategoryNames.ItemLike)
        {
            text = text.Replace(
                $"applyActorDefaults(require(\"categories/{category}\"),   Config.CategoryDefaults.{category})",
                $"applyActorDefaults(\"{category}\", require(\"categories/{category}\"),   Config.CategoryDefaults.{category})",
                StringComparison.Ordinal);
            text = text.Replace(
                $"applyActorDefaults(require(\"categories/{category}\"),    Config.CategoryDefaults.{category})",
                $"applyActorDefaults(\"{category}\", require(\"categories/{category}\"),    Config.CategoryDefaults.{category})",
                StringComparison.Ordinal);
            text = text.Replace(
                $"applyActorDefaults(require(\"categories/{category}\"), Config.CategoryDefaults.{category})",
                $"applyActorDefaults(\"{category}\", require(\"categories/{category}\"), Config.CategoryDefaults.{category})",
                StringComparison.Ordinal);
        }

        if (!text.Contains(PatchMarker, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Could not patch config.lua safely. Its structure did not match the expected ItemAndContainerModifier config.");
        }

        File.WriteAllText(configPath, text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    public static void WriteUiConfigAtomic(string uiConfigPath, UiConfigState state)
    {
        var lua = GenerateUiConfigLua(state);
        ValidateGeneratedLua(lua);

        var directory = Path.GetDirectoryName(uiConfigPath) ?? ".";
        Directory.CreateDirectory(directory);


        if (File.Exists(uiConfigPath))
        {
            var backup = CreateUniqueSiblingPath(uiConfigPath, ".codex-backup-before-apply-");
            File.Copy(uiConfigPath, backup, overwrite: false);
        }

        var tempPath = Path.Combine(directory, Path.GetFileName(uiConfigPath) + "." + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
            {
                writer.Write(lua);
                writer.Flush();
                stream.Flush(flushToDisk: true);
            }

            if (File.Exists(uiConfigPath))
            {
                File.Replace(tempPath, uiConfigPath, null);
            }
            else
            {
                File.Move(tempPath, uiConfigPath);
            }
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    public static string GenerateUiConfigLua(UiConfigState state)
    {
        var sb = new StringBuilder();
        sb.AppendLine("local UiConfig = {");
        WriteEnabledCategories(sb, state);
        WriteCategoryDefaults(sb, state);
        WriteItemOverrides(sb, state);
        WriteContainerOverrides(sb, state);
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("return UiConfig");
        return sb.ToString();
    }

    public static Process? LaunchVein(string gameFolder)
    {
        var candidates = new[]
        {
            Path.Combine(gameFolder, "Vein", "Binaries", "Win64", "Vein-Win64-Test.exe"),
            Path.Combine(gameFolder, "Vein", "Binaries", "Win64", "Vein.exe")
        };

        var exe = candidates.FirstOrDefault(File.Exists);
        return exe == null
            ? null
            : Process.Start(new ProcessStartInfo { FileName = exe, WorkingDirectory = Path.GetDirectoryName(exe), UseShellExecute = true });
    }

    private static void ParseCategoryFile(string path, string category, CategoryData categoryData)
    {
        var text = StripLuaLineComments(File.ReadAllText(path, Encoding.UTF8));
        if (CategoryNames.ContainerLike.Contains(category))
        {
            ParseContainerCategory(text, category, categoryData);
        }
        else
        {
            ParseItemCategory(text, category, categoryData);
        }
    }

    private static void ParseItemCategory(string text, string category, CategoryData categoryData)
    {
        foreach (Match match in ItemEntryPattern.Matches(text))
        {
            var className = match.Groups["class"].Value.Trim();
            var body = match.Groups["body"].Value;
            var values = new Dictionary<string, LuaValue>(StringComparer.OrdinalIgnoreCase);

            foreach (var key in new[] { "Weight", "MaxStack", "bStackable", "ExtraWeightCapacity", "RunSpeedMultiplier" })
            {
                var propMatch = Regex.Match(body, $@"\b{Regex.Escape(key)}\s*=\s*(?<value>nil|true|false|-?\d+(?:\.\d+)?)", RegexOptions.IgnoreCase);
                if (propMatch.Success)
                {
                    values[key] = LuaValue.FromText(propMatch.Groups["value"].Value);
                }
            }

            var cdo = CdoPathPattern.Match(body).Groups["path"].Value;
            categoryData.Items.Add(new CategoryItem(category, className, string.IsNullOrWhiteSpace(cdo) ? null : cdo, values));
        }
    }

    private static void ParseContainerCategory(string text, string category, CategoryData categoryData)
    {
        var classesMatch = ClassesBlockPattern.Match(text);
        if (!classesMatch.Success) throw new InvalidOperationException("Could not find classes table.");

        var overrides = new Dictionary<string, LuaValue>(StringComparer.OrdinalIgnoreCase);
        var overridesMatch = OverridesBlockPattern.Match(text);
        if (overridesMatch.Success)
        {
            foreach (Match match in OverridePattern.Matches(overridesMatch.Groups["body"].Value))
            {
                overrides[match.Groups["class"].Value] = LuaValue.FromText(match.Groups["value"].Value);
            }
        }

        foreach (Match match in QuotedClassPattern.Matches(classesMatch.Groups["body"].Value))
        {
            var className = match.Groups["class"].Value;
            var values = new Dictionary<string, LuaValue>(StringComparer.OrdinalIgnoreCase);
            if (overrides.TryGetValue(className, out var maxWeight))
            {
                values["MaxWeight"] = maxWeight;
            }
            categoryData.Items.Add(new CategoryItem(category, className, null, values));
        }
    }

    private static Dictionary<string, bool> ParseEnabledCategories(string configText)
    {
        var result = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        var match = EnabledCategoriesBlockPattern.Match(configText);
        if (!match.Success) return result;

        foreach (Match entry in EnabledCategoryEntryPattern.Matches(match.Groups["body"].Value))
        {
            result[entry.Groups["name"].Value] = entry.Groups["value"].Value.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        return result;
    }

    private static void ReadEnabledCategories(Dictionary<string, object?> root, UiConfigState state)
    {
        if (!TryGetTable(root, "EnabledCategories", out var table)) return;

        foreach (var category in CategoryNames.Ordered)
        {
            if (table.TryGetValue(category, out var value) && value is bool enabled)
            {
                state.EnabledCategories[category] = enabled;
            }
        }
    }

    private static void ReadCategoryDefaults(Dictionary<string, object?> root, UiConfigState state)
    {
        if (!TryGetTable(root, "CategoryDefaults", out var table)) return;

        foreach (var category in CategoryNames.Ordered)
        {
            if (!table.TryGetValue(category, out var value)) continue;

            if (CategoryNames.ContainerLike.Contains(category))
            {
                var luaValue = ToLuaValue(value);
                if (!luaValue.IsNil)
                {
                    state.CategoryDefaults[category] = new Dictionary<string, LuaValue>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["MaxWeight"] = luaValue
                    };
                }
                continue;
            }

            if (!TryAsTable(value, out var values)) continue;
            var parsed = ReadPropertyValues(values);
            if (parsed.Count > 0) state.CategoryDefaults[category] = parsed;
        }
    }

    private static void ReadItemOverrides(Dictionary<string, object?> root, UiConfigState state)
    {
        if (!TryGetTable(root, "ItemOverrides", out var table)) return;

        foreach (var category in CategoryNames.Ordered.Where(CategoryNames.ItemLike.Contains))
        {
            if (!table.TryGetValue(category, out var value) || !TryAsTable(value, out var items)) continue;

            var parsedItems = new Dictionary<string, Dictionary<string, LuaValue>>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in items)
            {
                if (!TryAsTable(item.Value, out var values)) continue;

                var parsedValues = ReadPropertyValues(values);
                if (parsedValues.Count > 0) parsedItems[item.Key] = parsedValues;
            }

            if (parsedItems.Count > 0) state.ItemOverrides[category] = parsedItems;
        }
    }

    private static void ReadContainerWeightOverrides(Dictionary<string, object?> root, UiConfigState state)
    {
        if (!TryGetTable(root, "ContainerWeightOverrides", out var table)) return;

        foreach (var category in CategoryNames.Ordered.Where(CategoryNames.ContainerLike.Contains))
        {
            if (!table.TryGetValue(category, out var value) || !TryAsTable(value, out var items)) continue;

            var parsedItems = new Dictionary<string, LuaValue>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in items)
            {
                var weight = ToLuaValue(item.Value);
                if (!weight.IsNil) parsedItems[item.Key] = weight;
            }

            if (parsedItems.Count > 0) state.ContainerWeightOverrides[category] = parsedItems;
        }
    }

    private static Dictionary<string, LuaValue> ReadPropertyValues(Dictionary<string, object?> table)
    {
        var values = new Dictionary<string, LuaValue>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in new[] { "Weight", "MaxStack", "bStackable", "MaxWeight", "ExtraWeightCapacity", "RunSpeedMultiplier" })
        {
            if (!table.TryGetValue(key, out var value)) continue;

            var luaValue = ToLuaValue(value);
            if (!luaValue.IsNil) values[key] = luaValue;
        }
        return values;
    }

    private static bool TryGetTable(Dictionary<string, object?> root, string key, out Dictionary<string, object?> table)
    {
        table = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        return root.TryGetValue(key, out var value) && TryAsTable(value, out table);
    }

    private static bool TryAsTable(object? value, out Dictionary<string, object?> table)
    {
        if (value is Dictionary<string, object?> valueTable)
        {
            table = valueTable;
            return true;
        }

        table = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        return false;
    }

    private static LuaValue ToLuaValue(object? value)
    {
        return value switch
        {
            null => LuaValue.Nil,
            LuaValue luaValue => luaValue,
            bool b => new LuaValue(b),
            decimal d => new LuaValue(d),
            string s => new LuaValue(s),
            _ => LuaValue.Nil
        };
    }

    private static void WriteEnabledCategories(StringBuilder sb, UiConfigState state)
    {
        sb.AppendLine("    EnabledCategories = {");
        foreach (var category in CategoryNames.Ordered)
        {
            if (state.EnabledCategories.TryGetValue(category, out var enabled))
            {
                sb.AppendLine($"        {category} = {(enabled ? "true" : "false")},");
            }
        }
        sb.AppendLine("    },");
    }

    private static void WriteCategoryDefaults(StringBuilder sb, UiConfigState state)
    {
        sb.AppendLine("    CategoryDefaults = {");
        foreach (var category in CategoryNames.Ordered)
        {
            if (!state.CategoryDefaults.TryGetValue(category, out var values) || values.Count == 0) continue;

            if (CategoryNames.ContainerLike.Contains(category) && values.TryGetValue("MaxWeight", out var maxWeight))
            {
                sb.AppendLine($"        {category} = {maxWeight.ToLua()},");
                continue;
            }

            sb.AppendLine($"        {category} = {{");
            foreach (var pair in values.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
            {
                sb.AppendLine($"            {pair.Key} = {pair.Value.ToLua()},");
            }
            sb.AppendLine("        },");
        }
        sb.AppendLine("    },");
    }

    private static void WriteItemOverrides(StringBuilder sb, UiConfigState state)
    {
        sb.AppendLine("    ItemOverrides = {");
        foreach (var category in CategoryNames.Ordered.Where(CategoryNames.ItemLike.Contains))
        {
            if (!state.ItemOverrides.TryGetValue(category, out var items) || items.Count == 0) continue;
            sb.AppendLine($"        {category} = {{");
            foreach (var item in items.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
            {
                if (item.Value.Count == 0) continue;
                sb.AppendLine($"            [\"{EscapeLuaString(item.Key)}\"] = {{");
                foreach (var value in item.Value.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
                {
                    sb.AppendLine($"                {value.Key} = {value.Value.ToLua()},");
                }
                sb.AppendLine("            },");
            }
            sb.AppendLine("        },");
        }
        sb.AppendLine("    },");
    }

    private static void WriteContainerOverrides(StringBuilder sb, UiConfigState state)
    {
        sb.AppendLine("    ContainerWeightOverrides = {");
        foreach (var category in CategoryNames.Ordered.Where(CategoryNames.ContainerLike.Contains))
        {
            if (!state.ContainerWeightOverrides.TryGetValue(category, out var values) || values.Count == 0) continue;
            sb.AppendLine($"        {category} = {{");
            foreach (var pair in values.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
            {
                sb.AppendLine($"            [\"{EscapeLuaString(pair.Key)}\"] = {pair.Value.ToLua()},");
            }
            sb.AppendLine("        },");
        }
        sb.AppendLine("    },");
    }

    private static void ValidateGeneratedLua(string lua)
    {
        var balance = 0;
        foreach (var ch in lua)
        {
            if (ch == '{') balance++;
            if (ch == '}') balance--;
            if (balance < 0) throw new InvalidOperationException("Generated Lua braces are invalid.");
        }

        if (balance != 0 || !lua.Contains("return UiConfig", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Generated Lua failed the basic syntax check.");
        }
    }


    private static Dictionary<string, object?> ReadUiConfigRoot(string uiConfigPath)
    {
        try
        {
            return LuaTableParser.ParseUiConfig(File.ReadAllText(uiConfigPath, Encoding.UTF8));
        }
        catch (InvalidDataException ex)
        {
            throw new InvalidDataException("Invalid ui_config.lua at '" + uiConfigPath + "': " + ex.Message, ex);
        }
        catch (FormatException ex)
        {
            throw new InvalidDataException("Could not parse ui_config.lua at '" + uiConfigPath + "': " + ex.Message, ex);
        }
        catch (OverflowException ex)
        {
            throw new InvalidDataException("Could not parse ui_config.lua at '" + uiConfigPath + "': " + ex.Message, ex);
        }
    }

    private static string CreateUniqueBackupFolder(string modFolder)
    {
        var backupParent = Path.Combine(modFolder, "Backups");
        Directory.CreateDirectory(backupParent);

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff", CultureInfo.InvariantCulture);
        var backupRoot = Path.Combine(backupParent, timestamp);
        if (!Directory.Exists(backupRoot)) return backupRoot;

        for (var index = 1; ; index++)
        {
            var candidate = Path.Combine(backupParent, timestamp + "-" + index.ToString(CultureInfo.InvariantCulture));
            if (!Directory.Exists(candidate)) return candidate;
        }
    }

    private static string CreateUniqueSiblingPath(string path, string suffix)
    {
        var directory = Path.GetDirectoryName(path) ?? ".";
        var fileName = Path.GetFileName(path);
        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture);
        var candidate = Path.Combine(directory, fileName + suffix + timestamp);
        if (!File.Exists(candidate) && !Directory.Exists(candidate)) return candidate;

        for (var index = 1; ; index++)
        {
            var numbered = Path.Combine(directory, fileName + suffix + timestamp + "-" + index.ToString(CultureInfo.InvariantCulture));
            if (!File.Exists(numbered) && !Directory.Exists(numbered)) return numbered;
        }
    }

    private static void CopyIfExists(string source, string destination)
    {
        if (File.Exists(source)) File.Copy(source, destination, overwrite: true);
    }

    private static void BackupModsFile(string modsFile)
    {
        if (!File.Exists(modsFile)) return;

        var backup = modsFile + ".vein-manager-backup-" + DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        File.Copy(modsFile, backup, overwrite: false);
    }

    private static string ReplaceOnce(string text, string oldValue, string newValue)
    {
        var index = text.IndexOf(oldValue, StringComparison.Ordinal);
        if (index < 0) return text;
        return text[..index] + newValue + text[(index + oldValue.Length)..];
    }

    private static string EscapeLuaString(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private static string StripLuaLineComments(string text)
    {
        var sb = new StringBuilder(text.Length);
        using var reader = new StringReader(text);

        while (reader.ReadLine() is { } line)
        {
            var commentIndex = line.IndexOf("--", StringComparison.Ordinal);
            sb.AppendLine(commentIndex >= 0 ? line[..commentIndex] : line);
        }

        return sb.ToString();
    }

    private sealed class LuaTableParser
    {
        private readonly string _text;
        private int _index;

        private LuaTableParser(string text)
        {
            _text = StripLuaLineComments(text);
        }

        public static Dictionary<string, object?> ParseUiConfig(string text)
        {
            var parser = new LuaTableParser(text);
            var returnMatch = ReturnUiConfigPattern.Match(parser._text);
            if (!returnMatch.Success)
            {
                throw new InvalidDataException("ui_config.lua is missing 'return UiConfig'. Add it after the UiConfig table.");
            }

            var marker = parser._text.IndexOf("local UiConfig", StringComparison.Ordinal);
            if (marker < 0 || marker > returnMatch.Index) marker = parser._text.IndexOf("UiConfig", StringComparison.Ordinal);
            if (marker < 0 || marker > returnMatch.Index)
            {
                throw new InvalidDataException("ui_config.lua is missing a UiConfig table before 'return UiConfig'.");
            }

            var tableStart = parser._text.IndexOf('{', marker);
            if (tableStart < 0 || tableStart > returnMatch.Index)
            {
                throw new InvalidDataException("ui_config.lua is missing the opening '{' for the UiConfig table.");
            }

            parser._index = tableStart;
            return parser.ParseTable();
        }

        private Dictionary<string, object?> ParseTable()
        {
            var table = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            Expect('{');

            while (true)
            {
                SkipWhitespace();
                if (Peek() == '}')
                {
                    _index++;
                    return table;
                }

                var key = ParseKey();
                SkipWhitespace();
                Expect('=');
                table[key] = ParseValue();

                SkipWhitespace();
                if (Peek() == ',') _index++;
            }
        }

        private string ParseKey()
        {
            SkipWhitespace();
            if (Peek() == '[')
            {
                _index++;
                SkipWhitespace();
                var key = ParseString();
                SkipWhitespace();
                Expect(']');
                return key;
            }

            return ParseIdentifier();
        }

        private object? ParseValue()
        {
            SkipWhitespace();
            return Peek() switch
            {
                '{' => ParseTable(),
                '"' => ParseString(),
                '-' or >= '0' and <= '9' => ParseNumber(),
                _ => ParseKeywordValue()
            };
        }

        private object? ParseKeywordValue()
        {
            var identifier = ParseIdentifier();
            if (identifier.Equals("true", StringComparison.OrdinalIgnoreCase)) return true;
            if (identifier.Equals("false", StringComparison.OrdinalIgnoreCase)) return false;
            if (identifier.Equals("nil", StringComparison.OrdinalIgnoreCase)) return null;
            return identifier;
        }

        private decimal ParseNumber()
        {
            var start = _index;
            if (Peek() == '-') _index++;
            while (!IsEnd && char.IsDigit(Peek())) _index++;
            if (!IsEnd && Peek() == '.')
            {
                _index++;
                while (!IsEnd && char.IsDigit(Peek())) _index++;
            }

            var raw = _text[start.._index];
            try
            {
                return decimal.Parse(raw, NumberStyles.Float, CultureInfo.InvariantCulture);
            }
            catch (FormatException ex)
            {
                throw CreateFormatException("Invalid Lua number '" + raw + "'.", ex);
            }
            catch (OverflowException ex)
            {
                throw CreateFormatException("Lua number '" + raw + "' is outside the supported range.", ex);
            }
        }

        private string ParseIdentifier()
        {
            SkipWhitespace();
            var start = _index;
            if (IsEnd || !(char.IsLetter(Peek()) || Peek() == '_'))
            {
                throw CreateFormatException("Expected Lua identifier.");
            }

            _index++;
            while (!IsEnd && (char.IsLetterOrDigit(Peek()) || Peek() == '_')) _index++;
            return _text[start.._index];
        }

        private string ParseString()
        {
            Expect('"');
            var sb = new StringBuilder();
            while (!IsEnd)
            {
                var ch = _text[_index++];
                if (ch == '"') return sb.ToString();
                if (ch == '\\' && !IsEnd)
                {
                    var escaped = _text[_index++];
                    sb.Append(escaped switch
                    {
                        '\\' => '\\',
                        '"' => '"',
                        'n' => '\n',
                        'r' => '\r',
                        't' => '\t',
                        _ => escaped
                    });
                    continue;
                }
                sb.Append(ch);
            }

            throw CreateFormatException("Unterminated Lua string.");
        }

        private void Expect(char expected)
        {
            SkipWhitespace();
            if (IsEnd || _text[_index] != expected)
            {
                throw CreateFormatException("Expected '" + expected + "'.");
            }
            _index++;
        }


        private FormatException CreateFormatException(string message, Exception? innerException = null)
        {
            return new FormatException(message + " At character " + (_index + 1).ToString(CultureInfo.InvariantCulture) + ".", innerException);
        }
        private char Peek() => IsEnd ? '\0' : _text[_index];

        private bool IsEnd => _index >= _text.Length;

        private void SkipWhitespace()
        {
            while (!IsEnd && char.IsWhiteSpace(_text[_index])) _index++;
        }
    }

    private const string UiSupportBlock = """
local UI_CONFIG_SUPPORT_BEGIN = true
local function load_ui_config()
    local ok, ui_config = pcall(require, "ui_config")
    if ok and type(ui_config) == "table" then
        return ui_config
    end

    return {}
end

local ui_config = load_ui_config()

local function apply_ui_enabled_categories(enabled_categories)
    local overrides = ui_config.EnabledCategories
    if type(overrides) ~= "table" then
        return
    end

    for category_name, enabled in pairs(overrides) do
        if type(enabled) == "boolean" then
            enabled_categories[category_name] = enabled
        end
    end
end

local function get_category_defaults(category_name)
    local defaults = ui_config.CategoryDefaults
    if type(defaults) ~= "table" then
        return nil
    end

    local category_defaults = defaults[category_name]
    if type(category_defaults) == "table" then
        return category_defaults
    end

    return nil
end

local function get_category_default(category_name, key)
    local defaults = ui_config.CategoryDefaults
    if type(defaults) ~= "table" then
        return nil
    end

    local category_defaults = defaults[category_name]
    if type(category_defaults) == "table" then
        return category_defaults[key]
    end

    return category_defaults
end

local function get_item_overrides(category_name)
    local overrides = ui_config.ItemOverrides
    if type(overrides) ~= "table" then
        return {}
    end

    local category_overrides = overrides[category_name]
    if type(category_overrides) == "table" then
        return category_overrides
    end

    return {}
end

local function get_container_overrides(category_name)
    local overrides = ui_config.ContainerWeightOverrides
    if type(overrides) ~= "table" then
        return {}
    end

    local category_overrides = overrides[category_name]
    if type(category_overrides) == "table" then
        return category_overrides
    end

    return {}
end

local function read_max_weight(value)
    if type(value) == "table" then
        return value.MaxWeight
    end

    return value
end

local function apply_property_table(destination, source)
    if type(source) ~= "table" then
        return
    end

    for key, value in pairs(source) do
        if value ~= nil then
            destination[key] = value
        end
    end
end

local function apply_defaults_to_nil(destination, defaults)
    if type(defaults) ~= "table" then
        return
    end

    for key, value in pairs(defaults) do
        if destination[key] == nil then
            destination[key] = value
        end
    end
end
local UI_CONFIG_SUPPORT_END = true
""";

    private static readonly Regex ReturnUiConfigPattern = new(@"\breturn\s+UiConfig\b", RegexOptions.Compiled);
    private static readonly Regex ItemEntryPattern = new(@"\[""(?<class>[^""]+)""\]\s*=\s*\{(?<body>.*?)\}", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex QuotedClassPattern = new(@"""(?<class>[^""]+_C)""", RegexOptions.Compiled);
    private static readonly Regex OverridePattern = new(@"\[""(?<class>[^""]+)""\]\s*=\s*(?<value>-?\d+(?:\.\d+)?)", RegexOptions.Compiled);
    private static readonly Regex SteamLibraryPathPattern = new(@"""path""\s+""(?<path>[^""]+)""", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex CdoPathPattern = new(@"_CDOPath\s*=\s*""(?<path>[^""]+)""", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex ClassesBlockPattern = new(@"classes\s*=\s*\{(?<body>.*?)\}\s*,", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex OverridesBlockPattern = new(@"overrides\s*=\s*\{(?<body>.*?)\}", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex EnabledCategoriesBlockPattern = new(@"EnabledCategories\s*=\s*\{(?<body>.*?)\}", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex EnabledCategoryEntryPattern = new(@"(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*=\s*(?<value>true|false)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
}
