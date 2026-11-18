# 5.1 Camos

Where every weapon stands on the camo ladder, and what to do next on each.

## What the game does

**The ladder for one weapon** `[P-PROG]`, with the group names as the camo page shows them `[S19]`:

| Group (count) | Camos | Opens when |
|---|---|---|
| **Military** (8) | Military Camo 1 to 8 | In order, at weapon milestones that differ per weapon. The camo page gives each as a number of weapon unlocks still to earn (`Earn n more weapon unlocks using the <WEAPON>` `[S25]`) |
| **Special** (3) | Three camos unique to the weapon | Challenges open at weapon levels; an earned Special camo is usable on every weapon |
| **Completionist** (4) | Gold, Platinum, Harmonic Break, Divergence | Gold opens after all 8 Military and 3 Special |
| **Prestige** (4) | Pyroclast, Pyroclast Terminus, Pyroclast Scatter, Pyroclast Cascade | At Weapon Prestige 1 level 1, Weapon Prestige 1 maximum, and Weapon Prestige Master levels 150 and 250 |

**The four Completionist steps** `[P-PROG]`:

| Step | Opens when | Challenge |
|---|---|---|
| **Gold** | All 8 Military and 3 Special done | 15 eliminations in one Multiplayer match, or 100 in one DMZ match |
| **Platinum** | Gold on every weapon in the class. The camo page shows the gate as a count (`Complete n Gold Camo Challenges using <CLASS> a/b` `[S26]`) | By class, below |
| **Harmonic Break** | Platinum on 33 weapons (`Complete 33 Platinum Camo Challenges a/b` `[S19]`) | By class, below |
| **Divergence** | Awarded for 33 Harmonic Break challenges | — (it opens the Mastery camos) |

| Class | Platinum challenge | Harmonic Break challenge |
|---|---|---|
| Assault rifles | Headshots | Double kills |
| SMGs | Kills from behind | Hipfire kills |
| Pistols | Kills from behind | Hipfire kills |
| Shotguns | Hipfire kills | Kills without reloading |
| LMGs | Kills without dying | Kills without releasing the trigger |
| Melee | Kills without dying | Kills from behind |
| Marksman rifles | Prone kills | One-shot kills |
| Sniper rifles | Prone kills | One-shot kills |
| Launchers | Kills without taking damage | Kills |

**Mode Mastery** `[P-PROG]`, `[P-WZ]`, `[S14]`: three camos per mode.

| Mode | Mastery camos |
|---|---|
| Multiplayer | Mercurial Drift, Polyatomic Reforged, Orion Reforged |
| DMZ | Chiral, Ripple Tide, Helio |
| Warzone | Parallax, Damascus Reforged, Empyros |

Multiplayer's three, in order: 10 kills in a match, ×250; 3 kills without taking damage, ×300; 5 kills without
dying, ×500.

**Across modes** `[P-PROG]`: one shared camo progress for all modes, with higher counts in DMZ; a camo earned in any
mode is unlocked in every mode.

**Not known:** whether Mastery counters are per weapon or pooled; the Special camo challenges for most weapons; the
kill counts for reticle unlocks. `[unknown]`

## What the user sees

- **A board:** one row per weapon, grouped by class. Each row shows `n/15`, the four group counts, and the next
  step with its progress.
- **Class gates:** for each class, how many weapons have Gold, and which are missing it.
- **The long counters:** Platinum weapons out of 33, Harmonic Breaks out of 33, and each Mastery camo's count.
- **Closest to done:** the camo steps nearest completion across all weapons.
- **A style mark on each step,** from a play-style setting. The default is aimed play: aiming down sights early,
  pre-aiming and holding lanes, with little hipfire. Under it, a step that needs hipfire, kills from behind or prone
  kills is marked "off your style", and one that needs headshots, longshots, mounted or aimed kills is marked "your
  style". The user can change the setting.
- **Stale and unknown are visible.** A weapon whose camo page hasn't been read shows "not read yet", not zeros.

## What the user does

- Opens a weapon's camo page in the game and takes a screenshot, or lets the live reader see it when that is on.
  The row updates.
- Picks up to nine camo steps to track, to match the game's own tracked list `[S15]`.
- Marks a step as "skip for now". It stays on the board but drops out of plans.

## Data and where it comes from

| Data | Source |
|---|---|
| The ladder's shape, the Completionist rules, the per-class challenge types, the Mastery camos | `[P-PROG]`, `[P-WZ]` |
| Each weapon's group counts, next step, requirement text and progress | The user's camo screens (`04-capture-and-readers.md`, 4.2: camo challenges, one weapon's camos, tracked camos) |
| Special camo names and challenges per weapon | The user's screens, harvested into the catalog as they are opened |
| The style marks | A fixed table in the app from challenge wording to "your style / neutral / off your style", applied to the play-style setting |

## Tests

- **Ladder order:** given group counts, the next step is the one the rules above say. Gold is never offered before
  11 of 11; Platinum never before the class gate.
- **Counts add up:** Military + Special + Completionist equals the weapon's `n/15` when all three are read.
- **Golden cases:** the confirmed-values set (`04-capture-and-readers.md`, 4.9) for the Camo challenges, Tracked
  camos and One weapon's camos screens produces its confirmed values.
- **Unknown stays unknown:** a weapon with no camo read contributes nothing to "closest to done".
- **Style marks:** each known challenge wording maps to exactly one mark under each style setting; an unrecognised
  wording maps to neutral and is listed for review.
