# 5.6 Blueprint building

Design decision (2026-11-09): it covers all four parts: a build planner, the user's saved builds, a blueprint
inventory and Build Codes. Design decision (2026-11-10): all four parts stay, as checked against the screens already
sampled.

## What the game does

**Gunsmith** `[P-LOAD]`, `[S72]`, `[S73]`
- **Five attachments** per weapon (`ATTACHMENTS n/5` on screen), plus an **Apex** slot that doesn't count.
- **An Apex can block slots.** A slot it blocks shows `BLOCKED BY APEX` `[S52]`; `[P-LOAD]` says many Apex
  attachments block categories.
- **Slots** `[P-LOAD]`: optics, lasers, barrels, muzzles, underbarrels, stocks, rear grips, comb, ammunition and
  magazines; some weapons add bolt, guard, trigger action, cylinder and fire mods.
- **Five stat bars:** Damage, Range, Accuracy, Mobility, Handling. The Gunsmith shows each as a number with the
  change an attachment makes (in the invented form `50 +5` or `38 -4`), alongside fire rate and rounds `[S73]`.
- **Each attachment** shows a description with `PROS` and `CONS` `[S50]`.
- **Details view.** `TOGGLE DETAILS` shows exact figures: damage by range with multipliers, fire rate,
  aim-down-sight in and out times, sprint-to-fire time, reload time, bullet penetration and more `[S51]`. **These
  are the game's own numbers, on the user's own screen, so the planner needs no outside stat site.**
- **Locked attachments** say what they need: `EARN n MORE WEAPON UNLOCKS USING THE <WEAPON>` `[S50]`.

**Gunny** `[P-LOAD]`, the "Attachment Optimizer": suggested builds made only from unlocked attachments, in three
kinds (close-range, balanced, long-range). The Gunsmith shows a Gunny control `[S52]`. The suggestion screen itself
is not sampled. `[unknown]`

**Saved builds** `[S70]`, `[S71]`: a weapon has a `BUILDS` screen with `+ CREATE NEW BUILD`, the saved builds by
name, each build's stats, and the actions `FIRING RANGE`, `PREVIEW WEAPON` and `EQUIP WEAPON AND OPEN GUNSMITH`.
The Gunsmith has `SELECT BUILD`. No official post describes any of this.

**Blueprints** `[P-PROG]`, `[P-BP]`
- **Store and Battle Pass blueprints:** the base weapon stays available for good.
- **Weapon Prestige** doesn't relock blueprint attachments.
- **Mastercraft and Reactive blueprints:** the barrel, stock and other attachments can be swapped, and the
  blueprint's look stays.
- The builds screen carries the word "blueprint" `[S70]`; how owned blueprints are laid out there is to confirm.
- Whether a blueprint's attachments can be used before they are unlocked. `[unknown]`

**Build Codes** `[P-WZ]`, `[S75]`: the Warzone post names "Build Sharing & Build Codes" without describing them.
The game has an `IMPORT WEAPON BUILD` dialog that asks for a Build Code. Where a code is shown for a saved build,
and what a code looks like, are not sampled. `[unknown]`

## The four parts

### A. Build planner

**What the user sees.** The user picks a weapon and a goal. The planner proposes five attachments and, if it is
unlocked, the Apex, from what the user has unlocked. For each pick it shows the reason in numbers from the user's own
screens. Below the build it lists what a better build would need and how many unlocks away each missing attachment
is.

**Goals at first:** a camo challenge type (headshots, longshots, hipfire, kills from behind, prone, mounted,
double kills, kills without reloading); lane holding while aimed; fast leveling on small maps.

**How it decides.** Plain rules, no model:
- Each goal ranks the stats that matter for it. Lane holding ranks recoil and range first and tolerates slow
  handling. Hipfire ranks hipfire spread and mobility.
- Each attachment is scored from its stat changes and its Details figures. An attachment whose figures haven't
  been captured can't be scored and is left out, and the planner says how many were left out.
- A build must be legal: one attachment per slot, five at most, nothing in a slot the Apex blocks.
- Gunny's three suggestions, when captured, are shown beside the planner's as a comparison.

**What it is not.** It is not "the meta". It knows only the user's unlocked attachments and the figures on their
own screens. It says so on the page.

### B. My saved builds

**What the user sees.** Every build saved in the game, per weapon, with its attachments, its stats and a goal tag.
Each build shows whether it is equipped in a loadout, whether a Weapon Prestige would break it, and whether a patch
note mentions its weapon or attachments.

**What the user does.** Captures a weapon's builds screen, and the Gunsmith for a build to record its attachments.
Tags the build with a goal. Keeps notes on it.

### C. Blueprint inventory

**What the user sees.** Per weapon, the blueprints owned, and what each means: its base weapon stays available
through a Classic Prestige, and its attachments stay through a Weapon Prestige. The prestige checklist and token
advice use this.

**What the user does.** Captures each weapon's builds screen once, and again after getting a new blueprint.

### D. Build Codes

**What the user sees.** A code stored beside a saved build, and a large, easy-to-read display of it for typing into
the game's import dialog.

**What the user does.** Captures or types a code into the app, and types it into the game themselves.

**What the app never does.** It never types, pastes or sends anything into the game.

## Data and where it comes from

| Data | Source |
|---|---|
| Slots, the Apex, stat names, Gunny, blueprint rules | `[P-LOAD]`, `[P-PROG]`, `[P-BP]` |
| Attachment names per weapon and slot, and whether each is unlocked | Attachment lists `[S50]`, harvested into the catalog and confirmed by the user |
| What each attachment changes | The stat changes on the list `[S50]` and the Details view `[S51]` |
| The user's builds, their names and stats | The builds screen `[S71]` and the Gunsmith `[S73]` |
| Owned blueprints | The builds screen `[S70]` |
| Build Codes | The user's own capture or typing |
| Goal rankings | A fixed table in the app, reviewed by a maintainer in build stage 6 |

**The cost of this feature is captures.** Scoring an attachment needs its list entry and its Details figures once.
A weapon with ten slots of about eight attachments is about 80 entries. The app tracks coverage per weapon and
never pretends an uncaptured attachment is bad or good.

## Tests

- **Legal builds only:** five or fewer attachments, one per slot, none in a blocked slot, all unlocked.
- **No figure, no score:** an attachment without captured figures is excluded and counted.
- **Golden cases:** the confirmed-values set (`04-capture-and-readers.md`, 4.9) for the Attachment list, Details,
  Builds, Gunsmith and Import a build screens produces its confirmed values.
- **Stat arithmetic:** a build's shown stat equals the base stat plus the changes of its attachments wherever the
  game shows all three; a mismatch is reported, not smoothed over.
- **Weapon Prestige breakage** matches the rule in `weapon-leveling.md`.
- **Build Code display** round-trips: the code stored is the code shown, character for character, in a typeface
  where `0`/`O` and `1`/`I`/`l` are distinct.
