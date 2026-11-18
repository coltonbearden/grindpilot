# 6. The session planner

What to play next, worked out by plain rules. No model is involved, so the same state always gives the same plan
and every line can be traced to the values behind it.

## 6.1 What it follows

The planner follows the user's own preferences, set on the app's Preferences page. Each is a setting with a
default, so an untouched install plans from the account state alone.

| Setting | What it changes | Default |
|---|---|---|
| **Goals** | Which kinds of goal the planner lists: weapon levels, camos, challenges, player level and Prestige | All of them, in Multiplayer |
| **Prestige first** | When Prestige is available, the plan's first line is the Prestige checklist (6.4) | On |
| **Token use** | Where the token advice may put Permanent Unlock Tokens: non-weapon items only (such as perks), or any item (6.4) | Non-weapon items only |
| **Play style** | Challenge wordings that suit the user's style and wordings that don't (6.3, the Style part) | None set: every goal scores as neutral |
| **Mode and map preferences** | Modes, maps and playlists the planner may suggest by name, and core or hardcore per kind of goal (6.3) | Empty: changes nothing |

The settings file carries one example play style, not set by default: "aimed", whose wordings are listed under
Style in 6.3.

## 6.2 What a plan contains

About 25 lines, in this order:

1. **Status.** Prestige and level; the weapon being leveled; camo totals.
2. **Do first.** Anything that should happen before a match: "Prestige is available: run the checklist"; "a token
   is unspent"; "reroll this daily".
3. **Play this.** One weapon and loadout, and why.
4. **It also advances.** The other goals the same match moves: up to five, each with its progress.
5. **Dailies.** Each one, with a style mark.
6. **Second choice.** A different weapon and its bundle, for when the user wants a change.
7. **News.** Patch-note lines that touch a weapon or build in the plan.
8. **Read these first.** Screens whose values are stale or unknown and would change the plan.

## 6.3 How it decides

**Step 1: list the open goals.** From the account state, for the kinds of goal the Goals setting includes:
- each weapon's next unlock, next Military camo and Apex;
- each open camo step;
- each daily, and the bonus for three;
- each tracked calling-card challenge, and each one near its next tier;
- player level and the next Prestige.

**Step 2: score each goal.** Four parts, each from 0 to 1, multiplied:

| Part | Rule |
|---|---|
| **Closeness** | Progress divided by target. A goal with unknown progress scores 0 and is listed under "Read these first" |
| **Style** | With a play style set (6.1): 1.0 for wording that suits it; 0.7 neutral; 0.3 for wording that doesn't. With none set, the default, every goal scores 0.7. The example style "aimed" suits headshots, longshots, mounted, aimed kills and holding an objective, and doesn't suit hipfire, from behind, prone, sliding and point blank |
| **Worth** | Dailies and the daily bonus by their XP; a camo step that opens a gate for a whole class above one that doesn't; an unlock the user has marked as wanted above others |
| **Freshness** | 1.0 for a value read today, 0.8 for a stale one, 0.6 for one read by a single engine |

The numbers in this table are starting values. They live in one settings file, where they can be tuned.

**Step 3: build bundles.** A bundle is one weapon plus the goals a match with it can advance:
- goals for that weapon;
- goals for its class;
- goals that don't care about the weapon, such as wins or objective captures;
- minus any pair that can't be done together (a hipfire goal with an aimed one counts once, the higher).

The bundle's score is the sum of its top five goals. The best bundle is "Play this"; the best one with a different
weapon class is "Second choice".

**Step 4: pick the loadout.** The saved loadout whose goal tag matches the bundle, if one exists and is healthy
(`05-features/loadouts.md`). Otherwise the plan says which build the planner would use and that the user needs to
set it up.

**Step 5: name a mode only with a reason.** The planner suggests a mode when:
- a goal's own text names one;
- an official post gives a reason, with its tag shown; or
- the user's own preferences name one (below), shown as "your preference".

Otherwise it says "any mode". It never recommends a mode from general knowledge.

**The user's preferences.** Design decision (2026-11-10): the user can set mode preferences. On the Preferences page
the user sets standing choices the planner may suggest by name:
- modes they like for leveling a weapon, for camo steps, and for objectives;
- maps or playlists they prefer or avoid;
- core, hardcore or either, per kind of goal.

A preference is the user's statement, not a game fact. The plan labels it as theirs, a goal's own wording still
overrides it (a challenge that names a mode wins), and an empty Preferences page changes nothing. Mode and map names
the user picks come from the catalog, so a name the app hasn't seen is added the usual way (`03-account-state.md`,
3.5).

## 6.4 Rules that override scores

- **Prestige first.** With the Prestige first setting on (the default, 6.1), at level 55 the first line is the
  Prestige checklist, whatever else scores well. With it off, the checklist stays under "Do first".
- **Tokens follow the Token use setting.** With the default, non-weapon items only, tokens never go to a weapon.
- **A daily is never ranked below a goal with less XP** when both fit the same bundle.
- **Nothing rests silently on a bad value.** A line that depends on a stale or single-engine value carries a mark,
  and if every goal in the top bundle does, the plan leads with "Read these first".
- **Skipped means skipped.** A goal the user marked "skip for now" doesn't appear.

## 6.5 Where the plan shows up

- **In the app,** as the dashboard's "Next session" card, redrawn whenever the state changes.
- **As a file,** on request: markdown, in `%LOCALAPPDATA%\GrindPilot-data\plans\`, dated.
- **Through the optional task export, only as tasks, and only these** (`09-optional-integrations.md`): "Prestige is
  available", and "A saved build is named in a patch note". The export is off by default and set up locally by
  whoever runs the app. A session plan itself is never exported.

## 6.6 Tests

- **Same in, same out.** A fixed state gives a byte-identical plan.
- **Snapshots.** A set of prepared states (new account, mid-Prestige, level 55, everything stale) each give the
  expected plan, with the default settings and with each setting changed.
- **Properties,** checked over generated states:
  - with the default Token use setting, no plan ever spends a token on a weapon;
  - with a play style set, an off-style goal never outranks an otherwise equal on-style one; with none set, style
    never changes an order;
  - a goal with unknown progress never appears in a bundle;
  - a bundle never holds two goals that can't be done together;
  - with Prestige first on, at level 55 the Prestige line is first;
  - a mode appears in a plan only with one of the three reasons, and a preference never overrides a mode a goal
    names.
- **Traceable.** Every number in a plan resolves to an observation in the store.
