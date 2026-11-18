using System.Text;
using System.Text.Json;

namespace GrindPilot.Core.Catalog;

/// <summary>One fact about a catalog entry, with its source tags. A fact two official posts state differently has two values: a conflict.</summary>
public sealed record FactValue(string Value, IReadOnlyList<string> Sources, string? Note);

public sealed record Fact(string Name, IReadOnlyList<FactValue> Values)
{
    public bool IsConflict => Values.Count > 1;
}

/// <summary>A game thing the readers may accept: a weapon, a camo, a perk. Every entry carries its sources (03-account-state.md, 3.5).</summary>
public sealed record CatalogEntry(
    string Kind,
    string Name,
    string Slug,
    IReadOnlyList<string> Sources,
    IReadOnlyList<string> Shown,
    IReadOnlyDictionary<string, Fact> Facts)
{
    public bool HasConflict => Facts.Values.Any(f => f.IsConflict);

    public Fact? FactNamed(string name) => Facts.TryGetValue(name, out var f) ? f : null;
}

/// <summary>An official post (<c>P-…</c>), or an in-game screen a maintainer confirmed by eye (<c>S…</c>; no picture published).</summary>
public sealed record SourceInfo(string Tag, string Title, string? Date, string? Url, string? FeedId);

/// <summary>A season, from the official posts, so player level can be kept per season (3.6).</summary>
public sealed record SeasonInfo(string Name, DateOnly Starts, IReadOnlyList<string> Sources);

/// <summary>
/// The closed list of game things, loaded from <c>catalog/*.json</c>. Names come only from official posts (<c>P-…</c>)
/// or in-game screens confirmed by eye (<c>S…</c>); nothing comes from a third-party list.
/// </summary>
public sealed class GameCatalog
{
    private readonly Dictionary<string, List<CatalogEntry>> _byKind;
    private readonly Dictionary<(string Kind, string Folded), CatalogEntry> _byFoldedName;
    private readonly Dictionary<(string Kind, string Slug), CatalogEntry> _bySlug;

    public IReadOnlyList<CatalogEntry> Entries { get; }
    public IReadOnlyDictionary<string, SourceInfo> SourceTags { get; }
    public IReadOnlyList<SeasonInfo> Seasons { get; }

    public GameCatalog(IEnumerable<CatalogEntry> entries, IEnumerable<SourceInfo> sources, IEnumerable<SeasonInfo> seasons)
    {
        Entries = entries.ToList();
        SourceTags = sources.ToDictionary(s => s.Tag, StringComparer.Ordinal);
        Seasons = seasons.OrderBy(s => s.Starts).ToList();
        _byKind = Entries.GroupBy(e => e.Kind).ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);
        _byFoldedName = new();
        _bySlug = new();
        foreach (var e in Entries)
        {
            foreach (var spelling in e.Shown.Prepend(e.Name))
            {
                // The catalog's own spellings are upper-cased first: their lower-case l is a real L. Only text read
                // from a screen gets the l-for-I look-alike.
                _byFoldedName.TryAdd((e.Kind, Fold(spelling.ToUpperInvariant())), e);
            }
            if (!_bySlug.TryAdd((e.Kind, e.Slug), e))
            {
                throw new InvalidDataException($"Catalog has two {e.Kind} entries with the slug '{e.Slug}'.");
            }
        }
    }

    public IReadOnlyList<string> Kinds => _byKind.Keys.Order(StringComparer.Ordinal).ToList();

    public IReadOnlyList<CatalogEntry> OfKind(string kind) =>
        _byKind.TryGetValue(kind, out var list) ? list : [];

    /// <summary>
    /// Matches a name as shown on a screen. Case, spacing and the look-alike characters of 4.6 don't count. A lower-case
    /// l in the text read may be a real L (mixed-case labels such as "Assault Rifle") or a misread I, so both are tried.
    /// </summary>
    public CatalogEntry? Match(string kind, string shown)
    {
        if (_byFoldedName.TryGetValue((kind, Fold(shown.ToUpperInvariant())), out var e))
        {
            return e;
        }
        return _byFoldedName.TryGetValue((kind, Fold(shown)), out e) ? e : null;
    }

    public CatalogEntry? BySlug(string kind, string slug) =>
        _bySlug.TryGetValue((kind, slug), out var e) ? e : null;

    /// <summary>The season an observation falls in, or null before the first known season.</summary>
    public SeasonInfo? SeasonAt(DateTimeOffset when)
    {
        var day = DateOnly.FromDateTime(when.Date);
        SeasonInfo? current = null;
        foreach (var s in Seasons)
        {
            if (s.Starts <= day)
            {
                current = s;
            }
        }
        return current;
    }

    /// <summary>
    /// Folds a name for matching: upper case, single spaces, and the look-alike pairs O/0, I/1/l, S/5, B/8 counted as equal
    /// (04-capture-and-readers.md, 4.6). Any other difference is a non-match.
    /// </summary>
    public static string Fold(string text)
    {
        var sb = new StringBuilder(text.Length);
        var lastWasSpace = true;
        foreach (var raw in text.Trim())
        {
            var c = raw == 'l' ? 'I' : char.ToUpperInvariant(raw);
            c = c switch
            {
                '0' => 'O',
                '1' => 'I',
                '5' => 'S',
                '8' => 'B',
                _ => c,
            };
            if (char.IsWhiteSpace(c))
            {
                if (!lastWasSpace)
                {
                    sb.Append(' ');
                }
                lastWasSpace = true;
                continue;
            }
            sb.Append(c);
            lastWasSpace = false;
        }
        return sb.ToString().TrimEnd();
    }

    /// <summary>A key-safe form of a name: lower case letters and digits, everything else one hyphen.</summary>
    public static string Slugify(string name)
    {
        var sb = new StringBuilder(name.Length);
        foreach (var raw in name.Trim())
        {
            var c = char.ToLowerInvariant(raw);
            if (c is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                sb.Append(c);
            }
            else if (sb.Length > 0 && sb[^1] != '-')
            {
                sb.Append('-');
            }
        }
        return sb.ToString().Trim('-');
    }
}

