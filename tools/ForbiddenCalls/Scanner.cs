using System.IO.Compression;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text.Json;

namespace GrindPilot.Tools.ForbiddenCalls;

/// <summary>One reference to a forbidden call, found in one file.</summary>
public sealed record Hit(string File, string Kind, string Symbol)
{
    public override string ToString() => $"{File}: {Kind} {Symbol}";
}

/// <summary>The list in forbidden.json: Windows calls that open another process, read or write its memory, start a thread in it, hook or send input, or take focus.</summary>
public sealed class ForbiddenList
{
    public required IReadOnlyList<string> NativeEntryPoints { get; init; }
    public required IReadOnlyDictionary<string, IReadOnlyList<string>> ManagedMembers { get; init; }

    public static ForbiddenList Load(string path)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(path), new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });
        var root = doc.RootElement;
        return new ForbiddenList
        {
            NativeEntryPoints = root.GetProperty("nativeEntryPoints").EnumerateArray().Select(e => e.GetString()!).ToList(),
            ManagedMembers = root.GetProperty("managedMembers").EnumerateObject()
                .ToDictionary(p => p.Name, p => (IReadOnlyList<string>)p.Value.EnumerateArray().Select(e => e.GetString()!).ToList()),
        };
    }

    /// <summary>A native name matches an entry exactly, or with Windows' A/W suffix, or as kernel32's K32 alias.</summary>
    public bool IsForbiddenNative(string name)
    {
        var bare = name.StartsWith("K32", StringComparison.Ordinal) ? name[3..] : name;
        foreach (var entry in NativeEntryPoints)
        {
            if (bare == entry || bare == entry + "A" || bare == entry + "W")
            {
                return true;
            }
        }
        return false;
    }

    public bool IsForbiddenMember(string type, string member) =>
        ManagedMembers.TryGetValue(type, out var members) && members.Contains(member);
}

/// <summary>Reads managed metadata (P/Invokes and member references) and native import tables. It never runs anything it reads.</summary>
public sealed class Scanner(ForbiddenList list)
{
    public IEnumerable<Hit> ScanFile(string path) => ScanBytes(Path.GetFileName(path), File.ReadAllBytes(path));

    /// <summary>Scans a .nupkg or .zip without unpacking it to disk: every PE inside it.</summary>
    public IEnumerable<Hit> ScanArchive(string path)
    {
        using var zip = ZipFile.OpenRead(path);
        var hits = new List<Hit>();
        foreach (var entry in zip.Entries.Where(e => IsPeName(e.FullName)))
        {
            using var s = entry.Open();
            using var ms = new MemoryStream();
            s.CopyTo(ms);
            hits.AddRange(ScanBytes($"{Path.GetFileName(path)}!{entry.FullName}", ms.ToArray()));
        }
        return hits;
    }

