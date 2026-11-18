using GrindPilot.Core.Catalog;

namespace GrindPilot.Core.Store;

/// <summary>Readable names for keys, for the Account page: <c>weapon.kastov-762.camos.military</c> → "Kastov 762 · Camos · Military".</summary>
public sealed class KeyLabels(GameCatalog catalog)
{
    private static readonly Dictionary<string, string> Words = new(StringComparer.Ordinal)
    {
        ["player"] = "Player",
        ["level"] = "Level",
        ["prestige"] = "Prestige",
        ["prestige-track"] = "Prestige track",
        ["status"] = "Status",
        ["daily"] = "Daily",
        ["countdown"] = "Time left",
        ["text"] = "Challenge",
        ["progress"] = "Progress",
        ["xp"] = "XP",
        ["bonus"] = "Bonus",
        ["unlocks"] = "Unlocks",
        ["label"] = "Label shown",
        ["camos"] = "Camos",
        ["camo"] = "Camo",
        ["unlocked"] = "Unlocked",
        ["next"] = "Next camo",
        ["challenge"] = "Challenge",
        ["requirement"] = "Requirement",
        ["next-mastery"] = "Next mastery",
        ["tracked"] = "Tracked",
        ["loadout"] = "Loadout",
        ["primary"] = "Primary",
        ["secondary"] = "Secondary",
        ["class"] = "Class",
        ["tactical"] = "Tactical",
        ["lethal"] = "Lethal",
        ["field-upgrade"] = "Field upgrade",
        ["mastery-set"] = "Mastery set",
        ["kind"] = "Kind",
        ["challenges"] = "Challenges",
        ["build"] = "Build",
        ["name"] = "Name",
    };

    private static readonly string[] Kinds = ["weapon", "camo", "camo-group", "mastery-set", "challenge"];

    public string For(string key)
    {
        var parts = key.Split('.');
        var words = new List<string>();
        for (var i = 0; i < parts.Length; i++)
        {
            var p = parts[i];
            // The first part of a weapon, camo, set or challenge key names its kind; the next part is the thing itself.
            if (i == 0 && p is "weapon" or "mastery-set" or "challenge" or "build")
            {
                continue;
            }
            words.Add(Name(p));
        }
        return string.Join(" · ", words);
    }

    private string Name(string part)
    {
        foreach (var kind in Kinds)
        {
            if (catalog.BySlug(kind, part) is { } e)
            {
                return e.Name;
            }
        }
        if (Words.TryGetValue(part, out var w))
        {
            return w;
        }
        if (part.Length == 2 && part.All(char.IsDigit))
        {
            return part; // a loadout number
        }
        return part.Length == 1 && char.IsDigit(part[0]) ? "#" + part : part;
    }
}
