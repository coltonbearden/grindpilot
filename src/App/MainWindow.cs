using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using GrindPilot.App.Pages;

namespace GrindPilot.App;

/// <summary>
/// GrindPilot's window: a list of pages on the left, the page on the right. Closing it hides it to the tray; Quit in
/// the tray ends the app. It opens on the monitor the user picks, and never draws over or takes focus from the game.
/// </summary>
public sealed class MainWindow : Window
{
    private readonly AppServices _services;
    private readonly List<IPage> _pages;
    private readonly ListBox _nav;
    private readonly ContentControl _content;
    private DispatcherTimer? _timer;
    private bool _quitting;

    public MainWindow(AppServices services)
    {
        _services = services;
        Title = "GrindPilot";
        Width = services.Settings.WindowWidth ?? 1100;
        Height = services.Settings.WindowHeight ?? 760;
        MinWidth = 820;
        MinHeight = 520;
        WindowStartupLocation = WindowStartupLocation.Manual;

        _pages =
        [
            new AccountPage(services),
            new CatalogPage(services),
            new StatusPage(services),
            new SettingsPage(services, this),
        ];

        _nav = new ListBox
        {
            ItemsSource = _pages.Select(p => p.Title).ToList(),
            Margin = new Thickness(8, 0, 8, 8),
        };
        _nav.SelectionChanged += (_, _) => ShowPage(_nav.SelectedIndex);
        _content = new ContentControl();

        var left = new DockPanel { Width = 220 };
        var name = new TextBlock
        {
            Text = "GrindPilot",
            FontSize = 20,
            FontWeight = FontWeight.Bold,
            Margin = new Thickness(18, 20, 18, 14),
        };
        DockPanel.SetDock(name, Dock.Top);
        left.Children.Add(name);
        var footer = new TextBlock
        {
            Text = "Your own companion app. Not affiliated with or endorsed by Activision.",
            FontSize = 11,
            Opacity = 0.55,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(18, 8, 18, 14),
        };
        DockPanel.SetDock(footer, Dock.Bottom);
        left.Children.Add(footer);
        left.Children.Add(_nav);

        var root = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*") };
        root.Children.Add(left);
        Grid.SetColumn(_content, 1);
        root.Children.Add(_content);
        Content = root;

        _nav.SelectedIndex = 0;
        Opened += (_, _) => PlaceOnChosenMonitor();
    }

    internal IReadOnlyList<string> PageTitles => _pages.Select(p => p.Title).ToList();

    internal Control? CurrentPage => _content.Content as Control;

    internal T Page<T>() where T : class => _pages.OfType<T>().Single();

    public void ShowPage(int index)
    {
        if (index < 0 || index >= _pages.Count)
        {
            return;
        }
        _pages[index].Refresh();
        _content.Content = _pages[index].View;
        if (_nav.SelectedIndex != index)
        {
            _nav.SelectedIndex = index;
        }
    }

    public void ShowPage(string title) => ShowPage(_pages.FindIndex(p => p.Title == title));

    /// <summary>Starts the update check, the daily re-check and the status refresh. Not started in tests.</summary>
    public void StartBackgroundWork()
    {
        _services.Updates.Changed += _ => Dispatcher.UIThread.Post(RefreshCurrent);
        _ = Task.Run(() => _services.Updates.CheckAndDownloadAsync());
        var lastCheck = DateTimeOffset.Now;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };
        _timer.Tick += (_, _) =>
        {
            RefreshCurrent();
            if (DateTimeOffset.Now - lastCheck > TimeSpan.FromDays(1))
            {
                lastCheck = DateTimeOffset.Now;
                _ = Task.Run(() => _services.Updates.CheckAndDownloadAsync());
            }
            if (_services.Store?.BackupIfDue() is { } backup)
            {
                _services.Log.Info("store", $"daily backup {Path.GetFileName(backup)}");
            }
        };
        _timer.Start();
    }

    private void RefreshCurrent()
    {
        var index = _nav.SelectedIndex;
        if (index >= 0 && index < _pages.Count && _pages[index] is StatusPage)
        {
            _pages[index].Refresh();
        }
    }

    public void ShowFromTray()
    {
        Show();
        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }
        Activate();
    }

    public void Quit(IClassicDesktopStyleApplicationLifetime desktop)
    {
        _quitting = true;
        RememberPlacement();
        _timer?.Stop();
        desktop.Shutdown();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        RememberPlacement();
        if (!_quitting)
        {
            // Closing the window hides it; the app stays in the tray until Quit.
            e.Cancel = true;
            Hide();
        }
        base.OnClosing(e);
    }

    /// <summary>Moves the window to the screen the user picked in Settings.</summary>
    internal void MoveToMonitor(int index)
    {
        var screens = Screens.All;
        if (index < 0 || index >= screens.Count)
        {
            return;
        }
        var area = screens[index].WorkingArea;
        var scale = screens[index].Scaling;
        var w = (int)(Width * scale);
        var h = (int)(Height * scale);
        Position = new PixelPoint(area.X + Math.Max(0, (area.Width - w) / 2), area.Y + Math.Max(0, (area.Height - h) / 2));
    }

    private void PlaceOnChosenMonitor()
    {
        var settings = _services.Settings;
        var screens = Screens.All;
        if (screens.Count == 0)
        {
            return;
        }
        if (settings.WindowX is { } x && settings.WindowY is { } y && screens.Any(s => s.Bounds.Contains(new PixelPoint(x + 40, y + 40))))
        {
            Position = new PixelPoint(x, y);
            return;
        }
        if (settings.MonitorIndex is { } chosen && chosen < screens.Count)
        {
            MoveToMonitor(chosen);
            return;
        }
        // First run: the game normally takes the main monitor, so start on another one when there is one.
        var other = screens.Select((s, i) => (s, i)).FirstOrDefault(p => !p.s.IsPrimary);
        MoveToMonitor(other.s is null ? 0 : other.i);
    }

    private void RememberPlacement()
    {
        if (WindowState != WindowState.Normal)
        {
            return;
        }
        var s = _services.Settings;
        s.WindowX = Position.X;
        s.WindowY = Position.Y;
        s.WindowWidth = Width;
        s.WindowHeight = Height;
        _services.SaveSettings();
    }
}
