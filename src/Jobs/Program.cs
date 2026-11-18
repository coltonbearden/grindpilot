using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using GrindPilot.Core;
using GrindPilot.Core.Catalog;
using GrindPilot.Core.Store;
using GrindPilot.Windows;
using Velopack;
using Velopack.Logging;
using Velopack.Sources;

// GrindPilot.Jobs: checks a person or CI can run, and (from stage 8) the scheduled jobs. Output is counts and
// versions only: no value from the store, and never a token.
//
//   GrindPilot.Jobs check-store [--store <state.db>]   rebuild the current state from observations, on a copy
//   GrindPilot.Jobs check-feed [--prerelease]          read the update repo's releases, download the newest package
//                                                      and check its SHA256 against the feed; a token is used only
//                                                      when GRINDPILOT_RELEASES_TOKEN is set (a private repo), never
//                                                      from the command line

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: GrindPilot.Jobs check-store [--store <path>] | check-feed [--prerelease]");
    return 2;
}

// Before anything reads the data folder: move a pre-0.2.0 folder into place, once (DataFolderMove).
if (args[0] == "check-store" && !args.Contains("--store"))
{
    var move = DataFolderMove.RunForThisUser();
    if (move.Outcome is DataMoveOutcome.Moved or DataMoveOutcome.Failed or DataMoveOutcome.BothPresent)
    {
        Console.WriteLine(move.Describe());
    }
}

return args[0] switch
{
    "check-store" => CheckStore(args.SkipWhile(a => a != "--store").Skip(1).FirstOrDefault()),
    "check-feed" => await CheckFeed(args.Contains("--prerelease")),
    _ => Usage(),
};

static int Usage()
{
    Console.Error.WriteLine("Unknown command.");
    return 2;
}

static int CheckStore(string? storePath)
{
    var source = storePath ?? AppPaths.Default().StateDb;
    if (!File.Exists(source))
    {
        Console.WriteLine($"No store at {source}");
        return 1;
    }
    var catalog = CatalogLoader.LoadFolder(Path.Combine(AppContext.BaseDirectory, "data", "catalog"));

    // Work on a consistent copy, made with SQLite's backup API, so the running app's store is only read.
    var copy = Path.Combine(Path.GetTempPath(), $"grindpilot-check-{Guid.NewGuid():N}.db");
    try
    {
        using (var from = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = source, Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString()))
        using (var to = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = copy, Pooling = false }.ToString()))
        {
            from.Open();
            to.Open();
            from.BackupDatabase(to);
        }
        using var store = AccountStore.Open(copy, catalog, Path.Combine(Path.GetTempPath(), "grindpilot-check-backups"));
        var counts = store.Counts();
        var check = store.CheckRebuild();
        Console.WriteLine($"store: {source}");
        Console.WriteLine($"captures {counts.Captures}, observations {counts.Observations}, keys {counts.Keys} (known {counts.KnownKeys}, stale {counts.StaleKeys}, held {counts.HeldKeys})");
        Console.WriteLine($"rebuilt from observations: {check.Keys} keys from {check.Observations} observations");
        Console.WriteLine(check.Matches ? "RESULT: the rebuilt state matches the store's current state" : $"RESULT: differs on {check.Differences.Count} keys");
        return check.Matches ? 0 : 1;
    }
    finally
    {
        SqliteConnection.ClearAllPools();
        File.Delete(copy);
    }
}

static async Task<int> CheckFeed(bool prerelease)
{
    var token = Environment.GetEnvironmentVariable("GRINDPILOT_RELEASES_TOKEN") is { Length: > 0 } t ? t : null;
    Console.WriteLine($"update repo: {UpdateService.RepoUrl}; {(token is null ? "no token" : "with a token")}{(prerelease ? "; pre-releases included" : "")}");
    var source = new GithubSource(UpdateService.RepoUrl, token, prerelease);
    var log = new NullVelopackLogger();
    var feed = await source.GetReleaseFeed(log, "GrindPilot", "win");
    var full = feed.Assets.Where(a => a.Type == VelopackAssetType.Full).OrderByDescending(a => a.Version).FirstOrDefault();
    if (full is null)
    {
        Console.WriteLine("RESULT: no full release in the feed");
        return 1;
    }
    var file = Path.Combine(Path.GetTempPath(), $"grindpilot-feed-{Guid.NewGuid():N}.nupkg");
    try
    {
        await source.DownloadReleaseEntry(log, full, file, _ => { }, CancellationToken.None);
        var size = new FileInfo(file).Length;
        string sha;
        await using (var s = File.OpenRead(file))
        {
            sha = Convert.ToHexString(await SHA256.HashDataAsync(s));
        }
        var ok = size == full.Size && string.Equals(sha, full.SHA256, StringComparison.OrdinalIgnoreCase);
        Console.WriteLine($"feed: {feed.Assets.Length} assets; newest full release {full.Version} ({full.FileName}, {full.Size} bytes)");
        Console.WriteLine(ok ? "RESULT: downloaded; size and SHA256 match the feed" : "RESULT: the download doesn't match the feed");
        return ok ? 0 : 1;
    }
    finally
    {
        File.Delete(file);
    }
}
