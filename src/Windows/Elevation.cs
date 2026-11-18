using System.Security.Principal;

namespace GrindPilot.Windows;

/// <summary>Whether this process runs elevated. Velopack installs per user with no admin prompt; this shows it on the status page.</summary>
public static class Elevation
{
    public static bool IsElevated()
    {
        using var identity = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }
}
