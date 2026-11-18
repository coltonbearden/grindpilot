using GrindPilot.Core.Catalog;

namespace GrindPilot.Core.Store;

/// <summary>
/// Works out layer 3 (current state) from layer 2 (observations), by the rules of 03-account-state.md, 3.2 and 3.6.
/// Pure: the same observations always give the same state, so the store's own table can always be checked against it.
/// </summary>
public sealed class StateEngine
{
    public const string PrestigeKey = "player.prestige";
    public static readonly TimeSpan DisagreementWindow = TimeSpan.FromMinutes(1);

    public const string HeldDrop = "lower than the last value, with no Prestige or season change between";
    public const string HeldDisagreement = "two captures disagree within a minute";

    private readonly GameCatalog _catalog;
    private readonly KeySchema _schema;

    public StateEngine(GameCatalog catalog)
    {
        _catalog = catalog;
        _schema = new KeySchema(catalog);
    }

    /// <summary>Every key's state, from all observations.</summary>
    public Dictionary<string, KeyState> ComputeAll(IEnumerable<ObservationRecord> observations)
    {
        var all = observations.ToList();
        var resets = ResetTimes(all.Where(o => o.Key == PrestigeKey));
        return all
            .GroupBy(o => o.Key, StringComparer.Ordinal)
            .Select(g => ComputeKey(g.Key, g, resets))
            .ToDictionary(s => s.Key, StringComparer.Ordinal);
    }

    /// <summary>
    /// The times at which a Prestige change was observed: each good <c>player.prestige</c> observation whose value differs
    /// from the good one before it.
    /// </summary>
    public static IReadOnlyList<DateTimeOffset> ResetTimes(IEnumerable<ObservationRecord> prestigeObservations)
    {
        var times = new List<DateTimeOffset>();
        string? last = null;
        foreach (var o in Order(prestigeObservations).Where(o => o.Outcome.IsGood()))
        {
            if (last is not null && o.Value != last)
            {
                times.Add(o.ObservedAt);
            }
            last = o.Value;
        }
        return times;
    }

    public KeyState ComputeKey(string key, IEnumerable<ObservationRecord> observations, IReadOnlyList<DateTimeOffset> resetTimes)
    {
        var monotonic = _schema.RuleFor(key)?.Monotonic ?? false;

        // Values taken so far, newest last. A disagreement takes the newest back off.
        var accepted = new List<ObservationRecord>();
        DateTimeOffset? staleSince = null;
        string? held = null;
        long? heldId = null;

        foreach (var o in Order(observations))
        {
            switch (o.Outcome)
            {
                case Outcome.Unread:
                    if (accepted.Count > 0 && staleSince is null)
                    {
                        staleSince = o.ObservedAt;
                    }
                    continue;
                case Outcome.Rejected:
                    continue;
            }

            var previous = accepted.Count > 0 ? accepted[^1] : null;

            if (previous is not null
                && previous.CaptureId != o.CaptureId
                && previous.Value != o.Value
                && (o.ObservedAt - previous.ObservedAt).Duration() <= DisagreementWindow)
            {
                // Neither is taken: the field goes to review, and the value before both stands.
                accepted.RemoveAt(accepted.Count - 1);
                held = HeldDisagreement;
                heldId = o.Id;
                staleSince = null;
                continue;
            }

            if (monotonic && previous is not null)
            {
                var before = KeySchema.OrderingNumber(previous.Value);
                var now = KeySchema.OrderingNumber(o.Value);
                var explained = IsExplained(key, previous.ObservedAt, o.ObservedAt, resetTimes);
                if (before is not null && now is not null && now < before && !explained)
                {
                    held = HeldDrop;
                    heldId = o.Id;
                    continue;
                }
            }

            accepted.Add(o);
            staleSince = null;
            held = null;
            heldId = null;
        }

        if (accepted.Count == 0)
        {
            return new KeyState(key, StateStatus.Unknown, null, null, null, null, null, held, heldId);
        }

        var top = accepted[^1];
        return new KeyState(
            key,
            staleSince is null ? StateStatus.Current : StateStatus.Stale,
            top.Value,
            top.Outcome,
            top.Id,
            top.ObservedAt,
            staleSince,
            held,
            heldId);
    }

    /// <summary>A drop is explained by a Prestige change or a season change between the two observations (3.6).</summary>
    private bool IsExplained(string key, DateTimeOffset from, DateTimeOffset to, IReadOnlyList<DateTimeOffset> resetTimes)
    {
        if (key == PrestigeKey)
        {
            return false;
        }
        if (resetTimes.Any(t => t > from && t <= to))
        {
            return true;
        }
        return _catalog.SeasonAt(from)?.Name != _catalog.SeasonAt(to)?.Name;
    }

    public static IEnumerable<ObservationRecord> Order(IEnumerable<ObservationRecord> observations) =>
        observations.OrderBy(o => o.ObservedAt).ThenBy(o => o.Id);
}
