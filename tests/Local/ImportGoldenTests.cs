using GrindPilot.Core;
using GrindPilot.Core.Catalog;
using GrindPilot.Core.Import;
using GrindPilot.Core.Store;

namespace GrindPilot.Local.Tests;

/// <summary>
/// Each import pair in the maintainer's test library (<see cref="TestLibrary"/>) is valid against the current catalog
/// and rebuilds cleanly in a scratch store. Prints counts only, never a value. Skips when the library holds no pair.
/// </summary>
public sealed class ImportGoldenTests
{
    private static readonly string Repo = ShippedData.FindRepoRoot(AppContext.BaseDirectory);

    [Fact]
    public void Every_import_in_the_test_library_is_valid_and_rebuilds()
    {
        var pairs = TestLibrary.Imports();
        Assert.SkipWhen(pairs.Count == 0, $"No import pair in {TestLibrary.Root}");

        var catalog = CatalogLoader.LoadFolder(Path.Combine(Repo, "catalog"));
        foreach (var (map, samples) in pairs)
        {
            var import = ConfirmedImport.Load(map, samples);
            Assert.Empty(import.Validate());

            var dir = Path.Combine(Path.GetTempPath(), "grindpilot-golden", Guid.NewGuid().ToString("N"));
            try
            {
                using (var store = AccountStore.Open(Path.Combine(dir, "state.db"), catalog))
                {
                    var r = import.Run(store, @"C:\Users\someone");
                    Assert.False(r.AlreadyDone);
                    Assert.True(r.Observations > 0, $"{Path.GetFileName(map)} imported nothing");
                    var check = store.CheckRebuild();
                    Assert.True(check.Matches, $"{Path.GetFileName(map)}: rebuild differs on {check.Differences.Count} keys");
                    Assert.Equal(r.Observations, check.Observations);
                }
            }
            finally
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                try
                {
                    Directory.Delete(dir, true);
                }
                catch (IOException)
                {
                }
            }
        }
    }
}
