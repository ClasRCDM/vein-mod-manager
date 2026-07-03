-- VEIN_MasterDump_Throttled.lua
-- Windows-only Cheat Engine Lua workflow for offline/local VEIN testing.
-- Dumping is read-only. This script does not write target process memory.

local target_process_candidates = {
    "Vein-Win64-Test.exe",
    "Vein-Win64-DebugGame.exe"
}
local target_process = target_process_candidates[1]
local target_process_pid = nil

-- CPU safety knobs.
local throttleEvery = 100
local throttleMs = 50
local maxGamePathHits = 100000
local maxKeywordHits = 20000
local contextBytes = 512
local maxCandidates = 2000

local chunkSize = 0x40000
local maxEditTemplateEntries = 150
local checkpointEveryChunks = 25

local focused_keywords = {
    "UObject", "UClass", "UFunction", "UProperty", "FName", "FText", "FVector",
    "FRotator", "FTransform", "BlueprintGeneratedClass", "Default__", "K2_",
    "Receive", "OnRep", "Server_", "Client_", "Multicast_",
    "Dumpster", "BP_Dumpster", "BP_Dumpster01x", "BP_ConcreteDumpster",
    "Container", "Containers", "Industrial", "Commercial", "Trash",
    "BuildObjects", "Furniture", "Placeables",
    "Health", "MaxHealth", "HP", "Stamina", "MaxStamina", "Endurance",
    "Fatigue", "Hunger", "Thirst", "Hydration", "Calories", "Nutrition",
    "Weight", "Carry", "Capacity", "Encumbered", "Overweight",
    "Damage", "DamageTypes", "Bleeding", "Bleed", "Blood", "BloodLoss",
    "Wound", "Wounds", "Pain", "Injury", "Infection", "Sickness",
    "Poison", "Radiation", "Temperature", "Wetness", "Condition",
    "Conditions", "Stats", "Perks", "Strength", "Agility", "Vitality",
    "Immunity", "Medicine", "Healing", "Death", "Multiplier", "Speed",
    "Sprint", "Jump", "Loot", "Spawn", "Inventory",
    "CarryWeight", "MaxCarry", "InventoryWeight", "StorageWeight",
    "ContainerWeight", "Volume", "StackSize", "Durability", "Freshness",
    "Rotten", "Spoiled", "ZombieInfection", "Zombified", "Exerted",
    "OverThirsty", "OverHungry", "WellFed", "Wet", "ItemList",
    "ItemListCollection", "Spawnlist", "Spawnlists", "LootTable", "Search",
    "Dismantle", "Scrap", "Craft", "Recipe", "Workbench", "Schematic",
    "Actor", "RootComponent", "SceneComponent", "Transform", "Location",
    "RelativeLocation", "WorldLocation", "Rotation", "Yaw", "Pitch", "Roll",
    "Scale", "Bounds", "Collision", "Trace", "HitResult", "Target", "Move",
    "Place", "Placement", "Ghost", "ConstructionGhost", "Difficulty",
    "Respawn", "Decay", "PVP", "Raiding", "Permadeath", "Scarcity",
    "Hordes", "Zombies", "WalkSpeed", "RunSpeed", "CrouchWalkSpeed",
    "SwimSpeed", "TimeMultiplier", "NightTimeMultiplier", "Superadmin",
    "Debug", "Command", "Console", "Teleport", "ForceSave", "Give",
    "SetTime", "Weather", "Analyze", "Inspect", "LocationMarker", "LM_",
    "Cemetery", "Graveyard", "Grave", "Tomb", "Headstone", "MapIcon",
    "T_Icon", "T_Stat", "T_Conditions", "T_Building", "T_Warehouse",
    "T_Church"
}

local categories = {
    { label = "BP_ Blueprints", prefixes = { "BP_" } },
    { label = "BO_ Build Objects", prefixes = { "BO_" } },
    { label = "FR_ Furniture", prefixes = { "FR_" } },
    { label = "ST_ Stats", prefixes = { "ST_" } },
    { label = "STP_ Stat Perks", prefixes = { "STP_" } },
    { label = "GS_ Game Settings", prefixes = { "GS_" } },
    { label = "C_ Conditions", prefixes = { "C_" } },
    { label = "DT_ Damage Types", prefixes = { "DT_" } },
    { label = "DC_ Death Causes", prefixes = { "DC_" } },
    { label = "IL_ Item Lists", prefixes = { "IL_" } },
    { label = "ILC_ Item List Collections", prefixes = { "ILC_" } },
    { label = "IT_ Item Types", prefixes = { "IT_" } },
    { label = "RP_ Recipes", prefixes = { "RP_" } },
    { label = "SM_ Static Meshes", prefixes = { "SM_" } },
    { label = "SK_ Skeletal Meshes", prefixes = { "SK_" } },
    { label = "MI_ Materials", prefixes = { "MI_" } },
    { label = "T_ and TX_ Textures", prefixes = { "T_", "TX_" } },
    { label = "Other", prefixes = {} }
}

local log_lines = {}
local current_output_dir = nil
local write_checkpoint = nil
local last_checkpoint_chunk = 0
local stop_requested = false
local mirror_to_lua_engine = false
local assets_set = {}
local assets = {}
local keyword_hits = {}
local candidates = {}
local candidate_seen = {}
local exact_hex_entries = {}

