using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace Vein.Ue4ss.DumpLog;

internal sealed class Ue4ssService
{
    private const string SteamAppId = "1857950";
    private const string GameProcessName = "Vein-Win64-Test";
    private const int SwShow = 5;
    private const int KeyUp = 0x0002;
    private const byte VkControl = 0x11;
    private const byte VkJ = 0x4A;
    private const byte VkH = 0x48;
    private const byte VkNumpad7 = 0x67;
    private const byte VkNumpad8 = 0x68;
    private const byte VkNumpad6 = 0x66;
    private const byte VkNumpad9 = 0x69;
    private const byte VkR = 0x52;
    private static readonly object CheatEngineDumpStartLock = new();
    private static readonly ModSuggestionRule ActorSuggestionRule = new(
        "actors-ai-spawns",
        "Actors, AI, and world behavior",
        ["Actor", "Pawn", "Character", "/Actors/", "/Characters/"],
        "Actor-like entries can help find spawn logic, NPC behavior, interaction targets, or world objects.",
        "AI tweaks, spawn lists, interaction mods, behavior hooks, and world-event experiments.",
        "Actor/behavior lead: verify class ownership in SDK headers or Lua hooks before changing spawn or AI behavior.",
        ["Build an NPC/actor lookup list for mod scripts.", "Find likely interaction actors for custom use prompts.", "Compare actor names against Lua logs to locate safe hook points."],
        ["Search the exact object path in JSON/objects.json.", "Check SDK headers for matching class names.", "Hook read-only first in Lua, then test one behavior change at a time."]);
    private static readonly ModSuggestionRule PropertySuggestionRule = new(
        "properties-values",
        "Properties and tunable values",
        ["Property", "FProperty", "BoolProperty", "IntProperty", "FloatProperty", "ObjectProperty", "StructProperty", "ArrayProperty"],
        "Property entries are the best leads for readable values, flags, references, and config-like fields.",
        "Balance changes, inventory limits, crafting values, toggles, UI labels, and config discovery.",
        "Property lead: search nearby owning objects/classes before editing; value fields are where tuning usually starts.",
        ["Find candidate fields for weight, stack size, durability, speed, or visibility tweaks.", "Build a property map for a UE4SS Lua mod.", "Use property names to decide what to watch in a live memory pass."],
        ["Do not write memory from the dump alone.", "Find the owning class or object first.", "Test with a backup save and change one property at a time."]);
    private static readonly ModSuggestionRule StaticMeshSuggestionRule = new(
        "static-meshes-world-art",
        "Static meshes and world art",
        ["StaticMesh", "SM_", "/Meshes/", "/Mesh/"],
        "Static mesh entries are useful for identifying placeable objects, props, map art, and visual asset names.",
        "Map edits, object replacement, prop catalogs, placement helpers, and visual mod planning.",
        "Mesh lead: useful for object catalogs and replacements; pair with actors or blueprints before gameplay edits.",
        ["Create a searchable prop/object catalog.", "Find meshes tied to furniture, containers, signs, or map landmarks.", "Plan texture/material replacement work by mesh family."],
        ["Search for matching Blueprint or actor names.", "Use mesh names as visual references, not gameplay authority.", "Confirm in-game by spawning/viewing safely before packaging a mod."]);
    private static readonly ModSuggestionRule[] ModSuggestionRules =
    [
        new("inventory-containers", "Inventory, containers, and storage", ["Inventory", "Container", "Backpack", "Bag", "Stash", "Chest", "Loot", "ItemContainer", "Storage"], "These names usually point at inventory UI, storage actors, item containers, loot tables, or backpack logic.", "Stack-size mods, backpack capacity, loot organization, quick-deposit features, and container sorting.", "Inventory/container lead: look for stack, weight, slot, container, and loot fields before changing values.", ["Make larger or smarter backpacks.", "Create quick-stack or deposit-all helpers.", "Find loot table/container names for balance mods."], ["Open Markdown/props.md and search the same name for value fields.", "Check Lua logs for existing container/backpack hooks.", "Change one stack/weight/container value at a time and verify in a throwaway save."]),
        new("items-crafting", "Items, crafting, and usable objects", ["Item", "Craft", "Recipe", "Resource", "Consumable", "Food", "Drink", "Weapon", "Ammo", "Tool", "Durability"], "Item and crafting names are useful leads for recipes, resources, consumables, weapons, durability, and equipment rules.", "Recipe balance, item stats, durability tuning, new item lists, and crafting helper mods.", "Item/crafting lead: check recipes, item definitions, durability, and quantity fields around this name.", ["Build a readable item ID/reference sheet for modders.", "Find recipes to rebalance crafting costs.", "Locate durability, ammo, or consumable candidates."], ["Cross-check with JSON/props.json for quantity/stat fields.", "Search SDK headers for the class name.", "Avoid changing live values until the owning class is confirmed."]),
        new("vehicles-movement", "Vehicles and movement", ["Vehicle", "Car", "Truck", "Wheel", "Fuel", "Tire", "Movement", "Speed", "Sprint", "Stamina"], "Vehicle and movement names can point at handling, fuel, speed, tire, stamina, or movement-related systems.", "Vehicle tuning, fuel balance, sprint/movement changes, repair systems, and handling experiments.", "Vehicle/movement lead: search for speed, fuel, engine, stamina, and movement properties near this object.", ["Find vehicle handling or fuel candidates.", "Map movement/sprint-related properties.", "Create repair or vehicle-balance notes."], ["Prefer read-only watches before movement edits.", "Check server/client ownership before multiplayer movement changes.", "Test movement changes offline first to avoid rubberbanding."]),
        new("building-placement", "Building, placement, and world objects", ["Build", "Place", "Furniture", "Structure", "Door", "Wall", "Floor", "Foundation", "Barricade", "Deploy"], "Building and placement names are leads for construction rules, furniture placement, doors, walls, and deployable objects.", "Placement helpers, build cost changes, furniture mods, clean-world tools, and deployable object rules.", "Building/placement lead: confirm authority and persistence before editing placed-world behavior.", ["Find placeable furniture and structure classes.", "Build a clean object catalog for map or base mods.", "Locate cost, ownership, or placement rule candidates."], ["Check whether the object is server-authoritative.", "Back up saves before changing persistence-related systems.", "Search for parent/child or placement component names."]),
        new("weather-environment", "Weather, time, and environment", ["Weather", "Rain", "Storm", "Fog", "Temperature", "Season", "DayNight", "TimeOfDay", "Environment", "Sky"], "Environment names can point at weather state, time-of-day, atmosphere, temperature, and visual world systems.", "Weather mods, lighting tweaks, fog/rain tuning, time speed changes, and environment presets.", "Weather/environment lead: good for visual or balance changes; verify whether values are replicated.", ["Find weather state objects and presets.", "Create time/weather tuning notes.", "Identify environment assets for visual mods."], ["Watch values live before changing timing or weather.", "Check whether the server controls the state.", "Keep visual-only changes separate from gameplay balance."]),
        new("ui-hud", "UI, HUD, and menus", ["Widget", "HUD", "Menu", "UI", "UMG", "Button", "Panel", "Screen", "InventoryWidget"], "UI names help locate widgets, HUD panels, menus, buttons, and screens for usability or display mods.", "HUD cleanup, better inventory screens, debug overlays, mod manager UI, and accessibility tweaks.", "UI lead: search widgets and panels before changing gameplay code.", ["Find inventory or HUD widget names.", "Plan UI text/layout patches.", "Build a modder-facing screen reference."], ["Search for widget names in Lua logs and SDK headers.", "Keep UI-only edits separate from data edits.", "Test at multiple resolutions."]),
        new("audio-effects", "Audio, effects, and feedback", ["Sound", "Audio", "SFX", "Music", "Cue", "Particle", "Niagara", "Effect", "VFX"], "Audio/effect names are useful for sound replacement, feedback tuning, particles, and visual effects.", "Sound packs, alert feedback, VFX swaps, ambience changes, and effect catalogs.", "Audio/VFX lead: useful for replacement planning and feedback mods.", ["Build a sound/effect asset list.", "Find ambience or feedback cues.", "Locate particle/effect names for visual replacement."], ["Pair cue/effect names with where they are triggered.", "Keep replacement assets organized by source path.", "Test volume and looping in-game."])
    ];

    public Ue4ssService(AppPaths paths)
    {
        Paths = paths;
    }

    public AppPaths Paths { get; }

    public string ObjectDumpPath => Path.Combine(Paths.Ue4ssDirectory, "UE4SS_ObjectDump.txt");

    public string ObjectDumpPlainTextPath => Path.Combine(Paths.Ue4ssDirectory, "UE4SS_ObjectDump.plain-text.txt");

    public string ObjectDumpReadableJsonPath => Path.Combine(Paths.Ue4ssDirectory, "UE4SS_ObjectDump.readable.json");

    public string ObjectDumpMarkdownPath => Path.Combine(Paths.Ue4ssDirectory, "UE4SS_ObjectDump.readable.md");

    public string DumpsRoot => Path.Combine(Paths.Ue4ssDirectory, "Dumps");

    public string MarkdownDirectory => Path.Combine(DumpsRoot, "Markdown");

    public string JsonDirectory => Path.Combine(DumpsRoot, "JSON");

    public string PlainTextDirectory => Path.Combine(DumpsRoot, "PlainText");

    public string LogsDirectory => Path.Combine(DumpsRoot, "Logs");

    public string ExportsDirectory => Path.Combine(DumpsRoot, "Exports");

    public string GameFilesDirectory => Path.Combine(DumpsRoot, "GameFiles");

    public string GameFilesMirrorDirectory => Path.Combine(GameFilesDirectory, "Mirror");

    public string AppLogPath => Path.Combine(LogsDirectory, "app.log");

    public string LuaLogExportPath => Path.Combine(LogsDirectory, "ue4ss_lua.log");

    public string DumpErrorsPath => Path.Combine(LogsDirectory, "dump_errors.log");

    public string LatestExportZipPath => Path.Combine(ExportsDirectory, "latest_export.zip");

    public string? CheatEngineWorkflowDirectory => ResolveSiblingToolDirectory("vein-cheat-engine-dump-workflow");

    public string? CheatEngineExecutablePath => ResolveCheatEnginePath();

    public event EventHandler? LogFileChanged;

    private FileSystemWatcher? _watcher;
    private string _liveDumpCacheKey = "";
    private string _liveDumpCacheText = "";
    private static readonly ConditionalWeakTable<ParsedObjectDumpLine, CachedSuggestionMatch> SuggestionCache = new();

    public void StartWatching()
    {
        StopWatching();

        var directory = Path.GetDirectoryName(Paths.Ue4ssLog);
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            return;
        }

