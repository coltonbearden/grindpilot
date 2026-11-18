using System.Globalization;
using System.Security.Cryptography;

namespace GrindPilot.Core;

public enum DataMoveOutcome
{
    /// <summary><c>GRINDPILOT_DATA</c> is set: the folders under local app data are left alone.</summary>
    Skipped,
    /// <summary>There is no pre-0.2.0 folder, or the move already happened.</summary>
    NothingToMove,
    /// <summary>The old folder was copied, every file's hash matched, and the copy became the data folder.</summary>
    Moved,
    /// <summary>The copy failed or a hash didn't match: the old folder is untouched and stays in use for this start.</summary>
    Failed,
    /// <summary>Both folders exist and the new one carries no move record: neither is touched.</summary>
    BothPresent,
}

public sealed record DataMoveResult(DataMoveOutcome Outcome, string Detail, int Files = 0, long Bytes = 0)
{
    public string Describe() => Outcome switch
    {
        DataMoveOutcome.Moved => $"data folder moved: {Files} files, {Bytes} bytes, every hash matched; {Detail}",
        _ => $"data folder: {Outcome}: {Detail}",
    };
}

/// <summary>
/// Moves the pre-0.2.0 data folder <c>%LOCALAPPDATA%\mw4-grind\</c> to <c>%LOCALAPPDATA%\GrindPilot-data\</c>, once, at
/// start, and only when the new folder doesn't exist yet. It copies into a temporary sibling, checks every file's SHA-256
/// against the original, renames the copy into place, writes a move record, and then renames the old folder to
/// <c>mw4-grind.moved-&lt;yyyyMMdd&gt;</c>. A failed copy or a hash that doesn't match removes the temporary copy and
/// leaves the old folder exactly as it was. Deleting the renamed old folder is left to a later release, once the new
/// folder has been in use for 14 days. Runs before anything else opens a file in either folder.
/// </summary>
public static class DataFolderMove
{
    public const string RecordFileName = "moved-from.txt";
    private const string MutexName = @"Local\GrindPilot.data-move";

    /// <summary>Copies one file. Tests swap it to make a copy fail or come out different.</summary>
    public delegate void FileCopier(string source, string destination);

    public static DataMoveResult RunForThisUser(DateTime? today = null)
    {
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(AppPaths.DataOverrideVariable)))
        {
            return new DataMoveResult(DataMoveOutcome.Skipped, $"{AppPaths.DataOverrideVariable} is set");
        }
        using var mutex = new Mutex(false, MutexName);
        var held = false;
        try
        {
            try
            {
                held = mutex.WaitOne(TimeSpan.FromSeconds(60));
            }
            catch (AbandonedMutexException)
            {
                held = true;
            }
            if (!held)
            {
                return new DataMoveResult(DataMoveOutcome.Failed, "another start held the move for 60 s");
            }
            return Run(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), today ?? DateTime.Now);
        }
        finally
        {
            if (held)
            {
                mutex.ReleaseMutex();
            }
        }
    }

    public static DataMoveResult Run(string localAppData, DateTime today, FileCopier? copier = null)
    {
        copier ??= (s, d) => File.Copy(s, d, overwrite: false);
        var legacy = Path.Combine(localAppData, AppPaths.LegacyDataFolderName);
        var current = Path.Combine(localAppData, AppPaths.DataFolderName);

        if (!Directory.Exists(legacy))
        {
            return new DataMoveResult(DataMoveOutcome.NothingToMove, "no pre-0.2.0 folder");
        }
        if (Directory.Exists(current))
        {
            // A move whose last step (renaming the old folder) failed: the new folder says where it came from.
            if (File.Exists(Path.Combine(current, RecordFileName)))
            {
                return RenameOld(legacy, today, "copied at an earlier start; ");
            }
            return new DataMoveResult(DataMoveOutcome.BothPresent, "both folders exist and the new one has no move record; neither touched");
        }

        var temp = Path.Combine(localAppData, $"{AppPaths.DataFolderName}.moving-{Guid.NewGuid():N}");
        int files = 0;
        long bytes = 0;
        try
        {
            Directory.CreateDirectory(temp);
            foreach (var dir in Directory.EnumerateDirectories(legacy, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(Path.Combine(temp, Path.GetRelativePath(legacy, dir)));
            }
            foreach (var file in Directory.EnumerateFiles(legacy, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(legacy, file);
                var target = Path.Combine(temp, relative);
                copier(file, target);
                if (!HashOf(file).AsSpan().SequenceEqual(HashOf(target)))
                {
                    throw new IOException($"the copy of {relative} doesn't match the original");
                }
                files++;
                bytes += new FileInfo(file).Length;
            }
            File.WriteAllText(Path.Combine(temp, RecordFileName),
                $"from {legacy}{Environment.NewLine}on {today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}{Environment.NewLine}files {files}, bytes {bytes}, every SHA-256 matched{Environment.NewLine}");
            Directory.Move(temp, current);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            TryDelete(temp);
            return new DataMoveResult(DataMoveOutcome.Failed, $"{ex.GetType().Name}: {ex.Message}; the old folder is untouched and stays in use");
        }

        return RenameOld(legacy, today, "") with { Files = files, Bytes = bytes };
    }

    private static DataMoveResult RenameOld(string legacy, DateTime today, string prefix)
    {
        var target = $"{legacy}.moved-{today.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}";
        for (var n = 2; Directory.Exists(target) || File.Exists(target); n++)
        {
            target = $"{legacy}.moved-{today.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}-{n}";
        }
        try
        {
            Directory.Move(legacy, target);
            return new DataMoveResult(DataMoveOutcome.Moved, $"{prefix}the old folder is now {Path.GetFileName(target)}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new DataMoveResult(DataMoveOutcome.Moved, $"{prefix}the old folder couldn't be renamed yet ({ex.GetType().Name}); the next start tries again");
        }
    }

    private static byte[] HashOf(string path)
    {
        using var s = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return SHA256.HashData(s);
    }

    private static void TryDelete(string dir)
    {
        try
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }
}
