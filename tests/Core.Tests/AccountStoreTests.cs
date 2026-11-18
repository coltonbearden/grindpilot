using GrindPilot.Core.Store;

namespace GrindPilot.Core.Tests;

/// <summary>The store's tests, one or more per line of 03-account-state.md, 3.7.</summary>
public sealed class AccountStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "grindpilot-tests", Guid.NewGuid().ToString("N"));
    private readonly AccountStore _store;
    private static readonly DateTimeOffset T0 = new(2026, 11, 10, 11, 0, 0, TimeSpan.FromHours(-5));

    public AccountStoreTests()
    {
        _store = AccountStore.Open(Path.Combine(_dir, "state.db"), TestCatalog.Build());
    }

    public void Dispose()
    {
        _store.Dispose();
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        try
        {
            Directory.Delete(_dir, true);
        }
        catch (IOException)
        {
        }
    }

    private long Capture(DateTimeOffset at, string? sha = null) =>
        _store.AddCapture(new NewCapture(CaptureKind.Screenshot, sha ?? Guid.NewGuid().ToString("N"), null, at));

    private long Observe(string key, string? value, Outcome outcome, DateTimeOffset at, long? capture = null) =>
        _store.AddObservation(new NewObservation(capture ?? Capture(at), "test screen", "test-1", key, value, outcome, at));

    // 3.7: "Rebuilding the current state from observations gives the same state as the running store."

    [Fact]
    public void Rebuild_matches_after_a_simple_history()
    {
        Observe("player.level", "3", Outcome.Confirmed, T0);
        Observe("player.level", "4", Outcome.Read, T0.AddHours(1));
        Observe("player.prestige", "4", Outcome.Confirmed, T0);
        var check = _store.CheckRebuild();
        Assert.True(check.Matches, string.Join(", ", check.Differences));
        Assert.Equal(2, check.Keys);
        Assert.Equal(3, check.Observations);
    }

    [Fact]
    public void Rebuild_matches_after_random_histories()
    {
        // A seeded random history: values up and down, unread and rejected reads, Prestige changes, captures close in
        // time and out of order. The store updates only the touched key, except for a Prestige change, which updates
        // every key; the rebuild works everything out again from the observations alone.
        string[] keys = ["player.level", "player.prestige", "weapon.kastov-762.unlocks", "weapon.kastov-762.camos.military", "daily.1.progress"];
        Outcome[] outcomes = [Outcome.Confirmed, Outcome.Read, Outcome.ReadSingle, Outcome.Unread, Outcome.Rejected];
        for (var seed = 1; seed <= 12; seed++)
        {
            using var store = AccountStore.Open(Path.Combine(_dir, $"random-{seed}.db"), TestCatalog.Build());
            var rng = new Random(seed);
            for (var i = 0; i < 60; i++)
            {
                var key = keys[rng.Next(keys.Length)];
                var outcome = outcomes[rng.Next(outcomes.Length)];
                var at = T0.AddSeconds(rng.Next(0, 30 * 24 * 3600)); // spans the Season 1 start
                if (rng.Next(4) == 0)
                {
                    at = T0.AddSeconds(rng.Next(0, 120)); // several captures within a minute of each other
                }
                var value = key switch
                {
                    "player.level" => rng.Next(1, 56).ToString(System.Globalization.CultureInfo.InvariantCulture),
                    "player.prestige" => rng.Next(0, 11).ToString(System.Globalization.CultureInfo.InvariantCulture),
                    "weapon.kastov-762.camos.military" => $"{rng.Next(0, 9)}/8",
                    "daily.1.progress" => $"{rng.Next(0, 4)}/3",
                    _ => $"{rng.Next(0, 72)}/71",
                };
                var capture = store.AddCapture(new NewCapture(CaptureKind.Screenshot, null, null, at));
                store.AddObservation(new NewObservation(capture, "s", "p", key, value, outcome, at));
            }
            var check = store.CheckRebuild();
            Assert.True(check.Matches, $"seed {seed}: {string.Join(", ", check.Differences)}");
        }
    }

    // 3.7: "An unread observation never changes a current value, only its staleness."

    [Fact]
    public void Unread_keeps_the_value_and_marks_it_stale()
    {
        Observe("player.level", "3", Outcome.Confirmed, T0);
        Observe("player.level", null, Outcome.Unread, T0.AddHours(2));
        var s = _store.StateOf("player.level");
        Assert.Equal("3", s.Value);
        Assert.Equal(StateStatus.Stale, s.Status);
        Assert.Equal(T0.AddHours(2), s.StaleSince);
        Assert.Equal("Stale", StateText.Sureness(s));
    }

    [Fact]
    public void A_good_read_after_unread_clears_staleness()
    {
        Observe("player.level", "3", Outcome.Confirmed, T0);
        Observe("player.level", null, Outcome.Unread, T0.AddHours(2));
        Observe("player.level", "5", Outcome.Read, T0.AddHours(3));
        var s = _store.StateOf("player.level");
        Assert.Equal("5", s.Value);
        Assert.Equal(StateStatus.Current, s.Status);
        Assert.Null(s.StaleSince);
    }

    [Fact]
    public void Unread_alone_leaves_the_key_unknown()
    {
        Observe("player.level", null, Outcome.Unread, T0);
        var s = _store.StateOf("player.level");
        Assert.Equal(StateStatus.Unknown, s.Status);
        Assert.Null(s.Value);
    }

    [Fact]
    public void A_rejected_read_never_becomes_the_value()
    {
        Observe("player.level", "3", Outcome.Confirmed, T0);
        Observe("player.level", "30", Outcome.Rejected, T0.AddHours(1));
        Assert.Equal("3", _store.StateOf("player.level").Value);
    }

    // 3.7: "Unknown is never rendered or summed as zero."

    [Fact]
    public void Unknown_renders_as_unknown_not_zero()
    {
        var s = _store.StateOf("weapon.pila.unlocks");
        Assert.Equal(StateStatus.Unknown, s.Status);
        Assert.Equal("Unknown", StateText.Value(s));
        Assert.NotEqual("0", StateText.Value(s));
    }

    [Fact]
    public void A_sum_with_an_unknown_part_is_unknown()
    {
        Observe("weapon.kastov-762.camos.military", "8/8", Outcome.Confirmed, T0);
        var known = _store.StateOf("weapon.kastov-762.camos.military");
        var unknown = _store.StateOf("weapon.kastov-762.camos.special");
        Assert.Equal(8, StateText.Sum([known]));
        Assert.Null(StateText.Sum([known, unknown]));
        Assert.Equal("Unknown", StateText.SumText([known, unknown]));
    }

    // 3.7: "A value below the last confirmed one is held unless a Prestige or season change sits between them."

    [Fact]
    public void A_drop_without_a_reason_is_held()
    {
        Observe("weapon.kastov-762.unlocks", "48/71", Outcome.Confirmed, T0);
        Observe("weapon.kastov-762.unlocks", "12/71", Outcome.Read, T0.AddHours(1));
        var s = _store.StateOf("weapon.kastov-762.unlocks");
        Assert.Equal("48/71", s.Value);
        Assert.Equal(StateEngine.HeldDrop, s.Held);
        Assert.True(_store.CheckRebuild().Matches);
    }

    [Fact]
    public void A_drop_after_a_prestige_change_is_taken()
    {
        Observe("player.prestige", "4", Outcome.Confirmed, T0);
        Observe("player.level", "40", Outcome.Confirmed, T0.AddMinutes(5));
        Observe("player.prestige", "5", Outcome.Read, T0.AddHours(1));
        Observe("player.level", "2", Outcome.Read, T0.AddHours(2));
        var s = _store.StateOf("player.level");
        Assert.Equal("2", s.Value);
        Assert.Null(s.Held);
        Assert.True(_store.CheckRebuild().Matches);
    }

    [Fact]
    public void A_prestige_change_observed_later_releases_a_held_drop()
    {
        Observe("player.prestige", "4", Outcome.Confirmed, T0);
        Observe("player.level", "40", Outcome.Confirmed, T0.AddMinutes(5));
        Observe("player.level", "2", Outcome.Read, T0.AddHours(2));
        Assert.Equal("40", _store.StateOf("player.level").Value);

        // The Prestige screen read afterwards shows the change happened between the two level reads.
        Observe("player.prestige", "5", Outcome.Read, T0.AddHours(1));
        Assert.Equal("2", _store.StateOf("player.level").Value);
        Assert.True(_store.CheckRebuild().Matches);
    }

    [Fact]
    public void A_drop_across_a_season_start_is_taken()
    {
        var before = new DateTimeOffset(2026, 10, 25, 20, 0, 0, TimeSpan.FromHours(-4));
        var after = new DateTimeOffset(2026, 10, 29, 20, 0, 0, TimeSpan.FromHours(-4));
        Observe("player.level", "50", Outcome.Confirmed, before);
        Observe("player.level", "1", Outcome.Read, after);
        Assert.Equal("1", _store.StateOf("player.level").Value);
        var season = _store.ObservationsFor("player.level").Select(o => o.Season).ToList();
        Assert.Equal(["Launch", "Season 1"], season);
    }

    // 3.6: "Two captures disagree within a minute."

    [Fact]
    public void Two_captures_disagreeing_within_a_minute_are_both_set_aside()
    {
        Observe("player.level", "3", Outcome.Confirmed, T0);
        Observe("player.level", "7", Outcome.Read, T0.AddHours(1));
        Observe("player.level", "8", Outcome.Read, T0.AddHours(1).AddSeconds(30));
        var s = _store.StateOf("player.level");
        Assert.Equal("3", s.Value);
        Assert.Equal(StateEngine.HeldDisagreement, s.Held);
    }

    [Fact]
    public void Two_captures_agreeing_within_a_minute_are_fine()
    {
        Observe("player.level", "7", Outcome.Read, T0);
        Observe("player.level", "7", Outcome.Read, T0.AddSeconds(30));
        var s = _store.StateOf("player.level");
        Assert.Equal("7", s.Value);
        Assert.Null(s.Held);
    }

    // 3.7: "The store refuses a name outside the catalog and any value matching the account-number pattern."

    [Theory]
    [InlineData("loadout.01.secondary", "SOME OTHER GUN")]
    [InlineData("loadout.01.tactical", "Flash Bang")]
    [InlineData("weapon.not-a-weapon.unlocks", "3/70")]
    [InlineData("mastery-set.made-up-mastery.progress", "1/6")]
    public void Names_outside_the_catalog_are_refused(string key, string value)
    {
        Assert.Throws<RefusedValueException>(() => Observe(key, value, Outcome.Read, T0));
        Assert.Equal(0, _store.Counts().Observations);
    }

    [Theory]
    [InlineData("daily.1.text", "Player#1234567 joined")]
    [InlineData("daily.1.text", "Get 3 kills #42")]
    [InlineData("daily.1.text", "Code 12345678")]
    [InlineData("build.kastov-762.mine.name", "MY BUILD 9999999")]
    public void Account_number_patterns_are_refused(string key, string value)
    {
        Assert.Throws<RefusedValueException>(() => Observe(key, value, Outcome.Confirmed, T0));
        Assert.Equal(0, _store.Counts().Observations);
    }

    [Fact]
    public void A_key_without_a_rule_is_refused() =>
        Assert.Throws<RefusedValueException>(() => Observe("player.name", "anything", Outcome.Confirmed, T0));

    [Theory]
    [InlineData("player.level", "56")]
    [InlineData("player.level", "0")]
    [InlineData("weapon.kastov-762.camos.unlocked", "16/15")]
    [InlineData("daily.1.progress", "4/3")]
    [InlineData("daily.countdown", "25H 1M")]
    [InlineData("prestige.4.status", "MAYBE")]
    public void Values_outside_their_rule_are_refused(string key, string value) =>
        Assert.Throws<RefusedValueException>(() => Observe(key, value, Outcome.Read, T0));

    [Fact]
    public void Names_are_stored_in_the_catalog_spelling()
    {
        Observe("loadout.01.secondary", "PlLA", Outcome.Read, T0); // a lower-case l read for I counts as a look-alike
        Observe("loadout.01.tactical", "SMOKE GRENADE", Outcome.Confirmed, T0);
        Assert.Equal("PILA", _store.StateOf("loadout.01.secondary").Value);
        Assert.Equal("Smoke Grenade", _store.StateOf("loadout.01.tactical").Value);
    }

    [Fact]
    public void Count_pairs_are_stored_plainly()
    {
        Observe("weapon.kastov-762.camos.military", "MILITARY [5/8]".Replace("MILITARY ", "", StringComparison.Ordinal), Outcome.Read, T0);
        Observe("weapon.kastov-762.unlocks", "23 /71", Outcome.Read, T0);
        Assert.Equal("5/8", _store.StateOf("weapon.kastov-762.camos.military").Value);
        Assert.Equal("23/71", _store.StateOf("weapon.kastov-762.unlocks").Value);
    }

    // 3.2: "Observations are never changed or deleted."

    [Fact]
    public void Observations_can_be_neither_changed_nor_deleted()
    {
        Observe("player.level", "3", Outcome.Confirmed, T0);
        var update = Assert.Throws<Microsoft.Data.Sqlite.SqliteException>(() => _store.ExecuteRaw("UPDATE observations SET value = '9';"));
        Assert.Contains("never changed", update.Message, StringComparison.Ordinal);
        var delete = Assert.Throws<Microsoft.Data.Sqlite.SqliteException>(() => _store.ExecuteRaw("DELETE FROM observations;"));
        Assert.Contains("never deleted", delete.Message, StringComparison.Ordinal);
        Assert.Equal("3", _store.ObservationsFor("player.level").Single().Value);
    }

    [Fact]
    public void A_capture_with_the_same_sha256_is_stored_once()
    {
        var a = Capture(T0, "abc");
        var b = Capture(T0.AddHours(1), "abc");
        Assert.Equal(a, b);
        Assert.Equal(1, _store.Counts().Captures);
    }

    [Fact]
    public void Every_value_traces_to_its_capture()
    {
        var capture = _store.AddCapture(new NewCapture(CaptureKind.Screenshot, "def", @"C:\captures\one.png", T0));
        Observe("player.level", "3", Outcome.Confirmed, T0, capture);
        var s = _store.StateOf("player.level");
        var obs = _store.ObservationsFor("player.level").Single(o => o.Id == s.ObservationId);
        var c = _store.Capture(obs.CaptureId)!;
        Assert.Equal(@"C:\captures\one.png", c.Path);
        Assert.Equal(T0, s.ObservedAt);
        Assert.Equal("Confirmed", StateText.Sureness(s));
    }

    // 3.1: backups.

    [Fact]
    public void The_daily_backup_runs_only_when_something_changed()
    {
        var time = new ManualTime(T0);
        using var store = AccountStore.Open(Path.Combine(_dir, "b.db"), TestCatalog.Build(), Path.Combine(_dir, "b-backups"), time);
        Assert.Null(store.BackupIfDue());

        var c = store.AddCapture(new NewCapture(CaptureKind.Screenshot, "x", null, T0));
        store.AddObservation(new NewObservation(c, "s", "p", "player.level", "3", Outcome.Confirmed, T0));
        var first = store.BackupIfDue();
        Assert.NotNull(first);
        Assert.True(File.Exists(first));

        time.Advance(TimeSpan.FromHours(2));
        store.AddObservation(new NewObservation(c, "s", "p", "player.level", "4", Outcome.Confirmed, T0.AddHours(2)));
        Assert.Null(store.BackupIfDue()); // less than a day since the last one

        time.Advance(TimeSpan.FromDays(1));
        Assert.NotNull(store.BackupIfDue());
    }

    [Fact]
    public void Only_the_newest_14_backups_are_kept()
    {
        var time = new ManualTime(T0);
        using var store = AccountStore.Open(Path.Combine(_dir, "k.db"), TestCatalog.Build(), Path.Combine(_dir, "k-backups"), time);
        for (var i = 0; i < 20; i++)
        {
            store.Backup("test");
            time.Advance(TimeSpan.FromDays(1));
        }
        Assert.Equal(AccountStore.BackupsKept, Directory.GetFiles(Path.Combine(_dir, "k-backups"), "state-*.db").Length);
    }

    [Fact]
    public void A_backup_is_a_whole_store()
    {
        Observe("player.level", "3", Outcome.Confirmed, T0);
        var file = _store.Backup("test");
        using var copy = AccountStore.Open(file, TestCatalog.Build(), Path.Combine(_dir, "copy-backups"));
        Assert.Equal("3", copy.StateOf("player.level").Value);
        Assert.True(copy.CheckRebuild().Matches);
    }

    private sealed class ManualTime(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;
        public void Advance(TimeSpan by) => _now += by;
        public override DateTimeOffset GetUtcNow() => _now.ToUniversalTime();
    }
}
