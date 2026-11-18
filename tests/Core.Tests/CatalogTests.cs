using GrindPilot.Core.Catalog;

namespace GrindPilot.Core.Tests;

public class CatalogTests
{
    [Theory]
    [InlineData("KASTOV 762", "Kastov 762")]
    [InlineData("kastov  762", "Kastov 762")]
    [InlineData("KAST0V 762", "Kastov 762")] // O read as 0
    [InlineData("PlLA", "PILA")] // l read for I
    [InlineData("P1LA", "PILA")]
    public void Look_alike_characters_count_as_equal(string shown, string expected) =>
        Assert.Equal(expected, TestCatalog.Build().Match("weapon", shown)?.Name);

    [Theory]
    [InlineData("KASTOV 763")]
    [InlineData("KASTOV")]
    [InlineData("PILAS")]
    public void Any_other_difference_is_a_non_match(string shown) =>
        Assert.Null(TestCatalog.Build().Match("weapon", shown));

    [Fact]
    public void Names_match_only_within_their_kind() =>
        Assert.Null(TestCatalog.Build().Match("camo", "PILA"));

    [Theory]
    [InlineData("Astaroth .57", "astaroth-57")]
    [InlineData("SO PILOT-RX 11\" SHORT BARREL", "so-pilot-rx-11-short-barrel")]
    [InlineData("VICTORIOUS!", "victorious")]
    [InlineData("Kastov 762", "kastov-762")]
    public void Slugs_are_key_safe(string name, string slug) =>
        Assert.Equal(slug, GameCatalog.Slugify(name));

    [Fact]
    public void A_fact_with_two_official_values_is_a_conflict()
    {
        var osprey = TestCatalog.Build().Match("weapon", "CRONEN OSPREY")!;
        Assert.True(osprey.HasConflict);
        Assert.True(osprey.FactNamed("unlockLevel")!.IsConflict);
        Assert.False(osprey.FactNamed("class")!.IsConflict);
    }

    [Fact]
    public void Seasons_are_found_by_date()
    {
        var c = TestCatalog.Build();
        Assert.Null(c.SeasonAt(new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero)));
        Assert.Equal("Launch", c.SeasonAt(new DateTimeOffset(2026, 10, 24, 12, 0, 0, TimeSpan.Zero))?.Name);
        Assert.Equal("Season 1", c.SeasonAt(new DateTimeOffset(2026, 11, 10, 12, 0, 0, TimeSpan.Zero))?.Name);
    }
}
