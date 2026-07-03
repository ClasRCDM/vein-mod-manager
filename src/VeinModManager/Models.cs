using System.Globalization;

namespace VEIN_Item_And_Container_Modifier;

public static class CategoryNames
{
    public static readonly string[] Ordered =
    {
        "vehicles",
        "containers",
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
        "weapons"
    };

    public static readonly HashSet<string> ContainerLike = new(StringComparer.OrdinalIgnoreCase)
    {
        "vehicles",
        "containers"
    };

    public static readonly HashSet<string> ItemLike = Ordered
        .Where(name => !ContainerLike.Contains(name))
        .ToHashSet(StringComparer.OrdinalIgnoreCase);
}

public sealed record CategoryItem(
    string Category,
    string ClassName,
    string? CdoPath,
    Dictionary<string, LuaValue> ExistingValues);

public sealed class CategoryData
{
    public string Name { get; init; } = string.Empty;
    public List<CategoryItem> Items { get; } = new();
    public string? ParseError { get; set; }
}

public sealed class ModData
{
    public string ModFolder { get; init; } = string.Empty;
    public Dictionary<string, CategoryData> Categories { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, bool> BaseEnabledCategories { get; } = new(StringComparer.OrdinalIgnoreCase);
    public int ItemCount => Categories.Values.Sum(category => category.Items.Count);
}

public sealed record ConfigInstallResult(
    string BackupPath,
    string InstalledPath,
    UiConfigState State);

public readonly record struct LuaValue(object? Value)
{
    public bool IsNil => Value is null;

    public static LuaValue Nil => new(null);

    public static LuaValue FromText(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return Nil;
        var value = raw.Trim();
        if (value.Equals("nil", StringComparison.OrdinalIgnoreCase)) return Nil;
        if (value.Equals("true", StringComparison.OrdinalIgnoreCase)) return new LuaValue(true);
        if (value.Equals("false", StringComparison.OrdinalIgnoreCase)) return new LuaValue(false);
        if (decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
        {
            return new LuaValue(number);
        }
        return new LuaValue(value.Trim('"'));
    }

    public string ToLua()
    {
        return Value switch
        {
            null => "nil",
            bool b => b ? "true" : "false",
            decimal d => d.ToString("0.################", CultureInfo.InvariantCulture),
            double d => d.ToString("0.################", CultureInfo.InvariantCulture),
            float f => f.ToString("0.################", CultureInfo.InvariantCulture),
            int i => i.ToString(CultureInfo.InvariantCulture),
            long l => l.ToString(CultureInfo.InvariantCulture),
            string s => "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"",
            _ => Convert.ToString(Value, CultureInfo.InvariantCulture) ?? "nil"
        };
    }
}

public sealed class UiConfigState
{
    public Dictionary<string, bool> EnabledCategories { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, Dictionary<string, LuaValue>> CategoryDefaults { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, Dictionary<string, Dictionary<string, LuaValue>>> ItemOverrides { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, Dictionary<string, LuaValue>> ContainerWeightOverrides { get; } = new(StringComparer.OrdinalIgnoreCase);

    public void Clear()
    {
        EnabledCategories.Clear();
        CategoryDefaults.Clear();
        ItemOverrides.Clear();
        ContainerWeightOverrides.Clear();
    }

    public void CopyFrom(UiConfigState other)
    {
        Clear();

        foreach (var pair in other.EnabledCategories)
        {
            EnabledCategories[pair.Key] = pair.Value;
        }

        foreach (var pair in other.CategoryDefaults)
        {
            CategoryDefaults[pair.Key] = new Dictionary<string, LuaValue>(pair.Value, StringComparer.OrdinalIgnoreCase);
        }

        foreach (var category in other.ItemOverrides)
        {
            ItemOverrides[category.Key] = category.Value.ToDictionary(
                pair => pair.Key,
                pair => new Dictionary<string, LuaValue>(pair.Value, StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase);
        }

        foreach (var pair in other.ContainerWeightOverrides)
        {
            ContainerWeightOverrides[pair.Key] = new Dictionary<string, LuaValue>(pair.Value, StringComparer.OrdinalIgnoreCase);
        }
    }

    public int CountEdits(ModData? modData)
    {
        var enabledEdits = EnabledCategories.Count(pair =>
        {
            if (modData == null) return true;
            return !modData.BaseEnabledCategories.TryGetValue(pair.Key, out var baseValue) || baseValue != pair.Value;
        });

        return enabledEdits
            + CategoryDefaults.Values.Sum(values => values.Count)
            + ItemOverrides.Values.Sum(items => items.Values.Sum(values => values.Count))
            + ContainerWeightOverrides.Values.Sum(values => values.Count);
    }
}
