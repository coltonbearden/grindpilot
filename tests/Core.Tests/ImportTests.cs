using GrindPilot.Core.Catalog;
using GrindPilot.Core.Import;
using GrindPilot.Core.Store;

namespace GrindPilot.Core.Tests;

/// <summary>
/// The confirmed-values import, with the invented pair in <c>tests/fixtures/</c> and the real catalog. A maintainer's
/// own pair is checked by the local suite (<c>tests/Local/ImportGoldenTests.cs</c>), never from the repo.
/// </summary>
public sealed class ImportTests : IDisposable
{
    private static readonly string Repo = ShippedData.FindRepoRoot(AppContext.BaseDirectory);
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "grindpilot-tests", Guid.NewGuid().ToString("N"));
    private readonly AccountStore _store;
    private readonly ConfirmedImport _import;

    public ImportTests()
    {
        var catalog = CatalogLoader.LoadFolder(Path.Combine(Repo, "catalog"));
        _store = AccountStore.Open(Path.Combine(_dir, "state.db"), catalog);
        _import = Fixture();
    }

    public static ConfirmedImport Fixture()
    {
        var repo = ShippedData.FindRepoRoot(AppContext.BaseDirectory);
        return ConfirmedImport.Load(
            Path.Combine(repo, "tests", "fixtures", "import-map.json"),
            Path.Combine(repo, "tests", "fixtures", "import-samples.json"));
    }

    public void Dispose()
    {
        _store.Dispose();
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        try
        {
            Directory.Delete(_dir, true);
        }
        catch (IOException)
        {
        }
    }

    private static readonly TimeZoneInfo Zone =
        TimeZoneInfo.FindSystemTimeZoneById(OperatingSystem.IsWindows() ? "Eastern Standard Time" : "America/New_York");

    private ImportResult Run() => _import.Run(_store, @"C:\Users\someone", Zone);

    [Fact]
    public void Every_confirmed_field_is_mapped_or_explained() =>
        Assert.Empty(_import.Validate());

    [Fact]
    public void All_fields_are_in_the_map() =>
        Assert.Equal(Enumerable.Range(1, 4), _import.Map.Select(e => e.N).Order());

    [Fact]
    public void The_import_stores_the_confirmed_values()
    {
        var r = Run();
        Assert.False(r.AlreadyDone);
        Assert.Equal(3, r.FieldsImported);
        Assert.Equal(1, r.FieldsSkipped);
        Assert.Equal(3, r.Observations);
        Assert.Equal(2, r.Captures);

        Assert.Equal("2", _store.StateOf("player.prestige").Value);
        Assert.Equal("17", _store.StateOf("player.level").Value);
        Assert.Equal("3/15", _store.StateOf("weapon.cronen-osprey.camos.unlocked").Value);
    }

    [Fact]
    public void Every_imported_value_is_confirmed_and_traces_to_its_screenshot()
    {
        Run();
        foreach (var s in _store.CurrentState().Values)
        {
            Assert.Equal(Outcome.Confirmed, s.Outcome);
            var obs = _store.ObservationsFor(s.Key).Single();
            var capture = _store.Capture(obs.CaptureId)!;
            Assert.Equal(CaptureKind.Screenshot, capture.Kind);
            Assert.StartsWith(@"C:\Users\someone\Videos\Captures\", capture.Path, StringComparison.Ordinal);
            Assert.Equal(64, capture.Sha256!.Length);
            Assert.Contains("Invented", obs.Note, StringComparison.Ordinal);
            Assert.Equal(new DateOnly(2026, 1, 2), DateOnly.FromDateTime(capture.TakenAt.DateTime));
        }
    }

    [Fact]
    public void The_import_runs_once()
    {
        Run();
        var again = Run();
        Assert.True(again.AlreadyDone);
        Assert.Equal(3, _store.Counts().Observations);
    }

    [Fact]
    public void Rebuilding_after_the_import_matches()
    {
        Run();
        var check = _store.CheckRebuild();
        Assert.True(check.Matches, string.Join(", ", check.Differences));
        Assert.Equal(3, check.Observations);
    }

    [Fact]
    public void The_time_comes_from_the_file_name()
    {
        var t = ConfirmedImport.TakenAt("Capture 2026.11.10 - 11.05.25.99.png", Zone);
        Assert.Equal(new DateTimeOffset(2026, 11, 10, 11, 5, 25, 990, TimeSpan.FromHours(-5)), t);
    }
}
