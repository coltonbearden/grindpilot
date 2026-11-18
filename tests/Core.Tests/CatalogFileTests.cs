using GrindPilot.Core.Catalog;

namespace GrindPilot.Core.Tests;

/// <summary>
/// The real catalog in catalog/, checked in CI without the network: its shape, its counts, its known conflicts, and
/// that every source tag is a known post or a named in-game screen in sources.json. tests/Local checks each entry
/// against the post itself.
/// </summary>
public class CatalogFileTests
{
    private static readonly string Repo = ShippedData.FindRepoRoot(AppContext.BaseDirectory);
    private static readonly GameCatalog Catalog = CatalogLoader.LoadFolder(Path.Combine(Repo, "catalog"));

    [Fact]
    public void It_holds_the_33_launch_weapons_in_9_classes()
    {
        var launch = Catalog.OfKind("weapon").Where(w => w.Sources.Contains("P-LOAD")).ToList();
        Assert.Equal(33, launch.Count);
        Assert.Equal(9, launch.Select(w => w.FactNamed("class")!.Values[0].Value).Distinct().Count());
        Assert.Equal(9, Catalog.OfKind("weapon-class").Count);
    }

    [Fact]
    public void It_holds_the_season_1_weapons() =>
        Assert.Equal(
            ["CSG-35", "Cronen Osprey", "JAK 12", "ZLR Talonhawk"],
            Catalog.OfKind("weapon").Where(w => w.Sources.Contains("P-S1")).Select(w => w.Name).Order(StringComparer.Ordinal));

    [Fact]
    public void Every_weapon_has_a_class_and_every_launch_weapon_an_unlock_level()
    {
        foreach (var w in Catalog.OfKind("weapon"))
        {
            Assert.True(w.FactNamed("class") is not null, w.Name);
            if (w.Sources.Contains("P-LOAD"))
            {
                Assert.True(w.FactNamed("unlockLevel") is not null, w.Name);
            }
        }
    }

    [Fact]
    public void The_six_unlock_level_conflicts_of_P_LOAD_are_kept() =>
        Assert.Equal(6, Catalog.OfKind("weapon").Count(w => w.FactNamed("unlockLevel")?.IsConflict == true));

    [Fact]
    public void The_throwing_knife_is_48_or_49()
    {
        var knife = Catalog.Match("lethal", "Throwing Knife")!;
        Assert.True(knife.FactNamed("unlockLevel")!.IsConflict);
        Assert.Equal(["48", "49"], knife.FactNamed("unlockLevel")!.Values.Select(v => v.Value).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void Weapon_prestige_resetting_to_1_or_to_zero_is_kept() =>
        Assert.True(Catalog.Match("rule", "Weapon Prestige level reset")!.FactNamed("resetsTo")!.IsConflict);

    [Fact]
    public void The_camo_ladder_has_its_groups_in_order()
    {
        var camos = Catalog.OfKind("camo");
        Assert.Equal(8, camos.Count(c => c.FactNamed("group")!.Values[0].Value == "Military"));
        Assert.Equal(["Gold", "Platinum", "Harmonic Break", "Divergence"],
            camos.Where(c => c.FactNamed("group")!.Values[0].Value == "Completionist")
                 .OrderBy(c => int.Parse(c.FactNamed("order")!.Values[0].Value, System.Globalization.CultureInfo.InvariantCulture))
                 .Select(c => c.Name));
        Assert.Equal(4, camos.Count(c => c.FactNamed("group")!.Values[0].Value == "Prestige"));
        Assert.Equal(9, camos.Count(c => c.FactNamed("group")!.Values[0].Value.StartsWith("Mode Mastery", StringComparison.Ordinal)));
    }

    [Fact]
    public void Every_entry_has_a_source_and_every_source_is_known()
    {
        foreach (var e in Catalog.Entries)
        {
            Assert.True(e.Sources.Count > 0, $"{e.Kind} {e.Name} has no source");
            foreach (var tag in AllTags(e))
            {
                Assert.True(Catalog.SourceTags.ContainsKey(tag), $"{e.Kind} {e.Name} cites {tag}, which isn't in sources.json");
            }
        }
    }

    [Fact]
    public void Screen_sources_are_named_screens_with_no_picture()
    {
        var cited = Catalog.Entries.SelectMany(AllTags).Where(t => t.StartsWith('S')).ToHashSet();
        var listed = Catalog.SourceTags.Values.Where(s => s.Tag.StartsWith('S')).ToList();
        Assert.Equal(cited.Order(StringComparer.Ordinal), listed.Select(s => s.Tag).Order(StringComparer.Ordinal));
        foreach (var s in listed)
        {
            Assert.StartsWith("In-game screen: ", s.Title, StringComparison.Ordinal);
            Assert.Null(s.Url);
        }
    }

    private static IEnumerable<string> AllTags(CatalogEntry e) =>
        e.Sources.Concat(e.Facts.Values.SelectMany(f => f.Values.SelectMany(v => v.Sources)));

    [Theory]
    [InlineData("weapon", "CRONEN OSPREY")]
    [InlineData("weapon", "KASTOV 762")]
    [InlineData("weapon", "HAN 86")]
    [InlineData("weapon", "PILA")]
    [InlineData("weapon", "PATRIOT XMR")]
    [InlineData("weapon", "ASTAROTH .57")]
    [InlineData("weapon", "VEL 46")]
    [InlineData("weapon", "MAR-9")]
    [InlineData("tactical", "SMOKE GRENADE")]
    [InlineData("field-upgrade", "PORTABLE RADAR")]
    [InlineData("perk", "BOMB SQUAD")]
    [InlineData("perk", "COLD-BLOODED")]
    [InlineData("camo", "PLATINUM")]
    [InlineData("camo", "HARMONIC BREAK")]
    [InlineData("camo", "MILITARY CAMO 2")]
    [InlineData("mastery-set", "CAPTURE ZONE MASTERY")]
    [InlineData("challenge", "VICTORIOUS!")]
    [InlineData("attachment", "ZLR COLD ECHO")]
    [InlineData("weapon-class", "SMG")]
    [InlineData("weapon-class", "Assault Rifle")]
    public void Every_name_on_the_confirmed_screens_is_in_the_catalog(string kind, string shown) =>
        Assert.NotNull(Catalog.Match(kind, shown));

    [Fact]
    public void Seasons_come_from_the_posts()
    {
        Assert.Equal("Launch", Catalog.SeasonAt(new DateTimeOffset(2026, 10, 30, 12, 0, 0, TimeSpan.FromHours(-4)))?.Name);
        Assert.Equal("Season 1", Catalog.SeasonAt(new DateTimeOffset(2026, 11, 10, 11, 0, 0, TimeSpan.FromHours(-5)))?.Name);
    }
}
