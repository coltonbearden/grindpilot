using System.Globalization;
using Microsoft.Data.Sqlite;
using GrindPilot.Core.Catalog;

namespace GrindPilot.Core.Store;

public sealed record RebuildCheck(int Keys, int Observations, bool Matches, IReadOnlyList<string> Differences);

public sealed record StoreCounts(int Captures, int Observations, int Keys, int KnownKeys, int StaleKeys, int HeldKeys);

/// <summary>
/// The account store: one SQLite file holding captures, observations and the current state (03-account-state.md).
/// It is the only copy the app writes. Observations are never changed or deleted; the current state can always be
/// rebuilt from them, and <see cref="CheckRebuild"/> proves it.
/// </summary>
public sealed class AccountStore : IDisposable
{
    public const int SchemaVersion = 1;
    public const int BackupsKept = 14;
    public static readonly TimeSpan BackupInterval = TimeSpan.FromDays(1);

    private readonly SqliteConnection _db;
    private readonly GameCatalog _catalog;
    private readonly KeySchema _schema;
    private readonly StateEngine _engine;
    private readonly TimeProvider _time;

    public string DatabasePath { get; }
    public string BackupsFolder { get; }
    public KeySchema Schema => _schema;
    public GameCatalog Catalog => _catalog;

    private AccountStore(string path, string backupsFolder, GameCatalog catalog, TimeProvider time)
    {
        DatabasePath = path;
        BackupsFolder = backupsFolder;
        _catalog = catalog;
        _schema = new KeySchema(catalog);
        _engine = new StateEngine(catalog);
        _time = time;
        _db = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ToString());
        _db.Open();
        Exec("PRAGMA foreign_keys = ON;");
    }

    /// <summary>Opens (or creates) the store. Before any change to an existing store's layout, a backup is taken first (3.1).</summary>
    public static AccountStore Open(string path, GameCatalog catalog, string? backupsFolder = null, TimeProvider? time = null)
    {
        var folder = Path.GetDirectoryName(Path.GetFullPath(path))!;
        Directory.CreateDirectory(folder);
        var store = new AccountStore(path, backupsFolder ?? Path.Combine(folder, "backups"), catalog, time ?? TimeProvider.System);
        try
        {
            store.Migrate();
        }
        catch
        {
            store.Dispose();
            throw;
        }
        return store;
    }

    private void Migrate()
    {
        Exec("CREATE TABLE IF NOT EXISTS meta (key TEXT PRIMARY KEY, value TEXT);");
        var version = int.Parse(GetMeta("schema_version") ?? "0", CultureInfo.InvariantCulture);
        if (version > SchemaVersion)
        {
            throw new InvalidDataException($"The store's layout is version {version}; this app knows up to {SchemaVersion}.");
        }
        if (version == SchemaVersion)
        {
            return;
        }
        if (version > 0)
        {
            Backup($"before-layout-{SchemaVersion}");
        }

        using var tx = _db.BeginTransaction();
        Exec("""
            CREATE TABLE captures (
              id INTEGER PRIMARY KEY,
              kind TEXT NOT NULL CHECK (kind IN ('screenshot', 'recording', 'live')),
              sha256 TEXT UNIQUE,
              path TEXT,
              taken_at TEXT NOT NULL,
              game_version TEXT,
              source TEXT
            );
            CREATE TABLE observations (
              id INTEGER PRIMARY KEY,
              capture_id INTEGER NOT NULL REFERENCES captures(id),
              screen TEXT NOT NULL,
              profile_version TEXT NOT NULL,
              key TEXT NOT NULL,
              value TEXT,
              outcome TEXT NOT NULL CHECK (outcome IN ('confirmed', 'read', 'read-single', 'unread', 'rejected')),
              reads TEXT,
              observed_at TEXT NOT NULL,
              season TEXT,
              recorded_at TEXT NOT NULL,
              note TEXT
            );
            CREATE INDEX observations_key ON observations(key);
            CREATE TRIGGER observations_never_changed BEFORE UPDATE ON observations
              BEGIN SELECT RAISE(ABORT, 'observations are never changed'); END;
            CREATE TRIGGER observations_never_deleted BEFORE DELETE ON observations
              BEGIN SELECT RAISE(ABORT, 'observations are never deleted'); END;
            CREATE TABLE current_state (
              key TEXT PRIMARY KEY,
              status TEXT NOT NULL,
              value TEXT,
              outcome TEXT,
              observation_id INTEGER,
              observed_at TEXT,
              stale_since TEXT,
              held TEXT,
              held_observation_id INTEGER
            );
            """, tx);
        SetMeta("schema_version", SchemaVersion.ToString(CultureInfo.InvariantCulture), tx);
        tx.Commit();
    }

    // ---- Layer 1: captures ----

    /// <summary>Adds a capture, or returns the existing one with the same sha256.</summary>
    public long AddCapture(NewCapture c)
    {
        if (c.Sha256 is not null)
        {
            using var find = Cmd("SELECT id FROM captures WHERE sha256 = $s;");
            find.Parameters.AddWithValue("$s", c.Sha256);
            if (find.ExecuteScalar() is long existing)
            {
                return existing;
            }
        }
        using var cmd = Cmd("""
            INSERT INTO captures (kind, sha256, path, taken_at, game_version, source)
            VALUES ($k, $s, $p, $t, $g, $src) RETURNING id;
            """);
        cmd.Parameters.AddWithValue("$k", c.Kind.ToDb());
        cmd.Parameters.AddWithValue("$s", (object?)c.Sha256 ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$p", (object?)c.Path ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$t", Iso(c.TakenAt));
        cmd.Parameters.AddWithValue("$g", (object?)c.GameVersion ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$src", (object?)c.Source ?? DBNull.Value);
        return (long)cmd.ExecuteScalar()!;
    }

    public CaptureRecord? Capture(long id)
    {
        using var cmd = Cmd("SELECT id, kind, sha256, path, taken_at, game_version, source FROM captures WHERE id = $id;");
        cmd.Parameters.AddWithValue("$id", id);
        using var r = cmd.ExecuteReader();
        if (!r.Read())
        {
            return null;
        }
        return new CaptureRecord(
            r.GetInt64(0),
            OutcomeText.CaptureKindFromDb(r.GetString(1)),
            r.IsDBNull(2) ? null : r.GetString(2),
            r.IsDBNull(3) ? null : r.GetString(3),
            ParseIso(r.GetString(4)),
            r.IsDBNull(5) ? null : r.GetString(5),
            r.IsDBNull(6) ? null : r.GetString(6));
    }

    // ---- Layer 2: observations ----

    /// <summary>
    /// Stores one observation, after the key and value pass the store's rules, and brings the current state up to date.
    /// Throws <see cref="RefusedValueException"/> for a value the store must refuse.
    /// </summary>
    public long AddObservation(NewObservation o)
    {
        var value = _schema.Normalize(o.Key, o.Value, o.Outcome);
        if (Capture(o.CaptureId) is null)
        {
            throw new RefusedValueException($"Capture {o.CaptureId} doesn't exist.");
        }

        using var tx = _db.BeginTransaction();
        using (var cmd = Cmd("""
            INSERT INTO observations (capture_id, screen, profile_version, key, value, outcome, reads, observed_at, season, recorded_at, note)
            VALUES ($c, $sc, $pv, $k, $v, $o, $r, $at, $se, $rec, $n) RETURNING id;
            """, tx))
        {
            cmd.Parameters.AddWithValue("$c", o.CaptureId);
            cmd.Parameters.AddWithValue("$sc", o.Screen);
            cmd.Parameters.AddWithValue("$pv", o.ProfileVersion);
            cmd.Parameters.AddWithValue("$k", o.Key);
            cmd.Parameters.AddWithValue("$v", (object?)value ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$o", o.Outcome.ToDb());
            cmd.Parameters.AddWithValue("$r", (object?)o.Reads ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$at", Iso(o.ObservedAt));
            cmd.Parameters.AddWithValue("$se", (object?)_catalog.SeasonAt(o.ObservedAt)?.Name ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$rec", Iso(_time.GetUtcNow()));
            cmd.Parameters.AddWithValue("$n", (object?)o.Note ?? DBNull.Value);
            var id = (long)cmd.ExecuteScalar()!;

            // A Prestige change can explain a drop in any other key, so it brings every key up to date.
            // Any other observation changes only its own key. CheckRebuild proves the two paths agree.
            if (o.Key == StateEngine.PrestigeKey)
            {
                var all = _engine.ComputeAll(AllObservations(tx));
                Exec("DELETE FROM current_state;", tx);
                foreach (var s in all.Values)
                {
                    WriteState(s, tx);
                }
            }
            else
            {
                var resets = StateEngine.ResetTimes(ObservationsFor(StateEngine.PrestigeKey, tx));
                WriteState(_engine.ComputeKey(o.Key, ObservationsFor(o.Key, tx), resets), tx);
            }
            SetMeta("changed_since_backup", "1", tx);
            tx.Commit();
            return id;
        }
    }

    public IReadOnlyList<ObservationRecord> ObservationsFor(string key) => ObservationsFor(key, null);

    public IReadOnlyList<ObservationRecord> AllObservations() => AllObservations(null);

    private List<ObservationRecord> ObservationsFor(string key, SqliteTransaction? tx)
    {
        using var cmd = Cmd(SelectObservations + " WHERE key = $k ORDER BY id;", tx);
        cmd.Parameters.AddWithValue("$k", key);
        return ReadObservations(cmd);
    }

    private List<ObservationRecord> AllObservations(SqliteTransaction? tx)
    {
        using var cmd = Cmd(SelectObservations + " ORDER BY id;", tx);
        return ReadObservations(cmd);
    }

    private const string SelectObservations =
        "SELECT id, capture_id, screen, profile_version, key, value, outcome, reads, observed_at, season, note FROM observations";

    private static List<ObservationRecord> ReadObservations(SqliteCommand cmd)
    {
        var list = new List<ObservationRecord>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new ObservationRecord(
                r.GetInt64(0),
                r.GetInt64(1),
                r.GetString(2),
                r.GetString(3),
                r.GetString(4),
                r.IsDBNull(5) ? null : r.GetString(5),
                OutcomeText.FromDb(r.GetString(6)),
                r.IsDBNull(7) ? null : r.GetString(7),
                ParseIso(r.GetString(8)),
                r.IsDBNull(9) ? null : r.GetString(9),
                r.IsDBNull(10) ? null : r.GetString(10)));
        }
        return list;
    }

    // ---- Layer 3: current state ----

    public IReadOnlyDictionary<string, KeyState> CurrentState()
    {
        using var cmd = Cmd("SELECT key, status, value, outcome, observation_id, observed_at, stale_since, held, held_observation_id FROM current_state;");
        using var r = cmd.ExecuteReader();
        var map = new Dictionary<string, KeyState>(StringComparer.Ordinal);
        while (r.Read())
        {
            var s = new KeyState(
                r.GetString(0),
                Enum.Parse<StateStatus>(r.GetString(1)),
                r.IsDBNull(2) ? null : r.GetString(2),
                r.IsDBNull(3) ? null : OutcomeText.FromDb(r.GetString(3)),
                r.IsDBNull(4) ? null : r.GetInt64(4),
                r.IsDBNull(5) ? null : ParseIso(r.GetString(5)),
                r.IsDBNull(6) ? null : ParseIso(r.GetString(6)),
                r.IsDBNull(7) ? null : r.GetString(7),
                r.IsDBNull(8) ? null : r.GetInt64(8));
            map[s.Key] = s;
        }
        return map;
    }

    /// <summary>The state of one key; a key never observed is unknown, never zero.</summary>
    public KeyState StateOf(string key) =>
        CurrentState().TryGetValue(key, out var s) ? s : new KeyState(key, StateStatus.Unknown, null, null, null, null, null, null, null);

    /// <summary>Rebuilds the current state from the observations alone and compares it with the store's own table (3.7).</summary>
    public RebuildCheck CheckRebuild()
    {
        var observations = AllObservations(null);
        var rebuilt = _engine.ComputeAll(observations);
        var stored = CurrentState();
        var differences = new List<string>();
        foreach (var key in rebuilt.Keys.Union(stored.Keys).Order(StringComparer.Ordinal))
        {
            rebuilt.TryGetValue(key, out var a);
            stored.TryGetValue(key, out var b);
            if (a != b)
            {
                differences.Add(key);
            }
        }
        return new RebuildCheck(rebuilt.Count, observations.Count, differences.Count == 0, differences);
    }

    public StoreCounts Counts()
    {
        var state = CurrentState();
        return new StoreCounts(
            Scalar("SELECT COUNT(*) FROM captures;"),
            Scalar("SELECT COUNT(*) FROM observations;"),
            state.Count,
            state.Values.Count(s => s.Status != StateStatus.Unknown),
            state.Values.Count(s => s.Status == StateStatus.Stale),
            state.Values.Count(s => s.Held is not null));
    }

    private void WriteState(KeyState s, SqliteTransaction tx)
    {
        using var cmd = Cmd("""
            INSERT INTO current_state (key, status, value, outcome, observation_id, observed_at, stale_since, held, held_observation_id)
            VALUES ($k, $st, $v, $o, $oid, $at, $ss, $h, $hid)
            ON CONFLICT(key) DO UPDATE SET status = excluded.status, value = excluded.value, outcome = excluded.outcome,
              observation_id = excluded.observation_id, observed_at = excluded.observed_at, stale_since = excluded.stale_since,
              held = excluded.held, held_observation_id = excluded.held_observation_id;
            """, tx);
        cmd.Parameters.AddWithValue("$k", s.Key);
        cmd.Parameters.AddWithValue("$st", s.Status.ToString());
        cmd.Parameters.AddWithValue("$v", (object?)s.Value ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$o", (object?)s.Outcome?.ToDb() ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$oid", (object?)s.ObservationId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$at", s.ObservedAt is { } at ? Iso(at) : DBNull.Value);
        cmd.Parameters.AddWithValue("$ss", s.StaleSince is { } ss ? Iso(ss) : DBNull.Value);
        cmd.Parameters.AddWithValue("$h", (object?)s.Held ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$hid", (object?)s.HeldObservationId ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    // ---- Backups (3.1) ----

    public DateTimeOffset? LastBackup => GetMeta("last_backup") is { } s ? ParseIso(s) : null;

    /// <summary>Takes the daily backup when something changed since the last one and a day has passed. Returns its path, or null.</summary>
    public string? BackupIfDue()
    {
        if (GetMeta("changed_since_backup") != "1")
        {
            return null;
        }
        if (LastBackup is { } last && _time.GetUtcNow() - last < BackupInterval)
        {
            return null;
        }
        return Backup("daily");
    }

    /// <summary>Copies the store into the backups folder with SQLite's backup API, so the copy is consistent, and keeps the newest 14.</summary>
    public string Backup(string reason)
    {
        Directory.CreateDirectory(BackupsFolder);
        var now = _time.GetUtcNow();
        var file = Path.Combine(BackupsFolder, $"state-{now:yyyyMMdd-HHmmss}-{reason}.db");
        for (var i = 2; File.Exists(file); i++)
        {
            file = Path.Combine(BackupsFolder, $"state-{now:yyyyMMdd-HHmmss}-{reason}-{i}.db");
        }
        using (var dest = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = file, Pooling = false }.ToString()))
        {
            dest.Open();
            _db.BackupDatabase(dest);
        }
        SetMeta("last_backup", Iso(now), null);
        SetMeta("changed_since_backup", "0", null);
        Prune();
        return file;
    }

    private void Prune()
    {
        var files = Directory.EnumerateFiles(BackupsFolder, "state-*.db")
            .OrderByDescending(f => Path.GetFileName(f), StringComparer.Ordinal)
            .Skip(BackupsKept)
            .ToList();
        foreach (var f in files)
        {
            File.Delete(f);
        }
    }

    // ---- Meta and helpers ----

    public string? GetMeta(string key)
    {
        using var cmd = Cmd("SELECT value FROM meta WHERE key = $k;");
        cmd.Parameters.AddWithValue("$k", key);
        return cmd.ExecuteScalar() as string;
    }

    public void SetMeta(string key, string value) => SetMeta(key, value, null);

    private void SetMeta(string key, string value, SqliteTransaction? tx)
    {
        using var cmd = Cmd("INSERT INTO meta (key, value) VALUES ($k, $v) ON CONFLICT(key) DO UPDATE SET value = excluded.value;", tx);
        cmd.Parameters.AddWithValue("$k", key);
        cmd.Parameters.AddWithValue("$v", value);
        cmd.ExecuteNonQuery();
    }

    /// <summary>For tests of the never-changed rule: runs raw SQL against the store.</summary>
    internal int ExecuteRaw(string sql) => Exec(sql);

    private int Scalar(string sql)
    {
        using var cmd = Cmd(sql);
        return Convert.ToInt32(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    private int Exec(string sql, SqliteTransaction? tx = null)
    {
        using var cmd = Cmd(sql, tx);
        return cmd.ExecuteNonQuery();
    }

    private SqliteCommand Cmd(string sql, SqliteTransaction? tx = null)
    {
        var cmd = _db.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = tx;
        return cmd;
    }

    internal static string Iso(DateTimeOffset t) => t.ToString("O", CultureInfo.InvariantCulture);

    internal static DateTimeOffset ParseIso(string s) =>
        DateTimeOffset.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

    public void Dispose()
    {
        _db.Dispose();
    }
}
