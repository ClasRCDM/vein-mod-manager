local config = require("config")

local patched_vehicles = {}

local function log(message)
    if config.EnableLogging then
        print("[ContainerMod] " .. message .. "\n")
    end
end

local function is_valid(object)
    if object == nil then
        return false
    end

    local ok, valid = pcall(function()
        return object:IsValid()
    end)

    return ok and valid
end

local function is_default_object(object)
    local ok, is_default = pcall(function()
        return object:IsDefaultObject()
    end)

    return ok and is_default
end

local function get_full_name(object)
    local ok, name = pcall(function()
        return object:GetFullName()
    end)

    if ok then
        return name
    end

    return nil
end

local function get_class_name(object)
    local ok, class_name = pcall(function()
        return object:GetClass():GetFName():ToString()
    end)

    if ok and class_name ~= nil then
        return class_name
    end

    return "unknown"
end

local function find_all(class_name)
    local ok, objects = pcall(function()
        return FindAllOf(class_name)
    end)

    if ok and objects then
        return objects
    end

    return nil
end

local function apply_actor_properties(object, properties)
    if not is_valid(object) then
        return false
    end

    for property_name, value in pairs(properties) do
        if property_name ~= "_CDOPath" then
            pcall(function()
                object[property_name] = value
            end)
        end
    end

    return true
end

local function patch_actor_defaults()
    for class_name, properties in pairs(config.ActorProperties) do
        local cdo_path = properties._CDOPath
        if cdo_path then
            local cdo = StaticFindObject(cdo_path)
            if is_valid(cdo) then
                apply_actor_properties(cdo, properties)
                log("CDO patched: " .. class_name)
            end
        end

        local objects = find_all(class_name)
        if objects then
            local patched_count = 0

            for _, object in ipairs(objects) do
                pcall(function()
                    if is_valid(object) and not is_default_object(object) then
                        if apply_actor_properties(object, properties) then
                            patched_count = patched_count + 1
                        end
                    end
                end)
            end

            if patched_count > 0 then
                log(string.format("Patched %d existing %s", patched_count, class_name))
            end
        end
    end
end

local function register_actor_hooks()
    for class_name, properties in pairs(config.ActorProperties) do
        local cdo_path = properties._CDOPath
        if cdo_path then
            local class_path = cdo_path:gsub("%.Default__", ".")

            NotifyOnNewObject(class_path, function(object)
                ExecuteInGameThread(function()
                    if apply_actor_properties(object, properties) then
                        log("New " .. class_name .. " patched")
                    end
                end)
            end)
        end
    end
end

local function patch_vehicle_actor(actor, target_weight)
    if not is_valid(actor) or is_default_object(actor) then
        return 0
    end

    local actor_key = get_full_name(actor)
    if actor_key and patched_vehicles[actor_key] then
        return 0
    end

    local patched_count = 0

    for _, member_name in ipairs(config.VehicleInvMembers) do
        pcall(function()
            local component = actor[member_name]
            if is_valid(component) and component.MaxWeight ~= nil then
                component:SetPropertyValue("MaxWeight", target_weight)
                component.MaxWeight = target_weight
                patched_count = patched_count + 1
            end
        end)
    end

    if patched_count > 0 then
        if actor_key then
            patched_vehicles[actor_key] = true
        end

        log(string.format("Patched %s -> MaxWeight = %s", get_class_name(actor), tostring(target_weight)))
    end

    return patched_count
end

local function patch_vehicles()
    local found_count = 0

    for actor_class_name, target_weight in pairs(config.ContainerWeights) do
        local actors = find_all(actor_class_name)
        if actors then
            found_count = found_count + #actors

            for _, actor in ipairs(actors) do
                pcall(function()
                    patch_vehicle_actor(actor, target_weight)
                end)
            end
        end
    end

    return found_count
end

local function count_patched_vehicles()
    local patched_count = 0

    for _ in pairs(patched_vehicles) do
        patched_count = patched_count + 1
    end

    return patched_count
end

local function register_actor_sweep()
    ExecuteWithDelay(config.SweepOnStartMs, function()
        ExecuteInGameThread(function()
            patch_actor_defaults()
            log("Initial sweep complete")
        end)
    end)
end

local function register_vehicle_scan()
    if next(config.ContainerWeights) == nil then
        return
    end

    local elapsed = 0
    local scan_stopped = false

    LoopAsync(config.ScanIntervalMs, function()
        elapsed = elapsed + config.ScanIntervalMs

        local elapsed_snapshot = elapsed
        local should_stop = elapsed >= config.ScanStopAfterMs
        if should_stop then
            scan_stopped = true
        end

        ExecuteInGameThread(function()
            if scan_stopped and not should_stop then
                return
            end

            local found_count = patch_vehicles()

            log(string.format(
                "scan @ %ds: found=%d patched=%d",
                math.floor(elapsed_snapshot / 1000),
                found_count,
                count_patched_vehicles()
            ))

            if should_stop then
                log("scan complete")
            end
        end)

        return should_stop
    end)
end

register_actor_hooks()
register_actor_sweep()
register_vehicle_scan()

log("Item and container mod loaded")
