using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace GrindPilot.Tools.CatalogSeed;

/// <summary>One of Activision's posts, as Steam's news feed carries it.</summary>
public sealed record OfficialPost(string Tag, string Title, string FeedId, DateOnly Date, string Raw)
{
    /// <summary>The post as plain lines: markup removed, one block per line.</summary>
    public IReadOnlyList<string> Lines => PostText.Lines(Raw);

    public string Plain => string.Join('\n', Lines);
}

/// <summary>
/// Reads the official posts from Steam's public news API for app 1938090, the feed Activision itself publishes
/// (sources.md). Only the <c>steam_community_announcements</c> feed counts; the feed's third-party items are skipped.
/// Copies are kept only in the git-ignored <c>.scratch\posts\</c>.
/// </summary>
public static class OfficialPosts
{
    public const string FeedUrl = "https://api.steampowered.com/ISteamNews/GetNewsForApp/v2/?appid=1938090&count=300&maxlength=0&format=json";
    public const string AnnouncementsFeed = "steam_community_announcements";

    /// <summary>The posts the catalog cites, by tag and exact title (sources.md).</summary>
    public static readonly IReadOnlyDictionary<string, string> Titles = new Dictionary<string, string>
    {
        ["P-PROG"] = "Modern Warfare 4 Progression and Prestige",
        ["P-LOAD"] = "Modern Warfare 4 Operators, Weapons, Equipment, and Multiplayer Loadout Intel",
        ["P-S1"] = "Call of Duty: Modern Warfare 4 and Warzone - Season 1",
        ["P-WZ"] = "Call of Duty: Warzone Season 1: Everything You Need to Know",
        ["P-BP"] = "Modern Warfare 4 Season 1 Battle Pass System, BlackCell, and New Store Bundles",
        ["P-LIVE"] = "Call of Duty: Modern Warfare 4 is Live Worldwide — Play Now!",
    };

    public static async Task<IReadOnlyDictionary<string, OfficialPost>> FetchAsync(HttpClient http, string? cacheFolder = null)
    {
        var feed = await http.GetFromJsonAsync<Feed>(FeedUrl).ConfigureAwait(false)
            ?? throw new InvalidDataException("Steam's news feed returned nothing.");
        var posts = new Dictionary<string, OfficialPost>(StringComparer.Ordinal);
        foreach (var (tag, title) in Titles)
        {
            var item = feed.AppNews.NewsItems.FirstOrDefault(i => i.FeedName == AnnouncementsFeed && i.Title == title)
                ?? throw new InvalidDataException($"{tag} ('{title}') isn't in Steam's announcements feed.");
            var post = new OfficialPost(tag, item.Title, item.Gid,
                DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeSeconds(item.Date).UtcDateTime), item.Contents);
            posts[tag] = post;
            if (cacheFolder is not null)
            {
                Directory.CreateDirectory(cacheFolder);
                await File.WriteAllTextAsync(Path.Combine(cacheFolder, $"{tag}.txt"), item.Contents).ConfigureAwait(false);
            }
        }
        return posts;
    }

    private sealed record Feed([property: JsonPropertyName("appnews")] AppNews AppNews);

    private sealed record AppNews([property: JsonPropertyName("newsitems")] List<NewsItem> NewsItems);

    private sealed record NewsItem(
        [property: JsonPropertyName("gid")] string Gid,
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("feedname")] string FeedName,
        [property: JsonPropertyName("date")] long Date,
        [property: JsonPropertyName("contents")] string Contents);
}

/// <summary>Turns Steam's post markup into plain text and tables.</summary>
public static partial class PostText
{
    [GeneratedRegex(@"\[/?(p|b|i|u|h1|h2|h3|list|olist|\*|tr|td|th|table)(=[^\]]*)?\]", RegexOptions.CultureInvariant)]
    private static partial Regex BlockTags();

    [GeneratedRegex(@"\[img[^\]]*\].*?\[/img\]", RegexOptions.CultureInvariant | RegexOptions.Singleline)]
    private static partial Regex Images();

    [GeneratedRegex(@"\[/?[a-z0-9]+(=[^\]]*)?[^\]]*\]", RegexOptions.CultureInvariant)]
    private static partial Regex OtherTags();

    [GeneratedRegex(@"(?s)\[table[^\]]*\](.*?)\[/table\]", RegexOptions.CultureInvariant)]
    private static partial Regex Tables();

    [GeneratedRegex(@"(?s)\[tr[^\]]*\](.*?)\[/tr\]", RegexOptions.CultureInvariant)]
    private static partial Regex Rows();

    [GeneratedRegex(@"(?s)\[(td|th)[^\]]*\](.*?)\[/\1\]", RegexOptions.CultureInvariant)]
    private static partial Regex Cells();

    public static IReadOnlyList<string> Lines(string raw)
    {
        // Steam escapes a bracket that is text, not markup, as \[ (P-S1: "Levels: \[CLASSIFIED]").
        var text = raw.Replace(@"\[", "\u0001", StringComparison.Ordinal).Replace(@"\]", "\u0002", StringComparison.Ordinal);
        text = Images().Replace(text, "");
        text = BlockTags().Replace(text, "\n");
        text = OtherTags().Replace(text, "");
        text = text.Replace('\u0001', '[').Replace('\u0002', ']');
        text = System.Net.WebUtility.HtmlDecode(text);
        return text.Split('\n')
            .Select(l => Regex.Replace(l, @"[ \t ]+", " ").Trim())
            .Where(l => l.Length > 0)
            .ToList();
    }

    public static string Cell(string raw) => string.Join(" ", Lines(raw));

    /// <summary>Every table in the post, as rows of cell texts.</summary>
    public static IReadOnlyList<IReadOnlyList<IReadOnlyList<string>>> TablesOf(string raw) =>
        Tables().Matches(raw)
            .Select(t => (IReadOnlyList<IReadOnlyList<string>>)Rows().Matches(t.Groups[1].Value)
                .Select(r => (IReadOnlyList<string>)Cells().Matches(r.Groups[1].Value).Select(c => Cell(c.Groups[2].Value)).ToList())
                .ToList())
            .ToList();

    /// <summary>Text compared loosely: case, spacing and typographic quotes don't count.</summary>
    public static string Loose(string s) =>
        Regex.Replace(s.Replace('’', '\'').Replace('“', '"').Replace('”', '"').ToLowerInvariant(), @"\s+", " ").Trim();
}
