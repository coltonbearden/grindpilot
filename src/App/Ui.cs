using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace GrindPilot.App;

/// <summary>Small builders so the pages read as what they show.</summary>
internal static class Ui
{
    public static TextBlock Title(string text) => new()
    {
        Text = text,
        FontSize = 26,
        FontWeight = FontWeight.SemiBold,
        Margin = new Thickness(0, 0, 0, 4),
    };

    public static TextBlock Subtitle(string text) => new()
    {
        Text = text,
        Opacity = 0.75,
        TextWrapping = TextWrapping.Wrap,
        Margin = new Thickness(0, 0, 0, 16),
    };

    public static TextBlock Section(string text) => new()
    {
        Text = text,
        FontSize = 17,
        FontWeight = FontWeight.SemiBold,
        Margin = new Thickness(0, 18, 0, 6),
    };

    public static TextBlock Text(string text, double opacity = 1, bool wrap = true) => new()
    {
        Text = text,
        Opacity = opacity,
        TextWrapping = wrap ? TextWrapping.Wrap : TextWrapping.NoWrap,
        VerticalAlignment = VerticalAlignment.Center,
    };

    public static TextBlock Problem(string text) => new()
    {
        Text = text,
        Foreground = new SolidColorBrush(Color.FromRgb(0xD1, 0x34, 0x38)),
        TextWrapping = TextWrapping.Wrap,
        Margin = new Thickness(0, 0, 0, 8),
    };

    /// <summary>A label and its value side by side, the shape of every line on the status page.</summary>
    public static Grid Row(string label, Control value)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("240,*"), Margin = new Thickness(0, 3) };
        var l = Text(label, 0.75);
        grid.Children.Add(l);
        Grid.SetColumn(value, 1);
        grid.Children.Add(value);
        return grid;
    }

    public static Grid Row(string label, string value) => Row(label, Text(value));

    public static Border Card(Control content) => new()
    {
        Child = content,
        Padding = new Thickness(14, 10),
        Margin = new Thickness(0, 0, 0, 8),
        CornerRadius = new CornerRadius(6),
        BorderThickness = new Thickness(1),
        BorderBrush = new SolidColorBrush(Color.FromArgb(0x40, 0x80, 0x80, 0x80)),
    };

    public static ScrollViewer Page(params Control[] children)
    {
        var stack = new StackPanel { Margin = new Thickness(28, 24, 28, 28) };
        foreach (var c in children)
        {
            stack.Children.Add(c);
        }
        return new ScrollViewer { Content = stack };
    }
}

/// <summary>A page of the window: it rebuilds itself from the store when shown.</summary>
internal interface IPage
{
    string Title { get; }
    Control View { get; }
    void Refresh();
}
