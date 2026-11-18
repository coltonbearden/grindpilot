using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using GrindPilot.Tools.CatalogSeed;

// Seeds the catalog files that come straight from the official posts' tables and weapon blocks:
//   weapons.json, weapon-classes.json, tactical.json, lethal.json, field-upgrade.json, killstreak.json, perk.json,
//   attachment-slots.json and sources.json.
// The camo ladder, the Mastery Sets, the rules and the names only in-game screens carry are entered by hand in the
// other catalog files. tests/Local checks every entry, seeded or hand-entered, against the post it cites.
//
//   dotnet run --project tools/CatalogSeed -- [--repo <root>]

var repo = Directory.GetCurrentDirectory();
for (var i = 0; i < args.Length; i++)
{
    if (args[i] == "--repo")
    {
        repo = args[++i];
    }
}
var catalogDir = Path.Combine(repo, "catalog");
var cache = Path.Combine(repo, ".scratch", "posts");

using var http = new HttpClient();
http.DefaultRequestHeaders.UserAgent.ParseAdd("grindpilot-catalog-seed");
var posts = await OfficialPosts.FetchAsync(http, cache);
var load = posts["P-LOAD"];
var s1 = posts["P-S1"];
var prog = posts["P-PROG"];

// ---- P-LOAD's tables: weapons by class with their unlock level, equipment, field upgrades, killstreaks, perks ----

var weaponClassFromTable = new Dictionary<string, string>
{
    ["Assault Rifle Weapon"] = "Assault Rifle",
    ["Submachine Gun"] = "Submachine Gun",
    ["Shotgun"] = "Shotgun",
    ["Light Machine Gun"] = "Light Machine Gun",
    ["Marksman Rifle"] = "Marksman Rifle",
    ["Sniper Rifle"] = "Sniper Rifle",
    ["Pistol"] = "Pistol",
    ["Launcher"] = "Launcher",
    ["Melee Weapon"] = "Melee Weapon",
};
// Matched without regard to case: the post writes "Finn LMG" in its table and "FiNN LMG" in the weapon's block.
// The table's spelling is the catalog name.
var weaponTable = new Dictionary<string, (string Class, int Level)>(StringComparer.OrdinalIgnoreCase);
var equipment = new Dictionary<string, List<(string Name, int Level, string? Extra)>>
{
    ["tactical"] = [],
    ["lethal"] = [],
    ["field-upgrade"] = [],
    ["killstreak"] = [],
    ["perk"] = [],
};
var levelRow = new Regex(@"^Level (\d+): (.+)$");
foreach (var table in PostText.TablesOf(load.Raw))
{
    var head = table[0][0];
    var what = Regex.Match(head, @":\s*(.+?) Unlock Criteria$").Groups[1].Value;
    string? killCost = null;
    foreach (var row in table.Skip(1))
    {
        var cell = row[0];
        var cost = Regex.Match(cell, @"^(\d+)-Kill Requirement$");
        if (cost.Success)
        {
            killCost = cost.Groups[1].Value;
            continue;
        }
        var m = levelRow.Match(cell);
        if (!m.Success)
        {
            throw new InvalidDataException($"P-LOAD table '{head}': unexpected row '{cell}'.");
        }
        var level = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
        var name = m.Groups[2].Value.Trim();
        if (weaponClassFromTable.TryGetValue(what, out var cls))
        {
            weaponTable[name] = (cls, level);
        }
        else if (what == "Tactical Equipment")
        {
            equipment["tactical"].Add((name, level, null));
        }
        else if (what == "Lethal Equipment")
        {
            equipment["lethal"].Add((name, level, null));
        }
        else if (what == "Field Upgrade")
        {
            equipment["field-upgrade"].Add((name, level, null));
        }
        else if (what == "Killstreak")
        {
            equipment["killstreak"].Add((name, level, killCost));
        }
        else if (what.StartsWith("Perk ", StringComparison.Ordinal))
        {
            equipment["perk"].Add((name, level, what.Replace(" Category", "", StringComparison.Ordinal)));
        }
        else
        {
            throw new InvalidDataException($"P-LOAD table '{head}' is of a kind the seed doesn't know.");
        }
    }
}
if (weaponTable.Count != 33)
{
    throw new InvalidDataException($"P-LOAD's tables name {weaponTable.Count} weapons; the spec says 33.");
}

// ---- The weapon blocks: P-LOAD's 33, and Season 1's in P-S1 ----

