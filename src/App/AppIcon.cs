using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace GrindPilot.App;

/// <summary>
/// The window and tray icon, drawn when the app starts, so no image file enters the repo (CLAUDE.md, "What never goes
/// in this repo"). A designed icon is a later polish item.
/// </summary>
internal static class AppIcon
{
    public static WindowIcon Draw()
    {
        var bitmap = new RenderTargetBitmap(new PixelSize(64, 64), new Vector(96, 96));
        using (var ctx = bitmap.CreateDrawingContext())
        {
            ctx.DrawRectangle(new SolidColorBrush(Color.FromRgb(0x2B, 0x5C, 0xD6)), null, new Rect(0, 0, 64, 64), 14, 14);
            var text = new FormattedText(
                "GP",
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Bold),
                30,
                Brushes.White);
            ctx.DrawText(text, new Point((64 - text.Width) / 2, (64 - text.Height) / 2));
        }
        return new WindowIcon(bitmap);
    }
}
