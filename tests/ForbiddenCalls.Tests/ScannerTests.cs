using System.Diagnostics;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;

namespace GrindPilot.Tools.ForbiddenCalls.Tests;

public class ScannerTests
{
    private static readonly ForbiddenList List = ForbiddenList.Load(Path.Combine(AppContext.BaseDirectory, "forbidden.json"));

    private static List<Hit> ScanThisAssembly() =>
        new Scanner(List).ScanFile(typeof(ScannerTests).Assembly.Location).ToList();

    [Fact]
    public void A_pinvoke_to_OpenProcess_is_found() =>
        Assert.Contains(ScanThisAssembly(), h => h.Kind == "p/invoke" && h.Symbol == "kernel32.dll!OpenProcess");

    [Fact]
    public void A_W_suffixed_entry_point_is_found() =>
        Assert.Contains(ScanThisAssembly(), h => h.Kind == "p/invoke" && h.Symbol == "user32.dll!SendInput");

    [Fact]
    public void Reading_another_process_MainModule_is_found() =>
        Assert.Contains(ScanThisAssembly(), h => h.Kind == "managed-call" && h.Symbol == "System.Diagnostics.Process.get_MainModule");

    [Fact]
    public void Process_names_alone_are_not_forbidden()
    {
        var hits = ScanThisAssembly();
        Assert.DoesNotContain(hits, h => h.Symbol.Contains("GetProcessesByName", StringComparison.Ordinal));
        Assert.DoesNotContain(hits, h => h.Symbol.Contains("get_ProcessName", StringComparison.Ordinal));
    }

    [Fact]
    public void The_tool_itself_makes_no_forbidden_call() =>
        Assert.Empty(new Scanner(List).ScanFile(typeof(Scanner).Assembly.Location));

    [Theory]
    [InlineData("OpenProcess", true)]
    [InlineData("SendMessageW", true)]
    [InlineData("K32EnumProcessModules", true)]
    [InlineData("SetWindowsHookExW", true)]
    [InlineData("CredReadW", false)]
    [InlineData("OpenProcessToken", false)]
    [InlineData("GetCurrentProcess", false)]
    public void Native_names_match_with_their_suffixes(string name, bool forbidden) =>
        Assert.Equal(forbidden, List.IsForbiddenNative(name));

    [Fact]
    public void Native_import_tables_are_read()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }
        var notepad = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "notepad.exe");
        using var pe = new PEReader(File.OpenRead(notepad));
        var imports = Scanner.NativeImports(pe).ToList();
        Assert.NotEmpty(imports);
        Assert.All(imports, i => Assert.False(string.IsNullOrWhiteSpace(i.Name)));
    }

    [Fact]
    public void Velopack_s_process_scan_can_never_be_reviewed_in()
    {
        // The stock helpers read every process's path (QueryFullProcessImageNameW) and setup opens every process.
        // The patched ones don't; reviewing either import would let a stock helper pass (docs/build.md).
        using var doc = System.Text.Json.JsonDocument.Parse(
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "dependency-review.json")),
            new System.Text.Json.JsonDocumentOptions { CommentHandling = System.Text.Json.JsonCommentHandling.Skip });
        var entries = doc.RootElement.GetProperty("reviewed").EnumerateArray()
            .Select(e => (File: e.GetProperty("file").GetString()!, Symbol: e.GetProperty("symbol").GetString()!))
            .ToList();
        Assert.NotEmpty(entries);
        Assert.DoesNotContain(entries, e => e.Symbol.Contains("QueryFullProcessImageName", StringComparison.Ordinal));
        Assert.DoesNotContain(entries, e => e.File is "setup_x64.exe" or "GrindPilot-win-Setup.exe" && e.Symbol.EndsWith("!OpenProcess", StringComparison.Ordinal));
    }

    [Fact]
    public void A_file_that_is_not_a_pe_gives_nothing() =>
        Assert.Empty(new Scanner(List).ScanBytes("not-a-pe.dll", [1, 2, 3, 4]));
}

/// <summary>
/// Fixtures for the tests above: forbidden references that exist in this test assembly's metadata. Nothing calls them.
/// </summary>
internal static partial class Fixtures
{
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr OpenProcess(uint access, bool inherit, uint pid);

    [DllImport("user32.dll", EntryPoint = "SendInput")]
    public static extern uint SendInputFixture(uint count, IntPtr inputs, int size);

    public static string? NeverCalled(Process p) => p.MainModule?.FileName;

    public static int Allowed() => Process.GetProcessesByName("none").Length + Process.GetCurrentProcess().ProcessName.Length;
}
