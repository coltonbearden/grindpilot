using System.Globalization;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;
using GrindPilot.Windows;

namespace GrindPilot.App.Pages;

/// <summary>
/// "Inputs and status" (8.4): which inputs are on, the last capture read, both jobs' last results and the app's version,
/// plus the update state, the store's health and whether the game is running.
/// </summary>
internal sealed class StatusPage(AppServices services) : IPage
{
    /// <summary>One line about this version, shown on this page.</summary>
    internal const string WhatsNew = "0.2.0: GrindPilot's first public release. Updates come from its public releases, with no token.";

    public string Title => "Inputs and status";
    // One host that stays on screen; Refresh replaces what it shows, so a refresh is seen without switching pages.
    private readonly ContentControl _host = new();

    public Control View => _host;

    public void Refresh()
    {
        var parts = new List<Control>
        {
            Ui.Title("Inputs and status"),
            Ui.Subtitle("GrindPilot reads only your own screens, and never touches the game's memory, files or process. " +
                        "Nothing reads anything yet: each input arrives in its own stage."),
        };
        foreach (var p in services.Problems)
        {
            parts.Add(Ui.Problem(p));
        }

        parts.Add(Ui.Section("Inputs"));
        parts.Add(Ui.Row("Screenshots", "Off. The first reader arrives in stage 2."));
        parts.Add(Ui.Row("Recordings", "Off. Arrives in stage 9, read only after the game has closed."));
        parts.Add(Ui.Row("Live screen", "Off. Arrives in stage 10, and first runs only on your word."));
        parts.Add(Ui.Row("Last capture read", "None yet"));

        parts.Add(Ui.Section("Jobs"));
        parts.Add(Ui.Row("Nightly reader self-test", "Not set up (stage 8)"));
        parts.Add(Ui.Row("Daily patch watch", "Not set up (stage 8)"));

        parts.Add(Ui.Section("GrindPilot"));
        var version = services.Updates.InstalledVersion;
        parts.Add(Ui.Row("Version", version ?? $"{typeof(StatusPage).Assembly.GetName().Version} (a build, not installed)"));
        parts.Add(Ui.Row("What's new", WhatsNew));
        var u = services.Updates.Status;
        parts.Add(Ui.Row("Updates", u.Detail + (u.LastCheck is { } t ? $"  (checked {t:yyyy-MM-dd HH:mm})" : "")));
        parts.Add(Ui.Row("Update token", TokenState()));
        var check = new Button { Content = "Check for updates now", Margin = new Avalonia.Thickness(240, 6, 0, 0) };
        check.Click += async (_, _) =>
        {
            check.IsEnabled = false;
            await Task.Run(() => services.Updates.CheckAndDownloadAsync());
            Dispatcher.UIThread.Post(Refresh);
        };
        parts.Add(check);
        parts.Add(Ui.Row("Running elevated", Elevation.IsElevated() ? "Yes (it shouldn't be)" : "No"));
        var game = services.Game.Check();
        parts.Add(Ui.Row("Call of Duty", game.Running ? $"Running ({game.Reason}): updates wait" : "Not running"));

        parts.Add(Ui.Section("Account store"));
        if (services.Store is { } store)
        {
            var c = store.Counts();
            parts.Add(Ui.Row("Where", store.DatabasePath));
            parts.Add(Ui.Row("Holds", string.Create(CultureInfo.InvariantCulture,
                $"{c.Captures} captures, {c.Observations} observations, {c.KnownKeys} known values ({c.StaleKeys} stale, {c.HeldKeys} held for review)")));
            var rebuild = store.CheckRebuild();
            parts.Add(Ui.Row("Rebuilt from observations", rebuild.Matches
                ? $"Matches ({rebuild.Keys} values)"
                : $"Differs on {rebuild.Differences.Count} values"));
            parts.Add(Ui.Row("Last backup", store.LastBackup is { } b ? $"{b.ToLocalTime():yyyy-MM-dd HH:mm} in {store.BackupsFolder}" : "None yet"));
        }
        else
        {
            parts.Add(Ui.Problem("The store isn't open."));
        }
        parts.Add(Ui.Row("Log", services.Log.Today));

        _host.Content = Ui.Page([.. parts]);
    }

    private string TokenState()
    {
        try
        {
            return services.Tokens.Read() is null ? "None (not needed for public releases)" : "Saved in Windows Credential Manager";
        }
        catch (Exception)
        {
            return "Couldn't be read";
        }
    }
}
