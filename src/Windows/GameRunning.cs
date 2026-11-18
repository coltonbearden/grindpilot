using System.Diagnostics;
using Microsoft.Win32;

namespace GrindPilot.Windows;

/// <summary>Whether Call of Duty is running, and which signal said so.</summary>
public sealed record GameState(bool Running, string Reason);

/// <summary>The two signals the check reads. Split out so the rule can be tested without Steam or the game.</summary>
public interface IGameSignals
{
    /// <summary>Steam's <c>HKCU\Software\Valve\Steam\RunningAppID</c>, or null when Steam hasn't written it.</summary>
    long? SteamRunningAppId();

    /// <summary>The names in the process list. Nothing else is asked of any process.</summary>
    IReadOnlyCollection<string> ProcessNames();
}

/// <summary>
/// The door that knows whether the game is running (09-optional-integrations.md, 9.3). It reads one registry value and
/// the names in the process list, and nothing else. A third signal some tools use, a process whose executable sits in
/// a game folder, isn't used here: reading a process's path means opening the process.
/// </summary>
public sealed class GameRunningCheck(IGameSignals signals)
{
    public const long SteamAppId = 1938090;

    /// <summary>The game's executables, matched by name only (their paths aren't read; see the class summary).</summary>
    public static readonly IReadOnlyList<string> ProcessNamesToMatch = ["cod", "sp26-cod"];

    public static GameRunningCheck ForThisPc() => new(new WindowsGameSignals());

    public GameState Check()
    {
        var appId = signals.SteamRunningAppId();
        if (appId == SteamAppId)
        {
            return new GameState(true, "Steam reports Call of Duty running");
        }
        var names = signals.ProcessNames();
        foreach (var n in ProcessNamesToMatch)
        {
            if (names.Contains(n, StringComparer.OrdinalIgnoreCase))
            {
                return new GameState(true, $"a process named {n} is running");
            }
        }
        return new GameState(false, "not running");
    }
}

internal sealed class WindowsGameSignals : IGameSignals
{
    public long? SteamRunningAppId()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
        return key?.GetValue("RunningAppID") switch
        {
            int i => i,
            long l => l,
            _ => null,
        };
    }

    public IReadOnlyCollection<string> ProcessNames()
    {
        // Process.GetProcesses reads one system snapshot of the process list, NtQuerySystemInformation
        // (SystemProcessInformation), and gives every entry its name from it: the image name, or "System", "Idle" or the
        // process id (dotnet/runtime v10.0.0, ProcessManager.Win32.cs lines 290-294 and 341-355). Each Process is built
        // with that snapshot entry (Process.cs lines 1099-1104), so its ProcessName getter returns the snapshot's name and
        // never reaches the fallback that opens the process (Process.Windows.cs lines 896-916). Read 2026-11-17. Nothing
        // else is asked of any process; each Process object is disposed at once.
        var processes = Process.GetProcesses();
        try
        {
            return processes.Select(p => p.ProcessName).ToList();
        }
        finally
        {
            foreach (var p in processes)
            {
                p.Dispose();
            }
        }
    }
}
