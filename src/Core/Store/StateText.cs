namespace GrindPilot.Core.Store;

/// <summary>How a value is shown and counted. Unknown is never rendered or summed as zero (3.2, 3.7).</summary>
public static class StateText
{
    public const string UnknownText = "Unknown";

    public static string Value(KeyState s) => s.Status == StateStatus.Unknown || s.Value is null ? UnknownText : s.Value;

    /// <summary>The sureness label of 3.3.</summary>
    public static string Sureness(KeyState s) => s.Status switch
    {
        StateStatus.Unknown => "Unknown",
        StateStatus.Stale => "Stale",
        _ => s.Outcome switch
        {
            Outcome.Confirmed => "Confirmed",
            Outcome.Read => "Read",
            Outcome.ReadSingle => "Read, one engine",
            _ => "Unknown",
        },
    };

    /// <summary>
    /// Adds up a number from each state. If any of them is unknown (or isn't a number), the total is unknown too, never a
    /// smaller number that hides the gap.
    /// </summary>
    public static int? Sum(IEnumerable<KeyState> states)
    {
        var total = 0;
        foreach (var s in states)
        {
            if (s.Status == StateStatus.Unknown || KeySchema.OrderingNumber(s.Value) is not { } n)
            {
                return null;
            }
            total += n;
        }
        return total;
    }

    public static string SumText(IEnumerable<KeyState> states) =>
        Sum(states) is { } n ? n.ToString(System.Globalization.CultureInfo.InvariantCulture) : UnknownText;
}