/// <summary>Reads the catalog folder: <c>sources.json</c>, <c>seasons.json</c> and one file per kind.</summary>
public static class CatalogLoader
{
    public static GameCatalog LoadFolder(string folder)
    {
        if (!Directory.Exists(folder))
        {
            throw new DirectoryNotFoundException($"Catalog folder not found: {folder}");
        }

        var sources = new List<SourceInfo>();
        var seasons = new List<SeasonInfo>();
        var entries = new List<CatalogEntry>();

        foreach (var file in Directory.EnumerateFiles(folder, "*.json").Order(StringComparer.Ordinal))
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(file), new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });
            var root = doc.RootElement;
            var name = Path.GetFileName(file);
            if (name == "sources.json")
            {
                foreach (var s in root.GetProperty("sources").EnumerateArray())
                {
                    sources.Add(new SourceInfo(
                        s.GetProperty("tag").GetString()!,
                        s.GetProperty("title").GetString()!,
                        OptString(s, "date"),
                        OptString(s, "url"),
                        OptString(s, "feedId")));
                }
                if (root.TryGetProperty("screens", out var screens))
                {
                    foreach (var s in screens.EnumerateArray())
                    {
                        sources.Add(new SourceInfo(s.GetProperty("tag").GetString()!, s.GetProperty("title").GetString()!, null, null, null));
                    }
                }
                continue;
            }
            if (name == "seasons.json")
            {
                foreach (var s in root.GetProperty("seasons").EnumerateArray())
                {
                    seasons.Add(new SeasonInfo(
                        s.GetProperty("name").GetString()!,
                        DateOnly.Parse(s.GetProperty("starts").GetString()!, System.Globalization.CultureInfo.InvariantCulture),
                        Strings(s, "sources")));
                }
                continue;
            }

            var kind = root.GetProperty("kind").GetString()!;
            foreach (var e in root.GetProperty("entries").EnumerateArray())
            {
                var entryName = e.GetProperty("name").GetString()!;
                var facts = new Dictionary<string, Fact>(StringComparer.Ordinal);
                if (e.TryGetProperty("facts", out var factsEl))
                {
                    foreach (var f in factsEl.EnumerateObject())
                    {
                        var values = new List<FactValue>();
                        foreach (var v in f.Value.EnumerateArray())
                        {
                            var value = v.GetProperty("value");
                            values.Add(new FactValue(
                                value.ValueKind == JsonValueKind.String ? value.GetString()! : value.GetRawText(),
                                Strings(v, "sources"),
                                OptString(v, "note")));
                        }
                        facts[f.Name] = new Fact(f.Name, values);
                    }
                }
                entries.Add(new CatalogEntry(
                    kind,
                    entryName,
                    OptString(e, "slug") ?? GameCatalog.Slugify(entryName),
                    Strings(e, "sources"),
                    e.TryGetProperty("shown", out _) ? Strings(e, "shown") : [],
                    facts));
            }
        }

        return new GameCatalog(entries, sources, seasons);
    }

    private static string? OptString(JsonElement el, string prop) =>
        el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static IReadOnlyList<string> Strings(JsonElement el, string prop) =>
        el.TryGetProperty(prop, out var v) ? v.EnumerateArray().Select(x => x.GetString()!).ToList() : [];
}
