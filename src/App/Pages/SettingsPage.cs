using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace GrindPilot.App.Pages;

/// <summary>Settings: the optional update token (straight into Windows Credential Manager) and the monitor GrindPilot opens on.</summary>
internal sealed class SettingsPage(AppServices services, MainWindow window) : IPage
{
    public string Title => "Settings";
    // One host that stays on screen; Refresh replaces what it shows, so a refresh is seen without switching pages.
    private readonly ContentControl _host = new();

    public Control View => _host;

    public void Refresh()
    {
        var parts = new List<Control> { Ui.Title("Settings") };

        parts.Add(Ui.Section("Update token (optional)"));
        parts.Add(Ui.Subtitle("GrindPilot updates itself from its public GitHub releases, which need no token. A token is " +
                              "only for a build that updates from a private fork: a read-only GitHub token for that one " +
                              "repo. Paste it here and press Save: it goes straight into Windows Credential Manager as " +
                              "\"grindpilot-releases-token\" and is never shown again."));
        var box = new TextBox { PasswordChar = '•', Width = 460, HorizontalAlignment = HorizontalAlignment.Left };
        var state = Ui.Text(TokenState(), 0.8);
        var save = new Button { Content = "Save token" };
        var remove = new Button { Content = "Remove token", Margin = new Thickness(8, 0, 0, 0) };
        save.Click += (_, _) =>
        {
            var text = box.Text ?? "";
            box.Text = "";
            if (text.Trim().Length == 0)
            {
                state.Text = "Nothing to save: the box was empty.";
                return;
            }
            try
            {
                services.Tokens.Save(text);
                services.Log.Info("settings", "update token saved to Credential Manager");
                state.Text = "Saved. Check for updates on the Inputs and status page.";
            }
            catch (Exception ex)
            {
                services.Log.Error("settings", "token save failed", ex);
                state.Text = "Couldn't save it to Credential Manager.";
            }
        };
        remove.Click += (_, _) =>
        {
            try
            {
                services.Tokens.Delete();
                services.Log.Info("settings", "update token removed");
                state.Text = TokenState();
            }
            catch (Exception ex)
            {
                services.Log.Error("settings", "token remove failed", ex);
                state.Text = "Couldn't remove it.";
            }
        };
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8) };
        buttons.Children.Add(save);
        buttons.Children.Add(remove);
        parts.Add(box);
        parts.Add(buttons);
        parts.Add(state);

        parts.Add(Ui.Section("Monitor"));
        parts.Add(Ui.Subtitle("GrindPilot opens on this monitor. Pick one the game isn't on."));
        var screens = window.Screens.All;
        var picker = new ComboBox
        {
            ItemsSource = screens.Select((s, i) => $"Monitor {i + 1}: {s.Bounds.Width}×{s.Bounds.Height}{(s.IsPrimary ? " (main)" : "")}").ToList(),
            SelectedIndex = services.Settings.MonitorIndex is { } m && m < screens.Count ? m : -1,
            Width = 320,
        };
        picker.SelectionChanged += (_, _) =>
        {
            if (picker.SelectedIndex < 0)
            {
                return;
            }
            services.Settings.MonitorIndex = picker.SelectedIndex;
            services.Settings.WindowX = null;
            services.Settings.WindowY = null;
            services.SaveSettings();
            window.MoveToMonitor(picker.SelectedIndex);
        };
        parts.Add(picker);

        _host.Content = Ui.Page([.. parts]);
    }

    private string TokenState()
    {
        try
        {
            return services.Tokens.Read() is null ? "No token saved." : "A token is saved.";
        }
        catch (Exception)
        {
            return "Credential Manager couldn't be read.";
        }
    }
}
