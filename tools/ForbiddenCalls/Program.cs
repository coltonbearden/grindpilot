using System.Text.Json;
using GrindPilot.Tools.ForbiddenCalls;

// The forbidden-call check (02-rules-and-limits.md, 2.4; docs/build.md, "The forbidden-call check").
//
//   ForbiddenCalls --own <folder> [--own <folder>] [--deps <folder|.nupkg|.zip|.exe>]... [--runtime <app.deps.json>]
//                  [--config <folder>] [--write-review]
//
// Our own assemblies (GrindPilot*.dll) may reference no forbidden call unless allowed.json names the
// exact call and why. Every other PE (third-party packages, Velopack's binaries) may reference one only when
// dependency-review.json names it with a reason. The .NET runtime's own files are left out (see dependency-review.json).

var own = new List<string>();
var deps = new List<string>();
var runtimeDeps = new List<string>();
var config = AppContext.BaseDirectory;
var writeReview = false;
for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--own": own.Add(args[++i]); break;
        case "--deps": deps.Add(args[++i]); break;
        case "--runtime": runtimeDeps.Add(args[++i]); break;
        case "--config": config = args[++i]; break;
        case "--write-review": writeReview = true; break;
        default:
            Console.Error.WriteLine($"Unknown argument: {args[i]}");
            return 2;
    }
}
if (own.Count == 0)
{
    Console.Error.WriteLine("Name at least one --own folder.");
    return 2;
}

var list = ForbiddenList.Load(Path.Combine(config, "forbidden.json"));
var scanner = new Scanner(list);
var allowed = Review.Load(Path.Combine(config, "allowed.json"), "allowed");
var reviewed = Review.Load(Path.Combine(config, "dependency-review.json"), "reviewed");
var runtimeFiles = RuntimeFiles.From(runtimeDeps);

var ownHits = new List<Hit>();
var ownFiles = 0;
foreach (var folder in own)
{
    foreach (var file in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories).Where(Classify.IsOwn))
    {
        ownFiles++;
        ownHits.AddRange(scanner.ScanFile(file));
    }
}

var depHits = new List<Hit>();
var depFiles = 0;
foreach (var target in deps)
{
    if (Directory.Exists(target))
    {
        foreach (var file in Directory.EnumerateFiles(target, "*", SearchOption.AllDirectories)
                     .Where(f => Scanner.IsPeName(f) && !Classify.IsOwn(f) && !runtimeFiles.Contains(Path.GetFileName(f))))
        {
            depFiles++;
            depHits.AddRange(scanner.ScanFile(file));
        }
    }
    else if (target.EndsWith(".nupkg", StringComparison.OrdinalIgnoreCase) || target.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
    {
        depFiles++;
        depHits.AddRange(scanner.ScanArchive(target).Where(h =>
            !Classify.IsOwn(h.File) && !runtimeFiles.Contains(Classify.InnerName(h.File))));
    }
    else if (File.Exists(target))
    {
        depFiles++;
        depHits.AddRange(scanner.ScanFile(target));
    }
    else
    {
        Console.Error.WriteLine($"Not found: {target}");
        return 2;
    }
}

var ownFailures = ownHits.Where(h => !allowed.Covers(h)).ToList();
var depFailures = depHits.Where(h => !reviewed.Covers(h)).ToList();

Console.WriteLine($"Scanned {ownFiles} of our own assemblies and {depFiles} dependency files ({runtimeFiles.Count} .NET runtime files left out).");
Console.WriteLine($"Our own: {ownHits.Count} forbidden references, {ownHits.Count - ownFailures.Count} allowed by allowed.json.");
Console.WriteLine($"Dependencies: {depHits.Count} forbidden references, {depHits.Count - depFailures.Count} reviewed in dependency-review.json.");

foreach (var h in ownFailures)
{
    Console.WriteLine($"FORBIDDEN in our own code: {h}");
}
foreach (var h in depFailures)
{
    Console.WriteLine($"NOT REVIEWED in a dependency: {h}");
}

if (writeReview && depFailures.Count > 0)
{
    var skeleton = depFailures
        .Select(h => new { file = Classify.InnerName(h.File), symbol = h.Symbol, reason = "TODO: why this is acceptable" })
        .DistinctBy(x => (x.file, x.symbol));
    Console.WriteLine(JsonSerializer.Serialize(skeleton, new JsonSerializerOptions { WriteIndented = true }));
}

if (ownFailures.Count > 0 || depFailures.Count > 0)
{
    Console.WriteLine("RESULT: FAIL");
    return 1;
}
Console.WriteLine("RESULT: PASS");
return 0;

internal static class Classify
{
    /// <summary>Our own managed assemblies. The apphosts (GrindPilot.exe) are Microsoft's native launcher, a runtime file.</summary>
    public static bool IsOwn(string path)
    {
        var name = InnerName(path);
        return name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
            && name.StartsWith("GrindPilot", StringComparison.Ordinal);
    }

    /// <summary>The file name of a scanned file, or of an entry inside an archive (<c>pkg.nupkg!lib/app/x.dll</c>).</summary>
    public static string InnerName(string label)
    {
        var bang = label.LastIndexOf('!');
        var inner = bang >= 0 ? label[(bang + 1)..] : label;
        return Path.GetFileName(inner.Replace('/', Path.DirectorySeparatorChar));
    }
}

/// <summary>An allow or review list: each entry names a file, a symbol and a reason.</summary>
internal sealed class Review
{
    private readonly HashSet<(string File, string Symbol)> _entries;

    private Review(HashSet<(string, string)> entries) => _entries = entries;

    public static Review Load(string path, string property)
    {
        var set = new HashSet<(string, string)>();
        if (File.Exists(path))
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path), new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });
            foreach (var e in doc.RootElement.GetProperty(property).EnumerateArray())
            {
                var reason = e.GetProperty("reason").GetString();
                if (string.IsNullOrWhiteSpace(reason) || reason.StartsWith("TODO", StringComparison.Ordinal))
                {
                    throw new InvalidDataException($"{Path.GetFileName(path)}: every entry needs a reason ({e}).");
                }
                set.Add((e.GetProperty("file").GetString()!, e.GetProperty("symbol").GetString()!));
            }
        }
        return new Review(set);
    }

    public bool Covers(Hit h) => _entries.Contains((Classify.InnerName(h.File), h.Symbol));
}

/// <summary>The .NET runtime's own files, read from the runtime pack's entries in an app's deps.json.</summary>
internal static class RuntimeFiles
{
    public static HashSet<string> From(IEnumerable<string> depsJsonFiles)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in depsJsonFiles)
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(file));
            var root = doc.RootElement;
            var packs = root.GetProperty("libraries").EnumerateObject()
                .Where(l => l.Value.TryGetProperty("type", out var t) && t.GetString() == "runtimepack")
                .Select(l => l.Name)
                .ToHashSet(StringComparer.Ordinal);
            foreach (var target in root.GetProperty("targets").EnumerateObject())
            {
                foreach (var lib in target.Value.EnumerateObject().Where(l => packs.Contains(l.Name)))
                {
                    foreach (var section in new[] { "runtime", "native" })
                    {
                        if (lib.Value.TryGetProperty(section, out var files))
                        {
                            foreach (var f in files.EnumerateObject())
                            {
                                set.Add(Path.GetFileName(f.Name));
                            }
                        }
                    }
                }
            }
            // The apphost: Microsoft's native launcher, renamed to the app's own name.
            set.Add(Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(file)) + ".exe");
        }
        return set;
    }
}
