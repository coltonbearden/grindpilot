namespace GrindPilot.Readers;

/// <summary>
/// Marks the Readers assembly. No reader is built in stage 1: the pipeline, profiles and engines arrive in stage 2
/// (docs/spec/04-capture-and-readers.md, 4.4 to 4.7).
/// </summary>
public static class ReadersAssembly
{
    public const string Stage = "Readers start in stage 2.";
}
