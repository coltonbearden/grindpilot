# 3. Account state

One local store holds everything the app knows about the player's account: level, prestige, weapons, camos,
challenges, loadouts, unlocks and tokens. Every value records where it came from and how sure the read was, and
keeps its history.

## 3.1 Where it lives

- **One SQLite file:** `%LOCALAPPDATA%\GrindPilot-data\state.db` on the user's PC. It is the only copy the app
  writes.
- **A new install starts with an empty account.** No import ships with the app; every value comes from the user's
  own captures or the user's own entries.
- **Not in the app's install folder.** The program installs to `%LOCALAPPDATA%\GrindPilot\`: the updater replaces its
  `current` folder on every update, and an uninstall deletes it whole (`08-app-shape-and-stack.md`, 8.6).
- **Backed up by the app:** before any change to the store's layout, and once a day when something changed, a copy
  goes to `%LOCALAPPDATA%\GrindPilot-data\backups\` (the newest 14 are kept). Design decision (2026-11-17): a user's
  own backup should take `backups\`, not the live `state.db` and not `test-library\`. Without one, the app's own
  copies are the only other copy.
- **Nothing in it identifies anyone.** No player name, no account number, no text of a frame, no image.

## 3.2 Three layers

**1. Captures.** One row per thing that was read: a screenshot file, a recording, or a live session.

| Field | Meaning |
|---|---|
| `id`, `kind` | `screenshot`, `recording` or `live` |
| `sha256`, `path` | For files. The path is kept only for screenshots and recordings the user made |
| `taken_at` | From the file name or the frame's time |
| `game_version` | The version string a profile reads from the screen, when one is shown `[unknown]` until a profile reads it |

**2. Observations.** One row per field per read. Observations are never changed or deleted; this is the history.

| Field | Meaning |
|---|---|
| `capture_id`, `screen`, `profile_version` | Where it was read |
| `key` | What it is, for example `weapon.kastov-762.unlocks` or `daily.2.progress` |
| `value` | The value, or empty when unread |
| `outcome` | `confirmed` (the user), `read` (both engines agreed and the rules passed), `read-single` (one engine passed), `unread`, or `rejected` (the user said it was wrong) |
| `reads` | What each engine returned, for values that passed the name and number rules. A reading that failed them is recorded only as "failed" |
| `observed_at` | When the capture was taken |

**3. Current state.** What the app shows and plans from. It is worked out from observations and can always be
rebuilt from them.

- For each key, the newest observation whose outcome is `confirmed`, `read` or `read-single`.
- A key whose newest observation is `unread` keeps its last good value and is shown as **stale**, with the date.
- A key never read is **unknown**. Unknown is never zero.
- Every value shown anywhere in the app can be traced: tap it and see the capture, the date and the outcome.

## 3.3 How sure a value is

| Shown as | Meaning |
|---|---|
| **Confirmed** | The user confirmed it on the review list or the proof page |
| **Read** | Two engines agreed and every rule passed |
| **Read, one engine** | One engine passed the rules and the other didn't. Fine for display; flagged in a plan that depends on it |
| **Stale** | A good value, but a newer read of the same field failed |
| **Unknown** | Never read |

A plan or a checklist says so when it rests on a stale or one-engine value.

## 3.4 What is stored

Keys are grouped by the things they describe. Each line names the screen that feeds it (`04-capture-and-readers.md`).

**Player**
- Level, Prestige, prestige track. `[S02]`
- Each Prestige's status: completed, in progress or not started. `[S03]`
- Prestige Master level, once the player is past Prestige 10. Screen not yet sampled in that state. `[unknown]`
- Level unlocks: which item unlocks at which level, as shown on the level screen. `[S02]`

**Weapons** (one entry per weapon in the catalog)
- Class. `[S05]`
- Unlocks earned and the total, as the game shows them (`n /m`). `[S73]`, `[S44]`
- The label beside that figure, exactly as read (`PRESTIGE`, or `PRESTIGE MASTER <n>` with its level). `[S44]` What
  `PRESTIGE` means there is not settled (`05-features/weapon-leveling.md`), so the store keeps the words and
  infers nothing from them.
- Whether its Apex Attachment is unlocked, and how many unlocks remain when it isn't. `[S52]`

**Camos** (per weapon)
- Camos unlocked out of 15. `[S05]`
- The four group counts: Military of 8, Special of 3, Completionist of 4, Prestige of 4. `[S19]`
- For each camo seen: its name, group, status, requirement and progress. `[S19]`, `[S22]`
- Mode Mastery camos and their progress. `[S14]`
- Tracked camos (up to nine). `[S15]`

**Challenges**
- Dailies: each one's text, progress, XP and whether it is done; the bonus row; the countdown; the day it belongs to. `[S04]`
- Calling cards: categories, Mastery Sets with `n/6`, and each challenge seen with its tiers and progress. `[S46]`, `[S48]`
- Tracked challenges (up to nine). `[S48]`

**Loadouts**
- Each custom loadout by number and name, with its operator, weapons, equipment, perks, field upgrade and
  killstreaks. `[S39]`
- For each weapon in a loadout: the build it uses.

**Attachments**
- For each weapon and slot: the attachments in the list, whether each is unlocked, and for a locked one how many
  unlocks remain (`EARN <n> MORE WEAPON UNLOCKS …`). `[S50]`
- Whether an attachment is shared across weapons. `[S50]`
- Its effect on the five stats, and its exact numbers from the Details view. `[S50]`, `[S51]`

**Builds**
- Saved builds per weapon: the name, the fitted attachments by slot, the Apex, and the stats the game shows for it. `[S71]`, `[S73]`
- A build's Build Code, when the user has captured or typed it. `[S75]`
- Tags the app adds: the goal a build is for, and when it was last seen in a loadout.

**Blueprints**
- Per weapon: which blueprints are owned, as listed on the builds screen. Layout to confirm. `[S70]`

**Unlocks and tokens**
- Which perks, equipment, field upgrades and killstreaks are unlocked, and for a locked one the level that unlocks it
  (`UNLOCK AT LEVEL <n>`). `[S66]`
- Permanent Unlock Tokens held, and what each was spent on. The token screen is not sampled yet, so until it is
  the app takes these from the user by hand. `[unknown]`

**Matches** (from recordings and the live reader, build stages 9 and 10)
- Mode, map, result, the player's own score line, the XP and level change, and what unlocked. Nothing about other
  players.

## 3.5 The catalog

The catalog is the closed list of game things the readers may accept: weapons, classes, camos, attachments, slots,
perks, equipment, field upgrades, killstreaks, operators, modes, maps and challenge patterns.

- **It ships with the app as data**, separate from code, and every entry carries its source.
- **Two sources only:** an official post (`[P-…]`), or an in-game screen confirmed by eye (`[S…]`). Nothing comes
  from a third-party list.
- **It starts from the official posts:** the 33 launch weapons in 9 classes with their unlock levels and maximum
  levels `[P-LOAD]`, the Season 1 weapons `[P-S1]`, the camo ladder and Mastery camos `[P-PROG]`, `[P-WZ]`, the
  attachment slots `[P-LOAD]`, the calling-card categories `[P-PROG]`, and the perks and unlock levels the posts
  name `[P-LOAD]`.
- **It grows by harvest.** Most attachment names and many perks, killstreaks and equipment are not in any official
  post. When a reader meets a label in a place where a name belongs and the catalog has no match, the label goes to
  the review list as a **candidate**. The user confirms it, and it joins the catalog with the screen it came from.
  Until then, values that depend on it are unread.
- **Conflicts stay visible.** Where two official posts disagree, the catalog keeps both and marks the entry
  **conflict**; a confirmed screen settles it when it shows the value. Known ones: six weapon unlock levels differ
  between the table and the per-weapon lines of `[P-LOAD]`; the Throwing Knife is level 48 in `[P-PROG]`'s example
  and 49 in both tables; Weapon Prestige resets the level "to 1" in `[P-PROG]` and "back to zero" in `[P-LOAD]`.
- **Weapon progress is stored as the game shows it.** The game counts a weapon's progress as `n /m` unlocks. For
  some weapons `m` is one less than the official maximum level (Patriot XMR: 72 levels `[P-LOAD]`, `[S73]`; MAR-9:
  73, `[S50]`). For others it equals it (Cronen Osprey: 70 `[P-S1]`, `[S39]`; PILA: 46, `[S39]`). So the catalog
  keeps the total the game shows for each weapon and works nothing out from the official level.

## 3.6 Changes that look wrong

- **A value goes down.** Player level after a Prestige or a new season, or a weapon's unlocks after a Weapon
  Prestige, drop for a reason. The store accepts a drop when a Prestige or season change was observed since the
  last value; otherwise it holds the new value for review.
- **Season resets.** Season 1 reset Player Level `[P-WZ]`. The store keeps seasons as a dimension of player level so
  that history stays true.
- **Two captures disagree within a minute.** Both are stored; the current state takes neither and the field goes
  to review.

## 3.7 Tests

The tests use invented data in `tests/fixtures/`. A maintainer's own confirmed values live outside the repo, in the
data folder's `test-library\` (`%LOCALAPPDATA%\GrindPilot-data\test-library\`).

- Rebuilding the current state from observations gives the same state as the running store.
- An unread observation never changes a current value, only its staleness.
- Unknown is never rendered or summed as zero.
- A value below the last confirmed one is held unless a Prestige or season change sits between them.
- The store refuses a name outside the catalog and any value matching the account-number pattern.