var blocks = WeaponBlocks(load.Lines).Concat(WeaponBlocks(s1.Lines).Select(b => b with { FromSeason1 = true })).ToList();
var slots = new SortedSet<string>(StringComparer.Ordinal);
var weapons = new JsonArray();
var alsoShown = new Dictionary<string, string>(StringComparer.Ordinal);
foreach (var block in blocks)
{
    var b = block;
    var tag = b.FromSeason1 ? "P-S1" : "P-LOAD";
    var facts = new JsonObject();
    if (b.FromSeason1)
    {
        // P-S1 writes the class as its block's second line does ("SMG, Battle Pass Tier 36, …").
        var cls = Value(ClassFromS1(b.Second!), tag, null);
        cls["quote"] = b.Second!;
        facts["class"] = new JsonArray(cls);
        facts["unlock"] = One(b.Second!.Split(',', 2)[1].Trim(), tag);
        facts["season"] = One("Season 1", tag);
    }
    else
    {
        var tableName = TableName(weaponTable.Keys, b.Name)
            ?? throw new InvalidDataException($"P-LOAD's block '{b.Name}' matches no weapon in its tables.");
        var (cls, tableLevel) = weaponTable[tableName];
        if (!string.Equals(tableName, b.Name, StringComparison.OrdinalIgnoreCase))
        {
            alsoShown[tableName] = b.Name;
        }
        b = b with { Name = tableName };
        facts["class"] = One(cls, tag);
        var lineLevel = b.UnlockLevel;
        facts["unlockLevel"] = lineLevel is null || lineLevel == tableLevel
            ? One(tableLevel.ToString(CultureInfo.InvariantCulture), tag)
            : new JsonArray(
                Value(tableLevel.ToString(CultureInfo.InvariantCulture), tag, "the unlock table"),
                Value(lineLevel.Value.ToString(CultureInfo.InvariantCulture), tag, "the weapon's own line"));
    }
    if (b.Levels is { } levels)
    {
        facts["maxLevel"] = One(levels, tag);
    }
    else if (b.LevelsClassified)
    {
        var classified = Value("not stated", tag, "the post shows [CLASSIFIED]");
        classified["quote"] = "Levels: [CLASSIFIED]";
        facts["maxLevel"] = new JsonArray(classified);
    }
    if (b.FireRate is { } fr)
    {
        facts["fireRate"] = One(fr, tag);
    }
    if (b.Rounds is { } rounds)
    {
        facts["rounds"] = One(rounds, tag);
    }
    if (b.Reserve is { } reserve)
    {
        facts["reserve"] = One(reserve, tag);
    }
    if (b.Attachments.Count > 0)
    {
        facts["attachmentTypes"] = One(string.Join(", ", b.Attachments), tag);
        foreach (var a in b.Attachments)
        {
            slots.Add(a);
        }
    }
    if (b.Apex is { } apex)
    {
        facts["apex"] = One(apex, tag);
    }
    var entry = new JsonObject { ["name"] = b.Name, ["sources"] = new JsonArray(tag), ["facts"] = facts };
    if (alsoShown.TryGetValue(b.Name, out var other))
    {
        entry["shown"] = new JsonArray(other);
    }
    weapons.Add(entry);
}
var missing = weaponTable.Keys
    .Where(k => !blocks.Any(b => !b.FromSeason1 && TableName([k], b.Name) is not null))
    .ToList();
if (missing.Count > 0)
{
    throw new InvalidDataException("No weapon block in P-LOAD for: " + string.Join(", ", missing));
}

Write("weapons.json", "weapon",
    "The 33 launch weapons [P-LOAD] and Season 1's [P-S1], seeded by tools/CatalogSeed from the posts' tables and weapon blocks. " +
    "Where P-LOAD's unlock table and a weapon's own line disagree, both values are kept: a conflict (3.5).",
    weapons);

// ---- Weapon classes, with the shorter spellings the posts and the game's screens use ----

var classes = new JsonArray();
foreach (var cls in weaponClassFromTable.Values)
{
    var e = new JsonObject { ["name"] = cls, ["sources"] = new JsonArray("P-LOAD") };
    if (cls == "Submachine Gun")
    {
        e["shown"] = new JsonArray("SMG");
    }
    if (cls == "Melee Weapon")
    {
        e["shown"] = new JsonArray("Melee");
    }
    classes.Add(e);
}
Write("weapon-classes.json", "weapon-class", "The nine weapon classes [P-LOAD]. 'SMG' and 'Melee' are how P-S1 and the screens write two of them.", classes);

// ---- Equipment, field upgrades, killstreaks, perks ----

