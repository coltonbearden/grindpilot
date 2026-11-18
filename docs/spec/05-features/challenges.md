# 5.3 Challenges

Today's dailies, calling-card challenges and what is tracked, in one place.

## What the game does

**Daily Challenges** `[P-PROG]`, `[S04]`
- Three Multiplayer dailies a day, and a bonus for finishing all three. The bonus row reads
  `Complete 3 Daily Challenges.` with its progress (`n/3`) and its own XP `[S04]`.
- One reroll a day, for one of them.
- **BlackCell** `[P-BP]`: owners get extra dailies, and those count toward the three. Ownership is per season, so the
  app keeps it as a setting the user changes each season, default "not owned", and the dailies block allows more
  than three rows.
- Warzone has its own set from Season 1 `[P-WZ]`.
- Each row shows its text, progress and XP, and the block shows a countdown `[S04]`.

**Calling-card challenges** `[P-PROG]`, `[S46]` to `[S48]`
- Tabs: Regular, Elite, Prestige, and Tracked & Near Complete.
- Regular's categories: Multiplayer Mastery, DMZ Mastery, Shared Mode Mastery, a Warzone one
  (`WZ MODE MASTERY` on screen) and an overall `GAME MASTERY`. Each holds Mastery Sets of six challenges; the
  Multiplayer sets are Objective, War, Capture Zone, Bomb, Cash and Dog Tag.
- A set holds tiered and single challenges. A tiered one shows its name, tiers done (`n/m COMPLETED`) and a running
  count against its requirement (`a/b`) `[S48]`.
- Up to nine can be tracked (`ADD TO TRACKED [n/9]`).

**Also:** operator unlock challenges `[P-LOAD]`, BlackCell point challenges `[P-BP]`, and season events `[P-S1]`.

**Not known:** weekly challenges, DMZ dailies, where event challenges are listed, and whether the game shows an
overall completion figure. `[unknown]`

## What the user sees

- **Today:** each daily with its progress and XP, the bonus row, the time left, and a mark on any daily that is off
  the user's play style (`camos.md`), with "reroll this one" on the worst of them while the reroll is unused.
- **Calling cards:** each category and set with its `n/6`, and inside a set each challenge's tier and count.
- **Near done:** the calling-card challenges closest to their next tier.
- **Tracked:** the nine tracked in the game, mirrored.
- **Staleness:** dailies read yesterday are shown as yesterday's and never counted as today's.

## What the user does

- Captures the Challenges home screen once a play session. It carries the dailies and one line each from camos
  and calling cards `[S04]`.
- Opens a Mastery Set and captures it to refresh that set.
- Tells the app when the reroll has been used, until a screen that shows it has been sampled. `[unknown]`
- Sets the BlackCell setting for the season.

## Data and where it comes from

| Data | Source |
|---|---|
| The rules for dailies, the bonus, the reroll, tracking | `[P-PROG]`, `[P-BP]`, `[P-WZ]` |
| Category and set names | `[P-PROG]`, confirmed against `[S46]`, `[S47]` |
| Today's dailies, their XP and progress, the countdown | The Challenges home screen `[S04]`, and the lobby as seen in a recording |
| Challenge names, tiers, requirement text, progress | Calling-card screens `[S48]` |
| The day a set of dailies belongs to | The capture time and the countdown read from the same screen |
| Whether BlackCell's extra dailies apply | The user's per-season setting |

## Tests

- **Golden cases:** the confirmed-values set (`04-capture-and-readers.md`, 4.9) for the Challenges home, Calling
  cards and One calling-card challenge screens produces its confirmed values.
- **Day boundary:** dailies captured before a reset are not shown as today's after it. The reset time is worked out
  from the countdown, not assumed.
- **Bonus row:** its count never exceeds 3, and it is derived from the screen, not from counting finished rows.
- **Tier arithmetic:** tiers done ≤ tiers total, and the running count ≤ its target.
- **Unrecognised wording** is stored as text of a known pattern or not at all; a challenge sentence that matches no
  pattern is unread and goes to review.
