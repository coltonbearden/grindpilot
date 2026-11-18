using System.Security.Cryptography;

namespace GrindPilot.Core.Tests;

/// <summary>
/// The one-time move of the pre-0.2.0 data folder <c>mw4-grind\</c> to <c>GrindPilot-data\</c>, in a scratch
/// local-app-data folder: the move with every hash, a second start, a failed copy, a copy that comes out different,
/// a new folder already there, and an old folder whose rename failed.
/// </summary>
public sealed class DataFolderMoveTests : IDisposable
{
    private static readonly DateTime Today = new(2026, 11, 18);
    private readonly string _local = Path.Combine(Path.GetTempPath(), "grindpilot-move-tests", Guid.NewGuid().ToString("N"));

    private string Legacy => Path.Combine(_local, AppPaths.LegacyDataFolderName);
    private string Current => Path.Combine(_local, AppPaths.DataFolderName);
    private string Renamed => Path.Combine(_local, "mw4-grind.moved-20261118");

    public DataFolderMoveTests()
    {
        Directory.CreateDirectory(Path.Combine(Legacy, "backups"));
        Directory.CreateDirectory(Path.Combine(Legacy, "logs"));
        Directory.CreateDirectory(Path.Combine(Legacy, "test-library", "expected"));
        Directory.CreateDirectory(Path.Combine(Legacy, "empty-folder"));
        File.WriteAllBytes(Path.Combine(Legacy, "state.db"), RandomNumberGenerator.GetBytes(64 * 1024));
        File.WriteAllBytes(Path.Combine(Legacy, "state.db-wal"), RandomNumberGenerator.GetBytes(4096));
        File.WriteAllText(Path.Combine(Legacy, "settings.json"), """{ "monitor": 2 }""");
        File.WriteAllBytes(Path.Combine(Legacy, "backups", "state-20261117.db"), RandomNumberGenerator.GetBytes(64 * 1024));
        File.WriteAllText(Path.Combine(Legacy, "logs", "grindpilot-20261117.log"), "a log line");
        File.WriteAllText(Path.Combine(Legacy, "test-library", "expected", "samples-x.json"), "{}");
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_local, true);
        }
        catch (IOException)
        {
        }
    }

    private static Dictionary<string, string> Hashes(string root) =>
        Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .ToDictionary(f => Path.GetRelativePath(root, f), f => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(f))));

    private static List<string> Folders(string root) =>
        Directory.EnumerateDirectories(root, "*", SearchOption.AllDirectories).Select(d => Path.GetRelativePath(root, d)).Order().ToList();

    [Fact]
    public void The_move_copies_every_file_checks_each_hash_and_renames_the_old_folder()
    {
        var before = Hashes(Legacy);
        var folders = Folders(Legacy);

        var r = DataFolderMove.Run(_local, Today);

        Assert.Equal(DataMoveOutcome.Moved, r.Outcome);
        Assert.Equal(before.Count, r.Files);
        Assert.False(Directory.Exists(Legacy));
        Assert.True(Directory.Exists(Renamed));
        var after = Hashes(Current);
        Assert.True(after.Remove(DataFolderMove.RecordFileName));
        Assert.Equal(before, after);
        Assert.Equal(folders, Folders(Current));
        Assert.Equal(before, Hashes(Renamed));
        Assert.Contains("every SHA-256 matched", File.ReadAllText(Path.Combine(Current, DataFolderMove.RecordFileName)), StringComparison.Ordinal);
        Assert.Empty(Directory.EnumerateDirectories(_local, "*.moving-*"));
        Assert.Equal(Current, AppPaths.ResolveUnder(_local));
    }

    [Fact]
    public void A_second_start_changes_nothing()
    {
        DataFolderMove.Run(_local, Today);
        var current = Hashes(Current);
        var renamed = Hashes(Renamed);

        var again = DataFolderMove.Run(_local, Today.AddDays(1));

        Assert.Equal(DataMoveOutcome.NothingToMove, again.Outcome);
        Assert.Equal(current, Hashes(Current));
        Assert.Equal(renamed, Hashes(Renamed));
        Assert.Equal(2, Directory.EnumerateDirectories(_local).Count());
    }

    [Fact]
    public void A_failed_copy_leaves_the_old_folder_untouched_and_in_use()
    {
        var before = Hashes(Legacy);
        var copied = 0;

        var r = DataFolderMove.Run(_local, Today, (s, d) =>
        {
            if (++copied == 3)
            {
                throw new IOException("disk full (test)");
            }
            File.Copy(s, d);
        });

        Assert.Equal(DataMoveOutcome.Failed, r.Outcome);
        Assert.Equal(before, Hashes(Legacy));
        Assert.False(Directory.Exists(Current));
        Assert.Empty(Directory.EnumerateDirectories(_local, "*.moving-*"));
        Assert.Equal(Legacy, AppPaths.ResolveUnder(_local));
    }

    [Fact]
    public void A_copy_that_comes_out_different_is_a_failure_too()
    {
        var before = Hashes(Legacy);

        var r = DataFolderMove.Run(_local, Today, (s, d) =>
        {
            File.Copy(s, d);
            if (Path.GetFileName(s) == "state.db")
            {
                using var f = File.OpenWrite(d);
                f.WriteByte(0x42);
            }
        });

        Assert.Equal(DataMoveOutcome.Failed, r.Outcome);
        Assert.Contains("doesn't match", r.Detail, StringComparison.Ordinal);
        Assert.Equal(before, Hashes(Legacy));
        Assert.False(Directory.Exists(Current));
        Assert.Empty(Directory.EnumerateDirectories(_local, "*.moving-*"));
    }

    [Fact]
    public void A_new_folder_already_there_is_never_overwritten()
    {
        Directory.CreateDirectory(Current);
        File.WriteAllText(Path.Combine(Current, "settings.json"), "new");
        var before = Hashes(Legacy);

        var r = DataFolderMove.Run(_local, Today);

        Assert.Equal(DataMoveOutcome.BothPresent, r.Outcome);
        Assert.Equal(before, Hashes(Legacy));
        Assert.Equal("new", File.ReadAllText(Path.Combine(Current, "settings.json")));
        Assert.Equal(Current, AppPaths.ResolveUnder(_local));
    }

    [Fact]
    public void An_old_folder_whose_rename_failed_is_renamed_at_the_next_start()
    {
        DataFolderMove.Run(_local, Today);
        // As if the last step had failed: the old folder is back under its own name beside the new one.
        Directory.Move(Renamed, Legacy);

        var r = DataFolderMove.Run(_local, Today);

        Assert.Equal(DataMoveOutcome.Moved, r.Outcome);
        Assert.Contains("earlier start", r.Detail, StringComparison.Ordinal);
        Assert.False(Directory.Exists(Legacy));
        Assert.True(Directory.Exists(Renamed));
    }

    [Fact]
    public void Nothing_to_move_on_a_new_install()
    {
        Directory.Delete(Legacy, true);
        Assert.Equal(DataMoveOutcome.NothingToMove, DataFolderMove.Run(_local, Today).Outcome);
        Assert.False(Directory.Exists(Current));
        Assert.Equal(Current, AppPaths.ResolveUnder(_local));
    }

    [Fact]
    public void The_data_folder_is_never_spelled_like_the_program_folder() =>
        Assert.NotEqual("GrindPilot", AppPaths.DataFolderName, StringComparer.OrdinalIgnoreCase);
}
