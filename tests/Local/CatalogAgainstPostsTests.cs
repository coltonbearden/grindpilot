using System.Text.Json;
using GrindPilot.Core;
using GrindPilot.Tools.CatalogSeed;

namespace GrindPilot.Local.Tests;

/// <summary>
/// Every catalog entry, and every fact of it, is found in the source it cites: an official post (read from Steam's
/// feed, or the copy in .scratch\posts while it exists) or an in-game screen whose confirmed values are in the
/// maintainer's test library (<see cref="TestLibrary"/>; screen-sourced entries are skipped when it is absent). An entry
/// may give a <c>quote</c>, the exact words to look for when its name isn't written that way in the post.
/// </summary>
public sealed class CatalogAgainstPostsTests
{
    private static readonly string Repo = ShippedData.FindRepoRoot(AppContext.BaseDirectory);
    private static readonly Lazy<IReadOnlyDictionary<string, OfficialPost>> Posts = new(LoadPosts);
    private static readonly Lazy<Dictionary<string, List<string>>> Confirmed = new(() => TestLibrary.ConfirmedBySample(PostText.Loose));

    private static IReadOnlyDictionary<string, OfficialPost> LoadPosts()
    {
        using var http = new HttpClient();
        http.DefaultRequestHeaders.UserAgent.ParseAdd("grindpilot-local-suite");
        return OfficialPosts.FetchAsync(http).GetAwaiter().GetResult();
    }

    public static TheoryData<string> CatalogFiles() =>
        new(Directory.EnumerateFiles(Path.Combine(Repo, "catalog"), "*.json")
            .Select(Path.GetFileName)
            .Where(f => f is not "sources.json" and not "seasons.json")
            .Select(f => f!)
            .Order(StringComparer.Ordinal));

    [Theory]
    [MemberData(nameof(CatalogFiles))]
    public void Every_entry_and_fact_is_in_its_source(string file)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(Repo, "catalog", file)),
            new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });
        var missing = new List<string>();
        var unchecked_ = 0;
        foreach (var e in doc.RootElement.GetProperty("entries").EnumerateArray())
        {
            var name = e.GetProperty("name").GetString()!;
            var quote = e.TryGetProperty("quote", out var q) ? q.GetString()! : name;
            foreach (var tag in Tags(e))
            {
                if (tag.StartsWith('S') && !Confirmed.Value.ContainsKey(tag))
                {
                    unchecked_++;
                    continue;
                }
                if (!Found(tag, tag.StartsWith('S') ? name : quote))
                {
                    missing.Add($"{name}: not found in {tag}");
                }
            }
            if (!e.TryGetProperty("facts", out var facts))
            {
                continue;
            }
            foreach (var f in facts.EnumerateObject())
            {
                foreach (var v in f.Value.EnumerateArray())
                {
                    var value = v.GetProperty("value").GetString()!;
                    var looked = v.TryGetProperty("quote", out var vq) ? [vq.GetString()!]
                        : value.Contains(", ", StringComparison.Ordinal) ? value.Split(", ") : new[] { value };
                    foreach (var tag in Tags(v))
                    {
                        if (tag.StartsWith('S') && !Confirmed.Value.ContainsKey(tag))
                        {
                            unchecked_++;
                            continue;
                        }
                        foreach (var text in looked)
                        {
                            if (!Found(tag, text))
                            {
                                missing.Add($"{name}.{f.Name} = '{text}': not found in {tag}");
                            }
                        }
                    }
                }
            }
        }
        Assert.True(missing.Count == 0, string.Join(Environment.NewLine, missing));
        Assert.SkipWhen(unchecked_ > 0, $"{unchecked_} screen-sourced values not checked: their confirmed values aren't in {TestLibrary.Root}");
    }

    [Fact]
    public void Seasons_are_in_their_posts()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(Repo, "catalog", "seasons.json")));
        foreach (var s in doc.RootElement.GetProperty("seasons").EnumerateArray())
        {
            var quote = s.GetProperty("quote").GetString()!;
            foreach (var tag in Tags(s))
            {
                Assert.True(Found(tag, quote), $"{s.GetProperty("name").GetString()}: '{quote}' not in {tag}");
            }
        }
    }

    [Fact]
    public void Feed_ids_match_the_feed()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(Repo, "catalog", "sources.json")));
        foreach (var s in doc.RootElement.GetProperty("sources").EnumerateArray())
        {
            var tag = s.GetProperty("tag").GetString()!;
            if (Posts.Value.TryGetValue(tag, out var post))
            {
                Assert.Equal(post.FeedId, s.GetProperty("feedId").GetString());
                Assert.StartsWith(s.GetProperty("title").GetString()!, post.Title, StringComparison.Ordinal);
            }
        }
    }

    private static IEnumerable<string> Tags(JsonElement el) =>
        el.TryGetProperty("sources", out var s) ? s.EnumerateArray().Select(x => x.GetString()!) : [];

    private static bool Found(string tag, string text)
    {
        var loose = PostText.Loose(text);
        if (tag.StartsWith("P-", StringComparison.Ordinal))
        {
            return Posts.Value.TryGetValue(tag, out var post) && PostText.Loose(post.Plain).Contains(loose, StringComparison.Ordinal);
        }
        return Confirmed.Value.TryGetValue(tag, out var values) && values.Any(v => v.Contains(loose, StringComparison.Ordinal));
    }
}