var progLines = prog.Plain;
foreach (var (kind, items) in equipment)
{
    var arr = new JsonArray();
    foreach (var (name, level, extra) in items)
    {
        var facts = new JsonObject();
        if (kind == "lethal" && name == "Throwing Knife" && progLines.Contains("reach Level 48 again to re-unlock the Throwing Knife", StringComparison.Ordinal))
        {
            // P-PROG's Permanent Unlock example puts the Throwing Knife at 48; the tables say 49 (3.5).
            facts["unlockLevel"] = new JsonArray(
                Value(level.ToString(CultureInfo.InvariantCulture), "P-LOAD", "the unlock table"),
                Value("48", "P-PROG", "the Permanent Unlock example"));
        }
        else
        {
            facts["unlockLevel"] = One(level.ToString(CultureInfo.InvariantCulture), "P-LOAD");
        }
        if (kind == "killstreak" && extra is not null)
        {
            facts["kills"] = One(extra, "P-LOAD");
        }
        if (kind == "perk" && extra is not null)
        {
            facts["row"] = One(extra, "P-LOAD");
        }
        arr.Add(new JsonObject { ["name"] = name, ["sources"] = new JsonArray("P-LOAD"), ["facts"] = facts });
    }
    Write($"{kind}.json", kind, $"From P-LOAD's unlock tables, seeded by tools/CatalogSeed.", arr);
}

// ---- Attachment slots: every attachment type a weapon block names ----

var slotArr = new JsonArray();
foreach (var s in slots)
{
    slotArr.Add(new JsonObject { ["name"] = s, ["sources"] = new JsonArray("P-LOAD") });
}
Write("attachment-slots.json", "attachment-slot", "Every attachment type P-LOAD's weapon blocks name.", slotArr);

// ---- Sources: sources.md's table, with each fetched post's id in Steam's feed ----

var sourcesMd = new (string Tag, string Date, string Title, string Url)[]
{
    ("P-PROG", "2026-10-19", "Modern Warfare 4 Progression and Prestige", "https://store.steampowered.com/news/app/1938090/view/671757096278033531"),
    ("P-LOAD", "2026-10-19", "Modern Warfare 4 Operators, Weapons, Equipment, and Multiplayer Loadout Intel", "https://store.steampowered.com/news/app/1938090/view/671757096278033470"),
    ("P-S1", "2026-10-28", "Call of Duty: Modern Warfare 4 and Warzone - Season 1", "https://store.steampowered.com/news/app/1938090/view/677387230220656499"),
    ("P-WZ", "2026-11-02", "Call of Duty: Warzone Season 1: Everything You Need to Know", "https://store.steampowered.com/news/app/1938090/view/717920261052041976"),
    ("P-BP", "2026-11-05", "Modern Warfare 4 Season 1 Battle Pass System, BlackCell, and New Store Bundles", "https://store.steampowered.com/news/app/1938090/view/717920898634482445"),
    ("P-LIVE", "2026-10-23", "Call of Duty: Modern Warfare 4 is Live Worldwide", "https://store.steampowered.com/news/app/1938090/view/677387230220654226"),
};
var srcArr = new JsonArray();
foreach (var (tag, date, title, url) in sourcesMd)
{
    var o = new JsonObject { ["tag"] = tag, ["title"] = title, ["date"] = date, ["url"] = url };
    if (posts.TryGetValue(tag, out var p))
    {
        o["feedId"] = p.FeedId;
        if (p.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) != date)
        {
            Console.WriteLine($"note: {tag}'s feed date is {p.Date:yyyy-MM-dd}, sources.md says {date}");
        }
    }
    srcArr.Add(o);
}
// The in-game screens the hand-entered files cite (S…) are kept as they are: each names the screen, never a picture.
var screens = File.Exists(Path.Combine(catalogDir, "sources.json"))
    ? JsonNode.Parse(File.ReadAllText(Path.Combine(catalogDir, "sources.json")))?["screens"]?.DeepClone() as JsonArray
    : null;
var sourcesDoc = new JsonObject
{
    ["about"] = "The official posts the catalog cites (docs/spec/sources.md). url is the post's page as sources.md lists it; " +
                "feedId is its id in Steam's news API, which differs from the page id. screens are in-game screens a " +
                "maintainer confirmed by eye, named by the screen they show; no picture is published.",
    ["sources"] = srcArr,
    ["screens"] = screens ?? new JsonArray(),
};
File.WriteAllText(Path.Combine(catalogDir, "sources.json"), Serialize(sourcesDoc));

Console.WriteLine($"Seeded {weapons.Count} weapons, {classes.Count} classes, {slots.Count} attachment slots, " +
                  string.Join(", ", equipment.Select(e => $"{e.Value.Count} {e.Key}")) + ".");
return 0;

// ---- helpers ----

void Write(string file, string kind, string about, JsonArray entries)
{
    Directory.CreateDirectory(catalogDir);
    var doc = new JsonObject { ["kind"] = kind, ["about"] = about, ["seededBy"] = "tools/CatalogSeed", ["entries"] = entries };
    File.WriteAllText(Path.Combine(catalogDir, file), Serialize(doc));
}

