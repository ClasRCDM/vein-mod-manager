local MOD_NAME = "VeinManagerParityClient"

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

local expected_mods = load_expected_mods()
if not expected_mods then
    return
end

log("Loaded approved mod manifest for client-side reporting.")
log("Runtime server report transport is scaffolded and must be wired to the verified VeinCF handshake path.")
