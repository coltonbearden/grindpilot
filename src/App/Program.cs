using Avalonia;
using GrindPilot.Core;
using GrindPilot.Windows;
using Velopack;

namespace GrindPilot.App;

internal static class Program
{
    public const string ShowEventName = @"Local\GrindPilot.show";
    private const string SingleInstanceName = @"Local\GrindPilot.single-instance";

    [STAThread]
    public static int Main(string[] args)
    {
        // Velopack's install, update and uninstall hooks run here and exit. Applying a downloaded update at start is
        // switched off: UpdateService applies it only after checking the game isn't running (8.6).
        VelopackApp.Build().SetAutoApplyOnStartup(false).Run();

        // Before anything opens a file in the data folder: move a pre-0.2.0 folder into place, once (DataFolderMove).
        var move = DataFolderMove.RunForThisUser();

        if (args.Contains("--update-check"))
        {
            return UpdateCheckMode.Run(move);
        }

        using var single = new Mutex(true, SingleInstanceName, out var first);
        if (!first)
        {
            // GrindPilot is already running (perhaps in the tray): ask it to show its window, and leave.
            try
            {
                using var show = EventWaitHandle.OpenExisting(ShowEventName);
                show.Set();
            }
            catch (WaitHandleCannotBeOpenedException)
            {
            }
            return 0;
        }

        var paths = AppPaths.Default();
        var log = new AppLog(paths.Logs);
        log.Info("start", $"GrindPilot {typeof(Program).Assembly.GetName().Version} starting; elevated: {(Elevation.IsElevated() ? "yes" : "no")}");
        LogMove(log, move);
        new UpdateService(new CredentialManagerTokenStore(), GameRunningCheck.ForThisPc(), log).ApplyPendingIfSafe(restart: true);

        return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args, Avalonia.Controls.ShutdownMode.OnExplicitShutdown);
    }

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace();

    internal static void LogMove(AppLog log, DataMoveResult move)
    {
        if (move.Outcome == DataMoveOutcome.Failed)
        {
            log.Warn("data", move.Describe());
        }
        else if (move.Outcome is DataMoveOutcome.Moved or DataMoveOutcome.BothPresent)
        {
            log.Info("data", move.Describe());
        }
    }
}

/// <summary>
/// <c>GrindPilot.exe --update-check</c>: runs the update service once with no window and exits. The release workflow
/// uses it to prove an installed release picks up and applies the next one. A token in <c>GRINDPILOT_RELEASES_TOKEN</c>
/// is used when set (a private fork's CI); otherwise Credential Manager's, if one is saved; otherwise none.
/// </summary>
internal static class UpdateCheckMode
{
    public const string TokenVariable = "GRINDPILOT_RELEASES_TOKEN";

    public static int Run(DataMoveResult move)
    {
        var paths = AppPaths.Default();
        var log = new AppLog(paths.Logs);
        Program.LogMove(log, move);
        ITokenStore tokens = Environment.GetEnvironmentVariable(TokenVariable) is { Length: > 0 }
            ? new EnvironmentTokenStore(TokenVariable)
            : new CredentialManagerTokenStore();
        var updates = new UpdateService(tokens, GameRunningCheck.ForThisPc(), log);
        log.Info("update-check", $"installed {updates.InstalledVersion ?? "none"}");

        updates.ApplyPendingIfSafe(restart: false); // exits the process when it applies an update

        var status = updates.CheckAndDownloadAsync().GetAwaiter().GetResult();
        log.Info("update-check", $"{status.State}: {status.Detail}");
        Directory.CreateDirectory(paths.Root);
        File.WriteAllText(Path.Combine(paths.Root, "update-check.txt"), $"{status.State}{Environment.NewLine}{status.Detail}{Environment.NewLine}");
        return status.State is UpdateState.UpToDate or UpdateState.Downloaded ? 0 : 3;
    }
}
