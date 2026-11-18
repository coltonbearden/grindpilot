using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using GrindPilot.Core.Store;

namespace GrindPilot.App.Pages;

/// <summary>
/// The account as the store holds it: every value with how sure it is and where it came from (03-account-state.md,
/// 3.2 and 3.3). Opening a value's line shows its capture, date and outcome. Unknown is shown as "Unknown", never 0.
/// </summary>
internal sealed class AccountPage(AppServices services) : IPage
{
    public string Title => "Account";
    // One host that stays on screen; Refresh replaces what it shows, so a refresh is seen without switching pages.
    private readonly ContentControl _host = new();

    public Control View => _host;

    public void Refresh()
    {
        var parts = new List<Control>
        {
            Ui.Title("Your account"),
            Ui.Subtitle("What GrindPilot knows, each value with how sure it is. Open a line to see where it came from. " +
                        "A new install starts empty: values arrive with the first reader (stage 2)."),
        };
        foreach (var p in services.Problems)
        {
            parts.Add(Ui.Problem(p));
        }

        var store = services.Store;
        var labels = services.Labels;
        if (store is null || labels is null)
        {
            _host.Content = Ui.Page([.. parts]);
            return;
        }

        var state = store.CurrentState();
        if (state.Count == 0)
        {
            parts.Add(Ui.Text("Nothing is stored yet."));
        }

        var schema = store.Schema;
        foreach (var group in state.Values
                     .GroupBy(s => schema.RuleFor(s.Key)?.Group ?? "Other")
                     .OrderBy(g => GroupOrder(g.Key)))
        {
            parts.Add(Ui.Section(group.Key));
            foreach (var s in group.OrderBy(s => s.Key, StringComparer.Ordinal))
            {
                parts.Add(Line(store, labels.For(s.Key), s));
            }
        }
        _host.Content = Ui.Page([.. parts]);
    }

    private static int GroupOrder(string group) => group switch
    {
        "Player" => 0,
        "Prestige" => 1,
        "Daily challenges" => 2,
        "Weapons" => 3,
        "Camos" => 4,
        "Loadouts" => 5,
        "Challenges" => 6,
        "Builds" => 7,
        _ => 9,
    };

    private static Control Line(AccountStore store, string label, KeyState s)
    {
        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("2*,*,150,170") };
        header.Children.Add(Ui.Text(label));
        var value = Ui.Text(StateText.Value(s));
        value.FontWeight = Avalonia.Media.FontWeight.SemiBold;
        Grid.SetColumn(value, 1);
        header.Children.Add(value);
        var sure = Ui.Text(StateText.Sureness(s), 0.8);
        Grid.SetColumn(sure, 2);
        header.Children.Add(sure);
        var when = Ui.Text(s.ObservedAt is { } at ? "as of " + at.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) : "", 0.6);
        Grid.SetColumn(when, 3);
        header.Children.Add(when);

        var detail = new StackPanel { Spacing = 2, Margin = new Thickness(4, 4, 0, 4) };
        var obs = s.ObservationId is { } id ? store.ObservationsFor(s.Key).FirstOrDefault(o => o.Id == id) : null;
        if (obs is not null && store.Capture(obs.CaptureId) is { } c)
        {
            detail.Children.Add(Ui.Text($"From: {Path.GetFileName(c.Path ?? "")} ({c.Kind.ToDb()}, taken {c.TakenAt.ToLocalTime():yyyy-MM-dd HH:mm:ss})", 0.8));
            detail.Children.Add(Ui.Text($"Screen: {obs.Screen}", 0.8));
            detail.Children.Add(Ui.Text($"Outcome: {obs.Outcome.ToDb()}", 0.8));
            if (obs.Note is { } note)
            {
                detail.Children.Add(Ui.Text(note, 0.65));
            }
        }
        if (s.Status == StateStatus.Stale && s.StaleSince is { } since)
        {
            detail.Children.Add(Ui.Text($"Stale: a newer read on {since.ToLocalTime():yyyy-MM-dd HH:mm} failed, so this is the last good value.", 0.8));
        }
        if (s.Held is { } held)
        {
            detail.Children.Add(Ui.Problem($"A newer value is held for review: {held}."));
        }
        detail.Children.Add(Ui.Text($"Key: {s.Key}", 0.5));

        return new Expander
        {
            Header = header,
            Content = detail,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0, 2),
        };
    }
}
