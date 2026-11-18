namespace GrindPilot.Readers.Tests;

public class ReadersTests
{
    [Fact]
    public void No_reader_is_built_in_stage_1() =>
        Assert.Equal("Readers start in stage 2.", ReadersAssembly.Stage);
}
