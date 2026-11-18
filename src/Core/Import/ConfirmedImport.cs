using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using GrindPilot.Core.Store;

namespace GrindPilot.Core.Import;

public sealed record ImportMapEntry(int N, IReadOnlyList<(string Key, string Pattern)> Set, string? Skip);

public sealed record ImportResult(bool AlreadyDone, int Observations, int Captures, int FieldsImported, int FieldsSkipped);

/// <summary>
/// Imports a set of values the user confirmed by eye against their own screenshots: a samples file (each screenshot's
/// file name, screen and SHA-256, and each confirmed field) and a map from those fields to account keys. It reads only
/// those two JSON files: it never opens, reads or hashes a screenshot. Each value is stored as <c>confirmed</c>, with the
/// map's note. Nothing ships with the app: a public install starts with an empty account. The tests use an invented
/// pair in <c>tests/fixtures/</c>; a maintainer's own pair lives outside the repo, in the data folder's
/// <c>test-library\</c>.
/// </summary>
public sealed class ConfirmedImport
{
    private static readonly Regex FileTime = new(
        @"(\d{4})\.(\d{2})\.(\d{2}) - (\d{2})\.(\d{2})\.(\d{2})\.(\d{2})",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public string ImportId { get; }
    public string CapturesFolder { get; }
    public string Note { get; }
    public IReadOnlyList<ImportMapEntry> Map { get; }

    private readonly Dictionary<string, (string Sha256, string File, string? Screen)> _samples;
    private readonly Dictionary<int, (string Sample, string Confirmed, string Field)> _fields;

    private ConfirmedImport(string id, string folder, string note, List<ImportMapEntry> map,
        Dictionary<string, (string, string, string?)> samples, Dictionary<int, (string, string, string)> fields)
    {
        ImportId = id;
        CapturesFolder = folder;
        Note = note;
        Map = map;
        _samples = samples;
        _fields = fields;
    }

    public static ConfirmedImport Load(string mapPath, string samplesPath)
    {
        using var mapDoc = JsonDocument.Parse(File.ReadAllText(mapPath));
        using var samplesDoc = JsonDocument.Parse(File.ReadAllText(samplesPath));
        var m = mapDoc.RootElement;

        var samples = new Dictionary<string, (string, string, string?)>(StringComparer.Ordinal);
        foreach (var s in samplesDoc.RootElement.GetProperty("samples").EnumerateArray())
        {
            samples[s.GetProperty("id").GetString()!] = (
                s.GetProperty("sha256").GetString()!,
                s.GetProperty("file").GetString()!,
                s.GetProperty("screen").ValueKind == JsonValueKind.String ? s.GetProperty("screen").GetString() : null);
        }
        var fields = new Dictionary<int, (string, string, string)>();
        foreach (var f in samplesDoc.RootElement.GetProperty("fields").EnumerateArray())
        {
            fields[f.GetProperty("n").GetInt32()] = (
                f.GetProperty("sample").GetString()!,
                f.GetProperty("confirmed").GetString()!,
                f.GetProperty("field").GetString()!);
        }

        var map = new List<ImportMapEntry>();
        foreach (var e in m.GetProperty("fields").EnumerateArray())
        {
            var set = new List<(string, string)>();
            if (e.TryGetProperty("set", out var setEl))
            {
                foreach (var k in setEl.EnumerateArray())
                {
                    set.Add((k.GetProperty("key").GetString()!,
                        k.TryGetProperty("pattern", out var p) ? p.GetString()! : "^(.*)$"));
                }
            }
            map.Add(new ImportMapEntry(
                e.GetProperty("n").GetInt32(),
                set,
                e.TryGetProperty("skip", out var skip) ? skip.GetString() : null));
        }

        return new ConfirmedImport(
            m.GetProperty("import").GetString()!,
            m.GetProperty("capturesFolder").GetString()!,
            m.GetProperty("note").GetString()!,
            map,
            samples,
            fields);
    }

    /// <summary>
    /// Every confirmed field is in the map exactly once, either with the keys it sets or with the reason it isn't
    /// imported; every pattern matches its confirmed value. Returns what is wrong, or nothing.
    /// </summary>
    public IReadOnlyList<string> Validate()
    {
        var problems = new List<string>();
        foreach (var n in _fields.Keys.Order())
        {
            var count = Map.Count(e => e.N == n);
            if (count != 1)
            {
                problems.Add($"field {n} is in the map {count} times");
            }
        }
        foreach (var e in Map)
        {
            if (!_fields.TryGetValue(e.N, out var f))
            {
                problems.Add($"map names field {e.N}, which isn't among the confirmed values");
                continue;
            }
            if (e.Set.Count == 0 && string.IsNullOrWhiteSpace(e.Skip))
            {
                problems.Add($"field {e.N} sets no key and gives no reason");
            }
            if (e.Set.Count > 0 && e.Skip is not null)
            {
                problems.Add($"field {e.N} both sets keys and is skipped");
            }
            foreach (var (key, pattern) in e.Set)
            {
                if (Extract(pattern, f.Confirmed) is null)
                {
                    problems.Add($"field {e.N}: the pattern for {key} doesn't match its confirmed value");
                }
            }
        }
        return problems;
    }

    /// <summary>Runs once: a store that already holds this import is left as it is.</summary>
    public ImportResult Run(AccountStore store, string userProfile, TimeZoneInfo? zone = null)
    {
        var doneKey = "import." + ImportId;
        if (store.GetMeta(doneKey) is not null)
        {
            return new ImportResult(true, 0, 0, 0, 0);
        }
        var problems = Validate();
        if (problems.Count > 0)
        {
            throw new InvalidDataException("The import map is wrong: " + string.Join("; ", problems));
        }

        var schema = new KeySchemaCheck(store);
        var plan = new List<(string Sample, string Key, string Value)>();
        foreach (var e in Map.Where(e => e.Set.Count > 0).OrderBy(e => e.N))
        {
            var f = _fields[e.N];
            foreach (var (key, pattern) in e.Set)
            {
                plan.Add((f.Sample, key, Extract(pattern, f.Confirmed)!));
            }
        }
        // Check every value against the store's rules before writing any, so a refusal leaves the store untouched.
        foreach (var (_, key, value) in plan)
        {
            schema.Check(key, value);
        }

        var captures = new Dictionary<string, long>(StringComparer.Ordinal);
        foreach (var (sample, key, value) in plan)
        {
            var s = _samples[sample];
            var takenAt = TakenAt(s.File, zone ?? TimeZoneInfo.Local);
            if (!captures.TryGetValue(sample, out var captureId))
            {
                captureId = store.AddCapture(new NewCapture(
                    CaptureKind.Screenshot,
                    s.Sha256,
                    Path.Combine(userProfile, CapturesFolder, s.File),
                    takenAt,
                    Source: $"import {ImportId}, sample {sample}"));
                captures[sample] = captureId;
            }
            store.AddObservation(new NewObservation(
                captureId,
                s.Screen ?? sample,
                "import-" + ImportId,
                key,
                value,
                Outcome.Confirmed,
                takenAt,
                Note: Note));
        }

        store.SetMeta(doneKey, AccountStore.Iso(DateTimeOffset.UtcNow));
        return new ImportResult(false, plan.Count, captures.Count, Map.Count(e => e.Set.Count > 0), Map.Count(e => e.Skip is not null));
    }

    /// <summary>The time in a screenshot's file name, which is the PC's local time when it was taken.</summary>
    public static DateTimeOffset TakenAt(string fileName, TimeZoneInfo zone)
    {
        var m = FileTime.Match(fileName);
        if (!m.Success)
        {
            throw new InvalidDataException($"No time in the file name '{fileName}'.");
        }
        int G(int i) => int.Parse(m.Groups[i].Value, CultureInfo.InvariantCulture);
        var local = new DateTime(G(1), G(2), G(3), G(4), G(5), G(6), G(7) * 10, DateTimeKind.Unspecified);
        return new DateTimeOffset(local, zone.GetUtcOffset(local));
    }

    private static string? Extract(string pattern, string confirmed)
    {
        var m = Regex.Match(confirmed, pattern, RegexOptions.CultureInvariant);
        return m.Success && m.Groups.Count > 1 ? m.Groups[1].Value : null;
    }

    /// <summary>Checks a value the way the store will, without writing.</summary>
    private sealed class KeySchemaCheck(AccountStore store)
    {
        public void Check(string key, string value) => store.Schema.Normalize(key, value, Outcome.Confirmed);
    }
}