        _watcher = new FileSystemWatcher(directory, Path.GetFileName(Paths.Ue4ssLog))
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
            EnableRaisingEvents = true
        };
        _watcher.Changed += OnLogFileChanged;
        _watcher.Created += OnLogFileChanged;
        _watcher.Renamed += OnLogFileChanged;
    }

    public void StopWatching()
    {
        if (_watcher is null)
        {
            return;
        }

        _watcher.EnableRaisingEvents = false;
        _watcher.Changed -= OnLogFileChanged;
        _watcher.Created -= OnLogFileChanged;
        _watcher.Renamed -= OnLogFileChanged;
        _watcher.Dispose();
        _watcher = null;
    }

    public Ue4ssStatus GetStatus()
    {
        var process = GetGameProcess();
        var logFound = File.Exists(Paths.Ue4ssLog);
        DateTime? logLastWrite = null;
        var logLive = false;
        if (logFound)
        {
            try
            {
                logLastWrite = File.GetLastWriteTime(Paths.Ue4ssLog);
                if (process is not null)
                {
                    process.Refresh();
                    logLive = logLastWrite.Value >= process.StartTime.AddSeconds(-10);
                }
            }
            catch
            {
                logLive = false;
            }
        }

        return new Ue4ssStatus(
            File.Exists(Path.Combine(Paths.Win64Directory, "Vein-Win64-Test.exe")),
            File.Exists(Path.Combine(Paths.Win64Directory, "dwmapi.dll")),
            File.Exists(Path.Combine(Paths.Ue4ssDirectory, "UE4SS.dll")),
            File.Exists(Paths.ModsTxt),
            File.Exists(Paths.KeybindsScript),
            logFound,
            logLive,
            logLastWrite,
            process is not null,
            process?.Id);
    }

    public string[] ReadLogTail(int maxLines)
    {
        if (!File.Exists(Paths.Ue4ssLog))
        {
            return
            [
                Timestamp("UE4SS.log has not been created yet."),
                Timestamp("Launch VEIN with UE4SS enabled and this panel will update in real time.")
            ];
        }

        try
        {
            var lines = ReadFileTail(Paths.Ue4ssLog, maxLines);
            return lines.Length == 0 ? [Timestamp("UE4SS.log exists but is empty.")] : lines;
        }
        catch (Exception ex)
        {
            return [Timestamp($"Could not read UE4SS.log: {ex.Message}")];
        }
    }

    public long GetLogFileLength()
    {
        try
        {
            return File.Exists(Paths.Ue4ssLog) ? new FileInfo(Paths.Ue4ssLog).Length : 0;
        }
        catch
        {
            return 0;
        }
    }

    public void AppendAppLog(string message, bool error = false)
    {
        try
        {
            EnsureDumpFolders();
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {(error ? "ERROR" : "INFO")} | {message}{Environment.NewLine}";
            File.AppendAllText(error ? DumpErrorsPath : AppLogPath, line, Encoding.UTF8);
        }
        catch
        {
            // Logging must never crash the UI.
        }
    }

    public void EnsureDumpFolders()
    {
        Directory.CreateDirectory(DumpsRoot);
        Directory.CreateDirectory(MarkdownDirectory);
        Directory.CreateDirectory(JsonDirectory);
        Directory.CreateDirectory(PlainTextDirectory);
        Directory.CreateDirectory(LogsDirectory);
        Directory.CreateDirectory(ExportsDirectory);
        Directory.CreateDirectory(GameFilesDirectory);
    }

    public IReadOnlyList<DumpOutputFile> EnumerateDumpFiles()
    {
        EnsureDumpFolders();
        return Directory.EnumerateFiles(DumpsRoot, "*", SearchOption.AllDirectories)
            .Where(path => !PathIsSameOrChildOf(path, GameFilesMirrorDirectory))
            .Select(path =>
            {
                var info = new FileInfo(path);
                return new DumpOutputFile(
                    path,
                    Path.GetRelativePath(DumpsRoot, path),
                    info.Length,
                    info.LastWriteTime);
            })
            .OrderBy(file => file.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public string[] ReadLogTailFromOffset(long offset, int maxLines)
    {
        if (!File.Exists(Paths.Ue4ssLog))
        {
            return [];
        }

        try
        {
            using var stream = new FileStream(Paths.Ue4ssLog, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var safeOffset = Math.Clamp(offset, 0, stream.Length);
            stream.Seek(safeOffset, SeekOrigin.Begin);
            using var reader = new StreamReader(stream, Encoding.UTF8, true);
            var text = reader.ReadToEnd();

            if (string.IsNullOrWhiteSpace(text))
            {
                return [];
            }

            var lines = text.Replace("\r\n", "\n").Replace('\r', '\n')
                .Split('\n', StringSplitOptions.RemoveEmptyEntries);
            return lines.Length <= maxLines ? lines : lines.Skip(lines.Length - maxLines).ToArray();
        }
        catch (Exception ex)
        {
            return [Timestamp($"Could not read new UE4SS.log lines: {ex.Message}")];
        }
    }

    public OperationResult LaunchGame()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = $"steam://rungameid/{SteamAppId}",
                UseShellExecute = true
            });
            return OperationResult.Ok("Asked Steam to launch VEIN.");
        }
        catch (Exception ex)
        {
            return OperationResult.Fail($"Could not launch VEIN through Steam: {ex.Message}");
        }
    }

    public OperationResult SendObjectDump()
    {
        var before = SnapshotFile(ObjectDumpPath);
        var result = SendHotkey("Object dump (Ctrl+J / DumpAllObjects)", VkJ);
        return result.Success ? WaitAndExportObjectDump(result, before, TimeSpan.FromSeconds(90), "UE4SS Object Dump") : result;
    }

    public OperationResult SendAllDumps()
    {
        var before = SnapshotFile(ObjectDumpPath);
        var result = SendHotkeySequence(
            "all UE4SS dumps",
            [
                ("Object dump (Ctrl+J / DumpAllObjects)", VkJ),
                ("USMAP dump (Ctrl+Num6 / DumpUSMAP)", VkNumpad6),
                ("SDK/CXX header generation (Ctrl+H / GenerateSDK)", VkH),
                ("UHT-compatible header generation (Ctrl+Num9 / GenerateUHTCompatibleHeaders)", VkNumpad9),
                ("all actor dump (Ctrl+Num7 / DumpAllActors)", VkNumpad7),
                ("static mesh dump (Ctrl+Num8 / DumpStaticMeshes)", VkNumpad8)
            ]);
        if (!result.Success)
        {
            return result;
        }

        var objectExport = WaitAndExportObjectDump(result, before, TimeSpan.FromSeconds(120), "UE4SS Dump All");
        if (!objectExport.Success)
        {
            return objectExport;
        }

        var gameFiles = ExportFullGameDump(copyFiles: true);
        return gameFiles.Success
            ? OperationResult.Ok($"{objectExport.Message} {gameFiles.Message}")
            : gameFiles;
    }

    public OperationResult StartCheatEngineMemoryDump()
    {
        var status = GetStatus();
        if (!status.GameRunning)
        {
            return OperationResult.Fail("VEIN is not running. Launch VEIN and wait for the menu before starting a Cheat Engine memory dump.");
        }

        var workflowRoot = ResolveSiblingToolDirectory("vein-cheat-engine-dump-workflow");
        if (workflowRoot is null)
        {
            return OperationResult.Fail("Cheat Engine dump workflow folder was not found beside the VEIN tools.");
        }

        var launcher = Path.Combine(workflowRoot, "Start-VEIN-CheatEngineDump.ps1");
        var luaScript = Path.Combine(workflowRoot, "VEIN_MasterDump_Throttled.lua");
        if (!File.Exists(launcher))
        {
            return OperationResult.Fail($"Missing Cheat Engine launcher: {launcher}");
        }

        if (!File.Exists(luaScript))
        {
            return OperationResult.Fail($"Missing Cheat Engine Lua dump script: {luaScript}");
        }

        var cheatEnginePath = ResolveCheatEnginePath();
        if (cheatEnginePath is null)
        {
            return OperationResult.Fail("Cheat Engine was not found. Set CHEAT_ENGINE_PATH or install Cheat Engine in its default folder.");
        }

        try
        {
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var controlDir = Path.Combine(desktop, "VEIN_Dump_Control");
            lock (CheatEngineDumpStartLock)
            {
                Directory.CreateDirectory(controlDir);
                var activeStatus = GetActiveCheatEngineDumpStatus(controlDir);
                if (activeStatus is not null)
                {
                    return OperationResult.Ok($"Cheat Engine dump is already running: {activeStatus}. Wait for it to finish or use the stop flag before starting another one.");
                }

                File.Delete(Path.Combine(controlDir, "pause.flag"));
                File.Delete(Path.Combine(controlDir, "stop.flag"));
                File.WriteAllText(Path.Combine(controlDir, "status.txt"), $"starting headless CE dump {DateTime.Now:yyyy-MM-dd HH:mm:ss}", Encoding.UTF8);

                var startInfo = new ProcessStartInfo
                {
                    FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe"),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    WorkingDirectory = workflowRoot
                };
                startInfo.ArgumentList.Add("-NoLogo");
                startInfo.ArgumentList.Add("-NoProfile");
                startInfo.ArgumentList.Add("-NonInteractive");
                startInfo.ArgumentList.Add("-WindowStyle");
                startInfo.ArgumentList.Add("Hidden");
                startInfo.ArgumentList.Add("-ExecutionPolicy");
                startInfo.ArgumentList.Add("Bypass");
                startInfo.ArgumentList.Add("-File");
                startInfo.ArgumentList.Add(launcher);
                startInfo.ArgumentList.Add("-CheatEnginePath");
                startInfo.ArgumentList.Add(cheatEnginePath);
                startInfo.ArgumentList.Add("-Priority");
                startInfo.ArgumentList.Add("Idle");
                startInfo.ArgumentList.Add("-CpuThreads");
                startInfo.ArgumentList.Add("1");
                startInfo.ArgumentList.Add("-Headless");

                Process.Start(startInfo);
            }

            var gameFiles = ExportFullGameDump(copyFiles: true);
            return gameFiles.Success
                ? OperationResult.Ok($"Started headless Cheat Engine memory dump. Output will appear on Desktop as VEIN_MasterDump_*. Status: {Path.Combine(controlDir, "status.txt")}. {gameFiles.Message}")
                : OperationResult.Fail($"Started headless Cheat Engine memory dump, but the full game file dump failed: {gameFiles.Message}");
        }
        catch (Exception ex)
        {
            return OperationResult.Fail($"Could not start Cheat Engine memory dump: {ex.Message}");
        }
    }

    private static string? GetActiveCheatEngineDumpStatus(string controlDir)
    {
        try
        {
            if (File.Exists(Path.Combine(controlDir, "stop.flag")))
            {
                return null;
            }

            var statusPath = Path.Combine(controlDir, "status.txt");
            if (!File.Exists(statusPath))
            {
                return null;
            }

            var lastWrite = File.GetLastWriteTime(statusPath);
            if (DateTime.Now - lastWrite > TimeSpan.FromMinutes(30))
            {
                return null;
            }

            var status = File.ReadAllText(statusPath, Encoding.UTF8).Trim();
            if (string.IsNullOrWhiteSpace(status))
            {
                return null;
            }

            var normalized = status.ToLowerInvariant();
            var activePrefixes = new[]
            {
                "starting",
                "attaching",
                "running",
                "checkpoint",
                "paused"
            };

            return activePrefixes.Any(prefix => normalized.StartsWith(prefix, StringComparison.Ordinal)) ? status : null;
        }
        catch
        {
            return null;
        }
    }

    public OperationResult SendSdkDump() => SendHotkey("SDK/CXX header generation (Ctrl+H / GenerateSDK)", VkH);

    public OperationResult SendUhtDump() => SendHotkey("UHT-compatible header generation (Ctrl+Num9 / GenerateUHTCompatibleHeaders)", VkNumpad9);

    public OperationResult SendStaticMeshDump() => SendHotkey("static mesh dump (Ctrl+Num8 / DumpStaticMeshes)", VkNumpad8);

    public OperationResult SendActorDump() => SendHotkey("all actor dump (Ctrl+Num7 / DumpAllActors)", VkNumpad7);

    public OperationResult SendUsmapDump() => SendHotkey("USMAP dump (Ctrl+Num6 / DumpUSMAP)", VkNumpad6);

    public OperationResult ExportReadableObjectDump() => ExportReadableObjectDump("Manual Export");

    public OperationResult ExportReadableObjectDump(string dumpMethod)
    {
        EnsureDumpFolders();
        if (!File.Exists(ObjectDumpPath))
        {
            var missing = $"Object dump was not found yet: {ObjectDumpPath}";
            AppendAppLog(missing, error: true);
            return OperationResult.Fail(missing);
        }

        var startedAt = DateTime.Now;
        var status = GetStatus();
        var warnings = new List<string>();
        try
        {
            int objectCount;
            int actorCount;
            int propertyCount;
            int staticMeshCount;
            var suggestions = new ModSuggestionCollector();

            using (var objects = CreateCategoryWriter("objects", "Objects", ObjectDumpPath, startedAt))
            using (var actors = CreateCategoryWriter("actors", "Actors", ObjectDumpPath, startedAt))
            using (var props = CreateCategoryWriter("props", "Properties", ObjectDumpPath, startedAt))
            using (var staticMeshes = CreateCategoryWriter("static_meshes", "Static Meshes", ObjectDumpPath, startedAt))
            {
                foreach (var line in ReadSharedLines(ObjectDumpPath))
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    var entry = ParseObjectDumpLine(line);
                    suggestions.Add(entry);
                    objects.Write(entry);
                    if (IsActorEntry(entry))
                    {
                        actors.Write(entry);
                    }

                    if (IsPropertyEntry(entry))
                    {
                        props.Write(entry);
                    }

                    if (IsStaticMeshEntry(entry))
                    {
                        staticMeshes.Write(entry);
                    }
                }

                objects.Finish();
                actors.Finish();
                props.Finish();
                staticMeshes.Finish();
                objectCount = objects.Count;
                actorCount = actors.Count;
                propertyCount = props.Count;
                staticMeshCount = staticMeshes.Count;
            }

            WriteSdkSummary(startedAt, dumpMethod, warnings);
            CopyLuaLogToExport(warnings);
            WriteFullGameDumpCatalog(startedAt, dumpMethod, copyFiles: false, warnings);
            WriteModSuggestions(startedAt, dumpMethod, suggestions);
            WriteDumpIndex(startedAt, dumpMethod, status, objectCount, actorCount, propertyCount, staticMeshCount, suggestions, warnings);
            CreateLatestExportZip(warnings);
            MirrorLegacyObjectExports();

            var fileCount = EnumerateDumpFiles().Count;
            var message = $"Dump export complete. Files: {fileCount:N0}. Objects: {objectCount:N0}. Actors: {actorCount:N0}. Properties: {propertyCount:N0}. Static meshes: {staticMeshCount:N0}. Output: {DumpsRoot}";
            AppendAppLog(message);
            return OperationResult.Ok(message);
        }
        catch (Exception ex)
        {
            var message = $"Could not export readable object dump: {ex.Message}";
            AppendAppLog(message, error: true);
            return OperationResult.Fail(message);
        }
    }

    public OperationResult ExportFullGameDump(bool copyFiles = true)
    {
        EnsureDumpFolders();
        var startedAt = DateTime.Now;
        var warnings = new List<string>();

        try
        {
            var result = WriteFullGameDumpCatalog(startedAt, copyFiles ? "Full Game File Dump" : "Full Game File Catalog", copyFiles, warnings);
            CreateLatestExportZip(warnings);

            var warningText = warnings.Count == 0 ? "" : $" Warnings: {warnings.Count:N0}.";
            var message = copyFiles
                ? $"Full game dump complete. Files cataloged: {result.FileCount:N0}. Files mirrored: {result.CopiedCount:N0}. Size cataloged: {FormatBytes(result.TotalBytes)}. Mirror: {GameFilesMirrorDirectory}.{warningText}"
                : $"Full game file catalog complete. Files cataloged: {result.FileCount:N0}. Size cataloged: {FormatBytes(result.TotalBytes)}. Output: {GameFilesDirectory}.{warningText}";
            AppendAppLog(message);
            return OperationResult.Ok(message);
        }
        catch (Exception ex)
        {
            var message = $"Could not export full game dump: {ex.Message}";
            AppendAppLog(message, error: true);
            return OperationResult.Fail(message);
        }
    }

    public string BuildLiveObjectDumpView(string? searchTerm = null, int maxLines = 1_000)
    {
        var status = GetStatus();
        if (!status.GameRunning)
        {
            return string.Join(Environment.NewLine,
            [
                "Live View waiting for VEIN.",
                "",
                "Launch VEIN and wait until the menu is loaded.",
                "When VEIN is running, this view refreshes from the real UE4SS object dump.",
                "",
                "Use Dump All or Export Files to create readable PlainText, Markdown, JSON, and mod suggestion files.",
                "Open Files for exact source and output paths. Start with Markdown/dump_index.md and Markdown/mod_suggestions.md.",
                "No stale rows are shown while VEIN is closed."
            ]);
        }

        if (!File.Exists(ObjectDumpPath))
        {
            var lines = new List<string>
            {
                "Live View source: UE4SS object dump",
                "",
                "UE4SS_ObjectDump.txt has not been generated yet.",
                "Launch VEIN, wait for the menu, then use Dump All or Objects + Props.",
                "The readable export will create Markdown/dump_index.md, Markdown/mod_suggestions.md, PlainText/*.txt, and JSON/*.json.",
                "Open Files for exact source and output paths."
            };
            lines.Add("");
            lines.Add("---- UE4SS.log live tail ----");
            lines.AddRange(ReadLogTail(80));
            return string.Join(Environment.NewLine, lines);
        }

        try
        {
            var info = new FileInfo(ObjectDumpPath);
            var previewLineLimit = Math.Clamp(maxLines, 100, 5_000);
            var cacheKey = string.IsNullOrWhiteSpace(searchTerm)
                ? $"{info.FullName}|{info.Length}|{info.LastWriteTimeUtc.Ticks}|preview:{previewLineLimit}"
                : "";
            if (string.IsNullOrWhiteSpace(searchTerm) && cacheKey == _liveDumpCacheKey)
            {
                return _liveDumpCacheText;
            }

            var lines = new List<string>
            {
                "Live View source: UE4SS object dump",
                $"Last write: {info.LastWriteTime:yyyy-MM-dd HH:mm:ss}  |  Size: {info.Length:N0} bytes",
                "Open Files for exact source/export paths. Use Markdown/mod_suggestions.md for readable modding leads.",
                ""
            };

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var matches = new List<string>();
                var scanned = 0;
                foreach (var line in ReadSharedLines(ObjectDumpPath))
                {
                    scanned++;
                    if (!line.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    matches.Add(BuildReadableObjectDumpLine(ParseObjectDumpLine(line), scanned));
                    if (matches.Count >= maxLines)
                    {
                        break;
                    }
                }

                lines.Add($"Search: {searchTerm}");
                lines.Add(matches.Count == 0
                    ? "No matches found."
                    : $"Showing {matches.Count:N0} matches. Results are plain names, paths, types, hex addresses, and decimal addresses.");
                lines.Add("");
                lines.AddRange(matches);
                return string.Join(Environment.NewLine, lines);
            }

            lines.Add($"Live preview: newest {previewLineLimit:N0} readable object rows from the current UE4SS dump.");
            lines.Add("Full plain text, Markdown, JSON, dump index, and mod suggestions are kept on disk to avoid choking the UI.");
            lines.Add("Format: preview index | type | name | hex address | decimal address | object path");
            lines.Add("");

            var rowIndex = 0;
            foreach (var line in ReadFileTail(ObjectDumpPath, previewLineLimit, 4_194_304))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                rowIndex++;
                lines.Add(BuildReadableObjectDumpLine(ParseObjectDumpLine(line), rowIndex));
            }

            lines.Insert(4, $"Preview rows: {rowIndex:N0}");
            var result = string.Join(Environment.NewLine, lines);
            _liveDumpCacheKey = cacheKey;
            _liveDumpCacheText = result;
            return result;
        }
        catch (Exception ex)
        {
            return $"Could not read live object dump safely: {ex.Message}";
        }
    }

    public string BuildLiveCheatEngineDumpView(int maxRunLogLines = 180, int maxCandidateLines = 120)
    {
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        var controlDir = Path.Combine(desktop, "VEIN_Dump_Control");
        var statusPath = Path.Combine(controlDir, "status.txt");
        var latest = GetLatestCheatEngineDumpDirectory();
        var lines = new List<string>
        {
            "Live Memory View: Cheat Engine memory dump",
            "Mode: real-time tail only. Full files stay on disk so the UI and game do not choke.",
            ""
        };

        if (File.Exists(statusPath))
        {
            try
            {
                lines.Add("Status: " + File.ReadAllText(statusPath, Encoding.UTF8).Trim());
            }
            catch (Exception ex)
            {
                lines.Add("Status: could not read status.txt - " + ex.Message);
            }
        }
        else
        {
            lines.Add("Status: waiting for Dump All to start the headless memory dump.");
        }

        if (latest is null)
        {
            lines.Add("");
            lines.Add("No VEIN_MasterDump_* folder has been created yet.");
            lines.Add("Go to Dump, choose Cheat Engine Memory Dump, then click Dump All.");
            return string.Join(Environment.NewLine, lines);
        }

        lines.Add("Latest memory dump folder: available. Open Files for the full path.");
        lines.Add("");

        var checkpoint = Path.Combine(latest.FullName, "VEIN_Checkpoint_Status.txt");
        if (File.Exists(checkpoint))
        {
            lines.Add("---- checkpoint ----");
            lines.AddRange(ReadFileTail(checkpoint, 20, 64_000));
            lines.Add("");
        }

        var runLog = Path.Combine(latest.FullName, "VEIN_RunLog.txt");
        if (File.Exists(runLog))
        {
            lines.Add("---- live run log ----");
            lines.AddRange(ReadFileTail(runLog, maxRunLogLines, 512_000));
            lines.Add("");
        }
        else
        {
            lines.Add("Run log has not appeared yet.");
            lines.Add("");
        }

        var candidates = Path.Combine(latest.FullName, "VEIN_Candidates.csv");
        if (File.Exists(candidates))
        {
            var candidateInfo = new FileInfo(candidates);
            lines.Add($"---- live candidates tail ({candidateInfo.Length:N0} bytes) ----");
            lines.AddRange(ReadFileTail(candidates, maxCandidateLines, 512_000));
        }
        else
        {
            lines.Add("Candidate CSV has not appeared yet.");
        }

        return string.Join(Environment.NewLine, lines);
    }

    public DirectoryInfo? GetLatestCheatEngineDumpDirectory()
    {
        try
        {
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            if (!Directory.Exists(desktop))
            {
                return null;
            }

            return new DirectoryInfo(desktop)
                .EnumerateDirectories("VEIN_MasterDump_*")
                .OrderByDescending(directory => directory.LastWriteTimeUtc)
                .FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    public OperationResult OpenWin64Folder() => OpenFolder(Paths.Win64Directory, "Win64 folder");

    public OperationResult OpenUe4ssFolder() => OpenFolder(Paths.Ue4ssDirectory, "UE4SS folder");

    public OperationResult OpenModsFolder() => OpenFolder(Paths.ModsDirectory, "UE4SS Mods folder");

    public OperationResult FocusUe4ssDebugWindow()
    {
        return OpenLogFile();
    }

    public OperationResult ClickRestartAllMods()
    {
        return SendHotkey("UE4SS mod hot reload (Ctrl+R)", VkR);
    }

    public IReadOnlyList<Ue4ssModEntry> ReadMods()
    {
        var entries = new List<Ue4ssModEntry>();
        if (!File.Exists(Paths.ModsTxt))
        {
            return entries;
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rawLine in File.ReadAllLines(Paths.ModsTxt))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith(';') || !line.Contains(':'))
            {
                continue;
            }

            var parts = line.Split(':', 2);
            var name = parts[0].Trim();
            if (name.Length == 0 || !seen.Add(name))
            {
                continue;
            }

            var enabled = parts[1].Trim().StartsWith('1');
            var path = Path.Combine(Paths.ModsDirectory, name);
            entries.Add(new Ue4ssModEntry(name, enabled, Directory.Exists(path), path));
        }

        var modDirectories = Directory.Exists(Paths.ModsDirectory)
            ? Directory.GetDirectories(Paths.ModsDirectory).OrderBy(Path.GetFileName)
            : Enumerable.Empty<string>();
        foreach (var directory in modDirectories)
        {
            var name = Path.GetFileName(directory);
            if (!seen.Add(name) || name.Equals("shared", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            entries.Add(new Ue4ssModEntry(name, false, true, directory));
        }

        return entries.OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public OperationResult SetModEnabled(string modName, bool enabled)
    {
        if (string.IsNullOrWhiteSpace(modName))
        {
            return OperationResult.Fail("No mod was selected.");
        }

        if (!File.Exists(Paths.ModsTxt))
        {
            return OperationResult.Fail($"mods.txt was not found: {Paths.ModsTxt}");
        }

        try
        {
            var backup = $"{Paths.ModsTxt}.codex-ui-backup-{DateTime.Now:yyyyMMdd-HHmmss}";
            File.Copy(Paths.ModsTxt, backup, overwrite: false);

            var lines = File.ReadAllLines(Paths.ModsTxt).ToList();
            var changed = false;
            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                var trimmed = line.Trim();
                if (trimmed.StartsWith(';') || !trimmed.Contains(':'))
                {
                    continue;
                }

                var parts = trimmed.Split(':', 2);
                if (!parts[0].Trim().Equals(modName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                lines[i] = $"{parts[0].Trim()} : {(enabled ? 1 : 0)}";
                changed = true;
                break;
            }

            if (!changed)
            {
                lines.Add($"{modName} : {(enabled ? 1 : 0)}");
            }

            File.WriteAllLines(Paths.ModsTxt, lines);
            return OperationResult.Ok($"{(enabled ? "Enabled" : "Disabled")} {modName}. Restart all mods or restart VEIN for the change to load.");
        }
        catch (Exception ex)
        {
            return OperationResult.Fail($"Could not update mods.txt: {ex.Message}");
        }
    }

    public OperationResult OpenLogFile()
    {
        if (!File.Exists(Paths.Ue4ssLog))
        {
            return OperationResult.Fail("UE4SS.log does not exist yet. Launch VEIN first.");
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "notepad.exe",
                ArgumentList = { Paths.Ue4ssLog },
                UseShellExecute = false
            });
            return OperationResult.Ok("Opened UE4SS.log in Notepad.");
        }
        catch (Exception ex)
        {
            return OperationResult.Fail($"Could not open UE4SS.log: {ex.Message}");
        }
    }

    private OperationResult OpenFolder(string path, string label)
    {
        if (!Directory.Exists(path))
        {
            return OperationResult.Fail($"{label} was not found: {path}");
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
            return OperationResult.Ok($"Opened {label}.");
        }
        catch (Exception ex)
        {
            return OperationResult.Fail($"Could not open {label}: {ex.Message}");
        }
    }

    public OperationResult OpenDumpsFolder()
    {
        try
        {
            EnsureDumpFolders();
            return OpenFolder(DumpsRoot, "organized Dumps folder");
        }
        catch (Exception ex)
        {
            return OperationResult.Fail($"Could not open organized Dumps folder: {ex.Message}");
        }
    }

    private OperationResult WaitAndExportObjectDump(OperationResult hotkeyResult, FileSnapshot before, TimeSpan timeout, string dumpMethod)
    {
        var wait = WaitForFileUpdate(ObjectDumpPath, before, timeout);
        if (!wait.Success)
        {
            var message = $"{hotkeyResult.Message} {wait.Message}";
            AppendAppLog(message, error: true);
            return OperationResult.Fail(message);
        }

        var export = ExportReadableObjectDump(dumpMethod);
        return export.Success
            ? OperationResult.Ok($"{hotkeyResult.Message} {wait.Message} {export.Message}")
            : export;
    }

    private static FileSnapshot SnapshotFile(string path)
    {
        try
        {
            if (!File.Exists(path)) return new FileSnapshot(false, 0, DateTime.MinValue);
            var info = new FileInfo(path);
            return new FileSnapshot(true, info.Length, info.LastWriteTimeUtc);
        }
        catch
        {
            return new FileSnapshot(false, 0, DateTime.MinValue);
        }
    }

    private static OperationResult WaitForFileUpdate(string path, FileSnapshot before, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                if (File.Exists(path))
                {
                    var info = new FileInfo(path);
                    var changed = !before.Exists ||
                        info.Length != before.Length ||
                        info.LastWriteTimeUtc > before.LastWriteTimeUtc.AddMilliseconds(500);
                    if (changed && IsFileStable(path, TimeSpan.FromSeconds(2)))
                    {
                        return OperationResult.Ok($"Object dump updated: {path} ({info.Length:N0} bytes).");
                    }
                }
            }
            catch
            {
                // Keep waiting while UE4SS has the file locked or mid-write.
            }

            Thread.Sleep(1000);
        }

        return OperationResult.Fail($"UE4SS did not update the object dump within {timeout.TotalSeconds:N0}s. Check UE4SS.log for the real failure: {path}");
    }

    private static bool IsFileStable(string path, TimeSpan interval)
    {
        var first = new FileInfo(path);
        Thread.Sleep(interval);
        var second = new FileInfo(path);
        return first.Length == second.Length && first.LastWriteTimeUtc == second.LastWriteTimeUtc;
    }

    private static IEnumerable<string> ReadSharedLines(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream, Encoding.UTF8, true);
        while (reader.ReadLine() is { } line)
        {
            yield return line;
        }
    }

    private static string[] ReadFileTail(string path, int maxLines, int maxBytes = 1_048_576)
    {
        if (maxLines <= 0)
        {
            return [];
        }

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        if (stream.Length == 0)
        {
            return [];
        }

        var bytesToRead = (int)Math.Min(maxBytes, stream.Length);
        stream.Seek(-bytesToRead, SeekOrigin.End);

        var buffer = new byte[bytesToRead];
        var read = stream.Read(buffer, 0, bytesToRead);
        var text = Encoding.UTF8.GetString(buffer, 0, read);
        var lines = text.Replace("\r\n", "\n").Replace('\r', '\n')
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);

        if (stream.Length > bytesToRead && lines.Length > 1)
        {
            lines = lines.Skip(1).ToArray();
        }

        return lines.Length <= maxLines ? lines : lines.Skip(lines.Length - maxLines).ToArray();
    }

    private static string? ResolveSiblingToolDirectory(string toolName)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var directCandidate = Path.Combine(directory.FullName, toolName);
            if (Directory.Exists(directCandidate))
            {
                return directCandidate;
            }

            var bundledCandidate = Path.Combine(directory.FullName, "tools", toolName);
            if (Directory.Exists(bundledCandidate))
            {
                return bundledCandidate;
            }

            if (directory.Name.Equals("tools", StringComparison.OrdinalIgnoreCase))
            {
                var toolsCandidate = Path.Combine(directory.FullName, toolName);
                if (Directory.Exists(toolsCandidate))
                {
                    return toolsCandidate;
                }
            }

            directory = directory.Parent;
        }

        return null;
    }

    private static string? ResolveCheatEnginePath()
    {
        var envPath = Environment.GetEnvironmentVariable("CHEAT_ENGINE_PATH");
        if (!string.IsNullOrWhiteSpace(envPath) && File.Exists(envPath))
        {
            return Path.GetFullPath(envPath);
        }

        var candidates = new[]
        {
            @"C:\Program Files\Cheat Engine\cheatengine-x86_64-SSE4-AVX2.exe",
            @"C:\Program Files\Cheat Engine\cheatengine-x86_64.exe",
            @"C:\Program Files\Cheat Engine 7.5\cheatengine-x86_64-SSE4-AVX2.exe",
            @"C:\Program Files\Cheat Engine 7.5\cheatengine-x86_64.exe",
            @"C:\Program Files\Cheat Engine 7.4\cheatengine-x86_64.exe",
            @"C:\Program Files (x86)\Cheat Engine 7.5\cheatengine-x86_64-SSE4-AVX2.exe",
            @"C:\Program Files (x86)\Cheat Engine 7.5\cheatengine-x86_64.exe",
            @"C:\Program Files (x86)\Cheat Engine 7.4\cheatengine-x86_64.exe"
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    private DumpCategoryWriter CreateCategoryWriter(string fileStem, string title, string source, DateTime exportedAt)
    {
        return new DumpCategoryWriter(
            title,
            source,
            exportedAt,
            Path.Combine(PlainTextDirectory, $"{fileStem}.txt"),
            Path.Combine(MarkdownDirectory, $"{fileStem}.md"),
            Path.Combine(JsonDirectory, $"{fileStem}.json"));
    }

    private void WriteSdkSummary(DateTime exportedAt, string dumpMethod, List<string> warnings)
    {
        var path = Path.Combine(MarkdownDirectory, "sdk_summary.md");
        var cxxDir = Path.Combine(Paths.Ue4ssDirectory, "CXXHeaderDump");
        var headers = Directory.Exists(cxxDir)
            ? Directory.EnumerateFiles(cxxDir, "*.hpp", SearchOption.AllDirectories).Select(file => new FileInfo(file)).OrderByDescending(file => file.LastWriteTime).ToArray()
            : [];
        var usmaps = Directory.Exists(Paths.Ue4ssDirectory)
            ? Directory.EnumerateFiles(Paths.Ue4ssDirectory, "*.usmap", SearchOption.TopDirectoryOnly).Select(file => new FileInfo(file)).OrderByDescending(file => file.LastWriteTime).ToArray()
            : [];

        using var writer = new StreamWriter(path, false, new UTF8Encoding(false));
        writer.WriteLine("# VEIN SDK / Header Summary");
        writer.WriteLine();
        writer.WriteLine($"- Exported: `{exportedAt:yyyy-MM-dd HH:mm:ss}`");
        writer.WriteLine($"- Dump method: `{dumpMethod}`");
        writer.WriteLine($"- CXX header folder: `{cxxDir}`");
        writer.WriteLine($"- Header files found: `{headers.Length:N0}`");
        writer.WriteLine($"- USMAP files found: `{usmaps.Length:N0}`");
        writer.WriteLine();
        writer.WriteLine("## Latest Header Files");
        writer.WriteLine();
        foreach (var header in headers.Take(60))
        {
            writer.WriteLine($"- `{header.Name}` ({header.Length:N0} bytes, {header.LastWriteTime:yyyy-MM-dd HH:mm:ss})");
        }

        if (headers.Length == 0)
        {
            writer.WriteLine("- No generated `.hpp` files were found yet. Use Generate SDK after VEIN reaches the menu.");
            warnings.Add($"No SDK header files found in {cxxDir}");
        }

        writer.WriteLine();
        writer.WriteLine("## USMAP Files");
        writer.WriteLine();
        foreach (var usmap in usmaps)
        {
            writer.WriteLine($"- `{usmap.Name}` ({usmap.Length:N0} bytes, {usmap.LastWriteTime:yyyy-MM-dd HH:mm:ss})");
        }

        if (usmaps.Length == 0)
        {
            writer.WriteLine("- No `.usmap` files were found yet. Use Dump USMAP if this UE4SS build supports it.");
            warnings.Add($"No USMAP files found in {Paths.Ue4ssDirectory}");
        }
    }

    private void CopyLuaLogToExport(List<string> warnings)
    {
        if (!File.Exists(Paths.Ue4ssLog))
        {
            warnings.Add($"UE4SS.log was not found: {Paths.Ue4ssLog}");
            File.WriteAllText(LuaLogExportPath, "UE4SS.log has not been created yet." + Environment.NewLine, Encoding.UTF8);
            return;
        }

        var lines = ReadFileTail(Paths.Ue4ssLog, 6000, 8_388_608)
            .Where(line =>
                line.Contains("[Lua]", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("RegisterHook", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("Mod", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("Error", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("Warning", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        File.WriteAllLines(LuaLogExportPath, lines.Length == 0 ? ["No Lua/mod lines found in current UE4SS.log tail."] : lines, Encoding.UTF8);
    }

    private FullGameDumpResult WriteFullGameDumpCatalog(DateTime exportedAt, string dumpMethod, bool copyFiles, List<string> warnings)
    {
        Directory.CreateDirectory(GameFilesDirectory);
        if (copyFiles)
        {
            Directory.CreateDirectory(GameFilesMirrorDirectory);
        }

        var markdownPath = Path.Combine(GameFilesDirectory, "full_game_dump.md");
        var plainTextPath = Path.Combine(GameFilesDirectory, "full_game_files.txt");
        var jsonPath = Path.Combine(GameFilesDirectory, "full_game_files.json");
        var entries = EnumerateGameInstallFiles(warnings).ToArray();
        var totalBytes = entries.Sum(entry => entry.Length);
        var copiedCount = 0;
        var copiedBytes = 0L;

        using var markdown = new StreamWriter(markdownPath, false, new UTF8Encoding(false));
        using var plain = new StreamWriter(plainTextPath, false, new UTF8Encoding(false));
        using var jsonStream = new FileStream(jsonPath, FileMode.Create, FileAccess.Write, FileShare.Read);
        using var json = new Utf8JsonWriter(jsonStream, new JsonWriterOptions { Indented = true });

        markdown.WriteLine("# VEIN Full Game File Dump");
        markdown.WriteLine();
        markdown.WriteLine($"- Exported: `{exportedAt:yyyy-MM-dd HH:mm:ss}`");
        markdown.WriteLine($"- Dump method: `{dumpMethod}`");
        markdown.WriteLine($"- Game root: `{Paths.VeinRoot}`");
        markdown.WriteLine($"- Files cataloged: `{entries.Length:N0}`");
        markdown.WriteLine($"- Total file size cataloged: `{FormatBytes(totalBytes)}`");
        markdown.WriteLine($"- Mirror files copied: `{copyFiles}`");
        markdown.WriteLine($"- Mirror folder: `{GameFilesMirrorDirectory}`");
        markdown.WriteLine();
        markdown.WriteLine("This file explains what each dumped game file is usually for. It does not decrypt encrypted assets or patch the game. Use it as a readable map of the full local VEIN install.");
        markdown.WriteLine();
        markdown.WriteLine("## What The Major File Types Mean");
        markdown.WriteLine();
        foreach (var guide in GetFileTypeGuides())
        {
            markdown.WriteLine($"- **{guide.Label}**: {guide.Explanation} Useful for: {guide.ModUse} Safe handling: {guide.SafeHandling}");
        }

        markdown.WriteLine();
        markdown.WriteLine("## Folder Summary");
        markdown.WriteLine();
        foreach (var group in entries.GroupBy(entry => GetTopFolder(entry.RelativePath)).OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase))
        {
            markdown.WriteLine($"- `{group.Key}`: {group.Count():N0} files, {FormatBytes(group.Sum(entry => entry.Length))}");
        }

        markdown.WriteLine();
        markdown.WriteLine("## Full File Catalog");
        markdown.WriteLine();
        markdown.WriteLine("| Path | Size | Type | What it does | Modding use | Safe handling | Mirror status |");
        markdown.WriteLine("|---|---:|---|---|---|---|---|");

        plain.WriteLine("VEIN Full Game File Dump");
        plain.WriteLine($"Exported: {exportedAt:yyyy-MM-dd HH:mm:ss}");
        plain.WriteLine($"Game root: {Paths.VeinRoot}");
        plain.WriteLine($"Files cataloged: {entries.Length:N0}");
        plain.WriteLine($"Total file size cataloged: {FormatBytes(totalBytes)}");
        plain.WriteLine($"Mirror folder: {GameFilesMirrorDirectory}");
        plain.WriteLine();

        json.WriteStartObject();
        json.WriteString("exported_at", exportedAt.ToString("O", CultureInfo.InvariantCulture));
        json.WriteString("dump_method", dumpMethod);
        json.WriteString("game_root", Paths.VeinRoot);
        json.WriteString("mirror_folder", GameFilesMirrorDirectory);
        json.WriteBoolean("mirror_files_copied", copyFiles);
        json.WriteNumber("file_count", entries.Length);
        json.WriteNumber("total_bytes", totalBytes);
        json.WriteStartArray("file_type_guide");
        foreach (var guide in GetFileTypeGuides())
        {
            json.WriteStartObject();
            json.WriteString("label", guide.Label);
            json.WriteString("matches", guide.Matches);
            json.WriteString("explanation", guide.Explanation);
            json.WriteString("mod_use", guide.ModUse);
            json.WriteString("safe_handling", guide.SafeHandling);
            json.WriteEndObject();
        }
        json.WriteEndArray();
        json.WriteStartArray("files");

        foreach (var entry in entries)
        {
            var explanation = ExplainGameFile(entry);
            var mirrorStatus = "Catalog only";
            if (copyFiles)
            {
                mirrorStatus = CopyGameFileToMirror(entry, warnings) ? "Copied" : "Skipped or failed";
                if (mirrorStatus.Equals("Copied", StringComparison.OrdinalIgnoreCase))
                {
                    copiedCount++;
                    copiedBytes += entry.Length;
                }
            }

            markdown.WriteLine($"| `{EscapeMarkdownCell(entry.RelativePath)}` | {EscapeMarkdownCell(FormatBytes(entry.Length))} | {EscapeMarkdownCell(explanation.Label)} | {EscapeMarkdownCell(explanation.Explanation)} | {EscapeMarkdownCell(explanation.ModUse)} | {EscapeMarkdownCell(explanation.SafeHandling)} | {EscapeMarkdownCell(mirrorStatus)} |");

            plain.WriteLine(entry.RelativePath);
            plain.WriteLine($"  Size: {FormatBytes(entry.Length)}");
            plain.WriteLine($"  Type: {explanation.Label}");
            plain.WriteLine($"  What it does: {explanation.Explanation}");
            plain.WriteLine($"  Modding use: {explanation.ModUse}");
            plain.WriteLine($"  Safe handling: {explanation.SafeHandling}");
            plain.WriteLine($"  Mirror status: {mirrorStatus}");
            plain.WriteLine();

            json.WriteStartObject();
            json.WriteString("relative_path", entry.RelativePath.Replace('\\', '/'));
            json.WriteString("full_path", entry.FullPath);
            json.WriteNumber("bytes", entry.Length);
            json.WriteString("size", FormatBytes(entry.Length));
            json.WriteString("last_write", entry.LastWriteTime.ToString("O", CultureInfo.InvariantCulture));
            json.WriteString("type", explanation.Label);
            json.WriteString("explanation", explanation.Explanation);
            json.WriteString("mod_use", explanation.ModUse);
            json.WriteString("safe_handling", explanation.SafeHandling);
            json.WriteString("mirror_status", mirrorStatus);
            json.WriteEndObject();
        }

        json.WriteEndArray();
        json.WriteNumber("mirrored_file_count", copiedCount);
        json.WriteNumber("mirrored_bytes", copiedBytes);
        json.WriteStartArray("warnings");
        foreach (var warning in warnings)
        {
            json.WriteStringValue(warning);
        }
        json.WriteEndArray();
        json.WriteEndObject();

        return new FullGameDumpResult(entries.Length, totalBytes, copiedCount, copiedBytes);
    }

    private IEnumerable<GameFileEntry> EnumerateGameInstallFiles(List<string> warnings)
    {
        var root = Path.GetFullPath(Paths.VeinRoot);
        var excludedDumpRoot = Path.GetFullPath(DumpsRoot);
        var pending = new Stack<string>();
        pending.Push(root);

        while (pending.Count > 0)
        {
            var directory = pending.Pop();
            if (PathIsSameOrChildOf(directory, excludedDumpRoot))
            {
                continue;
            }

            IEnumerable<string> children;
            try
            {
                children = Directory.EnumerateDirectories(directory);
            }
            catch (Exception ex)
            {
                warnings.Add($"Could not enumerate folder {directory}: {ex.Message}");
                continue;
            }

            foreach (var child in children)
            {
                if (!PathIsSameOrChildOf(child, excludedDumpRoot))
                {
                    pending.Push(child);
                }
            }

            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(directory);
            }
            catch (Exception ex)
            {
                warnings.Add($"Could not enumerate files in {directory}: {ex.Message}");
                continue;
            }

            foreach (var file in files)
            {
                if (PathIsSameOrChildOf(file, excludedDumpRoot))
                {
                    continue;
                }

                FileInfo info;
                try
                {
                    info = new FileInfo(file);
                }
                catch (Exception ex)
                {
                    warnings.Add($"Could not read file info for {file}: {ex.Message}");
                    continue;
                }

                yield return new GameFileEntry(info.FullName, Path.GetRelativePath(root, info.FullName), info.Length, info.LastWriteTime);
            }
        }
    }

    private bool CopyGameFileToMirror(GameFileEntry entry, List<string> warnings)
    {
        var destination = Path.Combine(GameFilesMirrorDirectory, entry.RelativePath);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            if (File.Exists(destination))
            {
                var current = new FileInfo(destination);
                var source = new FileInfo(entry.FullPath);
                if (current.Length == entry.Length && current.LastWriteTimeUtc >= source.LastWriteTimeUtc)
                {
                    return true;
                }
            }

            File.Copy(entry.FullPath, destination, overwrite: true);
            File.SetLastWriteTime(destination, entry.LastWriteTime);
            return true;
        }
        catch (Exception ex)
        {
            warnings.Add($"Could not mirror {entry.RelativePath}: {ex.Message}");
            return false;
        }
    }

    private static GameFileExplanation ExplainGameFile(GameFileEntry entry)
    {
        var relative = entry.RelativePath.Replace('\\', '/');
        var extension = Path.GetExtension(entry.FullPath).ToLowerInvariant();
        if (relative.StartsWith("Content/Paks/", StringComparison.OrdinalIgnoreCase) ||
            relative.Contains("/Content/Paks/", StringComparison.OrdinalIgnoreCase))
        {
            return extension switch
            {
                ".pak" => new("Unreal package archive", "Large packaged game content archive. This is where cooked assets are bundled.", "Asset override planning and package layout checks.", "Do not edit in place. Make separate mod pak files or use supported mod folders."),
                ".ucas" => new("Unreal IO store data", "Large cooked asset data container used by modern Unreal builds.", "Confirms the bulk content containers that make up the installed game.", "Treat as read-only. Do not hex-edit or replace without a verified packaging workflow."),
                ".utoc" => new("Unreal IO store table", "Index/table file that pairs with UCAS data containers.", "Helps identify the container structure used by the game build.", "Keep with its matching UCAS file. Do not separate or patch directly."),
                ".sig" => new("Package signature", "Signature file used to validate packaged content.", "Shows which package files have signature companions.", "Do not modify. Changing signed package files can stop the game from loading."),
                _ => new("Packaged content companion", "File stored beside Unreal packaged game content.", "Useful for understanding the content packaging layout.", "Treat as read-only unless using a tested mod packaging workflow.")
            };
        }

        return extension switch
        {
            ".exe" => new("Executable", "Program binary that starts VEIN or a helper process.", "Useful for process detection, launch paths, and troubleshooting.", "Do not patch the executable for normal modding."),
            ".dll" => new("Dynamic library", "Runtime library, Unreal dependency, Steam dependency, or UE4SS/proxy module.", "Useful for confirming UE4SS/proxy install state and dependency layout.", "Do not delete or replace unless you know exactly which loader/mod requires it."),
            ".ini" => new("Configuration file", "Plain text settings used by Unreal, UE4SS, or local tools.", "Useful for finding tweakable settings and startup configuration.", "Back up before editing and change one setting at a time."),
            ".log" => new("Log file", "Runtime output from VEIN, UE4SS, or dump tools.", "Useful for diagnosing crashes, missing mods, failed dumps, and Lua errors.", "Safe to read/copy. Do not treat old logs as current evidence."),
            ".lua" => new("Lua script", "UE4SS or tool script that can automate mod behavior or dump work.", "Useful as readable mod code and for understanding what a UE4SS mod does.", "Read before running. Avoid write-memory scripts unless you trust and understand them."),
            ".txt" => new("Text/config/dump file", "Plain text notes, UE4SS config, mod list, or generated dump output.", "Useful for mod lists, object dump output, and easy search.", "Safe to read; back up before editing config-like text files."),
            ".json" => new("Structured data", "Machine-readable structured data generated by tools or used by configuration.", "Useful for automation, search tools, mod indexes, and later processing.", "Preserve formatting and validate JSON after edits."),
            ".md" => new("Markdown report", "Human-readable report or guide generated by this dumper.", "Useful for modder notes, object explanations, and dump navigation.", "Safe to read and share if it does not contain private paths you care about."),
            ".usmap" => new("Unreal mappings file", "Reflection/name mapping output used by Unreal dump tooling.", "Useful for SDK generation, property names, and object interpretation.", "Keep with matching game build; stale mappings can mislead tools."),
            ".pdb" => new("Debug symbols", "Symbol/debug metadata for binaries when available.", "Useful for crash diagnosis and understanding function names.", "Large and read-only. Do not ship publicly unless licensing/source rules allow it."),
            ".ico" => new("Icon asset", "Windows icon resource used by the app or game helper.", "Useful only for UI branding.", "Safe to copy."),
            ".png" or ".jpg" or ".jpeg" or ".webp" or ".bmp" => new("Image asset", "Loose image, texture, or UI file.", "Useful for UI references, icons, documentation, or replacement planning.", "Do not overwrite originals; package replacements separately."),
            _ => new("Game/support file", "Installed VEIN file or dependency not recognized by the dumper.", "Useful for full install inventory and troubleshooting missing or changed files.", "Treat as read-only unless you know the owning tool.")
        };
    }

    private static IReadOnlyList<FileTypeGuide> GetFileTypeGuides()
    {
        return
        [
            new(".pak", "*.pak", "Unreal packaged content archive.", "Asset override planning and package layout checks.", "Do not edit directly; create separate mod pak output."),
            new(".ucas", "*.ucas", "Unreal IO store bulk data container.", "Confirms bulk cooked content files.", "Treat as read-only and keep with matching .utoc."),
            new(".utoc", "*.utoc", "Unreal IO store table/index file.", "Explains how IO store containers are paired.", "Do not separate from the matching .ucas."),
            new(".sig", "*.sig", "Package signature/validation companion.", "Shows signed content companions.", "Do not modify."),
            new(".exe", "*.exe", "Windows executable.", "Launch/process detection and troubleshooting.", "Do not patch for normal modding."),
            new(".dll", "*.dll", "Runtime library or loader module.", "UE4SS/proxy/dependency verification.", "Replace only with trusted matching versions."),
            new(".ini", "*.ini", "Plain text configuration.", "Safe setting discovery and controlled tweaks.", "Back up first."),
            new(".lua", "*.lua", "UE4SS/tool Lua script.", "Readable mod logic and dump automation.", "Read before running."),
            new(".log", "*.log", "Runtime logs.", "Crash/dump/mod failure diagnosis.", "Safe to read/copy."),
            new(".json", "*.json", "Structured data.", "Automation, searching, and tool imports.", "Validate after editing."),
            new(".md", "*.md", "Markdown report.", "Human-readable guides and dump navigation.", "Safe to read/share after checking private paths."),
            new(".usmap", "*.usmap", "Unreal mapping/reflection output.", "SDK and property-name interpretation.", "Keep build-matched.")
        ];
    }

    private void WriteModSuggestions(DateTime exportedAt, string dumpMethod, ModSuggestionCollector suggestions)
    {
        var markdownPath = Path.Combine(MarkdownDirectory, "mod_suggestions.md");
        var jsonPath = Path.Combine(JsonDirectory, "mod_suggestions.json");
        var buckets = suggestions.Buckets.ToArray();

        using (var markdown = new StreamWriter(markdownPath, false, new UTF8Encoding(false)))
        {
            markdown.WriteLine("# VEIN Mod Suggestions");
            markdown.WriteLine();
            markdown.WriteLine($"- Exported: `{exportedAt:yyyy-MM-dd HH:mm:ss}`");
            markdown.WriteLine($"- Dump method: `{dumpMethod}`");
            markdown.WriteLine($"- Purpose: turn raw dumped names into readable modding leads.");
            markdown.WriteLine();
            markdown.WriteLine("Use these as starting points, not proof that a value is safe to edit. Read first, test on a backup/save, then change one thing at a time.");
            markdown.WriteLine();

            if (buckets.Length == 0)
            {
                markdown.WriteLine("No suggestion groups were created from the current dump.");
            }

            foreach (var bucket in buckets)
            {
                markdown.WriteLine($"## {bucket.Title}");
                markdown.WriteLine();
                markdown.WriteLine($"- Matches: `{bucket.Count:N0}`");
                markdown.WriteLine($"- Why it matters: {bucket.Description}");
                markdown.WriteLine($"- Useful for: {bucket.UseCase}");
                markdown.WriteLine();
                markdown.WriteLine("### Suggested Mod Ideas");
                markdown.WriteLine();
                foreach (var idea in bucket.Ideas)
                {
                    markdown.WriteLine($"- {idea}");
                }

                markdown.WriteLine();
                markdown.WriteLine("### Safe Next Steps");
                markdown.WriteLine();
                foreach (var step in bucket.NextSteps)
                {
                    markdown.WriteLine($"- {step}");
                }

                markdown.WriteLine();
                markdown.WriteLine("### Example Dumped Names");
                markdown.WriteLine();
                foreach (var sample in bucket.Samples)
                {
                    markdown.WriteLine($"- `{sample.Name}` - `{sample.Type}` - `{sample.Path}`");
                }

                markdown.WriteLine();
            }
        }

        using var stream = new FileStream(jsonPath, FileMode.Create, FileAccess.Write, FileShare.Read);
        using var json = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
        json.WriteStartObject();
        json.WriteString("exported_at", exportedAt.ToString("O", CultureInfo.InvariantCulture));
        json.WriteString("dump_method", dumpMethod);
        json.WriteString("purpose", "Readable modding leads generated from VEIN dump names, paths, types, and metadata.");
        json.WriteStartArray("suggestions");
        foreach (var bucket in buckets)
        {
            json.WriteStartObject();
            json.WriteString("id", bucket.Id);
            json.WriteString("title", bucket.Title);
            json.WriteNumber("matches", bucket.Count);
            json.WriteString("description", bucket.Description);
            json.WriteString("use_case", bucket.UseCase);
            json.WriteStartArray("ideas");
            foreach (var idea in bucket.Ideas) json.WriteStringValue(idea);
            json.WriteEndArray();
            json.WriteStartArray("next_steps");
            foreach (var step in bucket.NextSteps) json.WriteStringValue(step);
            json.WriteEndArray();
            json.WriteStartArray("samples");
            foreach (var sample in bucket.Samples)
            {
                json.WriteStartObject();
                json.WriteString("type", sample.Type);
                json.WriteString("name", sample.Name);
                json.WriteString("path", sample.Path);
                json.WriteString("address_hex", string.IsNullOrWhiteSpace(sample.AddressHex) ? "" : $"0x{sample.AddressHex}");
                json.WriteString("address_decimal", sample.AddressDecimal);
                json.WriteEndObject();
            }
            json.WriteEndArray();
            json.WriteEndObject();
        }
        json.WriteEndArray();
        json.WriteEndObject();
    }

    private void WriteDumpIndex(DateTime exportedAt, string dumpMethod, Ue4ssStatus status, int objectCount, int actorCount, int propertyCount, int staticMeshCount, ModSuggestionCollector suggestions, IReadOnlyList<string> warnings)
    {
        var markdownPath = Path.Combine(MarkdownDirectory, "dump_index.md");
        var jsonPath = Path.Combine(JsonDirectory, "dump_index.json");
        var generatedFiles = EnumerateDumpFiles()
            .Where(file => !file.RelativePath.Equals("Exports\\latest_export.zip", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        using (var markdown = new StreamWriter(markdownPath, false, new UTF8Encoding(false)))
        {
            markdown.WriteLine("# VEIN Dump Index");
            markdown.WriteLine();
            markdown.WriteLine($"- Game detected: `{status.GameExeFound}`");
            markdown.WriteLine($"- Game running: `{status.GameRunning}`{(status.GameProcessId.HasValue ? $" PID `{status.GameProcessId}`" : "")}");
            markdown.WriteLine($"- Game path: `{Paths.VeinRoot}`");
            markdown.WriteLine($"- Win64 path: `{Paths.Win64Directory}`");
            markdown.WriteLine($"- UE4SS path: `{Paths.Ue4ssDirectory}`");
            markdown.WriteLine($"- Dump time: `{exportedAt:yyyy-MM-dd HH:mm:ss}`");
            markdown.WriteLine($"- Dump method used: `{dumpMethod}`");
            markdown.WriteLine($"- Total objects found: `{objectCount:N0}`");
            markdown.WriteLine($"- Total actors found: `{actorCount:N0}`");
            markdown.WriteLine($"- Total properties found: `{propertyCount:N0}`");
            markdown.WriteLine($"- Total static meshes found: `{staticMeshCount:N0}`");
            markdown.WriteLine();
            markdown.WriteLine("## Files Generated");
            markdown.WriteLine();
            foreach (var file in generatedFiles)
            {
                markdown.WriteLine($"- `{file.RelativePath}` ({file.Length:N0} bytes)");
            }

            markdown.WriteLine();
            markdown.WriteLine("## Errors and Warnings");
            markdown.WriteLine();
            if (warnings.Count == 0)
            {
                markdown.WriteLine("- No export warnings were recorded.");
            }
            else
            {
                foreach (var warning in warnings)
                {
                    markdown.WriteLine($"- {warning}");
                }
            }

            markdown.WriteLine();
            markdown.WriteLine("## Useful Next Steps For Modders");
            markdown.WriteLine();
            markdown.WriteLine("- Use `Markdown/objects.md` for human-readable object paths and addresses.");
            markdown.WriteLine("- Use `JSON/objects.json` when another tool needs structured data.");
            markdown.WriteLine("- Start with `Markdown/mod_suggestions.md` for readable mod ideas, safe next steps, and example dumped names.");
            markdown.WriteLine("- Use `GameFiles/full_game_dump.md` to understand every installed VEIN file, what each file type does, and which files are safe to read versus unsafe to patch.");
            markdown.WriteLine("- Check `Markdown/sdk_summary.md` for generated headers and USMAP status.");
            markdown.WriteLine("- Check `Logs/ue4ss_lua.log` and `Logs/dump_errors.log` when a dump button fails.");
            markdown.WriteLine();
            markdown.WriteLine("## Top Modding Leads");
            markdown.WriteLine();
            foreach (var bucket in suggestions.Buckets.Take(10))
            {
                markdown.WriteLine($"- **{bucket.Title}** (`{bucket.Count:N0}` matches): {bucket.UseCase}");
            }
        }

        using var stream = new FileStream(jsonPath, FileMode.Create, FileAccess.Write, FileShare.Read);
        using var json = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
        json.WriteStartObject();
        json.WriteString("game_path", Paths.VeinRoot);
        json.WriteString("win64_path", Paths.Win64Directory);
        json.WriteString("ue4ss_path", Paths.Ue4ssDirectory);
        json.WriteBoolean("game_detected", status.GameExeFound);
        json.WriteBoolean("game_running", status.GameRunning);
        if (status.GameProcessId.HasValue) json.WriteNumber("game_process_id", status.GameProcessId.Value);
        json.WriteString("dump_time", exportedAt.ToString("O", CultureInfo.InvariantCulture));
        json.WriteString("dump_method", dumpMethod);
        json.WriteNumber("total_objects", objectCount);
        json.WriteNumber("total_actors", actorCount);
        json.WriteNumber("total_properties", propertyCount);
        json.WriteNumber("total_static_meshes", staticMeshCount);
        json.WriteStartArray("files_generated");
        foreach (var file in generatedFiles)
        {
            json.WriteStartObject();
            json.WriteString("relative_path", file.RelativePath.Replace('\\', '/'));
            json.WriteString("full_path", file.FullPath);
            json.WriteNumber("bytes", file.Length);
            json.WriteString("last_write", file.LastWriteTime.ToString("O", CultureInfo.InvariantCulture));
            json.WriteEndObject();
        }
        json.WriteEndArray();
        json.WriteStartArray("warnings");
        foreach (var warning in warnings) json.WriteStringValue(warning);
        json.WriteEndArray();
        json.WriteStartArray("next_steps");
        json.WriteStringValue("Read Markdown/objects.md for object names, paths, classes, addresses, and metadata.");
        json.WriteStringValue("Use JSON/*.json for automation or another modding tool.");
        json.WriteStringValue("Read Markdown/mod_suggestions.md for mod ideas, safe next steps, and useful dumped-name examples.");
        json.WriteStringValue("Read GameFiles/full_game_dump.md for the full installed game file catalog and file-type explanations.");
        json.WriteStringValue("Inspect Logs/ue4ss_lua.log and Logs/dump_errors.log when a dump fails.");
        json.WriteEndArray();
        json.WriteStartArray("top_modding_leads");
        foreach (var bucket in suggestions.Buckets.Take(10))
        {
            json.WriteStartObject();
            json.WriteString("id", bucket.Id);
            json.WriteString("title", bucket.Title);
            json.WriteNumber("matches", bucket.Count);
            json.WriteString("use_case", bucket.UseCase);
            json.WriteEndObject();
        }
        json.WriteEndArray();
        json.WriteEndObject();
    }

    private void CreateLatestExportZip(List<string> warnings)
    {
        string? tempZip = null;
        try
        {
            if (File.Exists(LatestExportZipPath))
            {
                File.Delete(LatestExportZipPath);
            }

            tempZip = Path.Combine(Path.GetTempPath(), $"vein-dump-export-{Guid.NewGuid():N}.zip");
            using (var archive = ZipFile.Open(tempZip, ZipArchiveMode.Create))
            {
                foreach (var file in Directory.EnumerateFiles(DumpsRoot, "*", SearchOption.AllDirectories))
                {
                    var fullPath = Path.GetFullPath(file);
                    if (fullPath.Equals(Path.GetFullPath(LatestExportZipPath), StringComparison.OrdinalIgnoreCase) ||
                        PathIsSameOrChildOf(fullPath, Path.GetFullPath(GameFilesMirrorDirectory)))
                    {
                        continue;
                    }

                    archive.CreateEntryFromFile(file, Path.GetRelativePath(DumpsRoot, file), CompressionLevel.Optimal);
                }
            }

            File.Move(tempZip, LatestExportZipPath, overwrite: true);
            tempZip = null;
        }
        catch (Exception ex)
        {
            warnings.Add($"Could not create latest export zip: {ex.Message}");
            AppendAppLog($"Could not create latest export zip: {ex.Message}", error: true);
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(tempZip) && File.Exists(tempZip))
            {
                try { File.Delete(tempZip); } catch { }
            }
        }
    }

    private void MirrorLegacyObjectExports()
    {
        CopyIfExists(Path.Combine(PlainTextDirectory, "objects.txt"), ObjectDumpPlainTextPath);
        CopyIfExists(Path.Combine(MarkdownDirectory, "objects.md"), ObjectDumpMarkdownPath);
        CopyIfExists(Path.Combine(JsonDirectory, "objects.json"), ObjectDumpReadableJsonPath);
    }

    private static void CopyIfExists(string source, string destination)
    {
        if (File.Exists(source))
        {
            File.Copy(source, destination, overwrite: true);
        }
    }

    private static bool IsActorEntry(ParsedObjectDumpLine entry)
    {
        var haystack = $"{entry.Type} {entry.Name} {entry.Path}";
        var tokens = TokenizeSuggestionText(haystack).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return tokens.Contains("actor") ||
               tokens.Contains("pawn") ||
               tokens.Contains("character") ||
               ContainsAny(entry.Name, "BP_") ||
               ContainsAny(entry.Path, "/Actors/", "/Characters/", "BP_");
    }

    private static bool IsPropertyEntry(ParsedObjectDumpLine entry)
    {
        return ContainsAny(entry.Type, "Property", "FProperty", "BoolProperty", "IntProperty", "FloatProperty", "ObjectProperty", "StructProperty", "ArrayProperty") ||
               entry.Metadata.Any(field => field.Key.Equals("o", StringComparison.OrdinalIgnoreCase) || field.Label.Contains("Property", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsStaticMeshEntry(ParsedObjectDumpLine entry)
    {
        return ContainsAny(entry.Type, "StaticMesh") ||
               ContainsAny(entry.Name, "SM_", "StaticMesh") ||
               ContainsAny(entry.Path, "StaticMesh", "/Meshes/", "/Mesh/", "SM_");
    }

    private static IEnumerable<ModSuggestionRule> GetMatchingSuggestionRules(ParsedObjectDumpLine entry)
    {
        var haystack = $"{entry.Type} {entry.Name} {entry.Path}";
        var tokens = TokenizeSuggestionText(haystack).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var rule in ModSuggestionRules)
        {
            if (rule.Keywords.Any(keyword => SuggestionKeywordMatches(haystack, tokens, keyword)))
            {
                yield return rule;
            }
        }

        if (IsActorEntry(entry))
        {
            yield return ActorSuggestionRule;
        }

        if (IsPropertyEntry(entry))
        {
            yield return PropertySuggestionRule;
        }

        if (IsStaticMeshEntry(entry))
        {
            yield return StaticMeshSuggestionRule;
        }
    }

    private static string BuildEntrySuggestion(ParsedObjectDumpLine entry)
    {
        return GetSuggestionMatch(entry).Suggestion;
    }

    private static string[] GetSuggestionTags(ParsedObjectDumpLine entry)
    {
        return GetSuggestionMatch(entry).Tags;
    }

    private static CachedSuggestionMatch GetSuggestionMatch(ParsedObjectDumpLine entry)
    {
        return SuggestionCache.GetValue(entry, CreateSuggestionMatch);
    }

    private static CachedSuggestionMatch CreateSuggestionMatch(ParsedObjectDumpLine entry)
    {
        var rules = GetMatchingSuggestionRules(entry)
            .GroupBy(rule => rule.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToArray();

        var suggestion = rules.FirstOrDefault()?.RowSuggestion ??
            "Lookup anchor: search this name/path in JSON, SDK headers, UE4SS logs, or Lua before editing anything.";
        var tags = rules
            .Select(rule => rule.Id)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new CachedSuggestionMatch(rules, suggestion, tags.Length == 0 ? ["lookup-anchor"] : tags);
    }

    private static bool SuggestionKeywordMatches(string haystack, HashSet<string> tokens, string keyword)
    {
        if (keyword.Any(ch => !char.IsLetterOrDigit(ch)))
        {
            return haystack.Contains(keyword, StringComparison.OrdinalIgnoreCase);
        }

        var keywordTokens = TokenizeSuggestionText(keyword).ToArray();
        return keywordTokens.Length > 0 && keywordTokens.All(tokens.Contains);
    }

    private static IEnumerable<string> TokenizeSuggestionText(string value)
    {
        var token = new StringBuilder();
        for (var index = 0; index < value.Length; index++)
        {
            var current = value[index];
            if (!char.IsLetterOrDigit(current))
            {
                foreach (var flushed in FlushSuggestionToken(token))
                {
                    yield return flushed;
                }

                continue;
            }

            if (token.Length > 0 && char.IsUpper(current) && char.IsLower(value[index - 1]))
            {
                foreach (var flushed in FlushSuggestionToken(token))
                {
                    yield return flushed;
                }
            }

            token.Append(current);
        }

        foreach (var flushed in FlushSuggestionToken(token))
        {
            yield return flushed;
        }
    }

    private static IEnumerable<string> FlushSuggestionToken(StringBuilder token)
    {
        if (token.Length == 0)
        {
            yield break;
        }

        yield return token.ToString().ToLowerInvariant();
        token.Clear();
    }

    private static bool ContainsAny(string value, params string[] needles)
    {
        return needles.Any(needle => value.Contains(needle, StringComparison.OrdinalIgnoreCase));
    }

    private static bool PathIsSameOrChildOf(string path, string parent)
    {
        var normalizedPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var normalizedParent = Path.GetFullPath(parent).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return normalizedPath.Equals(normalizedParent, StringComparison.OrdinalIgnoreCase) ||
               normalizedPath.StartsWith(normalizedParent + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
               normalizedPath.StartsWith(normalizedParent + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetTopFolder(string relativePath)
    {
        var normalized = relativePath.Replace('\\', '/');
        var slash = normalized.IndexOf('/');
        return slash > 0 ? normalized[..slash] : "(root)";
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double value = bytes;
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return unit == 0
            ? $"{bytes:N0} {units[unit]}"
            : $"{value:N2} {units[unit]}";
    }

    private static ParsedObjectDumpLine ParseObjectDumpLine(string line)
    {
        var raw = line.TrimEnd();
        var addressHex = "";
        var addressDecimal = "";
        var rest = raw;

        if (raw.StartsWith("[", StringComparison.Ordinal))
        {
            var close = raw.IndexOf(']');
            if (close > 1)
            {
                addressHex = raw.Substring(1, close - 1).Trim();
                if (ulong.TryParse(addressHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var address))
                {
                    addressDecimal = address.ToString(CultureInfo.InvariantCulture);
                }

                rest = raw[(close + 1)..].TrimStart();
            }
        }

        var type = "";
        var objectPath = rest;
        var firstSpace = rest.IndexOf(' ');
        if (firstSpace > 0)
        {
            type = rest[..firstSpace].Trim();
            objectPath = rest[(firstSpace + 1)..].Trim();
        }

        var metadata = Array.Empty<ObjectDumpMetadataField>();
        var metadataStart = objectPath.IndexOf(" [", StringComparison.Ordinal);
        if (metadataStart >= 0)
        {
            metadata = ParseObjectDumpMetadata(objectPath[metadataStart..]);
            objectPath = objectPath[..metadataStart].TrimEnd();
        }

        var name = objectPath;
        var lastSeparator = Math.Max(
            objectPath.LastIndexOf(':'),
            Math.Max(objectPath.LastIndexOf('.'), objectPath.LastIndexOf('/')));
        if (lastSeparator >= 0 && lastSeparator < objectPath.Length - 1)
        {
            name = objectPath[(lastSeparator + 1)..];
        }

        return new ParsedObjectDumpLine(raw, addressHex, addressDecimal, type, objectPath, name, metadata);
    }

    private static ObjectDumpMetadataField[] ParseObjectDumpMetadata(string metadataText)
    {
        var fields = new List<ObjectDumpMetadataField>();
        var cursor = 0;
        while (cursor < metadataText.Length)
        {
            var open = metadataText.IndexOf('[', cursor);
            if (open < 0)
            {
                break;
            }

            var close = metadataText.IndexOf(']', open + 1);
            if (close < 0)
            {
                break;
            }

            var content = metadataText.Substring(open + 1, close - open - 1).Trim();
            var separator = content.IndexOf(':');
            if (separator > 0)
            {
                var key = content[..separator].Trim();
                var value = content[(separator + 1)..].Trim();
                fields.Add(BuildMetadataField(key, value));
            }

            cursor = close + 1;
        }

        return fields.ToArray();
    }

    private static ObjectDumpMetadataField BuildMetadataField(string key, string value)
    {
        var normalizedKey = key.ToLowerInvariant();
        var label = normalizedKey switch
        {
            "n" => "Name id",
            "c" => "Class address",
            "or" => "Outer address",
            "sps" => "Super/parent struct address",
            "f" => "Native function address",
            "o" => "Property offset",
            "owr" => "Owner address",
            _ => key
        };
        var isAddress = normalizedKey is "c" or "or" or "sps" or "f" or "owr";
        var numericBase = isAddress || LooksLikeHex(value) ? 16 : 10;
        var decimalValue = ulong.TryParse(
            value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? value[2..] : value,
            numericBase == 16 ? NumberStyles.HexNumber : NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var parsed)
            ? parsed.ToString(CultureInfo.InvariantCulture)
            : "";
        var displayValue = isAddress && !value.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? $"0x{value}"
            : value;

        return new ObjectDumpMetadataField(key, label, displayValue, decimalValue, isAddress);
    }

    private static bool LooksLikeHex(string value)
    {
        var normalized = value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? value[2..] : value;
        return normalized.Length > 0 && normalized.Any(char.IsLetter) && normalized.All(Uri.IsHexDigit);
    }

    private static string BuildReadableObjectDumpLine(ParsedObjectDumpLine entry, int index)
    {
        var addressHex = string.IsNullOrWhiteSpace(entry.AddressHex) ? "n/a" : $"0x{entry.AddressHex}";
        var addressDecimal = string.IsNullOrWhiteSpace(entry.AddressDecimal) ? "n/a" : entry.AddressDecimal;
        var type = string.IsNullOrWhiteSpace(entry.Type) ? "Unknown" : entry.Type;
        var name = string.IsNullOrWhiteSpace(entry.Name) ? "(unnamed)" : entry.Name;
        var path = string.IsNullOrWhiteSpace(entry.Path) ? entry.Raw : entry.Path;
        return $"{index:00000} | {CompactRight(type, 14),-14} | {CompactRight(name, 28),-28} | hex={addressHex} | dec={addressDecimal} | {CompactMiddle(path, 76)}";
    }

    private static string CompactRight(string value, int maxLength)
    {
        if (value.Length <= maxLength)
        {
            return value;
        }

        return string.Concat(value.AsSpan(0, Math.Max(0, maxLength - 1)), "~");
    }

    private static string CompactMiddle(string value, int maxLength)
    {
        if (value.Length <= maxLength)
        {
            return value;
        }

        if (maxLength <= 5)
        {
            return value[..maxLength];
        }

        var left = (maxLength - 3) / 2;
        var right = maxLength - 3 - left;
        return string.Concat(value.AsSpan(0, left), "...", value.AsSpan(value.Length - right, right));
    }

    private static void WritePlainObject(TextWriter writer, ParsedObjectDumpLine entry, int index)
    {
        writer.WriteLine($"[{index:N0}] {entry.Type} - {entry.Name}");
        writer.WriteLine($"Path: {entry.Path}");
        writer.WriteLine($"Address hex: 0x{entry.AddressHex}");
        writer.WriteLine($"Address decimal: {entry.AddressDecimal}");
        if (entry.Metadata.Count > 0)
        {
            writer.WriteLine("Metadata:");
            foreach (var field in entry.Metadata)
            {
                var decimalSuffix = string.IsNullOrWhiteSpace(field.DecimalValue)
                    ? ""
                    : $" (decimal {field.DecimalValue})";
                writer.WriteLine($"  {field.Label}: {field.Value}{decimalSuffix}");
            }
        }

        writer.WriteLine($"Raw: {entry.Raw}");
        writer.WriteLine();
    }

    private static void WriteMarkdownObject(TextWriter writer, ParsedObjectDumpLine entry, int index)
    {
        var metadata = entry.Metadata.Count == 0
            ? ""
            : string.Join("<br>", entry.Metadata.Select(field =>
            {
                var decimalSuffix = string.IsNullOrWhiteSpace(field.DecimalValue)
                    ? ""
                    : $" decimal {field.DecimalValue}";
                return $"{field.Label}: {field.Value}{decimalSuffix}";
            }));

        writer.WriteLine(string.Join("|",
        [
            "",
            index.ToString(CultureInfo.InvariantCulture),
            EscapeMarkdownCell(entry.Type),
            EscapeMarkdownCell(entry.Name),
            EscapeMarkdownCell(string.IsNullOrWhiteSpace(entry.AddressHex) ? "" : $"0x{entry.AddressHex}"),
            EscapeMarkdownCell(entry.AddressDecimal),
            EscapeMarkdownCell(entry.Path),
            EscapeMarkdownCell(metadata),
            EscapeMarkdownCell(BuildEntrySuggestion(entry)),
            ""
        ]));
    }

    private static void WriteJsonObject(Utf8JsonWriter writer, ParsedObjectDumpLine entry, int index)
    {
        writer.WriteStartObject();
        writer.WriteNumber("index", index);
        writer.WriteString("type", entry.Type);
        writer.WriteString("name", entry.Name);
        writer.WriteString("path", entry.Path);
        writer.WriteString("address_hex", string.IsNullOrWhiteSpace(entry.AddressHex) ? "" : $"0x{entry.AddressHex}");
        writer.WriteString("address_decimal", entry.AddressDecimal);
        writer.WriteStartArray("metadata");
        foreach (var field in entry.Metadata)
        {
            writer.WriteStartObject();
            writer.WriteString("key", field.Key);
            writer.WriteString("label", field.Label);
            writer.WriteString("value", field.Value);
            writer.WriteString("decimal", field.DecimalValue);
            writer.WriteBoolean("is_address", field.IsAddress);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
        writer.WriteString("mod_suggestion", BuildEntrySuggestion(entry));
        writer.WriteStartArray("suggestion_tags");
        foreach (var tag in GetSuggestionTags(entry))
        {
            writer.WriteStringValue(tag);
        }
        writer.WriteEndArray();
        writer.WriteString("raw", entry.Raw);
        writer.WriteEndObject();
    }

    private static string EscapeMarkdownCell(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("|", "\\|", StringComparison.Ordinal)
            .Replace("\r\n", "<br>", StringComparison.Ordinal)
            .Replace("\n", "<br>", StringComparison.Ordinal)
            .Replace("\r", "<br>", StringComparison.Ordinal);
    }

    private OperationResult SendHotkey(string name, byte virtualKey)
    {
        try
        {
            var status = GetStatus();
            var preflight = ValidateReadyForHotkey(status);
            if (preflight is not null)
            {
                return preflight;
            }

            var process = GetGameProcess();
            if (process is null)
            {
                return OperationResult.Fail("VEIN is not running. Click Launch VEIN and wait for the menu first.");
            }
            var handle = GetGameWindowHandle(process);

            if (handle == IntPtr.Zero)
            {
                return OperationResult.Fail("VEIN is running, but no game window is ready yet. Wait until the menu appears.");
            }

            ShowWindowAsync(handle, SwShow);
            Thread.Sleep(150);
            SetForegroundWindow(handle);
            Thread.Sleep(250);
            SendControlChord(virtualKey);

            return OperationResult.Ok($"Sent {name} to VEIN. Keep the game focused until UE4SS finishes, then check the live log or Open Dumps.");
        }
        catch (Exception ex)
        {
            return OperationResult.Fail($"Could not send {name} hotkey: {ex.Message}");
        }
    }

    private OperationResult SendHotkeySequence(string name, IReadOnlyList<(string Label, byte VirtualKey)> hotkeys)
    {
        try
        {
            var status = GetStatus();
            var preflight = ValidateReadyForHotkey(status);
            if (preflight is not null)
            {
                return preflight;
            }

            var process = GetGameProcess();
            if (process is null)
            {
                return OperationResult.Fail("VEIN is not running. Click Launch VEIN and wait for the menu first.");
            }
            var handle = GetGameWindowHandle(process);
            if (handle == IntPtr.Zero)
            {
                return OperationResult.Fail("VEIN is running, but no game window is ready yet. Wait until the menu appears.");
            }

            ShowWindowAsync(handle, SwShow);
            Thread.Sleep(150);
            SetForegroundWindow(handle);
            Thread.Sleep(350);

            foreach (var (_, virtualKey) in hotkeys)
            {
                SendControlChord(virtualKey);
                Thread.Sleep(650);
            }

            return OperationResult.Ok($"Sent {name} to VEIN. Keep the game focused until UE4SS finishes, then check the live log or Open Dumps.");
        }
        catch (Exception ex)
        {
            return OperationResult.Fail($"Could not send {name} hotkeys: {ex.Message}");
        }
    }

    private OperationResult? ValidateReadyForHotkey(Ue4ssStatus status)
    {
        var gameExe = Path.Combine(Paths.Win64Directory, "Vein-Win64-Test.exe");
        var proxyDll = Path.Combine(Paths.Win64Directory, "dwmapi.dll");
        var ue4ssDll = Path.Combine(Paths.Ue4ssDirectory, "UE4SS.dll");

        if (!status.GameExeFound)
        {
            return OperationResult.Fail($"VEIN executable was not found. Expected: {gameExe}");
        }

        if (!status.ProxyDllFound)
        {
            return OperationResult.Fail($"UE4SS proxy DLL is missing. Expected: {proxyDll}");
        }

        if (!status.Ue4ssDllFound)
        {
            return OperationResult.Fail($"UE4SS.dll is missing. Expected: {ue4ssDll}");
        }

        if (!status.KeybindsFound)
        {
            return OperationResult.Fail($"Dump keybind script is missing. Expected: {Paths.KeybindsScript}");
        }

        if (!status.GameRunning)
        {
            return OperationResult.Fail("VEIN is not running. Click Launch VEIN and wait until the main menu is loaded before dumping.");
        }

        return null;
    }

    private static void SendControlChord(byte virtualKey)
    {
        keybd_event(VkControl, 0, 0, 0);
        Thread.Sleep(80);
        keybd_event(virtualKey, 0, 0, 0);
        Thread.Sleep(80);
        keybd_event(virtualKey, 0, KeyUp, 0);
        Thread.Sleep(80);
        keybd_event(VkControl, 0, KeyUp, 0);
    }

    private static Process? GetGameProcess()
    {
        try
        {
            var processes = Process.GetProcessesByName(GameProcessName);
            if (processes.Length == 0)
            {
                return null;
            }

            var selected = processes.OrderByDescending(process => process.Id).First();
            foreach (var process in processes)
            {
                if (process.Id != selected.Id)
                {
                    process.Dispose();
                }
            }

            return selected;
        }
        catch
        {
            return null;
        }
    }

    private static IntPtr GetGameWindowHandle(Process process)
    {
        var windows = new List<(IntPtr Handle, string Title)>();
        EnumWindows((handle, _) =>
        {
            GetWindowThreadProcessId(handle, out var processId);
            if (processId != process.Id || !IsWindowVisible(handle))
            {
                return true;
            }

            var title = GetWindowTitle(handle);
            if (!string.IsNullOrWhiteSpace(title))
            {
                windows.Add((handle, title));
            }

            return true;
        }, IntPtr.Zero);

        var gameWindow = windows.FirstOrDefault(window =>
            window.Title.Contains("VEIN", StringComparison.OrdinalIgnoreCase) &&
            !window.Title.Contains("UE4SS", StringComparison.OrdinalIgnoreCase) &&
            !window.Title.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));
        if (gameWindow.Handle != IntPtr.Zero)
        {
            return gameWindow.Handle;
        }

        process.Refresh();
        return process.MainWindowHandle;
    }

    private static string GetWindowTitle(IntPtr handle)
    {
        var length = GetWindowTextLength(handle);
        if (length <= 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(length + 1);
        return GetWindowText(handle, builder, builder.Capacity) > 0
            ? builder.ToString()
            : string.Empty;
    }

    private static IntPtr FindWindowByTitle(string titlePart)
    {
        var match = IntPtr.Zero;
        EnumWindows((handle, _) =>
        {
            if (!IsWindowVisible(handle))
            {
                return true;
            }

            var title = GetWindowTitle(handle);
            if (title.Contains(titlePart, StringComparison.OrdinalIgnoreCase))
            {
                match = handle;
                return false;
            }

            return true;
        }, IntPtr.Zero);
        return match;
    }

    private void OnLogFileChanged(object sender, FileSystemEventArgs e)
    {
        LogFileChanged?.Invoke(this, EventArgs.Empty);
    }

    private static string Timestamp(string message) => $"{DateTime.Now:HH:mm:ss} | {message}";

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int processId);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out NativeRect rect);

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

    private const uint MouseLeftDown = 0x0002;
    private const uint MouseLeftUp = 0x0004;

    private sealed record FileSnapshot(bool Exists, long Length, DateTime LastWriteTimeUtc);

    private sealed record ModSuggestionRule(
        string Id,
        string Title,
        IReadOnlyList<string> Keywords,
        string Description,
        string UseCase,
        string RowSuggestion,
        IReadOnlyList<string> Ideas,
        IReadOnlyList<string> NextSteps);

    private sealed record ModSuggestionBucket(
        string Id,
        string Title,
        int Count,
        string Description,
        string UseCase,
        IReadOnlyList<string> Ideas,
        IReadOnlyList<string> NextSteps,
        IReadOnlyList<ParsedObjectDumpLine> Samples);

    private sealed class ModSuggestionCollector
    {
        private readonly Dictionary<string, ModSuggestionBucketBuilder> _buckets = new(StringComparer.OrdinalIgnoreCase);

        public IEnumerable<ModSuggestionBucket> Buckets => _buckets.Values
            .Select(bucket => bucket.ToBucket())
            .OrderByDescending(bucket => bucket.Count)
            .ThenBy(bucket => bucket.Title, StringComparer.OrdinalIgnoreCase);

        public void Add(ParsedObjectDumpLine entry)
        {
            foreach (var rule in GetSuggestionMatch(entry).Rules)
            {
                if (!_buckets.TryGetValue(rule.Id, out var bucket))
                {
                    bucket = new ModSuggestionBucketBuilder(rule);
                    _buckets.Add(rule.Id, bucket);
                }

                bucket.Add(entry);
            }
        }
    }

    private sealed class ModSuggestionBucketBuilder
    {
        private readonly ModSuggestionRule _rule;
        private readonly List<ParsedObjectDumpLine> _samples = [];

        public ModSuggestionBucketBuilder(ModSuggestionRule rule)
        {
            _rule = rule;
        }

        public int Count { get; private set; }

        public void Add(ParsedObjectDumpLine entry)
        {
            Count++;
            if (_samples.Count < 10)
            {
                _samples.Add(entry);
            }
        }

        public ModSuggestionBucket ToBucket()
        {
            return new ModSuggestionBucket(_rule.Id, _rule.Title, Count, _rule.Description, _rule.UseCase, _rule.Ideas, _rule.NextSteps, _samples);
        }
    }

    private sealed class DumpCategoryWriter : IDisposable
    {
        private readonly StreamWriter _plain;
        private readonly StreamWriter _markdown;
        private readonly FileStream _jsonStream;
        private readonly Utf8JsonWriter _json;
        private bool _finished;

        public DumpCategoryWriter(string title, string source, DateTime exportedAt, string plainPath, string markdownPath, string jsonPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(plainPath)!);
            Directory.CreateDirectory(Path.GetDirectoryName(markdownPath)!);
            Directory.CreateDirectory(Path.GetDirectoryName(jsonPath)!);

            _plain = new StreamWriter(plainPath, false, new UTF8Encoding(false));
            _markdown = new StreamWriter(markdownPath, false, new UTF8Encoding(false));
            _jsonStream = new FileStream(jsonPath, FileMode.Create, FileAccess.Write, FileShare.Read);
            _json = new Utf8JsonWriter(_jsonStream, new JsonWriterOptions { Indented = true });

            _plain.WriteLine($"VEIN {title}");
            _plain.WriteLine($"Source: {source}");
            _plain.WriteLine($"Exported: {exportedAt:yyyy-MM-dd HH:mm:ss}");
            _plain.WriteLine();

            _markdown.WriteLine($"# VEIN {title}");
            _markdown.WriteLine();
            _markdown.WriteLine($"- Source: `{source}`");
            _markdown.WriteLine($"- Exported: `{exportedAt:yyyy-MM-dd HH:mm:ss}`");
            _markdown.WriteLine();
            _markdown.WriteLine("| # | Type | Name | Hex address | Decimal address | Object path | Metadata | Mod suggestion |");
            _markdown.WriteLine("|---:|---|---|---|---:|---|---|---|");

            _json.WriteStartObject();
            _json.WriteString("category", title);
            _json.WriteString("source", source);
            _json.WriteString("exported_at", exportedAt.ToString("O", CultureInfo.InvariantCulture));
            _json.WriteStartArray("items");
        }

        public int Count { get; private set; }

        public void Write(ParsedObjectDumpLine entry)
        {
            Count++;
            WritePlainObject(_plain, entry, Count);
            WriteMarkdownObject(_markdown, entry, Count);
            WriteJsonObject(_json, entry, Count);
        }

        public void Finish()
        {
            if (_finished)
            {
                return;
            }

            _json.WriteEndArray();
            _json.WriteNumber("count", Count);
            _json.WriteEndObject();
            _json.Flush();
            _plain.Flush();
            _markdown.Flush();
            _finished = true;
        }

        public void Dispose()
        {
            Finish();
            _json.Dispose();
            _jsonStream.Dispose();
            _plain.Dispose();
            _markdown.Dispose();
        }
    }

    private sealed record ParsedObjectDumpLine(
        string Raw,
        string AddressHex,
        string AddressDecimal,
        string Type,
        string Path,
        string Name,
        IReadOnlyList<ObjectDumpMetadataField> Metadata);

    private sealed record ObjectDumpMetadataField(string Key, string Label, string Value, string DecimalValue, bool IsAddress);

    private sealed record CachedSuggestionMatch(
        IReadOnlyList<ModSuggestionRule> Rules,
        string Suggestion,
        string[] Tags);

    private sealed record FullGameDumpResult(int FileCount, long TotalBytes, int CopiedCount, long CopiedBytes);

    private sealed record GameFileEntry(string FullPath, string RelativePath, long Length, DateTime LastWriteTime);

    private sealed record GameFileExplanation(string Label, string Explanation, string ModUse, string SafeHandling);

    private sealed record FileTypeGuide(string Label, string Matches, string Explanation, string ModUse, string SafeHandling);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}

internal sealed record Ue4ssStatus(
    bool GameExeFound,
    bool ProxyDllFound,
    bool Ue4ssDllFound,
    bool ModsTxtFound,
    bool KeybindsFound,
    bool LogFound,
    bool LogLive,
    DateTime? LogLastWrite,
    bool GameRunning,
    int? GameProcessId);

internal sealed record OperationResult(bool Success, string Message)
{
    public static OperationResult Ok(string message) => new(true, message);

    public static OperationResult Fail(string message) => new(false, message);
}

internal sealed record DumpOutputFile(string FullPath, string RelativePath, long Length, DateTime LastWriteTime);

internal sealed record Ue4ssModEntry(string Name, bool Enabled, bool FolderFound, string Path);
