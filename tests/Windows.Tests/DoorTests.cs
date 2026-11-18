namespace GrindPilot.Windows.Tests;

public class GameRunningTests
{
    private sealed class Signals(long? appId, params string[] names) : IGameSignals
    {
        public long? SteamRunningAppId() => appId;
        public IReadOnlyCollection<string> ProcessNames() => names;
    }

    [Fact]
    public void Steam_reporting_the_game_counts() =>
        Assert.True(new GameRunningCheck(new Signals(1938090)).Check().Running);

    [Fact]
    public void Another_steam_game_does_not_count_as_call_of_duty() =>
        Assert.False(new GameRunningCheck(new Signals(570, "explorer")).Check().Running);

    [Theory]
    [InlineData("cod")]
    [InlineData("COD")]
    [InlineData("sp26-cod")]
    public void The_game_process_names_count(string name) =>
        Assert.True(new GameRunningCheck(new Signals(0, "explorer", name)).Check().Running);

    [Fact]
    public void Nothing_running_is_not_running()
    {
        var state = new GameRunningCheck(new Signals(null, "explorer", "codex", "cod-helper")).Check();
        Assert.False(state.Running);
        Assert.Equal("not running", state.Reason);
    }

    [Fact]
    public void The_real_signals_can_be_read_on_this_pc()
    {
        // Reads Steam's registry value and the process names; asserts only that the read works.
        var state = GameRunningCheck.ForThisPc().Check();
        Assert.False(string.IsNullOrWhiteSpace(state.Reason));
    }
}

public class TokenStoreTests
{
    [Fact]
    public void A_token_round_trips_through_credential_manager()
    {
        var store = new CredentialManagerTokenStore("grindpilot-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            Assert.Null(store.Read());
            store.Save("  test-value-not-a-token  ");
            Assert.Equal("test-value-not-a-token", store.Read());
            store.Delete();
            Assert.Null(store.Read());
        }
        finally
        {
            store.Delete();
        }
    }

    [Fact]
    public void An_empty_token_is_refused() =>
        Assert.Throws<ArgumentException>(() => new CredentialManagerTokenStore("grindpilot-test-empty").Save("   "));

    [Fact]
    public void The_app_uses_the_item_name_from_the_spec() =>
        Assert.Equal("grindpilot-releases-token", new CredentialManagerTokenStore().TargetName);

    [Fact]
    public void An_environment_token_is_read_only()
    {
        var store = new EnvironmentTokenStore("GRINDPILOT_TEST_TOKEN_" + Guid.NewGuid().ToString("N"));
        Assert.Null(store.Read());
        Assert.Throws<NotSupportedException>(() => store.Save("x"));
    }
}
