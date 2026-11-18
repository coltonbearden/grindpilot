using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using GrindPilot.Core;
using GrindPilot.Windows;

namespace GrindPilot.App;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var services = AppServices.Start(AppPaths.Default(), ShippedData.BesideProgram(), new CredentialManagerTokenStore());
            var window = new MainWindow(services);
            desktop.MainWindow = window;

            var icon = AppIcon.Draw();
            window.Icon = icon;
            var tray = new TrayIcon { Icon = icon, ToolTipText = "GrindPilot", Menu = new NativeMenu() };
            var open = new NativeMenuItem("Open GrindPilot");
            open.Click += (_, _) => window.ShowFromTray();
            var quit = new NativeMenuItem("Quit");
            quit.Click += (_, _) => window.Quit(desktop);
            tray.Menu.Items.Add(open);
            tray.Menu.Items.Add(new NativeMenuItemSeparator());
            tray.Menu.Items.Add(quit);
            tray.Clicked += (_, _) => window.ShowFromTray();
            TrayIcon.SetIcons(this, [tray]);

            ListenForShowRequests(window);
            desktop.Exit += (_, _) => services.Dispose();

            window.Show();
            window.StartBackgroundWork();
        }
        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>A second start of GrindPilot signals this event instead of opening a second copy.</summary>
    private static void ListenForShowRequests(MainWindow window)
    {
        var show = new EventWaitHandle(false, EventResetMode.AutoReset, Program.ShowEventName);
        var thread = new Thread(() =>
        {
            while (show.WaitOne())
            {
                Dispatcher.UIThread.Post(window.ShowFromTray);
            }
        })
        {
            IsBackground = true,
            Name = "GrindPilot show requests",
        };
        thread.Start();
    }
}
