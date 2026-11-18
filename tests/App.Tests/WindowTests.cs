using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using GrindPilot.App.Pages;
using GrindPilot.Core;
using GrindPilot.Windows;

[assembly: AvaloniaTestApplication(typeof(GrindPilot.App.Tests.TestAppBuilder))]

namespace GrindPilot.App.Tests;

public static class TestAppBuilder
{
    // Skia renders real pixels so a frame can be measured; it stays in memory and is never written to a file.
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UseSkia()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}

internal sealed class FakeTokens : ITokenStore
{
    public string? Value { get; private set; }
    public string? Read() => Value;
    public void Save(string token) => Value = token.Trim();
    public void Delete() => Value = null;
}

internal sealed class NoGame : IGameSignals
{
    public long? SteamRunningAppId() => 0;
    public IReadOnlyCollection<string> ProcessNames() => ["explorer"];
}

public sealed class WindowTests : IDisposable
{
    private static readonly string Repo = ShippedData.FindRepoRoot(AppContext.BaseDirectory);
    private readonly string _data = Path.Combine(Path.GetTempPath(), "grindpilot-app-tests", Guid.NewGuid().ToString("N"));
    private readonly FakeTokens _tokens = new();
    private AppServices? _services;

    /// <summary>A scratch data folder, seeded with the invented import in tests/fixtures/ unless <paramref name="seed"/> is false.</summary>
    private AppServices Services(bool seed = true)
    {
        _services = AppServices.Start(new AppPaths(_data), ShippedData.FromRepo(Repo), _tokens, new NoGame());
        if (seed)
        {
            Core.Import.ConfirmedImport.Load(
                    Path.Combine(Repo, "tests", "fixtures", "import-map.json"),
                    Path.Combine(Repo, "tests", "fixtures", "import-samples.json"))
                .Run(_services.Store!, @"C:\Users\someone");
        }
        return _services;
    }

    public void Dispose()
    {
        _services?.Dispose();
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        try
        {
            Directory.Delete(_data, true);
        }
        catch (IOException)
        {
        }
    }

    private static List<string> Texts(Control root) =>
        root.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text ?? "").ToList();

    private static MainWindow Open(AppServices services)
    {
        var w = new MainWindow(services);
        w.Show();
        Settle(w);
        return w;
    }

    /// <summary>Shows a page and lets layout run, so its controls are in the visual tree.</summary>
    private static void Go(MainWindow w, string page)
    {
        w.ShowPage(page);
        Settle(w);
    }

    private static void Settle(Window w)
    {
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        w.UpdateLayout();
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaFact]
    public void The_window_is_GrindPilot_with_its_four_pages()
    {
        var w = Open(Services());
        Assert.Equal("GrindPilot", w.Title);
        Assert.Equal(["Account", "Catalog", "Inputs and status", "Settings"], w.PageTitles);
        w.Close();
    }

    [AvaloniaFact]
    public void The_account_shows_the_confirmed_values()
    {
        var w = Open(Services());
        Go(w, "Account");
        var texts = Texts(w);
        Assert.Contains("Your account", texts);
        Assert.Contains("3/15", texts);
        Assert.Contains("17", texts);
        Assert.Contains("Confirmed", texts);
        Assert.Contains("Player", texts);
    }

    [AvaloniaFact]
    public void Each_value_traces_to_its_screenshot()
    {
        var w = Open(Services());
        Go(w, "Account");
        var first = w.GetVisualDescendants().OfType<Expander>().First();
        first.IsExpanded = true;
        Settle(w);
        Assert.Contains(Texts(first), t => t.StartsWith("From: Capture 2026.01.02", StringComparison.Ordinal));
        Assert.Contains(Texts(first), t => t == "Outcome: confirmed");
    }

    [AvaloniaFact]
    public void A_new_install_starts_empty_and_shows_no_zeros()
    {
        var w = Open(Services(seed: false));
        Go(w, "Account");
        var texts = Texts(w);
        Assert.Contains("Nothing is stored yet.", texts);
        Assert.DoesNotContain("0", texts);
    }

    [AvaloniaFact]
    public void The_catalog_browses_the_official_facts()
    {
        var w = Open(Services());
        Go(w, "Catalog");
        var texts = Texts(w);
        Assert.Contains("37 of 37", texts);
        Assert.Contains("Official posts disagree", texts);
        Assert.Contains("Kastov 762", texts);

        var page = w.Page<CatalogPage>();
        page.Search("Kastov");
        Settle(w);
        Assert.Contains("1 of 37", Texts(w));

        page.Search("");
        page.Select("camo");
        Settle(w);
        var camoTexts = Texts(w);
        Assert.Contains("Harmonic Break", camoTexts);
        Assert.Contains("Pyroclast Cascade", camoTexts);
    }

    [AvaloniaFact]
    public void Inputs_and_status_says_what_is_on_and_how_the_store_is()
    {
        var w = Open(Services());
        Go(w, "Inputs and status");
        var texts = Texts(w);
        Assert.Contains("Off. The first reader arrives in stage 2.", texts);
        Assert.Contains("Not set up (stage 8)", texts);
        Assert.Contains("Matches (3 values)", texts);
        Assert.Contains("Not running", texts);
        Assert.Contains("None (not needed for public releases)", texts);
        Assert.Contains(texts, t => t.EndsWith("(a build, not installed)", StringComparison.Ordinal));
        Assert.Contains(StatusPage.WhatsNew, texts);
    }

    [AvaloniaFact]
    public void A_refresh_shows_on_the_page_without_switching_pages()
    {
        // The update check, its button and the timer refresh the status page in place.
        var w = Open(Services());
        Go(w, "Inputs and status");
        Assert.Contains("None (not needed for public releases)", Texts(w));
        _tokens.Save("not-a-real-token");
        w.Page<StatusPage>().Refresh();
        Settle(w);
        var texts = Texts(w);
        Assert.Contains("Saved in Windows Credential Manager", texts);
        Assert.DoesNotContain("None (not needed for public releases)", texts);
    }

    [AvaloniaFact]
    public void A_pasted_token_goes_to_the_token_store_and_leaves_the_box()
    {
        var w = Open(Services());
        Go(w, "Settings");
        // The token box is the masked one (the monitor picker has a text box of its own).
        var box = w.GetVisualDescendants().OfType<TextBox>().Single(t => t.PasswordChar == '•');
        box.Text = "  not-a-real-token  ";
        var save = w.GetVisualDescendants().OfType<Button>().First(b => (b.Content as string) == "Save token");
        save.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Assert.Equal("not-a-real-token", _tokens.Value);
        Assert.Equal("", box.Text);
        Assert.Contains("Saved. Check for updates on the Inputs and status page.", Texts(w));
    }

    [AvaloniaFact]
    public void The_window_renders_a_frame()
    {
        var w = Open(Services());
        Go(w, "Account");
        var frame = w.CaptureRenderedFrame();
        Assert.NotNull(frame);
        Assert.True(frame!.PixelSize.Width > 0 && frame.PixelSize.Height > 0);

        // Count distinct colours in memory: a blank or failed render has one or two. Nothing is written to disk.
        using var buffer = frame.Lock();
        var bytes = new byte[buffer.RowBytes * buffer.Size.Height];
        Marshal.Copy(buffer.Address, bytes, 0, bytes.Length);
        var colours = new HashSet<int>();
        for (var i = 0; i + 3 < bytes.Length; i += 4 * 7)
        {
            colours.Add(BitConverter.ToInt32(bytes, i));
        }
        Assert.True(colours.Count > 10, $"only {colours.Count} colours");
    }
}
