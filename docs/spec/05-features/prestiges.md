# 5.4 Prestiges

Where the user is on the Prestige track, what the next Prestige costs, and where to spend a Permanent Unlock
Token.

**Defaults, each a setting:** the user prestiges as soon as a Prestige is available (the pace forecast assumes it),
and Permanent Unlock Tokens go on non-weapon items such as perks (token advice follows it). The reason for the token
default is the official one below: a Store or Battle Pass blueprint already keeps its base weapon available.

## What the game does

All `[P-PROG]` unless marked.

- **The track:** 55 player levels, then 10 Prestiges, then Prestige Master from level 56 to 1,000.
- **XP for levels 1 to 55:** from Multiplayer, DMZ and, from Season 1, Warzone. DMZ also has a separate track of 70
  levels.
- **Two paths,** chosen at level 55 and at each new Prestige:

  | Path | What goes back | What it gives or keeps |
  |---|---|---|
  | **Classic Prestige** | Level unlocks and Create-a-Class progress | A 20% XP boost, one Permanent Unlock Token, bonus cosmetics |
  | **Regular Prestige** | The level, to 1 | Keeps every level unlock and all Create-a-Class content |

- **A Permanent Unlock Token** unlocks one level-unlock item for good, and that item stays through later Classic
  Prestiges. The post's advice is a non-weapon item (perk, field upgrade, killstreak or equipment), because a Store
  or Battle Pass blueprint already keeps its base weapon available.
- **Rewards:** one every 10 levels in each Prestige. In Prestige Master, one every 5 to 10 levels up to 99, then one
  every 25.
- **Season 1 reset Player Level** `[P-WZ]`.
- **The screens:** the level screen shows the prestige track by name and the Prestige number `[S02]`; the Prestige
  tab shows ten tiles, each `COMPLETED`, `IN PROGRESS` or not started `[S03]`. A locked killstreak shows
  `UNLOCK AT LEVEL n` `[S66]`.

**A player report, not a game fact:** on 2026-11-10 a player reported that a Classic Prestige resets weapons. No
official post says so and no screen has shown it, and the report doesn't say which of levels, attachments and camos
go back. The app shows it as an unverified report, and pins the detail down by reading the user's weapon figures
either side of their next Prestige. `[unknown]`

**Conflicts and unknowns:**
- **Exactly what a Classic Prestige resets on a weapon:** its level, its attachments, its camos, or some of these.
  `[unknown]` beyond the report above.
- The Throwing Knife unlocks at 48 in the post's token example and 49 in both unlock tables. **Conflict.**
- How many tokens exist in total, and whether the 20% boost stacks across Prestiges. `[unknown]`
- What the token screen looks like. Not sampled yet. `[unknown]`

## What the user sees

- **Status:** Prestige, track, level out of 55, and each Prestige tile's state.
- **Before you prestige,** a checklist that appears from level 50:
  1. **What relocks:** every item unlocked by level that isn't covered by a token or a blueprint, by category.
  2. **Which loadouts break:** each custom loadout that uses a relocking item, with the level at which that item
     returns.
  3. **What stays:** items the user spent tokens on, blueprint weapons, and everything the post says is kept.
  4. **Weapons:** a warning that weapons may reset, shown as the unverified player report above, with every
     weapon's current figure listed so the user can see what may be at stake. Which parts reset is shown as unknown
     until the app has read the user's weapon figures before and after a Prestige.
  5. **Open business:** dailies not finished today, and tracked challenges within reach.
- **Token advice,** following the token setting:
  - By default, candidates are perks, field upgrades, killstreaks and equipment only, and a weapon is never
    suggested.
  - Each is scored by how late it unlocks (the longest wait after a reset) and by how many of the user's loadouts
    use it.
  - The list shows the level each candidate unlocks at and its source, and says so when that level is a conflict.
- **Pace:** levels per play day from the user's own history, and the date the next Prestige falls on at that pace.

## What the user does

- Captures the level screen and the Prestige tab now and then. One screenshot of each is enough.
- Enters by hand which items they have already spent tokens on. The app can't read that yet.
- Confirms the checklist before prestiging. Nothing in the app prestiges for the user or touches the game.

## Data and where it comes from

| Data | Source |
|---|---|
| The track's shape, both paths, the token rule | `[P-PROG]` |
| Level, Prestige, track, tile states | `[S02]`, `[S03]` |
| Which level unlocks which item | The level screen's unlock tiles `[S02]`, lock lines on equipment screens `[S66]`, and the tables in `[P-LOAD]` |
| Which loadouts use which items | The loadout reader (`loadouts.md`) |
| Tokens held and spent | The user, by hand, until the token screen is sampled |

## Tests

- **Token advice never contains a weapon** under the default token setting, whatever the scores.
- **Relock list:** given an unlock table and a set of token items, the list is every level-unlock item minus the
  token items and blueprint weapons.
- **Broken loadouts:** a loadout is listed exactly when it uses a relocking item.
- **A report is shown as a report:** the weapons line carries the 2026-11-10 player report, marked unverified, and
  reads "details unknown" until two observations either side of a Prestige exist. After that it shows what the
  screens showed.
- **Golden cases:** the confirmed-values set (`04-capture-and-readers.md`, 4.9) for the Player level and Prestige
  screens produces its confirmed values.
