namespace GrindPilot.Core;

/// <summary>
/// Where the app's data lives: <c>%LOCALAPPDATA%\GrindPilot-data\</c>, apart from the program's own folder
/// <c>%LOCALAPPDATA%\GrindPilot\</c>, which an uninstall deletes whole. Windows paths ignore case, so the data folder
/// must never be spelled like the program folder. Tests and CI point it elsewhere with <c>GRINDPILOT_DATA</c>.
/// Builds before 0.2.0 kept their data in <c>%LOCALAPPDATA%\mw4-grind\</c>; <see cref="DataFolderMove"/> moves it once.
/// </summary>
public sealed class AppPaths
{
    public const string DataFolderName = "GrindPilot-data";
    public const string LegacyDataFolderName = "mw4-grind";
    public const string DataOverrideVariable = "GRINDPILOT_DATA";

    public string Root { get; }

    public AppPaths(string root) => Root = root;

    /// <summary>
    /// The override when set; otherwise the data folder, or the pre-0.2.0 folder while a move hasn't happened yet (a move
    /// that failed leaves the old folder in use for that start, and the next start tries again).
    /// </summary>
    public static AppPaths Default()
    {
        var overridden = Environment.GetEnvironmentVariable(DataOverrideVariable);
        if (!string.IsNullOrWhiteSpace(overridden))
        {
            return new AppPaths(overridden);
        }
        return new AppPaths(ResolveUnder(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)));
    }

    /// <summary>The folder to use under a given local-app-data root (split out for tests).</summary>
    public static string ResolveUnder(string localAppData)
    {
        var current = Path.Combine(localAppData, DataFolderName);
        var legacy = Path.Combine(localAppData, LegacyDataFolderName);
        if (Directory.Exists(current) || !Directory.Exists(legacy))
        {
            return current;
        }
        return legacy;
    }

    public string StateDb => Path.Combine(Root, "state.db");
    public string Backups => Path.Combine(Root, "backups");
    public string Logs => Path.Combine(Root, "logs");
    public string Settings => Path.Combine(Root, "settings.json");
    public string TestLibrary => Path.Combine(Root, "test-library");
}

/// <summary>The data that ships beside the program: the catalog.</summary>
public sealed class ShippedData
{
    public string Root { get; }

    private ShippedData(string root, string catalogFolder)
    {
        Root = root;
        CatalogFolder = catalogFolder;
    }

    /// <summary>The <c>data</c> folder beside the running program.</summary>
    public static ShippedData BesideProgram()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "data");
        return new ShippedData(root, Path.Combine(root, "catalog"));
    }

    /// <summary>The repo's own <c>catalog/</c>, for tests.</summary>
    public static ShippedData FromRepo(string repoRoot) => new(repoRoot, Path.Combine(repoRoot, "catalog"));

    public string CatalogFolder { get; }

    /// <summary>Walks up from a folder to the repo root (the folder holding <c>GrindPilot.slnx</c>).</summary>
    public static string FindRepoRoot(string start)
    {
        var dir = new DirectoryInfo(start);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "GrindPilot.slnx")))
        {
            dir = dir.Parent;
        }
        return dir?.FullName ?? throw new DirectoryNotFoundException($"No GrindPilot.slnx above {start}.");
    }
}
