using System.Text.Json;

namespace GrindPilot.Core;

/// <summary>The app's settings, in <c>%LOCALAPPDATA%\GrindPilot-data\settings.json</c>. Nothing secret lives here.</summary>
public sealed class AppSettings
{
    /// <summary>The monitor the user picked, by its position in Windows' list. Null until one is picked.</summary>
    public int? MonitorIndex { get; set; }

    /// <summary>Where the window was last, so it opens there again.</summary>
    public int? WindowX { get; set; }
    public int? WindowY { get; set; }
    public double? WindowWidth { get; set; }
    public double? WindowHeight { get; set; }

    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public static AppSettings Load(string path)
    {
        try
        {
            return File.Exists(path) ? JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path)) ?? new() : new();
        }
        catch (JsonException)
        {
            return new();
        }
    }

    public void Save(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(this, Json));
        File.Move(tmp, path, overwrite: true);
    }
}
