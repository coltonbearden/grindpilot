# 1. What the app is and isn't

## 1.1 What it is

A Windows desktop app for a player on their own Windows PC. It keeps track of their Call of Duty: Modern Warfare 4
progress by reading their own screens, and tells them what to play next.

Design decision (2026-11-09): the first version covers camos, loadouts, weapon leveling, prestiges, challenges and
blueprint building, plus the recommended extras in the last row below.

**The first version, area by area:**

| Area | The spec |
|---|---|
| Camos | The camo board (`05-features/camos.md`) |
| Loadouts | The loadout book with health marks (`05-features/loadouts.md`) |
| Weapon leveling | The weapon list, next unlocks and pace (`05-features/weapon-leveling.md`) |
| Prestiges | Status, the before-you-prestige checklist and token advice (`05-features/prestiges.md`) |
| Challenges | Dailies, calling cards and the tracked list (`05-features/challenges.md`) |
| Blueprint building | A build planner, the user's saved builds, a blueprint inventory and Build Codes (`05-features/blueprint-building.md`) |
| Recommended extras | The session planner (`06-session-planner.md`); a review list where the user settles doubtful reads; a test library made of the user's own confirmed screens, kept in the data folder; and, through the optional task export when it is set up, tasks for the few things worth interrupting the user (`09-optional-integrations.md`) |

**How it works, in one paragraph.** The player presses a key on a menu screen, records a session, or lets the app
watch the screen. Two independent text readers read fixed regions of screens the app recognises. A value that both
agree on and that passes its checks is stored with where it came from; anything else is stored as unread and offered
to the user to settle. Plans, checklists and builds are worked out from the store by plain rules. A model can add
explanation, and never changes a value.

## 1.2 What it isn't

- **Not a cheat and not an edge in a match.** It shows nothing about other players, the map or the fight.
- **Not an overlay.** Its window is a normal window on another monitor.
- **Not automation.** It never presses a key, clicks or types for the user, in the game or anywhere else.
- **Not a memory reader or a file reader.** It never touches the game's process, memory or files.
- **Not a stats scraper.** It uses no unofficial API and no third-party stat site.
- **Not a tier list.** The build planner knows only the user's unlocked attachments and the figures on their own
  screens.
- **Not a record of other people.** No player name, account number or chat is read, stored or sent.
- **Not a service.** One user, one PC, no accounts, no server.
- **Not official.** It isn't affiliated with or endorsed by Activision.
- **Not cleared by Activision.** Activision's position on a program like this is unverified
  (`02-rules-and-limits.md`, 2.3). Using it is the user's decision, at their own risk.

## 1.3 The ideas, weighed

Design note (2026-11-09): nine ideas were weighed, not assumed.

| Idea | Verdict | Where |
|---|---|---|
| A one-pass account scan from the menus | **In.** A guided list of screens to capture, with coverage shown per weapon. It grows with each stage's profiles | Stages 2 to 5 |
| A planner that stacks camo, weapon-level, challenge and prestige goals into one match | **In** | Stage 4 |
| A build per challenge, limited to attachments the user has unlocked | **In,** as the build planner's goals | Stage 6 |
| Pace forecasts from the user's own history | **In,** per play day at first, per hour once recordings give match times. Never shown on fewer than five observations | Stages 3 and 9 |
| A before-you-prestige checklist | **In** | Stage 7 |
| A patch watch on the user's saved builds | **In, with a stated limit:** tuning notes aren't in any source this project may read, so it sees season posts and announcements only | Stage 8 |
| Unlock timestamps for the user's own videos | **In** | Stage 9 |
| The user's own screenshots and recordings kept as a test library, with an agent that proposes reader fixes after a game update and proves them against it | **In, attended.** The library is there from stage 2. A nightly self-test notices a game update and says so in the app, and through the optional task export when it is set up; the fix is a Claude Code session that must prove its change against the library. An agent that does this with no session open is on the later list | Stages 2 and 8 |
| A second-monitor dashboard instead of an overlay | **In,** from the first stage. It follows play live from stage 10 | Stages 1 and 10 |

## 1.4 Later

Not in the first version. Each needs its own decision before it is built.

- **An unattended reader-repair agent.** It needs an API key, a spending cap, a monitor and a design decision of its
  own.
- **Warzone and DMZ.** Their own Mastery camos appear on the camo board because the game shows them there. DMZ's
  70-level track, Operator Prestige, and Warzone's seasonal challenges are not tracked.
- **Reticle challenges, operator unlock challenges, BlackCell point challenges, the Battle Pass and events.**
- **Performance over time** from the Career and combat-record screens.
- **Reading in-match counters live,** such as kills this match toward Gold. The first live stage reads menus,
  after-match screens and unlock notices only.
- **Other resolutions, aspect ratios, UI scales and languages.**
- **A local image model** on the user's GPU. It would keep images on the PC, but it means installing a model runner
  and weights with an owner to keep them current, and it would compete with the game for the GPU.
- **Pasting patch notes by hand,** if the user wants the patch watch to see tuning changes.
- **A signed installer.**
- **Anything shared between people or PCs.**
