using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using GrindPilot.Core.Catalog;

namespace GrindPilot.App.Pages;

/// <summary>
/// A browser of the official facts: weapons and their unlock levels, the camo ladder, attachment slots, perks. Every
/// entry shows its source; where two official posts disagree, both values are shown and marked (3.5).
/// </summary>
internal sealed class CatalogPage : IPage
{
    private readonly AppServices _services;
    private readonly ListBox _kinds = new() { Width = 230 };
    private readonly TextBox _search = new() { Margin = new Thickness(0, 0, 0, 10) };
    private readonly StackPanel _entries = new();
    private readonly Grid _root;
    private List<string> _kindIds = [];

    public CatalogPage(AppServices services)
    {
        _services = services;
        _kinds.SelectionChanged += (_, _) => ShowEntries();
        _search.TextChanged += (_, _) => ShowEntries();

        _root = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*"), Margin = new Thickness(28, 24, 28, 0) };
        var left = new StackPanel { Margin = new Thickness(0, 0, 18, 0) };
        left.Children.Add(Ui.Title("Catalog"));
        left.Children.Add(Ui.Subtitle("Official facts, each with its source."));
        left.Children.Add(_kinds);
        _root.Children.Add(left);
        var right = new DockPanel();
        DockPanel.SetDock(_search, Dock.Top);
        right.Children.Add(_search);
        right.Children.Add(new ScrollViewer { Content = _entries });
        Grid.SetColumn(right, 1);
        _root.Children.Add(right);
    }

    public string Title => "Catalog";
    public Control View => _root;

    public static string KindName(string kind) => kind switch
    {
        "weapon" => "Weapons",
        "weapon-class" => "Weapon classes",
        "camo" => "Camo ladder",
        "camo-group" => "Camo groups",
        "mode-mastery-camo" => "Mode Mastery camos",
        "attachment-slot" => "Attachment slots",
        "attachment" => "Attachments",
        "perk" => "Perks",
        "lethal" => "Lethal equipment",
        "tactical" => "Tactical equipment",
        "field-upgrade" => "Field upgrades",
        "killstreak" => "Killstreaks",
        "calling-card-category" => "Calling-card categories",
        "mastery-set" => "Mastery sets",
        "challenge" => "Challenges",
        "operator" => "Operators",
        "mode" => "Modes",
        "map" => "Maps",
        _ => kind,
    };

    public void Refresh()
    {
        var catalog = _services.Catalog;
        if (catalog is null)
        {
            _entries.Children.Clear();
            _entries.Children.Add(Ui.Problem("The catalog couldn't be loaded."));
            return;
        }
        var previous = _kinds.SelectedIndex;
        _kindIds = catalog.Kinds.OrderBy(k => k == "weapon" ? 0 : k == "camo" ? 1 : 2).ThenBy(KindName).ToList();
        _kinds.ItemsSource = _kindIds.Select(k => $"{KindName(k)} ({catalog.OfKind(k).Count})").ToList();
        _kinds.SelectedIndex = previous >= 0 && previous < _kindIds.Count ? previous : 0;
        ShowEntries();
    }

    internal void Select(string kind)
    {
        var i = _kindIds.IndexOf(kind);
        if (i >= 0)
        {
            _kinds.SelectedIndex = i;
        }
        ShowEntries();
    }

    internal void Search(string text) => _search.Text = text;

    private void ShowEntries()
    {
        var catalog = _services.Catalog;
        _entries.Children.Clear();
        if (catalog is null || _kinds.SelectedIndex < 0 || _kinds.SelectedIndex >= _kindIds.Count)
        {
            return;
        }
        var kind = _kindIds[_kinds.SelectedIndex];
        var query = _search.Text?.Trim() ?? "";
        var entries = catalog.OfKind(kind)
            .Where(e => query.Length == 0 || e.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                        || e.Facts.Values.Any(f => f.Values.Any(v => v.Value.Contains(query, StringComparison.OrdinalIgnoreCase))))
            .OrderBy(e => Order(e))
            .ThenBy(e => e.FactNamed("class")?.Values[0].Value, StringComparer.Ordinal)
            .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        _entries.Children.Add(Ui.Text($"{entries.Count} of {catalog.OfKind(kind).Count}", 0.6));
        foreach (var e in entries)
        {
            _entries.Children.Add(EntryView(catalog, e));
        }
    }

    private static int Order(CatalogEntry e) =>
        e.FactNamed("order") is { } o && int.TryParse(o.Values[0].Value, out var n) ? n : int.MaxValue;

    private static Control EntryView(GameCatalog catalog, CatalogEntry e)
    {
        var summary = string.Join("   ", e.Facts.Values
            .Where(f => f.Name is not "order" and not "note")
            .Take(4)
            .Select(f => $"{Pretty(f.Name)}: {(f.IsConflict ? string.Join(" or ", f.Values.Select(v => v.Value)) : f.Values[0].Value)}"));
        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("260,*,Auto") };
        var name = Ui.Text(e.Name);
        name.FontWeight = Avalonia.Media.FontWeight.SemiBold;
        header.Children.Add(name);
        var facts = Ui.Text(summary, 0.8);
        Grid.SetColumn(facts, 1);
        header.Children.Add(facts);
        if (e.HasConflict)
        {
            var mark = Ui.Problem("Official posts disagree");
            mark.Margin = new Thickness(8, 0, 0, 0);
            Grid.SetColumn(mark, 2);
            header.Children.Add(mark);
        }

        var detail = new StackPanel { Spacing = 3, Margin = new Thickness(4, 4, 0, 6) };
        foreach (var f in e.Facts.Values)
        {
            foreach (var v in f.Values)
            {
                var line = $"{Pretty(f.Name)}: {v.Value}   [{string.Join(", ", v.Sources)}]{(v.Note is null ? "" : "  " + v.Note)}";
                detail.Children.Add(f.IsConflict ? Ui.Problem(line) : Ui.Text(line, 0.85));
            }
        }
        foreach (var tag in e.Sources)
        {
            detail.Children.Add(new SelectableTextBlock
            {
                Text = catalog.SourceTags.TryGetValue(tag, out var s)
                    ? $"{tag}: {s.Title}{(s.Date is null ? "" : ", " + s.Date)}{(s.Url is null ? "" : " — " + s.Url)}"
                    : $"{tag}: an in-game screen confirmed by eye",
                Opacity = 0.65,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            });
        }

        return new Expander
        {
            Header = header,
            Content = detail,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0, 2),
        };
    }

    private static string Pretty(string fact) => fact switch
    {
        "class" => "Class",
        "unlockLevel" => "Unlocks at level",
        "maxLevel" => "Max level",
        "group" => "Group",
        "opens" => "Opens",
        "season" => "Season",
        "slot" => "Slot",
        "row" => "Row",
        "mode" => "Mode",
        "requirement" => "Requirement",
        "challenge" => "Challenge",
        "count" => "Count",
        _ => fact,
    };
}
