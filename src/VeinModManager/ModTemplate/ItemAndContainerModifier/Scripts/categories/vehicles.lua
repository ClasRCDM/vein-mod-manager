-- vehicles.lua
-- All vehicles that can have their inventory MaxWeight patched.
-- CategoryDefaults.vehicles in config.lua sets the weight for everything in `classes`.
-- Add an entry to `overrides` to set a specific weight for one vehicle,
-- which takes priority over the category default.
return {
    classes = {
        -- WHEELED
        "BP_Ambulance_C",
        "BP_ATV_C",
        "BP_BoxTruck_C",
        "BP_BoxTruck2_C",
        "BP_BoxTruck2_Cab_C",
        "BP_CamperVan_C",
        "BP_GoKart_C",
        "BP_GolfCart_C",
        "BP_Hatchback_C",
        "BP_Motorcycle1_C",
        "BP_Pickup_C",
        "BP_PoliceCar_C",
        "BP_SchoolBus_C",
        "BP_Sedan1_C",
        "BP_Sportscar_C",
        "BP_SUV1_C",
        "BP_SUV2_C",
        "BP_Tractor_C",
        "BP_Van_C",
        "BP_Van_News_C",
        -- AIR
        "BP_Heli_Blackhawk_C",
        -- HUMAN-POWERED (inventory unconfirmed)
        --"BP_Bicycle_C",
        --"BP_Canoe_C",
        --"BP_Lawnmower_C",
        -- TRAINS (not in player vehicle list — untested if they have cargo inventory)
        -- "BP_Train_BoxCar_C",
        -- "BP_Train_Locomotive_C",
        -- "BP_Train_OilCar_C",
        -- "BP_Train_Wagon_C",
    },
    overrides = {
        -- ["BP_BoxTruck_C"] = 8000,
    },
}