static string Serialize(JsonNode node) =>
    node.ToJsonString(new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }).Replace("\r\n", "\n") + "\n";

static JsonArray One(string value, string tag) => new(Value(value, tag, null));

static JsonObject Value(string value, string tag, string? note)
{
    var o = new JsonObject { ["value"] = value, ["sources"] = new JsonArray(tag) };
    if (note is not null)
    {
        o["note"] = note;
    }
    return o;
}

// The table's name for a weapon block: the same name in any case, or one that extends the other by whole words
// ("FSS Carbon Edge 1095 Knife" in the table, "FSS Carbon Edge 1095" in its block).
static string? TableName(IEnumerable<string> tableNames, string blockName) =>
    tableNames.FirstOrDefault(k => string.Equals(k, blockName, StringComparison.OrdinalIgnoreCase))
    ?? tableNames.FirstOrDefault(k =>
        k.StartsWith(blockName + " ", StringComparison.OrdinalIgnoreCase) || blockName.StartsWith(k + " ", StringComparison.OrdinalIgnoreCase));

static string ClassFromS1(string second)
{
    var cls = second.Split(',')[0].Trim();
    return cls switch
    {
        "SMG" => "Submachine Gun",
        "Melee" => "Melee Weapon",
        _ => cls,
    };
}

static IEnumerable<WeaponBlock> WeaponBlocks(IReadOnlyList<string> lines)
{
    var prefix = new Regex(@"^(Primary|Secondary|Melee) Weapon: (.+?)( \((Launch|In-Season)\))?$");
    for (var i = 0; i + 2 < lines.Count; i++)
    {
        string name;
        string? second = null;
        int start;
        // A weapon's levels are a number; P-S1 prints "\" for one it doesn't state. The legend above the blocks
        // ("Levels:" followed by what the word means) is not a weapon.
        static bool IsLevels(IReadOnlyList<string> l, int at) => at < l.Count && Regex.IsMatch(l[at], @"^(\d+|\[CLASSIFIED\])$");
        var p = prefix.Match(lines[i]);
        if (p.Success && i + 3 < lines.Count && lines[i + 2] == "Levels:" && IsLevels(lines, i + 3))
        {
            name = p.Groups[2].Value;
            second = lines[i + 1];
            start = i + 2;
        }
        else if (lines[i + 1] == "Levels:" && IsLevels(lines, i + 2))
        {
            name = lines[i];
            start = i + 1;
        }
        else
        {
            continue;
        }

        string? Field(string key)
        {
            for (var j = start; j < Math.Min(lines.Count - 1, start + 14); j++)
            {
                if (lines[j] == key)
                {
                    return lines[j + 1];
                }
            }
            return null;
        }

        var levels = Field("Levels:");
        var criteria = Field("Available Criteria:") ?? "";
        var unlock = Regex.Match(criteria, @"Player Level (\d+)");
        var attachments = Field("Attachment Types:") ?? "";
        string? apex = null;
        for (var j = start; j < Math.Min(lines.Count, start + 20); j++)
        {
            if (lines[j].StartsWith("Apex Attachment: ", StringComparison.Ordinal))
            {
                apex = lines[j]["Apex Attachment: ".Length..].Trim();
                break;
            }
            if (j > start && lines[j] == "Levels:")
            {
                break;
            }
        }
        yield return new WeaponBlock(
            name.Trim(),
            second,
            levels is not null && Regex.IsMatch(levels, @"^\d+$") ? levels : null,
            Field("Rounds:") is { } r && Regex.IsMatch(r, @"^\d+$") ? r : null,
            Field("Reserve:") is { } rs && Regex.IsMatch(rs, @"^\d+$") ? rs : null,
            Field("Fire Rate:") is { } f && f.EndsWith("RPM", StringComparison.Ordinal) ? f : null,
            unlock.Success ? int.Parse(unlock.Groups[1].Value, CultureInfo.InvariantCulture) : null,
            attachments.Split(',').Select(a => a.Trim()).Where(a => a.Length > 0 && a is not "None" and not "N/A").Distinct().ToList(),
            apex,
            false)
        {
            LevelsClassified = levels == "[CLASSIFIED]",
        };
        i = start; // the block's own lines are not names of the next one
    }
}

internal sealed record WeaponBlock(
    string Name,
    string? Second,
    string? Levels,
    string? Rounds,
    string? Reserve,
    string? FireRate,
    int? UnlockLevel,
    IReadOnlyList<string> Attachments,
    string? Apex,
    bool FromSeason1)
{
    public bool LevelsClassified { get; init; }
}