    public static bool IsPeName(string name) =>
        name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);

    public IEnumerable<Hit> ScanBytes(string label, byte[] bytes)
    {
        PEReader pe;
        try
        {
            pe = new PEReader(new MemoryStream(bytes));
            _ = pe.PEHeaders;
        }
        catch (BadImageFormatException)
        {
            return [];
        }
        using (pe)
        {
            var hits = new List<Hit>();
            hits.AddRange(NativeImports(pe).Where(i => list.IsForbiddenNative(i.Name))
                .Select(i => new Hit(label, "native-import", $"{i.Dll}!{i.Name}")));
            if (pe.HasMetadata)
            {
                hits.AddRange(Managed(label, pe.GetMetadataReader()));
            }
            return hits.Distinct().ToList();
        }
    }

    private IEnumerable<Hit> Managed(string label, MetadataReader md)
    {
        foreach (var handle in md.MethodDefinitions)
        {
            var method = md.GetMethodDefinition(handle);
            var import = method.GetImport();
            if (import.Module.IsNil)
            {
                continue;
            }
            var entry = md.GetString(import.Name);
            if (entry.Length == 0)
            {
                entry = md.GetString(method.Name);
            }
            if (list.IsForbiddenNative(entry))
            {
                var module = md.GetString(md.GetModuleReference(import.Module).Name);
                yield return new Hit(label, "p/invoke", $"{module}!{entry}");
            }
        }

        foreach (var handle in md.MemberReferences)
        {
            var member = md.GetMemberReference(handle);
            var type = TypeName(md, member.Parent);
            var name = md.GetString(member.Name);
            if (type is not null && list.IsForbiddenMember(type, name))
            {
                yield return new Hit(label, "managed-call", $"{type}.{name}");
            }
        }
    }

    private static string? TypeName(MetadataReader md, EntityHandle parent)
    {
        if (parent.Kind == HandleKind.TypeReference)
        {
            var t = md.GetTypeReference((TypeReferenceHandle)parent);
            var ns = md.GetString(t.Namespace);
            return ns.Length > 0 ? $"{ns}.{md.GetString(t.Name)}" : md.GetString(t.Name);
        }
        if (parent.Kind == HandleKind.TypeDefinition)
        {
            var t = md.GetTypeDefinition((TypeDefinitionHandle)parent);
            var ns = md.GetString(t.Namespace);
            return ns.Length > 0 ? $"{ns}.{md.GetString(t.Name)}" : md.GetString(t.Name);
        }
        if (parent.Kind == HandleKind.TypeSpecification)
        {
            return null;
        }
        return null;
    }

    /// <summary>The functions a PE imports by name, from its import table and its delay-load table.</summary>
    internal static IEnumerable<(string Dll, string Name)> NativeImports(PEReader pe)
    {
        var headers = pe.PEHeaders;
        if (headers.PEHeader is null)
        {
            yield break;
        }
        var is64 = headers.PEHeader.Magic == PEMagic.PE32Plus;

        var imports = headers.PEHeader.ImportTableDirectory;
        if (imports.RelativeVirtualAddress != 0)
        {
            for (var desc = imports.RelativeVirtualAddress; ; desc += 20)
            {
                var originalFirstThunk = ReadInt32(pe, desc);
                var nameRva = ReadInt32(pe, desc + 12);
                var firstThunk = ReadInt32(pe, desc + 16);
                if (nameRva == 0)
                {
                    break;
                }
                var dll = ReadAscii(pe, nameRva);
                foreach (var fn in Thunks(pe, originalFirstThunk != 0 ? originalFirstThunk : firstThunk, is64))
                {
                    yield return (dll, fn);
                }
            }
        }

        var delay = headers.PEHeader.DelayImportTableDirectory;
        if (delay.RelativeVirtualAddress != 0)
        {
            for (var desc = delay.RelativeVirtualAddress; ; desc += 32)
            {
                var nameRva = ReadInt32(pe, desc + 4);
                var intRva = ReadInt32(pe, desc + 16);
                if (nameRva == 0)
                {
                    break;
                }
                var dll = ReadAscii(pe, nameRva);
                foreach (var fn in Thunks(pe, intRva, is64))
                {
                    yield return (dll, fn);
                }
            }
        }
    }

    private static IEnumerable<string> Thunks(PEReader pe, int rva, bool is64)
    {
        if (rva == 0)
        {
            yield break;
        }
        for (var at = rva; ; at += is64 ? 8 : 4)
        {
            var thunk = is64 ? ReadInt64(pe, at) : (uint)ReadInt32(pe, at);
            if (thunk == 0)
            {
                yield break;
            }
            var byOrdinal = is64 ? (thunk & 0x8000000000000000UL) != 0 : (thunk & 0x80000000UL) != 0;
            if (byOrdinal)
            {
                continue;
            }
            yield return ReadAscii(pe, (int)(thunk & 0x7FFFFFFF) + 2);
        }
    }

    private static int ReadInt32(PEReader pe, int rva) => pe.GetSectionData(rva).GetReader().ReadInt32();

    private static ulong ReadInt64(PEReader pe, int rva) => pe.GetSectionData(rva).GetReader().ReadUInt64();

    private static string ReadAscii(PEReader pe, int rva)
    {
        var r = pe.GetSectionData(rva).GetReader();
        var bytes = new List<byte>();
        while (r.RemainingBytes > 0)
        {
            var b = r.ReadByte();
            if (b == 0)
            {
                break;
            }
            bytes.Add(b);
        }
        return System.Text.Encoding.ASCII.GetString(bytes.ToArray());
    }
}
