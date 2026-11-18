using System.Text.Json;
using GrindPilot.Core;

namespace GrindPilot.Local.Tests;

/// <summary>
/// The maintainer's own test library, outside the repo: <c>test-library\</c> in the data folder
/// (<c>%LOCALAPPDATA%\GrindPilot-data\</c>, or <c>GRINDPILOT_DATA</c>). It holds confirmed values from the maintainer's
/// own screenshots (<c>expected\samples-&lt;id&gt;.json</c>) and their import maps (<c>imports\&lt;id&gt;.json</c>). None
/// of it is ever committed: tests that need it skip when it is absent.
/// </summary>
internal static class TestLibrary
{
    public static string Root => AppPaths.Default().TestLibrary;

    public static IReadOnlyList<(string Map, string Samples)> Imports()
    {
        var imports = Path.Combine(Root, "imports");
        if (!Directory.Exists(imports))
        {
            return [];
        }
        return Directory.EnumerateFiles(imports, "*.json")
            .Select(map => (map, Path.Combine(Root, "expected", $"samples-{Path.GetFileNameWithoutExtension(map)}.json")))
            .Where(p => File.Exists(p.Item2))
            .Order()
            .ToList();
    }

    /// <summary>Every confirmed value in the library, by sample tag, in <see cref="PostText.Loose"/> form.</summary>
    public static Dictionary<string, List<string>> ConfirmedBySample(Func<string, string> loose)
    {
        var bySample = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var expected = Path.Combine(Root, "expected");
        if (!Directory.Exists(expected))
        {
            return bySample;
        }
        foreach (var file in Directory.EnumerateFiles(expected, "samples-*.json"))
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(file));
            foreach (var f in doc.RootElement.GetProperty("fields").EnumerateArray())
            {
                var sample = f.GetProperty("sample").GetString()!;
                if (!bySample.TryGetValue(sample, out var list))
                {
                    bySample[sample] = list = [];
                }
                list.Add(loose(f.GetProperty("confirmed").GetString()!));
            }
        }
        return bySample;
    }
}
