using System.Globalization;

namespace GrindPilot.Core;

/// <summary>
/// The app's own log, one file a day in <c>%LOCALAPPDATA%\GrindPilot-data\logs\</c>, the newest 14 kept. It carries field keys,
/// outcomes, counts and timings, never values read from a screen and never the update token (04-capture-and-readers.md, 4.6).
/// </summary>
public sealed class AppLog
{
    private readonly string _folder;
    private readonly object _lock = new();

    public AppLog(string folder) => _folder = folder;

    public string Today => Path.Combine(_folder, $"grindpilot-{DateTime.Now:yyyyMMdd}.log");

    public void Info(string area, string message) => Write("INFO", area, message);

    public void Warn(string area, string message) => Write("WARN", area, message);

    public void Error(string area, string message, Exception? ex = null) =>
        Write("ERROR", area, ex is null ? message : $"{message}: {ex.GetType().Name}");

    private void Write(string level, string area, string message)
    {
        try
        {
            lock (_lock)
            {
                Directory.CreateDirectory(_folder);
                File.AppendAllText(Today, string.Create(CultureInfo.InvariantCulture,
                    $"{DateTimeOffset.Now:yyyy-MM-ddTHH:mm:ss.fffzzz} {level} [{area}] {message}{Environment.NewLine}"));
                foreach (var old in Directory.EnumerateFiles(_folder, "grindpilot-*.log")
                             .OrderByDescending(f => f, StringComparer.Ordinal).Skip(14))
                {
                    File.Delete(old);
                }
            }
        }
        catch (IOException)
        {
            // A log that can't be written never stops the app.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
