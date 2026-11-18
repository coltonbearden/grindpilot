using GrindPilot.Core.Catalog;

namespace GrindPilot.Core.Tests;

/// <summary>A small catalog for the store's tests. The real one is in catalog/ and is checked by CatalogFileTests.</summary>
internal static class TestCatalog
{
    public static readonly DateOnly SeasonOneStarts = new(2026, 10, 28);

    public static GameCatalog Build()
    {
        CatalogEntry E(string kind, string name, params (string Fact, string[] Values)[] facts) => new(
            kind,
            name,
            GameCatalog.Slugify(name),
            ["P-TEST"],
            [],
            facts.ToDictionary(
                f => f.Fact,
                f => new Fact(f.Fact, f.Values.Select(v => new FactValue(v, ["P-TEST"], null)).ToList())));

        return new GameCatalog(
            [
                E("weapon", "Kastov 762", ("class", ["Assault Rifle"])),
                E("weapon", "Cronen Osprey", ("class", ["Assault Rifle"]), ("unlockLevel", ["12", "14"])),
                E("weapon", "PILA", ("class", ["Launcher"])),
                E("weapon-class", "Assault Rifle"),
                E("camo-group", "Military"),
                E("camo-group", "Special"),
                E("camo-group", "Completionist"),
                E("camo-group", "Prestige"),
                E("camo", "Platinum"),
                E("camo", "Harmonic Break"),
                E("tactical", "Smoke Grenade"),
                E("field-upgrade", "Portable Radar"),
                E("challenge", "Victorious!"),
                E("mastery-set", "Capture Zone Mastery"),
            ],
            [new SourceInfo("P-TEST", "Test source", "2026-11-17", null, null)],
            [
                new SeasonInfo("Launch", new DateOnly(2026, 10, 23), ["P-TEST"]),
                new SeasonInfo("Season 1", SeasonOneStarts, ["P-TEST"]),
            ]);
    }
}
