local MOD_NAME = "VeinManagerParityServer"

local function log(message)
    print(string.format("[%s] %s", MOD_NAME, message))
end

local function load_expected_mods()
    local ok, expected_mods = pcall(require, "expected_mods")
    if not ok then
        log("expected_mods.lua was not found or could not be loaded: " .. tostring(expected_mods))
        return nil
    end

    if type(expected_mods) ~= "table" then
        log("expected_mods.lua did not return a table.")
        return nil
    end

    return expected_mods
end

local function count_required_mods(expected_mods)
    local required_mods = expected_mods.required_mods
    if type(required_mods) ~= "table" then
        return 0
    end

    return #required_mods
end

local expected_mods = load_expected_mods()
if not expected_mods then
    return
end

log(
    string.format(
        "Loaded %d approved mods. Enforcement mode: %s. Extra mods allowed: %s.",
        count_required_mods(expected_mods),
        tostring(expected_mods.enforcement_mode or "Log Only"),
        tostring(expected_mods.allow_extra_mods == true)
    )
)

log("Runtime client handshake is scaffolded. Keep enforcement in Log Only until the in-game reporter path is verified.")
