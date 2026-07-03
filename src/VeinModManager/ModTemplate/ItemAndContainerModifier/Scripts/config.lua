-- config.lua
local Config = {
    EnableLogging = true,
    SweepOnStartMs = 5000,

    -- Vehicle scan timing, in wall-clock milliseconds.
    ScanIntervalMs = 3000,
    ScanStopAfterMs = 90000,

    -- Component member names to try on a vehicle actor.
    VehicleInvMembers = {
        "Trunk",
        "Inventory",
        "Storage",
        "Cargo",
        "Bed",
        "Container",
    },

    EnabledCategories = {
        vehicles = true,
        containers = true,
        backpacks = true,
        crafting = false,
        consumables = false,
        ammo = false,
        skillmags = false,
        tools = false,
        junk = false,
        clothing = false,
        medical = false,
        schematics = false,
        farming = false,
        weapons = false,
    },

    CategoryDefaults = {
        vehicles = nil,
        containers = nil,

        backpacks = {
            Weight = nil,
            MaxStack = nil,
            bStackable = nil,
            ExtraWeightCapacity = nil,
            RunSpeedMultiplier = nil,
        },
        crafting = {
            Weight = nil,
            MaxStack = nil,
            bStackable = nil,
        },
        consumables = {
            Weight = nil,
            MaxStack = nil,
            bStackable = nil,
        },
        ammo = {
            Weight = nil,
            MaxStack = nil,
            bStackable = nil,
        },
        skillmags = {
            Weight = nil,
            MaxStack = nil,
            bStackable = nil,
        },
        tools = {
            Weight = nil,
            MaxStack = nil,
            bStackable = nil,
        },
        junk = {
            Weight = nil,
            MaxStack = nil,
            bStackable = nil,
        },
        clothing = {
            Weight = nil,
            MaxStack = nil,
            bStackable = nil,
        },
        medical = {
            Weight = nil,
            MaxStack = nil,
            bStackable = nil,
        },
        schematics = {
            Weight = nil,
            MaxStack = nil,
            bStackable = nil,
        },
        farming = {
            Weight = nil,
            MaxStack = nil,
            bStackable = nil,
        },
        weapons = {
            Weight = nil,
            MaxStack = nil,
            bStackable = nil,
        },
    },
}

-- UI_CONFIG_SUPPORT_BEGIN
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
-- UI_CONFIG_SUPPORT_END

local function build_container_weights(destination, category_name, category_module, default_weight)
    local category = require(category_module)
    local ui_default = get_category_default(category_name, "MaxWeight")
    local ui_overrides = get_container_overrides(category_name)

    for _, class_name in ipairs(category.classes) do
        local weight = read_max_weight(ui_overrides[class_name])
            or category.overrides[class_name]
            or ui_default
            or default_weight

        if weight ~= nil then
            destination[class_name] = weight
        end
    end
end

local function apply_actor_defaults(category_name, items, defaults)
    local ui_defaults = get_category_defaults(category_name)
    local ui_overrides = get_item_overrides(category_name)

    for class_name, properties in pairs(items) do
        apply_property_table(properties, ui_overrides[class_name])
        apply_defaults_to_nil(properties, ui_defaults)
        apply_defaults_to_nil(properties, defaults)
    end

    return items
end

local function merge(destination, source)
    for key, value in pairs(source) do
        destination[key] = value
    end
end

local function merge_actor_category(category_name)
    local items = require("categories/" .. category_name)
    local defaults = Config.CategoryDefaults[category_name]
    merge(Config.ActorProperties, apply_actor_defaults(category_name, items, defaults))
end

apply_ui_enabled_categories(Config.EnabledCategories)

local enabled = Config.EnabledCategories

Config.ContainerWeights = {}
if enabled.vehicles then
    build_container_weights(Config.ContainerWeights, "vehicles", "categories/vehicles", Config.CategoryDefaults.vehicles)
end
if enabled.containers then
    build_container_weights(Config.ContainerWeights, "containers", "categories/containers", Config.CategoryDefaults.containers)
end

Config.ActorProperties = {}
for _, category_name in ipairs({
    "backpacks",
    "crafting",
    "consumables",
    "ammo",
    "skillmags",
    "tools",
    "junk",
    "clothing",
    "medical",
    "schematics",
    "farming",
    "weapons",
}) do
    if enabled[category_name] then
        merge_actor_category(category_name)
    end
end

return Config
