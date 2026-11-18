# GrindPilot: the spec

GrindPilot is an unofficial Windows companion app for Call of Duty: Modern Warfare 4. This spec was written from the
game's real screens and from Activision's official posts, and is split into build stages that later build sessions
can each run without re-deciding anything.

## Status

The spec was written on 2026-11-10 and is the design the build stages follow. Its design decisions are listed
below. A build session that finds a decision no longer fits records that in `11-open-questions.md` and does not
change the decision on its own.

## The app in five lines

- It reads the player's own screens: screenshots they take, recordings after the game closes, and the live screen.
- Two independent text readers read fixed regions of screens it recognises. A value that fails its check is stored
  as unread, never guessed.
- One local store holds the player's level, prestige, weapons, camos, challenges, loadouts, builds and unlocks, each
  value with where it came from.
- Plain rules turn that into a camo board, a session plan, a prestige checklist and proposed builds.
- It never touches the game's memory, files or process, never sends input, and never draws over the game.

It is not affiliated with or endorsed by Activision. Activision's position on a program that reads screenshots,
recordings or the screen is unverified (`02-rules-and-limits.md`, 2.3), and whether to use one is the user's
decision, at their own risk.

## The parts

| # | Part | What it settles |
|---|---|---|
| 1 | [What the app is and isn't](01-scope.md) | The first version, the nine ideas weighed, and the "later" list |
| 2 | [Rules and limits](02-rules-and-limits.md) | The limits, the capture decision, and what Activision documents against what is unverified |
| 3 | [Account state](03-account-state.md) | The store, its history, how sure each value is, and the catalog |
| 4 | [Capture and readers](04-capture-and-readers.md) | The screens, the reader trial, the pipeline, the test library, and how other players stay out |
| 5 | Features: [camos](05-features/camos.md), [weapon leveling](05-features/weapon-leveling.md), [challenges](05-features/challenges.md), [prestiges](05-features/prestiges.md), [loadouts](05-features/loadouts.md), [blueprint building](05-features/blueprint-building.md) | For each: what the user sees, what they do, the data and its source, and the test |
| 6 | [The session planner](06-session-planner.md) | What to play next, by plain rules |
| 7 | [AI and agents](07-ai-and-agents.md) | Every model and agent use, what leaves the PC, cost, cap, owner, failure path |
| 8 | [App shape and stack](08-app-shape-and-stack.md) | The options against current docs, the choice, install, update and tests |
| 9 | [Optional integrations, off by default](09-optional-integrations.md) | The optional task export, the optional monitor push and the optional AI workspace: what each does, and how whoever runs the app sets it up locally |
| 10 | [Build stages](10-build-stages.md) | Eleven stages, each with scope, proofs, and what it needs from a person |
| 11 | [Open questions](11-open-questions.md) | Everything not known, and who or what can answer it |
| | [Sources](sources.md) | The official posts and the confirmed in-game screens behind every game fact |

## Design decisions

| Date | Area | What was decided |
|---|---|---|
| 2026-11-01 | Limits on touching the game | The app never reads or writes the game's memory, files or process, never automates input to it, and never injects an overlay into it (`02-rules-and-limits.md`, 2.1) |
| 2026-11-06 | Where the project's rules live | Rules for build sessions live in this repo, never in a user-level configuration |
| 2026-11-09 | What it covers | Camos, loadouts, weapon leveling, prestiges, challenges and blueprint building, plus the recommended extras in `01-scope.md`, 1.1 |
| 2026-11-09 | Capture | Screenshots, recordings after the game closes, and the live screen while playing; each is the user's choice, with Activision's position unverified (`02-rules-and-limits.md`, 2.2) |
| 2026-11-09 | When a screenshot is read | As each one is saved, so the app may run while the game runs |
| 2026-11-09 | Viewing images | Claude never opens an image or a video frame (`02-rules-and-limits.md`, 2.1) |
| 2026-11-09 | Where plans live | In the app and in plan files; the optional task export, when set up, may also add a few tasks to the user's own task list (`06-session-planner.md`, 6.5) |
| 2026-11-09 | What may run on its own | Scheduled jobs, besides the app itself (`07-ai-and-agents.md`) |
| 2026-11-09 | Blueprint building | Four parts: a build planner, saved builds, a blueprint inventory and Build Codes |
| 2026-11-09 | Screenshot tool | The NVIDIA App's screenshot key, on a key the user sets: its default Alt+F1 collides with the game's Party & Friends key |
| 2026-11-10 | The screens table | A maintainer's confirmed-values set was accepted as matching its pictures, in the limited sense `04-capture-and-readers.md`, 4.9, describes; it stays outside the repo |
| 2026-11-10 | Blueprint building, against the real screens | All four parts kept |
| 2026-11-10 | Classic Prestige and weapons | A player's report, not yet confirmed by an official post or a confirmed screen: a Classic Prestige resets weapons. The rest of what resets is open (`11-open-questions.md`, 11.1) |
| 2026-11-10 | Mode preferences in the planner | The user may set mode, map and playlist preferences that the planner suggests by name (`06-session-planner.md`, 6.3) |
| 2026-11-17 | The app's name (11.1 #8) | GrindPilot |
| 2026-11-17 | Where the program installs, after Velopack's uninstall turned out to delete its whole folder (11.1 #9) | The program in `%LOCALAPPDATA%\GrindPilot\`; the data apart, in `%LOCALAPPDATA%\GrindPilot-data\` |
| 2026-11-17 | Backups (11.1 #6) | The app's own daily copies in the data folder's `backups\` are what a user's own backup should take |
| 2026-11-17 | Velopack's installer and updater open every running program, the game included | GrindPilot builds Velopack's helpers from source with that scan removed (`docs/build.md`) |
| 2026-11-18 | Player habits in the planner | Play style, how soon to prestige and where tokens go are the user's settings, with defaults (`06-session-planner.md`, 6.1) |

## Calls made while writing the spec

Twelve calls were made while the spec was written and reviewed on 2026-11-10. All were kept, one with a change; two
were updated for the public repo.

| # | Call | Part | Outcome |
|---|---|---|---|
| 1 | The stack: C# on .NET 10, Avalonia for the window, Velopack for install and update | 8 | Kept |
| 2 | Two readers on every field, and reading only fixed regions of recognised screens | 4 | Kept |
| 3 | The catalog takes names only from official posts and confirmed in-game screens; new names are harvested and confirmed by the user | 3 | Kept |
| 4 | The planner: plain rules, scored goals, and a mode only with a reason | 6 | **Changed:** the planner may also suggest modes and maps the user has set as their own preferences (6.3) |
| 5 | Exactly two kinds of task from the optional task export: the Prestige checklist, and a patch note naming a build | 9 | Kept |
| 6 | Two scheduled jobs: a nightly reader self-test and a daily patch watch | 7 | Kept |
| 7 | Model features are optional and off by default, use `claude-opus-5-5`, and run only through the optional AI workspace, under a $10 monthly limit set on that workspace | 7 | Kept |
| 8 | Releases come from the public repo's GitHub releases with no token (a token only for a private fork); the installer is unsigned at first | 8 | Kept; updated for the public repo |
| 9 | The order of the eleven stages, with recordings and the live screen last | 10 | Kept |
| 10 | This repo's settings make the NVIDIA App's changing tools ask first, and block the Read tool on images and video | `CLAUDE.md` | Kept |
| 11 | Live reading stays off until stage 10, and first runs only when the person running that session turns it on | 10 | Kept |
| 12 | The store, test library and backups live in `%LOCALAPPDATA%\GrindPilot-data\`. A maintainer's own confirmed values stay there, in `test-library\`, outside the repo; the tests use invented data in `tests/fixtures/` | 3, 4 | Kept; updated for the public repo |
