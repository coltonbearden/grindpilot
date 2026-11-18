using GrindPilot.Core;
using GrindPilot.Core.Catalog;
using GrindPilot.Core.Store;
using GrindPilot.Windows;

namespace GrindPilot.App;

/// <summary>Everything the window uses, made once at start. Tests make their own with a scratch data folder.</summary>
public sealed class AppServices : IDisposable
{
    public required AppPaths Paths { get; init; }
    public required ShippedData Data { get; init; }
    public required AppLog Log { get; init; }
    public required AppSettings Settings { get; init; }
    public required GameRunningCheck Game { get; init; }
    public required ITokenStore Tokens { get; init; }
    public required UpdateService Updates { get; init; }
    public GameCatalog? Catalog { get; init; }
    public AccountStore? Store { get; init; }

    /// <summary>What went wrong at start, in plain words, if anything did. The window still opens and says so.</summary>
    public IReadOnlyList<string> Problems { get; init; } = [];

    public KeyLabels? Labels => Catalog is null ? null : new KeyLabels(Catalog);

    public static AppServices Start(AppPaths paths, ShippedData data, ITokenStore tokens, IGameSignals? signals = null)
    {
        var log = new AppLog(paths.Logs);
        var game = signals is null ? GameRunningCheck.ForThisPc() : new GameRunningCheck(signals);
        var problems = new List<string>();
        GameCatalog? catalog = null;
        AccountStore? store = null;

        try
        {
            catalog = CatalogLoader.LoadFolder(data.CatalogFolder);
            log.Info("start", $"catalog: {catalog.Entries.Count} entries, {catalog.SourceTags.Count} sources");
        }
        catch (Exception ex)
        {
            problems.Add("The catalog couldn't be loaded, so the account can't be shown.");
            log.Error("start", "catalog load failed", ex);
        }

        if (catalog is not null)
        {
            try
            {
                store = AccountStore.Open(paths.StateDb, catalog, paths.Backups);
                var counts = store.Counts();
                log.Info("start", counts.Observations == 0
                    ? "account: empty (no observations yet)"
                    : $"account: {counts.Keys} keys from {counts.Observations} observations");
                if (store.BackupIfDue() is { } backup)
                {
                    log.Info("store", $"daily backup {Path.GetFileName(backup)}");
                }
            }
            catch (Exception ex)
            {
                problems.Add("The account store couldn't be opened.");
                log.Error("start", "store open failed", ex);
            }
        }

        return new AppServices
        {
            Paths = paths,
            Data = data,
            Log = log,
            Settings = AppSettings.Load(paths.Settings),
            Game = game,
            Tokens = tokens,
            Updates = new UpdateService(tokens, game, log),
            Catalog = catalog,
            Store = store,
            Problems = problems,
        };
    }

    public void SaveSettings()
    {
        try
        {
            Settings.Save(Paths.Settings);
        }
        catch (IOException ex)
        {
            Log.Error("settings", "save failed", ex);
        }
    }

    public void Dispose() => Store?.Dispose();
}
