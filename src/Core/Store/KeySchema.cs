using System.Globalization;
using System.Text.RegularExpressions;
using GrindPilot.Core.Catalog;

namespace GrindPilot.Core.Store;

/// <summary>The field types of 04-capture-and-readers.md, 4.7, as far as stage 1 stores them.</summary>
public enum FieldType
{
    CountPair,
    Level,
    Xp,
    Countdown,
    StatusWord,
    Label,
    Sentence,
    Name,
}

/// <summary>One kind of key the store accepts. A key that matches no rule is refused.</summary>
public sealed record KeyRule(
    string Pattern,
    FieldType Type,
    string? NameKind = null,
    bool Monotonic = false,
    int Min = 0,
    int Max = int.MaxValue,
    IReadOnlyList<string>? Words = null,
    IReadOnlyDictionary<string, string>? SlugKinds = null,
    string Group = "Other")
{
    public Regex Regex { get; } = new("^" + Pattern + "$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
}

/// <summary>
/// The keys of 03-account-state.md, 3.4, that stage 1 stores. A slug inside a key (the weapon in
/// <c>weapon.kastov-762.unlocks</c>) must be a catalog entry of its kind, so a name outside the catalog never enters.
/// </summary>
public sealed class KeySchema
{
    private static readonly Regex AccountNumber = new(@"#\s*\d|\d{7,}", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex CountPairRx = new(@"^\[?\s*(\d{1,6})\s*/\s*(\d{1,6})\s*\]?$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex DigitsRx = new(@"^\d{1,6}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex CountdownRx = new(@"^(\d{1,2})H\s*(\d{1,2})M$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Dictionary<string, string> WeaponSlug = new() { ["weapon"] = "weapon" };

    public static readonly IReadOnlyList<KeyRule> Rules =
    [
        new(@"player\.level", FieldType.Level, Monotonic: true, Min: 1, Max: 55, Group: "Player"),
        new(@"player\.prestige", FieldType.Level, Monotonic: true, Min: 0, Max: 10, Group: "Player"),
        new(@"player\.prestige-track", FieldType.StatusWord, Words: ["CLASSIC TRACK"], Group: "Player"),
        new(@"prestige\.(?<n>10|[1-9])\.status", FieldType.StatusWord, Words: ["COMPLETED", "IN PROGRESS"], Group: "Prestige"),

        new(@"daily\.countdown", FieldType.Countdown, Group: "Daily challenges"),
        new(@"daily\.(?<n>[1-3])\.text", FieldType.Sentence, Group: "Daily challenges"),
        new(@"daily\.(?<n>[1-3])\.progress", FieldType.CountPair, Group: "Daily challenges"),
        new(@"daily\.(?<n>[1-3])\.xp", FieldType.Xp, Max: 100000, Group: "Daily challenges"),
        new(@"daily\.bonus\.progress", FieldType.CountPair, Group: "Daily challenges"),
        new(@"daily\.bonus\.xp", FieldType.Xp, Max: 100000, Group: "Daily challenges"),

        new(@"weapon\.(?<weapon>[a-z0-9-]+)\.unlocks", FieldType.CountPair, Monotonic: true, SlugKinds: WeaponSlug, Group: "Weapons"),
        new(@"weapon\.(?<weapon>[a-z0-9-]+)\.unlocks\.label", FieldType.Label, SlugKinds: WeaponSlug, Group: "Weapons"),
        new(@"weapon\.(?<weapon>[a-z0-9-]+)\.camos\.unlocked", FieldType.CountPair, Monotonic: true, Max: 15, SlugKinds: WeaponSlug, Group: "Camos"),
        new(@"weapon\.(?<weapon>[a-z0-9-]+)\.camos\.(?<group>[a-z0-9-]+)", FieldType.CountPair, Monotonic: true,
            SlugKinds: new Dictionary<string, string> { ["weapon"] = "weapon", ["group"] = "camo-group" }, Group: "Camos"),
        new(@"weapon\.(?<weapon>[a-z0-9-]+)\.camos\.next", FieldType.Name, NameKind: "camo", SlugKinds: WeaponSlug, Group: "Camos"),
        new(@"weapon\.(?<weapon>[a-z0-9-]+)\.camo\.(?<camo>[a-z0-9-]+)\.(challenge|requirement)", FieldType.Sentence,
            SlugKinds: new Dictionary<string, string> { ["weapon"] = "weapon", ["camo"] = "camo" }, Group: "Camos"),
        new(@"weapon\.(?<weapon>[a-z0-9-]+)\.camo\.(?<camo>[a-z0-9-]+)\.(progress|requirement\.progress)", FieldType.CountPair, Monotonic: true,
            SlugKinds: new Dictionary<string, string> { ["weapon"] = "weapon", ["camo"] = "camo" }, Group: "Camos"),
        new(@"camos\.next-mastery\.requirement", FieldType.Sentence, Group: "Camos"),
        new(@"camos\.next-mastery\.progress", FieldType.CountPair, Monotonic: true, Group: "Camos"),
        new(@"camos\.tracked", FieldType.CountPair, Max: 9, Group: "Camos"),

        new(@"loadout\.(?<n>\d{2})\.(primary|secondary)", FieldType.Name, NameKind: "weapon", Group: "Loadouts"),
        new(@"loadout\.(?<n>\d{2})\.primary\.class", FieldType.Name, NameKind: "weapon-class", Group: "Loadouts"),
        new(@"loadout\.(?<n>\d{2})\.tactical", FieldType.Name, NameKind: "tactical", Group: "Loadouts"),
        new(@"loadout\.(?<n>\d{2})\.lethal", FieldType.Name, NameKind: "lethal", Group: "Loadouts"),
        new(@"loadout\.(?<n>\d{2})\.field-upgrade", FieldType.Name, NameKind: "field-upgrade", Group: "Loadouts"),

        new(@"mastery-set\.(?<set>[a-z0-9-]+)\.progress", FieldType.CountPair, Monotonic: true,
            SlugKinds: new Dictionary<string, string> { ["set"] = "mastery-set" }, Group: "Challenges"),
        new(@"challenge\.(?<challenge>[a-z0-9-]+)\.text", FieldType.Sentence,
            SlugKinds: new Dictionary<string, string> { ["challenge"] = "challenge" }, Group: "Challenges"),
        new(@"challenge\.(?<challenge>[a-z0-9-]+)\.kind", FieldType.StatusWord, Words: ["TIERED CHALLENGE"],
            SlugKinds: new Dictionary<string, string> { ["challenge"] = "challenge" }, Group: "Challenges"),
        new(@"challenge\.(?<challenge>[a-z0-9-]+)\.progress", FieldType.CountPair, Monotonic: true,
            SlugKinds: new Dictionary<string, string> { ["challenge"] = "challenge" }, Group: "Challenges"),
        new(@"challenges\.tracked", FieldType.CountPair, Max: 9, Group: "Challenges"),

        new(@"build\.(?<weapon>[a-z0-9-]+)\.(?<build>[a-z0-9-]+)\.name", FieldType.Label, SlugKinds: WeaponSlug, Group: "Builds"),
    ];

    private readonly GameCatalog _catalog;

    public KeySchema(GameCatalog catalog) => _catalog = catalog;

    /// <summary>The rule a key falls under. More specific patterns are listed first where two could match.</summary>
    public KeyRule? RuleFor(string key)
    {
        KeyRule? found = null;
        foreach (var rule in Rules)
        {
            var m = rule.Regex.Match(key);
            if (!m.Success)
            {
                continue;
            }
            // weapon.x.camos.unlocked and weapon.x.camos.next also match the group pattern; prefer the literal rule.
            if (found is null || rule.Pattern.Count(ch => ch == '<') < found.Pattern.Count(ch => ch == '<'))
            {
                found = rule;
            }
        }
        return found;
    }

    /// <summary>
    /// Checks a key and its value and returns the value as stored (a count pair is stored as <c>a/b</c>, a name as the
    /// catalog's own spelling). Throws <see cref="RefusedValueException"/> when the store must refuse it (3.7, 4.6).
    /// </summary>
    public string? Normalize(string key, string? value, Outcome outcome)
    {
        if (AccountNumber.IsMatch(key))
        {
            throw new RefusedValueException("The key holds an account-number pattern.");
        }
        var rule = RuleFor(key) ?? throw new RefusedValueException($"The store has no rule for the key '{key}'.");
        CheckSlugs(rule, key);

        if (!outcome.IsGood())
        {
            // An unread or rejected observation carries no value worth checking, but never an account number.
            if (value is not null && AccountNumber.IsMatch(value))
            {
                throw new RefusedValueException("The value matches the account-number pattern.");
            }
            return outcome == Outcome.Unread ? null : value;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new RefusedValueException($"A {outcome.ToDb()} observation of '{key}' needs a value.");
        }
        var v = value.Trim();
        if (AccountNumber.IsMatch(v))
        {
            throw new RefusedValueException("The value matches the account-number pattern.");
        }

        switch (rule.Type)
        {
            case FieldType.CountPair:
            {
                var m = CountPairRx.Match(v);
                if (!m.Success)
                {
                    throw new RefusedValueException($"'{key}' needs a count pair such as 3/10.");
                }
                var a = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                var b = int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
                if (a > b || b > rule.Max)
                {
                    throw new RefusedValueException($"'{key}': the first number must not exceed the second, nor the second {rule.Max}.");
                }
                return $"{a}/{b}";
            }
            case FieldType.Level:
            case FieldType.Xp:
            {
                if (!DigitsRx.IsMatch(v))
                {
                    throw new RefusedValueException($"'{key}' needs a whole number.");
                }
                var n = int.Parse(v, CultureInfo.InvariantCulture);
                if (n < rule.Min || n > rule.Max)
                {
                    throw new RefusedValueException($"'{key}' must be between {rule.Min} and {rule.Max}.");
                }
                return n.ToString(CultureInfo.InvariantCulture);
            }
            case FieldType.Countdown:
            {
                var m = CountdownRx.Match(v.ToUpperInvariant());
                if (!m.Success)
                {
                    throw new RefusedValueException($"'{key}' needs a countdown such as 12H 05M.");
                }
                var h = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                var min = int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
                if (h > 24 || min > 59 || (h == 24 && min > 0))
                {
                    throw new RefusedValueException($"'{key}' must be within 24 hours.");
                }
                return $"{h}H {min}M";
            }
            case FieldType.StatusWord:
            {
                var word = rule.Words!.FirstOrDefault(w => string.Equals(w, v, StringComparison.OrdinalIgnoreCase));
                return word ?? throw new RefusedValueException($"'{key}' must be one of: {string.Join(", ", rule.Words!)}.");
            }
            case FieldType.Label:
                if (v.Length > 60)
                {
                    throw new RefusedValueException($"'{key}' is longer than a label.");
                }
                return v;
            case FieldType.Sentence:
                if (v.Length > 160)
                {
                    throw new RefusedValueException($"'{key}' is longer than a challenge sentence.");
                }
                return v;
            case FieldType.Name:
            {
                var entry = _catalog.Match(rule.NameKind!, v)
                    ?? throw new RefusedValueException($"'{v}' is not a {rule.NameKind} in the catalog.");
                return entry.Name;
            }
            default:
                throw new RefusedValueException($"'{key}' has an unknown field type.");
        }
    }

    private void CheckSlugs(KeyRule rule, string key)
    {
        if (rule.SlugKinds is null)
        {
            return;
        }
        var m = rule.Regex.Match(key);
        foreach (var (group, kind) in rule.SlugKinds)
        {
            var slug = m.Groups[group].Value;
            if (_catalog.BySlug(kind, slug) is null)
            {
                throw new RefusedValueException($"'{slug}' in '{key}' is not a {kind} in the catalog.");
            }
        }
    }

    /// <summary>The number a monotonic value is compared by: the first of a count pair, or the whole number.</summary>
    public static int? OrderingNumber(string? value)
    {
        if (value is null)
        {
            return null;
        }
        var m = CountPairRx.Match(value);
        if (m.Success)
        {
            return int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
        }
        return DigitsRx.IsMatch(value) ? int.Parse(value, CultureInfo.InvariantCulture) : null;
    }
}
