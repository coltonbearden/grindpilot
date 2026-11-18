using System.Net;
using System.Reflection;
using GrindPilot.Core;
using Velopack;
using Velopack.Sources;

namespace GrindPilot.Windows;

public enum UpdateState
{
    NotInstalled,
    WaitingForGame,
    Checking,
    UpToDate,
    Downloaded,
    TokenRefused,
    Failed,
}

/// <summary>What the "Inputs and status" page shows about updates.</summary>
public sealed record UpdateStatus(UpdateState State, string Detail, DateTimeOffset? LastCheck, string? Pending);

/// <summary>
/// The door to the repo's GitHub releases (08-app-shape-and-stack.md, 8.6). It checks when the app starts and once a
/// day while it is open, downloads in the background, and applies the update the next time the app starts. It never
/// checks, downloads or applies while the game is running. Public releases need no token; a token from
/// <see cref="ITokenStore"/> is used only when one is saved (a build that updates from a private fork), and goes only
/// to Velopack's GitHub source, which sends it as an <c>Authorization</c> header.
/// </summary>
public sealed class UpdateService
{
    /// <summary>
    /// The repo whose releases this build updates from: the <c>UpdateRepoUrl</c> build property (Directory.Build.props;
    /// the release workflow sets it to the repo it runs in, so a fork updates from its own releases).
    /// </summary>
    public static string RepoUrl { get; } =
        typeof(UpdateService).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "UpdateRepoUrl")?.Value is { Length: > 0 } url
            ? url
            : throw new InvalidOperationException("This build has no UpdateRepoUrl.");

    private readonly ITokenStore _tokens;
    private readonly GameRunningCheck _game;
    private readonly AppLog _log;
    private readonly Func<string?, UpdateManager> _managerFactory;

    public UpdateStatus Status { get; private set; } = new(UpdateState.NotInstalled, "Not checked yet", null, null);

    public event Action<UpdateStatus>? Changed;

    public UpdateService(ITokenStore tokens, GameRunningCheck game, AppLog log, Func<string?, UpdateManager>? managerFactory = null)
    {
        _tokens = tokens;
        _game = game;
        _log = log;
        _managerFactory = managerFactory ?? (token => new UpdateManager(new GithubSource(RepoUrl, token, prerelease: false)));
    }

    /// <summary>The installed version, or null when the app runs from a build folder rather than an install.</summary>
    public string? InstalledVersion
    {
        get
        {
            try
            {
                return _managerFactory(null).CurrentVersion?.ToString();
            }
            catch (Exception)
            {
                return null;
            }
        }
    }

    /// <summary>
    /// At start: applies an update downloaded earlier, if the game isn't running. Applying exits this process, so it
    /// returns only when nothing was applied. <paramref name="restart"/> is false in the window-less check mode.
    /// </summary>
    public void ApplyPendingIfSafe(bool restart = true)
    {
        UpdateManager mgr;
        try
        {
            mgr = _managerFactory(null);
        }
        catch (InvalidOperationException)
        {
            return;
        }
        if (!mgr.IsInstalled)
        {
            return;
        }
        var pending = mgr.UpdatePendingRestart;
        if (pending is null)
        {
            return;
        }
        var game = _game.Check();
        if (game.Running)
        {
            _log.Info("update", $"{pending.Version} is downloaded but not applied: {game.Reason}");
            Set(new UpdateStatus(UpdateState.WaitingForGame, $"{pending.Version} downloaded; applies at a start when the game is closed", Status.LastCheck, pending.Version.ToString()));
            return;
        }
        _log.Info("update", $"applying {pending.Version} (installed {mgr.CurrentVersion})");
        if (restart)
        {
            mgr.ApplyUpdatesAndRestart(pending);
        }
        else
        {
            mgr.ApplyUpdatesAndExit(pending);
        }
    }

    /// <summary>Checks this repo's releases and downloads a newer one. Applying waits for the next start.</summary>
    public async Task<UpdateStatus> CheckAndDownloadAsync(CancellationToken cancel = default)
    {
        var now = DateTimeOffset.Now;
        bool installed;
        try
        {
            installed = _managerFactory(null).IsInstalled;
        }
        catch (InvalidOperationException)
        {
            installed = false; // VelopackApp.Run() wasn't called: a test, or a build run outside Velopack
        }
        if (!installed)
        {
            return Set(new UpdateStatus(UpdateState.NotInstalled, "Running from a build folder, not an install: no updates", now, null));
        }
        var game = _game.Check();
        if (game.Running)
        {
            _log.Info("update", $"check skipped: {game.Reason}");
            return Set(new UpdateStatus(UpdateState.WaitingForGame, "Not checked while the game is running", Status.LastCheck, Status.Pending));
        }
        string? token;
        try
        {
            token = _tokens.Read();
        }
        catch (Exception ex)
        {
            _log.Error("update", "the token couldn't be read", ex);
            return Set(new UpdateStatus(UpdateState.Failed, "The update token couldn't be read from Credential Manager", now, null));
        }
        Set(new UpdateStatus(UpdateState.Checking, "Checking GitHub releases", Status.LastCheck, Status.Pending));
        try
        {
            var mgr = _managerFactory(token);
            if (mgr.UpdatePendingRestart is { } already)
            {
                return Set(new UpdateStatus(UpdateState.Downloaded, $"{already.Version} downloaded; applies at the next start", now, already.Version.ToString()));
            }
            var info = await mgr.CheckForUpdatesAsync().ConfigureAwait(false);
            if (info is null)
            {
                _log.Info("update", $"up to date at {mgr.CurrentVersion}");
                return Set(new UpdateStatus(UpdateState.UpToDate, $"Up to date ({mgr.CurrentVersion})", now, null));
            }
            var version = info.TargetFullRelease.Version.ToString();
            _log.Info("update", $"downloading {version}");
            await mgr.DownloadUpdatesAsync(info, cancelToken: cancel).ConfigureAwait(false);
            _log.Info("update", $"downloaded {version}; applies at the next start");
            return Set(new UpdateStatus(UpdateState.Downloaded, $"{version} downloaded; applies at the next start", now, version));
        }
        catch (HttpRequestException ex) when (token is not null && ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.NotFound)
        {
            _log.Warn("update", $"GitHub refused the token ({(int)ex.StatusCode!})");
            return Set(new UpdateStatus(UpdateState.TokenRefused, $"GitHub refused the saved token ({(int)ex.StatusCode!}): it may have expired, or remove it in Settings", now, null));
        }
        catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden)
        {
            _log.Warn("update", $"GitHub answered {(int)ex.StatusCode!} for the releases of {RepoUrl}");
            return Set(new UpdateStatus(UpdateState.Failed, $"GitHub answered {(int)ex.StatusCode!} for the releases (a private repo needs a token in Settings)", now, null));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _log.Error("update", "check failed", ex);
            return Set(new UpdateStatus(UpdateState.Failed, $"Check failed ({ex.GetType().Name})", now, null));
        }
    }

    private UpdateStatus Set(UpdateStatus s)
    {
        Status = s;
        Changed?.Invoke(s);
        return s;
    }
}
