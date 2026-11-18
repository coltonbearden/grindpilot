namespace GrindPilot.Core.Store;

/// <summary>How sure a read was (03-account-state.md, 3.2 and 3.3).</summary>
public enum Outcome
{
    /// <summary>The user confirmed it on the review list or the proof page.</summary>
    Confirmed,
    /// <summary>Both engines agreed and every rule passed.</summary>
    Read,
    /// <summary>One engine passed the rules and the other didn't.</summary>
    ReadSingle,
    /// <summary>The field couldn't be read. It never changes a value, only its staleness.</summary>
    Unread,
    /// <summary>The user said the read was wrong.</summary>
    Rejected,
}

public enum CaptureKind
{
    Screenshot,
    Recording,
    Live,
}

public static class OutcomeText
{
    public static string ToDb(this Outcome o) => o switch
    {
        Outcome.Confirmed => "confirmed",
        Outcome.Read => "read",
        Outcome.ReadSingle => "read-single",
        Outcome.Unread => "unread",
        Outcome.Rejected => "rejected",
        _ => throw new ArgumentOutOfRangeException(nameof(o)),
    };

    public static Outcome FromDb(string s) => s switch
    {
        "confirmed" => Outcome.Confirmed,
        "read" => Outcome.Read,
        "read-single" => Outcome.ReadSingle,
        "unread" => Outcome.Unread,
        "rejected" => Outcome.Rejected,
        _ => throw new InvalidDataException($"Unknown outcome '{s}'."),
    };

    public static bool IsGood(this Outcome o) => o is Outcome.Confirmed or Outcome.Read or Outcome.ReadSingle;

    public static string ToDb(this CaptureKind k) => k switch
    {
        CaptureKind.Screenshot => "screenshot",
        CaptureKind.Recording => "recording",
        CaptureKind.Live => "live",
        _ => throw new ArgumentOutOfRangeException(nameof(k)),
    };

    public static CaptureKind CaptureKindFromDb(string s) => s switch
    {
        "screenshot" => CaptureKind.Screenshot,
        "recording" => CaptureKind.Recording,
        "live" => CaptureKind.Live,
        _ => throw new InvalidDataException($"Unknown capture kind '{s}'."),
    };
}

/// <summary>Layer 1: one thing that was read.</summary>
public sealed record CaptureRecord(
    long Id,
    CaptureKind Kind,
    string? Sha256,
    string? Path,
    DateTimeOffset TakenAt,
    string? GameVersion,
    string? Source);

public sealed record NewCapture(
    CaptureKind Kind,
    string? Sha256,
    string? Path,
    DateTimeOffset TakenAt,
    string? GameVersion = null,
    string? Source = null);

/// <summary>Layer 2: one field of one read. Never changed or deleted.</summary>
public sealed record ObservationRecord(
    long Id,
    long CaptureId,
    string Screen,
    string ProfileVersion,
    string Key,
    string? Value,
    Outcome Outcome,
    string? Reads,
    DateTimeOffset ObservedAt,
    string? Season,
    string? Note);

public sealed record NewObservation(
    long CaptureId,
    string Screen,
    string ProfileVersion,
    string Key,
    string? Value,
    Outcome Outcome,
    DateTimeOffset ObservedAt,
    string? Reads = null,
    string? Note = null);

public enum StateStatus
{
    /// <summary>A good value, and no newer read of the field failed.</summary>
    Current,
    /// <summary>A good value, but a newer read of the same field failed.</summary>
    Stale,
    /// <summary>Never read. Unknown is never zero.</summary>
    Unknown,
}

/// <summary>Layer 3: what the app shows for one key, worked out from its observations.</summary>
public sealed record KeyState(
    string Key,
    StateStatus Status,
    string? Value,
    Outcome? Outcome,
    long? ObservationId,
    DateTimeOffset? ObservedAt,
    DateTimeOffset? StaleSince,
    string? Held,
    long? HeldObservationId);

/// <summary>Thrown when the store refuses a value: a name outside the catalog, an account-number pattern, a key it doesn't know.</summary>
public sealed class RefusedValueException(string message) : Exception(message);