local counters = {
    throttle = 0,
    regions = 0,
    chunks = 0,
    bytes = 0,
    path_hits = 0,
    keyword_hits = 0,
    candidate_reads = 0
}

local function now()
    return os.date("%Y-%m-%d %H:%M:%S")
end

local function log(message)
    local line = "[" .. now() .. "] " .. tostring(message)
    if mirror_to_lua_engine then
        print(line)
    end
    log_lines[#log_lines + 1] = line
    if current_output_dir then
        local handle = io.open(current_output_dir .. "\\VEIN_RunLog.txt", "ab")
        if handle then
            handle:write(line .. "\r\n")
            handle:close()
        end
        local json = tostring(message):gsub("\\", "\\\\"):gsub('"', '\\"')
        local json_handle = io.open(current_output_dir .. "\\VEIN_RunLog.jsonl", "ab")
        if json_handle then
            json_handle:write(string.format('{"time":"%s","event":"log","message":"%s","chunks":%d,"unique_paths":%d,"keyword_hits":%d,"candidates":%d}\r\n',
                now(), json, counters.chunks or 0, #assets, counters.keyword_hits or 0, #candidates))
            json_handle:close()
        end
    end
end

local control_dir = (os.getenv("USERPROFILE") or "C:\\Users\\Public") .. "\\Desktop\\VEIN_Dump_Control"
local pause_flag = control_dir .. "\\pause.flag"
local stop_flag = control_dir .. "\\stop.flag"

local function create_directory_native(path)
    if path == nil or path == "" then
        return false
    end

    for _, name in ipairs({ "forceDirectories", "createDirectory", "createDir" }) do
        local fn = _G[name]
        if type(fn) == "function" then
            local ok, result = pcall(fn, path)
            if ok and result ~= false then
                return true
            end
        end
    end

    return false
end

local function file_exists(path)
    local handle = io.open(path, "rb")
    if handle then
        handle:close()
        return true
    end
    return false
end

local function write_control_status(status)
    create_directory_native(control_dir)
    local handle = io.open(control_dir .. "\\status.txt", "wb")
    if handle then
        handle:write(tostring(status) .. " " .. now() .. "\r\n")
        handle:close()
    end
end

local function write_scan_status(label)
    write_control_status(string.format("%s chunks=%d regions=%d paths=%d keywords=%d candidates=%d",
        tostring(label or "running"),
        counters.chunks or 0,
        counters.regions or 0,
        #assets,
        counters.keyword_hits or 0,
        #candidates))
end

local function handle_control_flags()
    if file_exists(stop_flag) then
        stop_requested = true
        log("Stop requested by VEIN dump monitor.")
        write_control_status("stopped")
        if write_checkpoint then
            write_checkpoint("stopped")
        end
        return false
    end

    if file_exists(pause_flag) then
        log("Pause requested by VEIN dump monitor.")
        write_control_status("paused")
        if write_checkpoint then
            write_checkpoint("paused")
        end
        while file_exists(pause_flag) do
            if file_exists(stop_flag) then
                stop_requested = true
                log("Stop requested while paused.")
                write_control_status("stopped")
                if write_checkpoint then
                    write_checkpoint("stopped")
                end
                return false
            end
            sleep(500)
            processMessages()
        end
        log("Resume requested by VEIN dump monitor.")
        write_control_status("running")
    end

    return true
end

local function throttle(force)
    counters.throttle = counters.throttle + 1
    if force or (counters.throttle % throttleEvery == 0) then
        sleep(throttleMs)
        processMessages()
    end
end

local function try_open_process(candidate)
    local pid = nil
    pcall(function()
        pid = getProcessIDFromProcessName(candidate)
    end)

    if pid ~= nil and tonumber(pid) ~= nil and tonumber(pid) > 0 then
        log("Auto-attaching to " .. candidate .. " pid=" .. tostring(pid))
        local ok, err = pcall(openProcess, pid)
        return ok, err, pid
    end

    log("No PID found for " .. candidate .. "; trying name attach fallback.")
    local ok, err = pcall(openProcess, candidate)
    return ok, err, nil
end

local function escape_csv(value)
    local text = tostring(value or "")
    text = text:gsub('"', '""')
    return '"' .. text .. '"'
end

local function escape_lua(value)
    local text = tostring(value or "")
    text = text:gsub("\\", "\\\\"):gsub("\r", "\\r"):gsub("\n", "\\n"):gsub('"', '\\"')
    return '"' .. text .. '"'
end

local function file_write(path, text)
    local handle, err = io.open(path, "wb")
    if not handle then
        error("Unable to write " .. path .. ": " .. tostring(err))
    end
    handle:write(text)
    handle:close()
end

local function mkdir(path)
    if create_directory_native(path) then
        return
    end

    local probe = io.open(path .. "\\.__vein_write_probe", "wb")
    if probe then
        probe:close()
        os.remove(path .. "\\.__vein_write_probe")
        return
    end

    error("Directory does not exist and no native directory creator is available: " .. tostring(path))
end

local function basename(path)
    return tostring(path or ""):match("([^/]+)$") or tostring(path or "")
end

local function starts_with(text, prefix)
    return text:sub(1, #prefix) == prefix
end

local function category_for_path(path)
    local name = basename(path)
    for _, category in ipairs(categories) do
        if category.label ~= "Other" then
            for _, prefix in ipairs(category.prefixes) do
                if starts_with(name, prefix) then
                    return category.label
                end
            end
        end
    end
    return "Other"
end

local function add_asset(path, address, encoding)
    if not path or path == "" then
        return
    end
    if not starts_with(path, "/Game/") then
        return
    end
    if not assets_set[path] then
        assets_set[path] = { first_address = address, encodings = {} }
        assets[#assets + 1] = path
    end
    assets_set[path].encodings[encoding or "unknown"] = true
    counters.path_hits = counters.path_hits + 1
end

local function is_path_char(byte)
    return (byte >= 48 and byte <= 57) or
        (byte >= 65 and byte <= 90) or
        (byte >= 97 and byte <= 122) or
        byte == 95 or byte == 47 or byte == 45 or byte == 46
end

local function is_game_at(bytes, index)
    return bytes[index] == 47 and bytes[index + 1] == 71 and bytes[index + 2] == 97 and
        bytes[index + 3] == 109 and bytes[index + 4] == 101 and bytes[index + 5] == 47
end

local function is_game_utf16_at(bytes, index)
    return bytes[index] == 47 and bytes[index + 1] == 0 and
        bytes[index + 2] == 71 and bytes[index + 3] == 0 and
        bytes[index + 4] == 97 and bytes[index + 5] == 0 and
        bytes[index + 6] == 109 and bytes[index + 7] == 0 and
        bytes[index + 8] == 101 and bytes[index + 9] == 0 and
        bytes[index + 10] == 47 and bytes[index + 11] == 0
end

local function parse_ascii_path(bytes, index)
    local out = {}
    local i = index
    while i <= #bytes and is_path_char(bytes[i]) and #out < 512 do
        out[#out + 1] = string.char(bytes[i])
        i = i + 1
    end
    return table.concat(out), i
end

local function parse_utf16_path(bytes, index)
    local out = {}
    local i = index
    while i + 1 <= #bytes and bytes[i + 1] == 0 and is_path_char(bytes[i]) and #out < 512 do
        out[#out + 1] = string.char(bytes[i])
        i = i + 2
    end
    return table.concat(out), i
end

local function bytes_match_ascii(bytes, index, keyword)
    if index + #keyword - 1 > #bytes then
        return false
    end
    for i = 1, #keyword do
        if bytes[index + i - 1] ~= keyword:byte(i) then
            return false
        end
    end
    return true
end

local function bytes_match_utf16(bytes, index, keyword)
    if index + (#keyword * 2) - 1 > #bytes then
        return false
    end
    for i = 1, #keyword do
        local offset = index + ((i - 1) * 2)
        if bytes[offset] ~= keyword:byte(i) or bytes[offset + 1] ~= 0 then
            return false
        end
    end
    return true
end

local function read_bytes_safe(address, count)
    local ok, result = pcall(readBytes, address, count, true)
    if ok and type(result) == "table" and #result > 0 then
        return result
    end
    return nil
end

local function read_value_safe(kind, address)
    local ok, value
    if kind == "Float" then
        ok, value = pcall(readFloat, address)
    elseif kind == "Double" then
        ok, value = pcall(readDouble, address)
    else
        ok, value = pcall(readInteger, address)
    end
    if ok and value ~= nil then
        return value
    end
    return nil
end

local function keyword_confidence(keyword, kind, value)
    local lower = keyword:lower()
    local important_0_100 = {
        health = true, stamina = true, hunger = true, thirst = true,
        pain = true, bleeding = true, bleed = true, condition = true
    }
    local medium_big = {
        weight = true, carry = true, capacity = true, damage = true,
        speed = true, jump = true
    }

    local numeric = tonumber(value)
    if not numeric then
        return "Low"
    end

    for token in pairs(important_0_100) do
        if lower:find(token, 1, true) and numeric >= 0 and numeric <= 100 then
            return "High"
        end
    end

    if numeric >= 0 and numeric <= 250 then
        return "Medium"
    end

    for token in pairs(medium_big) do
        if lower:find(token, 1, true) and numeric >= 0 and numeric <= 10000 then
            return "Medium"
        end
    end

    return "Low"
end

local function plausible_value(kind, value)
    local numeric = tonumber(value)
    if not numeric then
        return false
    end
    if numeric ~= numeric then
        return false
    end
    if kind == "4 Bytes" then
        return numeric >= 0 and numeric <= 1000000
    end
    return numeric >= -100000 and numeric <= 100000
end

local function nearby_path_from_context(bytes)
    for i = 1, math.max(1, #bytes - 12) do
        if is_game_at(bytes, i) then
            local path = parse_ascii_path(bytes, i)
            if path and path ~= "" then
                return path
            end
        elseif is_game_utf16_at(bytes, i) then
            local path = parse_utf16_path(bytes, i)
            if path and path ~= "" then
                return path
            end
        end
    end
    return ""
end

local function add_candidate(keyword, kind, address, value, nearby_path, notes)
    if #candidates >= maxCandidates then
        return
    end
    if not plausible_value(kind, value) then
        return
    end

    local key = string.format("%s|%s|%X", keyword, kind, address)
    if candidate_seen[key] then
        return
    end
    candidate_seen[key] = true

    local confidence = keyword_confidence(keyword, kind, value)
    candidates[#candidates + 1] = {
        index = #candidates + 1,
        keyword = keyword,
        confidence = confidence,
        kind = kind,
        address = string.format("0x%X", address),
        value = tostring(value),
        nearby_path = nearby_path or "",
        notes = notes or "Candidate only; confirm by changing the stat in-game before editing."
    }
end

local function collect_candidates_near(keyword, hit_address)
    if #candidates >= maxCandidates then
        return
    end

    local start_address = math.max(0, hit_address - contextBytes)
    local bytes = read_bytes_safe(start_address, contextBytes * 2)
    local nearby_path = bytes and nearby_path_from_context(bytes) or ""

    for offset = -contextBytes, contextBytes, 4 do
        if #candidates >= maxCandidates then
            return
        end

        local address = hit_address + offset
        if address > 0 then
            local float_value = read_value_safe("Float", address)
            counters.candidate_reads = counters.candidate_reads + 1
            if float_value ~= nil then
                add_candidate(keyword, "Float", address, float_value, nearby_path, "Float near keyword hit; confirm in-game before editing.")
            end

            local int_value = read_value_safe("4 Bytes", address)
            counters.candidate_reads = counters.candidate_reads + 1
            if int_value ~= nil then
                add_candidate(keyword, "4 Bytes", address, int_value, nearby_path, "4-byte integer near keyword hit; confirm in-game before editing.")
            end

            if offset % 8 == 0 then
                local double_value = read_value_safe("Double", address)
                counters.candidate_reads = counters.candidate_reads + 1
                if double_value ~= nil then
                    add_candidate(keyword, "Double", address, double_value, nearby_path, "Double near keyword hit; confirm in-game before editing.")
                end
            end
        end
        throttle()
    end
end

local function add_keyword_hit(keyword, address, encoding)
    if counters.keyword_hits >= maxKeywordHits then
        return
    end

    counters.keyword_hits = counters.keyword_hits + 1
    keyword_hits[#keyword_hits + 1] = {
        keyword = keyword,
        address = string.format("0x%X", address),
        encoding = encoding
    }

    collect_candidates_near(keyword, address)
end

local keyword_ascii_by_first = {}
local keyword_utf16_by_first = {}
for _, keyword in ipairs(focused_keywords) do
    local first = keyword:byte(1)
    keyword_ascii_by_first[first] = keyword_ascii_by_first[first] or {}
    keyword_utf16_by_first[first] = keyword_utf16_by_first[first] or {}
    keyword_ascii_by_first[first][#keyword_ascii_by_first[first] + 1] = keyword
    keyword_utf16_by_first[first][#keyword_utf16_by_first[first] + 1] = keyword
end

local function scan_chunk(base_address, bytes)
    local i = 1
    while i <= #bytes do
        if counters.keyword_hits < maxKeywordHits then
            local ascii_candidates = keyword_ascii_by_first[bytes[i]]
            if ascii_candidates then
                for _, keyword in ipairs(ascii_candidates) do
                    if bytes_match_ascii(bytes, i, keyword) then
                        add_keyword_hit(keyword, base_address + i - 1, "ASCII")
                        break
                    end
                end
            end

            if bytes[i + 1] == 0 then
                local utf16_candidates = keyword_utf16_by_first[bytes[i]]
                if utf16_candidates then
                    for _, keyword in ipairs(utf16_candidates) do
                        if bytes_match_utf16(bytes, i, keyword) then
                            add_keyword_hit(keyword, base_address + i - 1, "UTF-16")
                            break
                        end
                    end
                end
            end
        end

        if counters.path_hits < maxGamePathHits and i + 5 <= #bytes and is_game_at(bytes, i) then
            local path = parse_ascii_path(bytes, i)
            add_asset(path, base_address + i - 1, "ASCII")
        elseif counters.path_hits < maxGamePathHits and i + 11 <= #bytes and is_game_utf16_at(bytes, i) then
            local path = parse_utf16_path(bytes, i)
            add_asset(path, base_address + i - 1, "UTF-16")
        end

        i = i + 1
        if i % 4096 == 0 then
            throttle()
        end
    end
end

local function region_field(region, names, default)
    for _, name in ipairs(names) do
        if region[name] ~= nil then
            return region[name]
        end
    end
    return default
end

local function enumerate_regions()
    if type(enumMemoryRegions) == "function" then
        local ok, regions = pcall(enumMemoryRegions)
        if ok and type(regions) == "table" then
            return regions
        end
    end

    return {
        { BaseAddress = 0x00010000, RegionSize = 0x7fff0000 }
    }
end

local function has_flag(value, flag)
    return math.floor(value / flag) % 2 == 1
end

local function is_readable_region(region)
    local state = tonumber(region_field(region, { "State", "state" }, nil))
    if state and state ~= 0x1000 then
        return false
    end

    local protect = tonumber(region_field(region, { "Protect", "protect" }, nil))
    if protect then
        if has_flag(protect, 0x01) or has_flag(protect, 0x100) then
            return false
        end
    end

    return true
end

local function scan_regions()
    local regions = enumerate_regions()
    log("Memory regions available: " .. tostring(#regions))

    for _, region in ipairs(regions) do
        local base = tonumber(region_field(region, { "BaseAddress", "base", "Base" }, 0)) or 0
        local size = tonumber(region_field(region, { "RegionSize", "size", "Size" }, 0)) or 0
        if base > 0 and size > 0 and is_readable_region(region) then
            counters.regions = counters.regions + 1
            local offset = 0
            while offset < size do
                if not handle_control_flags() then
                    return
                end
                if counters.path_hits >= maxGamePathHits and counters.keyword_hits >= maxKeywordHits then
                    return
                end

                local read_size = math.min(chunkSize, size - offset)
                local address = base + offset
                local bytes = read_bytes_safe(address, read_size)
                if bytes then
                    counters.chunks = counters.chunks + 1
                    counters.bytes = counters.bytes + #bytes
                    scan_chunk(address, bytes)
                    if counters.chunks % 20 == 0 then
                        log(string.format("Progress: chunks=%d unique_paths=%d keyword_hits=%d candidates=%d",
                            counters.chunks, #assets, counters.keyword_hits, #candidates))
                        write_scan_status("running")
                    end
                    if write_checkpoint and counters.chunks - last_checkpoint_chunk >= checkpointEveryChunks then
                        write_checkpoint("checkpoint")
                        write_scan_status("checkpoint")
                        last_checkpoint_chunk = counters.chunks
                    end
                end

                offset = offset + read_size
                throttle(true)
            end
        end
    end
end

local function text_to_ascii_hex(text)
    local out = {}
    for i = 1, #text do
        out[#out + 1] = string.format("%02X", text:byte(i))
    end
    return table.concat(out, " ")
end

local function text_to_utf16_hex(text)
    local out = {}
    for i = 1, #text do
        out[#out + 1] = string.format("%02X 00", text:byte(i))
    end
    return table.concat(out, " ")
end

local function path_has_keyword(path)
    local lower = path:lower()
    for _, keyword in ipairs(focused_keywords) do
        if lower:find(keyword:lower(), 1, true) then
            return true
        end
    end
    return false
end

local function build_exact_hex()
    local important = {
        "/Game/Vein/Containers/Commercial/Trash/BP_Dumpster01x",
        "/Game/Vein/Containers/Industrial/BP_ConcreteDumpster"
    }

    local included = {}
    for _, path in ipairs(important) do
        if assets_set[path] then
            exact_hex_entries[#exact_hex_entries + 1] = path
            included[path] = true
        else
            exact_hex_entries[#exact_hex_entries + 1] = path .. " (not found in loaded memory)"
            included[path] = true
        end
    end

    for _, path in ipairs(assets) do
        local name = basename(path)
        local prefix_match = starts_with(name, "BP_") or starts_with(name, "BO_") or
            starts_with(name, "ST_") or starts_with(name, "C_") or starts_with(name, "DT_") or
            starts_with(name, "GS_") or starts_with(name, "ILC_")
        if prefix_match and path_has_keyword(path) and not included[path] then
            exact_hex_entries[#exact_hex_entries + 1] = path
            included[path] = true
        end
    end
end

local function confidence_rank(confidence)
    if confidence == "High" then
        return 1
    end
    if confidence == "Medium" then
        return 2
    end
    return 3
end

local function sort_outputs()
    table.sort(assets)
    table.sort(candidates, function(a, b)
        local ar = confidence_rank(a.confidence)
        local br = confidence_rank(b.confidence)
        if ar ~= br then
            return ar < br
        end
        return a.keyword < b.keyword
    end)
    for i, candidate in ipairs(candidates) do
        candidate.index = i
    end
end

local function render_assets_all()
    local lines = {}
    for _, path in ipairs(assets) do
        lines[#lines + 1] = path
    end
    return table.concat(lines, "\r\n") .. "\r\n"
end

local function render_assets_by_category()
    local buckets = {}
    for _, category in ipairs(categories) do
        buckets[category.label] = {}
    end
    for _, path in ipairs(assets) do
        local label = category_for_path(path)
        buckets[label][#buckets[label] + 1] = path
    end

    local lines = { "# VEIN Assets By Category", "" }
    for _, category in ipairs(categories) do
        lines[#lines + 1] = "## " .. category.label
        lines[#lines + 1] = ""
        if #buckets[category.label] == 0 then
            lines[#lines + 1] = "_No loaded paths found._"
        else
            for _, path in ipairs(buckets[category.label]) do
                lines[#lines + 1] = "- `" .. path .. "`"
            end
        end
        lines[#lines + 1] = ""
    end
    return table.concat(lines, "\r\n") .. "\r\n"
end

local function render_keyword_dumps()
    local lines = {
        "# VEIN Keyword Dumps",
        "",
        "These are focused keyword hits found in currently loaded process memory.",
        ""
    }
    for _, hit in ipairs(keyword_hits) do
        lines[#lines + 1] = string.format("- `%s` at `%s` (%s)", hit.keyword, hit.address, hit.encoding)
    end
    if #keyword_hits == 0 then
        lines[#lines + 1] = "_No keyword hits found._"
    end
    lines[#lines + 1] = ""
    return table.concat(lines, "\r\n") .. "\r\n"
end

local function render_exact_hex()
    local lines = {
        "# VEIN Exact Hex",
        "",
        "ASCII and UTF-16 AOB hex for important exact asset paths found in loaded memory.",
        ""
    }
    for _, path in ipairs(exact_hex_entries) do
        if path:find("%(not found in loaded memory%)") then
            lines[#lines + 1] = "## " .. path
            lines[#lines + 1] = ""
            lines[#lines + 1] = "_Not present in the loaded memory scan._"
            lines[#lines + 1] = ""
        else
            lines[#lines + 1] = "## " .. path
            lines[#lines + 1] = ""
            lines[#lines + 1] = "**ASCII AOB**"
            lines[#lines + 1] = ""
            lines[#lines + 1] = "```text"
            lines[#lines + 1] = text_to_ascii_hex(path)
            lines[#lines + 1] = "```"
            lines[#lines + 1] = ""
            lines[#lines + 1] = "**UTF-16 AOB**"
            lines[#lines + 1] = ""
            lines[#lines + 1] = "```text"
            lines[#lines + 1] = text_to_utf16_hex(path)
            lines[#lines + 1] = "```"
            lines[#lines + 1] = ""
        end
    end
    return table.concat(lines, "\r\n")
end

local function render_candidates_csv()
    local lines = { "index,keyword,confidence,value type,address,current value,nearby /Game path,notes" }
    for _, candidate in ipairs(candidates) do
        lines[#lines + 1] = table.concat({
            candidate.index,
            escape_csv(candidate.keyword),
            escape_csv(candidate.confidence),
            escape_csv(candidate.kind),
            escape_csv(candidate.address),
            escape_csv(candidate.value),
            escape_csv(candidate.nearby_path),
            escape_csv(candidate.notes)
        }, ",")
    end
    return table.concat(lines, "\r\n") .. "\r\n"
end

local function render_edit_template()
    local lines = {
        "-- VEIN_EditTemplate.lua",
        "-- Generated from VEIN_MasterDump_Throttled.lua.",
        "-- Safe default: writes are disabled. Confirm one value in-game before editing it.",
        "",
        "local target_process = " .. escape_lua(target_process),
        "local ENABLE_WRITES = false",
        "",
        "local entries = {"
    }

    local count = math.min(#candidates, maxEditTemplateEntries)
    for i = 1, count do
        local candidate = candidates[i]
        lines[#lines + 1] = "    {"
        lines[#lines + 1] = "        name = " .. escape_lua("Candidate " .. candidate.index .. " " .. candidate.keyword) .. ","
        lines[#lines + 1] = "        keyword = " .. escape_lua(candidate.keyword) .. ","
        lines[#lines + 1] = "        confidence = " .. escape_lua(candidate.confidence) .. ","
        lines[#lines + 1] = "        type = " .. escape_lua(candidate.kind) .. ","
        lines[#lines + 1] = "        address = " .. escape_lua(candidate.address) .. ","
        lines[#lines + 1] = "        original_value = " .. escape_lua(candidate.value) .. ","
        lines[#lines + 1] = "        new_value = " .. escape_lua(candidate.value) .. ","
        lines[#lines + 1] = "        enabled = false,"
        lines[#lines + 1] = "        nearby_path = " .. escape_lua(candidate.nearby_path) .. ","
        lines[#lines + 1] = "    },"
    end

    lines[#lines + 1] = "}"
    lines[#lines + 1] = ""
    lines[#lines + 1] = "local function read_current(entry)"
    lines[#lines + 1] = "    local address = getAddress(entry.address)"
    lines[#lines + 1] = "    if entry.type == \"Float\" then return readFloat(address) end"
    lines[#lines + 1] = "    if entry.type == \"Double\" then return readDouble(address) end"
    lines[#lines + 1] = "    if entry.type == \"4 Bytes\" then return readInteger(address) end"
    lines[#lines + 1] = "    error(\"Unsupported value type: \" .. tostring(entry.type))"
    lines[#lines + 1] = "end"
    lines[#lines + 1] = ""
    lines[#lines + 1] = "local function write_current(entry)"
    lines[#lines + 1] = "    local address = getAddress(entry.address)"
    lines[#lines + 1] = "    local value = tonumber(entry.new_value)"
    lines[#lines + 1] = "    if not value then error(\"new_value must be numeric for \" .. entry.name) end"
    lines[#lines + 1] = "    if entry.type == \"Float\" then writeFloat(address, value); return end"
    lines[#lines + 1] = "    if entry.type == \"Double\" then writeDouble(address, value); return end"
    lines[#lines + 1] = "    if entry.type == \"4 Bytes\" then writeInteger(address, math.floor(value)); return end"
    lines[#lines + 1] = "    error(\"Unsupported value type: \" .. tostring(entry.type))"
    lines[#lines + 1] = "end"
    lines[#lines + 1] = ""
    lines[#lines + 1] = "-- To test one value: set ENABLE_WRITES = true, set exactly one entry.enabled = true,"
    lines[#lines + 1] = "-- edit that entry.new_value, run this script, then immediately verify in-game."
    lines[#lines + 1] = "-- Never mass-enable entries."
    lines[#lines + 1] = "openProcess(target_process)"
    lines[#lines + 1] = "for _, entry in ipairs(entries) do"
    lines[#lines + 1] = "    local ok, current = pcall(read_current, entry)"
    lines[#lines + 1] = "    if ok then"
    lines[#lines + 1] = "        print(entry.name .. \" | \" .. entry.type .. \" | \" .. entry.address .. \" | current=\" .. tostring(current) .. \" | original=\" .. tostring(entry.original_value))"
    lines[#lines + 1] = "    else"
    lines[#lines + 1] = "        print(entry.name .. \" | read failed: \" .. tostring(current))"
    lines[#lines + 1] = "    end"
    lines[#lines + 1] = ""
    lines[#lines + 1] = "    if ENABLE_WRITES == true and entry.enabled == true then"
    lines[#lines + 1] = "        print(\"Writing one enabled entry: \" .. entry.name .. \" -> \" .. tostring(entry.new_value))"
    lines[#lines + 1] = "        write_current(entry)"
    lines[#lines + 1] = "    end"
    lines[#lines + 1] = "end"
    lines[#lines + 1] = ""
    lines[#lines + 1] = "if ENABLE_WRITES ~= true then"
    lines[#lines + 1] = "    print(\"ENABLE_WRITES is false. No memory writes were performed.\")"
    lines[#lines + 1] = "end"
    lines[#lines + 1] = ""

    return table.concat(lines, "\r\n")
end

local function render_master_report()
    local buckets = {}
    for _, category in ipairs(categories) do
        buckets[category.label] = {}
    end
    for _, path in ipairs(assets) do
        local label = category_for_path(path)
        buckets[label][#buckets[label] + 1] = path
    end

    local lines = {
        "# VEIN Master Dump",
        "",
        "- Generation time: " .. now(),
        "- Target process: `" .. target_process .. "`",
        "- Warning: this is loaded process memory only, not full pak extraction.",
        "- Total unique `/Game` paths: " .. tostring(#assets),
        "- Total candidate editable values: " .. tostring(#candidates),
        "",
        "## High and Medium Candidate Editable Values",
        "",
        "Candidates are not assumed real. Confirm a value by changing the stat in-game before editing anything.",
        ""
    }

    local any_priority = false
    for _, candidate in ipairs(candidates) do
        if candidate.confidence == "High" or candidate.confidence == "Medium" then
            any_priority = true
            lines[#lines + 1] = string.format("- `%s` `%s` `%s` `%s` current `%s` near `%s`",
                candidate.confidence, candidate.keyword, candidate.kind, candidate.address,
                candidate.value, candidate.nearby_path)
        end
    end
    if not any_priority then
        lines[#lines + 1] = "_No high or medium candidates found._"
    end

    lines[#lines + 1] = ""
    lines[#lines + 1] = "## Categories"
    lines[#lines + 1] = ""

    for _, category in ipairs(categories) do
        lines[#lines + 1] = "### " .. category.label
        lines[#lines + 1] = ""
        if #buckets[category.label] == 0 then
            lines[#lines + 1] = "_No loaded paths found._"
        else
            for _, path in ipairs(buckets[category.label]) do
                lines[#lines + 1] = "- `" .. path .. "`"
            end
        end
        lines[#lines + 1] = ""
    end

    return table.concat(lines, "\r\n")
end

local function render_run_log()
    local lines = {
        "VEIN Master Dump Run Log",
        "Target process: " .. target_process,
        "Generated: " .. now(),
        "",
        "Settings:",
        "  throttleEvery=" .. tostring(throttleEvery),
        "  throttleMs=" .. tostring(throttleMs),
        "  maxGamePathHits=" .. tostring(maxGamePathHits),
        "  maxKeywordHits=" .. tostring(maxKeywordHits),
        "  contextBytes=" .. tostring(contextBytes),
        "  maxCandidates=" .. tostring(maxCandidates),
        "",
        "Counters:",
        "  regions=" .. tostring(counters.regions),
        "  chunks=" .. tostring(counters.chunks),
        "  bytes=" .. tostring(counters.bytes),
        "  raw path hits=" .. tostring(counters.path_hits),
        "  unique paths=" .. tostring(#assets),
        "  keyword hits=" .. tostring(counters.keyword_hits),
        "  candidate reads=" .. tostring(counters.candidate_reads),
        "  candidates=" .. tostring(#candidates),
        "",
        "Messages:"
    }
    for _, line in ipairs(log_lines) do
        lines[#lines + 1] = line
    end
    return table.concat(lines, "\r\n") .. "\r\n"
end

write_checkpoint = function(label)
    if not current_output_dir then
        return
    end

    sort_outputs()
    build_exact_hex()
    file_write(current_output_dir .. "\\VEIN_MasterDump.md", render_master_report() .. "\r\n\r\n_Status: scan still running; this is a checkpoint._\r\n")
    file_write(current_output_dir .. "\\VEIN_Assets_All.txt", render_assets_all())
    file_write(current_output_dir .. "\\VEIN_Assets_ByCategory.md", render_assets_by_category() .. "\r\n_Status: scan still running; this is a checkpoint._\r\n")
    file_write(current_output_dir .. "\\VEIN_KeywordDumps.md", render_keyword_dumps() .. "\r\n_Status: scan still running; this is a checkpoint._\r\n")
    file_write(current_output_dir .. "\\VEIN_ExactHex.md", render_exact_hex() .. "\r\n_Status: scan still running; this is a checkpoint._\r\n")
    file_write(current_output_dir .. "\\VEIN_Candidates.csv", render_candidates_csv())
    file_write(current_output_dir .. "\\VEIN_EditTemplate.lua", render_edit_template())
    file_write(current_output_dir .. "\\VEIN_Checkpoint_Status.txt",
        "Status: " .. tostring(label or "checkpoint") .. "\r\n" ..
        "Time: " .. now() .. "\r\n" ..
        "Chunks scanned: " .. tostring(counters.chunks) .. "\r\n" ..
        "Unique /Game paths: " .. tostring(#assets) .. "\r\n" ..
        "Keyword hits: " .. tostring(counters.keyword_hits) .. "\r\n" ..
        "Candidates: " .. tostring(#candidates) .. "\r\n"
    )
    local perf_exists = io.open(current_output_dir .. "\\VEIN_PerformanceLog.csv", "rb")
    if not perf_exists then
        file_write(current_output_dir .. "\\VEIN_PerformanceLog.csv", "time,label,chunks,regions,bytes,unique_paths,keyword_hits,candidates,candidate_reads\r\n")
    else
        perf_exists:close()
    end
    local perf_handle = io.open(current_output_dir .. "\\VEIN_PerformanceLog.csv", "ab")
    if perf_handle then
        perf_handle:write(string.format("%s,%s,%d,%d,%d,%d,%d,%d,%d\r\n",
            now(), tostring(label or "checkpoint"), counters.chunks, counters.regions, counters.bytes,
            #assets, counters.keyword_hits, #candidates, counters.candidate_reads))
        perf_handle:close()
    end
    log("Checkpoint wrote current reports.")
end

local function main()
    local desktop = os.getenv("USERPROFILE") .. "\\Desktop"
    local output_dir = os.getenv("VEIN_CE_OUTPUT_DIR") or (desktop .. "\\VEIN_MasterDump_" .. os.date("%Y%m%d_%H%M%S"))
    mkdir(output_dir)
    current_output_dir = output_dir
    log("Output folder: " .. output_dir)
    file_write(output_dir .. "\\VEIN_MasterDump.md", "# VEIN Master Dump\r\n\r\nScan running. See `VEIN_RunLog.txt` for live progress.\r\n")
    file_write(output_dir .. "\\VEIN_Assets_All.txt", "Scan running...\r\n")
    file_write(output_dir .. "\\VEIN_Assets_ByCategory.md", "# VEIN Assets By Category\r\n\r\nScan running...\r\n")
    file_write(output_dir .. "\\VEIN_KeywordDumps.md", "# VEIN Keyword Dumps\r\n\r\nScan running...\r\n")
    file_write(output_dir .. "\\VEIN_ExactHex.md", "# VEIN Exact Hex\r\n\r\nScan running...\r\n")
    file_write(output_dir .. "\\VEIN_Candidates.csv", "index,keyword,confidence,value type,address,current value,nearby /Game path,notes\r\n")
    file_write(output_dir .. "\\VEIN_EditTemplate.lua", "-- Scan running. This file will be replaced when the dump completes.\r\n")
    file_write(output_dir .. "\\VEIN_Checkpoint_Status.txt", "Status: starting\r\n")
    file_write(output_dir .. "\\VEIN_RunLog.jsonl", "")
    file_write(output_dir .. "\\VEIN_PerformanceLog.csv", "time,label,chunks,regions,bytes,unique_paths,keyword_hits,candidates,candidate_reads\r\n")
    write_control_status("attaching")

    local opened = false
    local last_error = nil
    for _, candidate in ipairs(target_process_candidates) do
        log("Opening process " .. candidate)
        local ok, err, pid = try_open_process(candidate)
        if ok then
            target_process = candidate
            target_process_pid = pid
            log("Attached to " .. target_process .. (target_process_pid and (" pid=" .. tostring(target_process_pid)) or " by process name"))
            opened = true
            break
        end

        last_error = err
    end

    if not opened then
        write_control_status("attach-failed")
        error("Unable to open VEIN process. Last error: " .. tostring(last_error))
    end

    write_scan_status("running")

    scan_regions()
    if stop_requested then
        sort_outputs()
        build_exact_hex()
        write_checkpoint("stopped")
        file_write(output_dir .. "\\VEIN_RunLog.txt", render_run_log())
        log("Stopped. Open " .. output_dir .. "\\VEIN_MasterDump.md")
        return
    end
    sort_outputs()
    build_exact_hex()
    write_checkpoint("finalizing")

    file_write(output_dir .. "\\VEIN_MasterDump.md", render_master_report())
    file_write(output_dir .. "\\VEIN_Assets_All.txt", render_assets_all())
    file_write(output_dir .. "\\VEIN_Assets_ByCategory.md", render_assets_by_category())
    file_write(output_dir .. "\\VEIN_KeywordDumps.md", render_keyword_dumps())
    file_write(output_dir .. "\\VEIN_ExactHex.md", render_exact_hex())
    file_write(output_dir .. "\\VEIN_Candidates.csv", render_candidates_csv())
    file_write(output_dir .. "\\VEIN_EditTemplate.lua", render_edit_template())
    file_write(output_dir .. "\\VEIN_RunLog.txt", render_run_log())

    log("Done. Open " .. output_dir .. "\\VEIN_MasterDump.md")
end

local ok, err = xpcall(main, debug.traceback)
if not ok then
    log("ERROR: " .. tostring(err))
    error(err)
end
