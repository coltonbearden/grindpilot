# 7. AI and agents

Design decision (2026-11-09): use AI and agents wherever they help, inside the rules in 7.1.

Every place a model or an agent is used, at build time and inside the app. For each: what it reads, what leaves the
PC, what it costs and its cap, who owns it, how a failure reaches a person, and how a wrong answer is caught before it
can change the account state.

## 7.1 Rules for all of them

- **Agents plan and watch. None plays, and none sends input** to the game or to anything else.
- **No model ever receives an image or a frame.** Design decision (2026-11-09): no model and no build session views a
  capture. Models get numbers and game labels from the account state, and text from official posts.
- **No model output is written into the account state.** Observations come from readers and from the player. A model
  produces words to read, flags to check, or a code change to prove.
- **The rule-based result is always there.** The plan, the build and the patch flags all work with every model
  switched off. A model adds explanation; it replaces nothing.
- **Every model call has a cap and an owner,** and a failure that matters reaches a person: in the app, on the
  "Inputs and status" page (`08-app-shape-and-stack.md`, 8.4), and through the optional monitor push or task export
  when the user has switched them on (`09-optional-integrations.md`); at build time, in the session's report.
- **Nothing here names a player.** The inputs hold no names by construction (`04-capture-and-readers.md`, 4.6).

## 7.2 At build time

Build sessions with Claude Code follow the spec and `CLAUDE.md`.

| # | What | Reads | Leaves the PC | Cost and cap | Owner | Failure reaches a person by | A wrong answer is caught by |
|---|---|---|---|---|---|---|---|
| B1 | **Build sessions.** A Claude Code session per build stage, following the spec and `CLAUDE.md` | This repo; the masked view of captures; current library docs through Context7 | The session's text, to Anthropic. No image: the repo's settings block the Read tool on image and video files | The maintainer's own Claude Code plan. No API key in the repo | The maintainer who starts the session | The session's report: what changed, and what the maintainer should check by hand | The stage's proofs (`10-build-stages.md`), the test suite, and the maintainer's own check |
| B2 | **Subagents inside a build session** (research, review, test writing) | What the main session passes them | The same as B1 | Inside B1 | The main session | The main session's report | The main session checks their work against the code and tests. A brief to a subagent restates the image rule, because some built-in subagents don't load `CLAUDE.md` |
| B3 | **Reader repair after a game update.** A build session a maintainer starts when the nightly self-test (S1 below) shows a screen has changed | The failing cases as masked views and field outcomes; the profiles; the replay tool | As B1 | As B1 | The maintainer who starts it | The self-test stays red until it passes again | A changed profile must pass the whole local test library with no case that used to be right now wrong, and the maintainer confirms the new samples on a local proof page before they become tests |
| B4 | **Dependency updates.** GitHub's Dependabot opens a pull request when a package the app uses has a new release `[ctx7: /github/docs]` | The repo's package list | Nothing new: the repo is already on GitHub | No key. GitHub's billing docs say standard runners are free for Dependabot `[ctx7: /github/docs]` | The repo's settings on GitHub | A failing pull request shows in the repo; each build session starts by merging the green ones and fixes or reports the rest (`docs/build.md`) | CI must pass, including the forbidden-call check and the test that a reader library's telemetry is off |

**Not at first:** an unattended reader-repair agent that runs with no session open. It would need an API key of its
own, a spend cap and a monitor. It is on the "later" list (`01-scope.md`).

## 7.3 Inside the app

All three are optional. Each is off until the user provides an API key (the optional AI workspace,
`09-optional-integrations.md`, 9.5) and switches the feature on.

| # | What | Reads | Leaves the PC | Owner | A wrong answer is caught by |
|---|---|---|---|---|---|
| A1 | **Session briefing.** Turns the rule-based plan into a short written briefing | The plan, the values behind it, and any news lines in it | Those numbers and game labels, to Anthropic's API | The app | It is shown beside the plan, labelled as written by a model. It changes no value and no ranking. A briefing that names a weapon, camo or challenge not in the plan it was given is thrown away by a check in code |
| A2 | **Patch-note matcher.** Decides whether a new line in an official post concerns one of the player's weapons, attachments or saved builds, when plain name matching can't tell | The new lines, and the names of the player's weapons, attachments and builds | The same, to Anthropic's API | The app | Its answer must be a structured list of matches, each citing the post's line and a catalog entry. A match whose line or entry doesn't exist is dropped. A kept match is a **flag** that links to the official post; the player reads the post |
| A3 | **Build explanation.** Says in words why the planner chose a build | The proposed build and the figures the planner used | The same, to Anthropic's API | The app | It is shown under the planner's own reasons, which stay. It can't change the build |

**How the calls are made:** through Anthropic's official C# SDK (the `Anthropic` NuGet package) and the Messages
API, one request per use, with a structured output where the result is machine-read (A2). No agent loop and no
tools: none of these needs one. The model, request shape and refusal handling follow the `claude-api` skill's
guidance current at build time, not this page.

**Model and cost.** Prices are from the `claude-api` skill's table, cached 2026-11-03.

