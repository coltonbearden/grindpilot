# 5.2 Weapon leveling

How far each weapon is from its next unlock, its maximum, and its Weapon Prestige.

## What the game does

- **Weapon levels** `[P-PROG]`: each one unlocks an attachment. Levels also open Military camos, Special camo
  challenges and optics.
- **Shared attachments** `[P-LOAD]`: one unlock covers every weapon that takes the attachment. A level whose
  attachment is already owned is skipped as a **Bonus Level Up**. Attachment lists flag these as
  `SHARED ATTACHMENT` `[S50]`.
- **The game counts in unlocks.** A weapon's figure reads `PRESTIGE n /m UNLOCKS` `[S73]`, and a locked attachment
  `EARN n MORE WEAPON UNLOCKS USING THE <WEAPON>` `[S50]`.
- **Maximum level** `[P-LOAD]`: per weapon, 37 to 75 at launch. Reaching it unlocks the weapon's **Apex
  Attachment** (it takes no slot) and opens Weapon Prestige. Launchers and the knife have no Apex.
- **Weapon Prestige** `[P-PROG]`, optional:
  - **Resets:** the weapon's level.
  - **Relocks:** its attachments, apart from the Apex and blueprint attachments.
  - **Keeps:** blueprints, camos, reticles and cosmetics; shared attachments stay unlocked on other weapons.
- **Weapon Prestige Master** `[P-PROG]`: starts automatically on reaching the maximum again, with no reset. A weapon
  list shows it as `PRESTIGE MASTER n` `[S44]`. Camos come at Master levels 150 and 250.
- **Shared across modes** `[P-PROG]`, `[P-DMZ]`: one weapon level for Multiplayer, DMZ and Warzone.
- **Double Weapon XP tokens** `[P-BP]`: paused outside matches.

**Conflicts and unknowns:**
- Weapon Prestige resets the level "to 1" `[P-PROG]` or "back to zero" `[P-LOAD]`. **Conflict.**
- The Weapon Prestige Master cap. `[P-PROG]` names a camo at level 250 and states no cap. `[unknown]`
- **What the word `PRESTIGE` in `PRESTIGE n /m UNLOCKS` refers to.** Every sampled weapon carried it. It could
  mean the weapon's own Weapon Prestige, or the unlocks earned during the player's current Prestige. An unverified
  player report that weapons reset on a Classic Prestige (`prestiges.md`) makes the second reading possible. The
  app stores the two numbers and the words exactly as shown, and gives them no meaning beyond that until screens
  settle it. `[unknown]`
- XP needed per weapon level. `[unknown]`

## What the user sees

- **A weapon list** by class: unlocks earned and total as the game shows them, whether the weapon is in a Weapon
  Prestige or Prestige Master, and its Apex state.
- **For one weapon:** what the next few levels unlock, when its list has been read; how many unlocks until the
  attachment the user is waiting for; how many until the next Military camo.
- **Pace:** unlocks earned per hour of play on that weapon, from the user's own history, once recordings or live
  reading give match times. Before that, unlocks per day.
- **A forecast** that is honest about what it rests on: "at your last seven days' pace, 9 more play days". It is
  never shown without at least five observations.
- **Weapon Prestige advice:** what the user would lose and keep, in the game's own terms, and which of their saved
  builds would break because they use attachments that relock.

## What the user does

- Captures the weapon select list (one screenshot per class shows every weapon's figure) or a weapon's Gunsmith.
- Marks a weapon as "leveling now". The session planner prefers it.
- Records by hand that a Double Weapon XP token is active, if plans should account for it. The app doesn't read
  tokens yet. `[unknown]` where they are shown.

## Data and where it comes from

| Data | Source |
|---|---|
| Weapons, classes, maximum levels, player level that unlocks each weapon | `[P-LOAD]`, `[P-S1]` |
| Unlocks earned and total, prestige state, Apex state | Weapon select and Gunsmith screens `[S44]`, `[S73]`, `[S52]` |
| What each remaining unlock is | Attachment lists `[S50]` and the Gunsmith's Progression tab |
| Pace | The store's own history of observations |

## Tests

- **Figures are stored as shown:** an invented `PRESTIGE 20 /60 UNLOCKS` gives earned 20, total 60 and the label as
  read; the app never converts between levels and unlocks, and never infers a Weapon Prestige from the label alone.
- **Golden cases:** the confirmed-values set (`04-capture-and-readers.md`, 4.9) for the Loadouts, Weapon select,
  Attachment list and Gunsmith screens produces its confirmed values.
- **Forecasts need data:** fewer than five observations gives "not enough history", not a number.
- **A drop is explained or held:** unlocks falling without a Weapon Prestige in between goes to review.
- **Weapon Prestige advice** lists exactly the saved builds that use a non-Apex, non-blueprint attachment of that
  weapon.