| Use | Model | Per call, roughly | When |
|---|---|---|---|
| A1 | `claude-opus-5-5` ($4 per million input tokens, $20 per million output) | About 3,000 tokens in and 1,500 out: about 4 cents | When the player asks for a briefing |
| A2 | `claude-opus-5-5` | About 6,000 in and 500 out: about 3 to 4 cents | Only on a day a new official post arrived and name matching left a line undecided |
| A3 | `claude-opus-5-5` | About 2,000 in and 800 out: about 2 to 3 cents | When the player asks |

Those are estimates from token counts, not measurements; the stage that builds each one measures it. A cheaper
model would cut them further; stage 11 makes that choice (`10-build-stages.md`).

**The caps, three deep:**
1. **A Claude Console workspace of its own** for the app, with a monthly spend limit the user chooses
   (`09-optional-integrations.md`, 9.5). This is the hard cap: Anthropic enforces it.
2. **In the app:** at most 20 calls a day across A1 to A3, and it stops calling when its own running estimate for
   the month passes 80% of the monthly limit the user enters beside the key.
3. **By design:** A2 runs only when rules leave something undecided, and A1 and A3 only when asked.

**The key.** One API key from that workspace, which the user creates and enters in Settings. It is stored in Windows
Credential Manager, known by its item name only (proposed: `grindpilot-claude-key`), and never in the repo, a file,
a command line or a log. Without a key, A1 to A3 are off and nothing goes to Anthropic.

**When a call fails** (no network, a refusal, the cap reached): the feature shows "briefing unavailable" with the
reason, and everything rule-based carries on. A failure in A2 during the scheduled patch watch is part of that
job's status (S2).

## 7.4 Scheduled jobs

Design decision (2026-11-09): the app may run scheduled jobs. Each is a Windows Task Scheduler task that the app
registers for the signed-in user only (no admin), in a `\GrindPilot\` task folder, when the user switches that job on
in Settings, and removes when the job is switched off or the app is uninstalled. It runs `GrindPilot.Jobs.exe` with
no window, and holds off while the game is running (`09-optional-integrations.md`, 9.3).

| # | Job | When | Uses a model | What it does | Failure reaches the user by |
|---|---|---|---|---|---|
| S1 | **Reader self-test** | Nightly | No | Replays the local test library on this PC (`04-capture-and-readers.md`, 4.8) through the real pipeline. Reports cases right, unread and wrong, and the share of live frames that matched no profile since the last run. With no test library on the PC, the replay is skipped and the status says so | Red on the "Inputs and status" page when any case is wrong, when unread cases rise above the last good run, or when unmatched frames jump (the sign of a game update). When configured, also a down on its monitor push (9.4) |
| S2 | **Patch watch** | Daily, and only does work when Steam's news feed has an official post it hasn't seen | A2, when switched on | Reads Steam's news feed for Call of Duty (app 1938090), announcements only: the same feed `tools/CatalogSeed` reads (`docs/build.md`, "The catalog"). Compares new lines with the player's weapons, attachments and saved builds. Flags each affected build, linking the official post, and, when the task export is on, adds one task per affected build: "Check your <weapon> build: a patch note mentions it" | Red on the "Inputs and status" page when the feed can't be read or its format changed, or when a task couldn't be exported. When configured, also a down on its monitor push (9.4) |

**What each needs before it first runs:** only to be switched on. The monitor push and the task export are extras,
off until the user configures them (`09-optional-integrations.md`, 9.2 and 9.4); the "Inputs and status" page shows
every run's result either way.

**What the patch watch reads, and its limit:**
- **Only official posts count.** It reads Activision's announcements in Steam's feed and nothing else for news
  (`02-rules-and-limits.md`).
- **When the feed isn't in the shape the reader expects,** the patch watch reports "feed format changed, not read"
  and goes red. It doesn't guess.
- **Steam's feed carries no patch notes** (`sources.md`). Weapon balance changes are published only on
  callofduty.com, which this project doesn't read. So the patch watch sees season posts and announcements, not
  tuning notes (`11-open-questions.md`, 11.1 #7).

**What a scheduled job never does:** start the game, read the screen, or run while the game is running unless the
job exists for that purpose. Neither of these does.

## 7.5 What leaves the PC, in one place

The installed app's traffic, and nothing else. A build session's traffic belongs to development, not the app (7.2).

| Goes to | What | When |
|---|---|---|
| **Nowhere** | Screenshots, recordings, frames, cut-out regions, the account store, the test library | Always |
| **GitHub** | A read of the update repo's releases, and the download of an update | At start and once a day while open (`08-app-shape-and-stack.md`, 8.6) |
| **Steam's news feed** | A read of the official announcements for app 1938090 | When the patch watch (S2) runs |
| **Anthropic's API** | Numbers and game labels from the account state; lines from official posts | Only with the optional AI workspace (`09-optional-integrations.md`, 9.5) and A1 to A3 switched on |
| **The task export's target** | A task: fixed phrases, a game label such as a weapon's name, a link to an official post | Only when the user has configured it (9.2) |
| **The monitor push's target** | A job's status: up or down, with counts and fixed reasons, no values from captures | Only when the user has configured it (9.4) |
