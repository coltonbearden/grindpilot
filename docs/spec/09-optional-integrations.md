# 9. Optional integrations, off by default

## 9.1 What this page is

The integrations a user may switch on to connect GrindPilot to tools they already run, and the one check that is
always on.

- **Each integration is off by default** and does nothing until the user configures it.
- **Each is configured locally,** on the user's own PC, in Settings: an address or a command is kept in the data
  folder (`%LOCALAPPDATA%\GrindPilot-data\`), readable by that Windows user only, and a key in Windows Credential
  Manager. None is ever written to the repo or a log, and no address or key is passed on a command line.
- **Nothing personal is in the repo:** no address, no key, no folder with a user name, no default target.
- **Everything rule-based works with all of them off.**
- **9.3 is not optional.** The game-running check is part of the app and always on. It sits here because other parts
  of the spec point to it as 9.3.

## 9.2 The optional task export

When something needs the player's attention, the app can add a task to the player's own task list.

- **The target** is one the user configures: a local endpoint the app sends each task to, or a command it runs with
  the task on its standard input. There is no default target, and with none configured nothing is exported.
- **What a task holds:** a title, a kind (do or review), a short body and a stable key. The words are fixed
  phrases, counts, game labels (a weapon's name) and a link to an official post. Never raw text read from a capture,
  never a player name and never a token.
- **The tasks it may export, and no others:**

  | Task | Kind | When |
  |---|---|---|
  | "Run the Prestige checklist: level 55 reached" | do | Once per Prestige, when level 55 is first read |
  | "Check your <weapon> build: a patch note mentions it" | review | From the patch watch, one per build, linking the official post (`07-ai-and-agents.md`, S2) |

- **A failing scheduled job is not exported as a task.** It shows on the "Inputs and status" page
  (`08-app-shape-and-stack.md`, 8.4) and, when configured, as a down on the monitor push (9.4).
- **Once per thing.** The app records what it has exported and doesn't export the same thing again: one task per
  Prestige, and one per build and post.
- **It only adds.** It never edits, closes or reads back a task in the user's list.
- **When an export fails,** the status says so on the "Inputs and status" page. The app doesn't retry in a loop.

## 9.3 The game-running check (always on)

Not optional: this is part of the app, and always on.

- **How it knows:** Steam's `RunningAppID` value under `HKCU\Software\Valve\Steam` is 1938090, Call of Duty's Steam
  app id; or a process named `cod`, or the campaign's `sp26-cod`, is running.
- **How it reads them:** one registry value, and the names from one system snapshot of the process list, read by
  process name only (`docs/build.md`, "The forbidden-call check"). It never opens the game's process or any other.
  Names are matched, not paths or folders, because reading a process's path means opening the process.
- **What it is used for:** holding recordings and scheduled jobs until the game has closed, never updating the app
  during play, and telling the live reader when to look. Updates and jobs wait while the game runs.
- **The line it doesn't cross:** it asks nothing else of the game's process.
- **Where:** `src/Windows/GameRunning.cs`.

## 9.4 The optional monitor push

Each scheduled job (`07-ai-and-agents.md`, S1 and S2) can send a heartbeat to an uptime monitor the user runs, for
example an Uptime Kuma push monitor.

- **The push address** for each job is one the user pastes into Settings. It is kept in the data folder, readable by
  that Windows user only, and never written to the repo or a log.
- **After each run,** the job sends its status: up with a short summary, or down with a reason.
- **A job that doesn't run at all** (the PC off, the task removed) misses its heartbeat and shows as down there. So
  the monitor's interval should allow for a PC that is off or signed out.
- **Reasons are plain text with no values from captures:** counts and fixed phrases only.
- **With no address configured,** nothing is sent; the "Inputs and status" page shows each run's result either way.

## 9.5 The optional AI workspace

The model features of `07-ai-and-agents.md` (A1 to A3) need an Anthropic API key, which the user provides.

- **Ideally the key comes from a Claude Console workspace of its own,** with a monthly spend limit the user chooses.
  That limit is the hard cap; the app's own caps sit under it (7.3).
- **The key** is entered in Settings and stored in Windows Credential Manager (proposed name:
  `grindpilot-claude-key`), never in the repo, a file, a command line or a log.
- **Without a key** the features are off and nothing goes to Anthropic. Everything rule-based works the same.
- **What is sent, and what never is:** `07-ai-and-agents.md`, 7.3 and 7.5. No image or frame, ever.

## 9.6 Backups

- **The app keeps its own daily copies** of the account store: the newest 14, in
  `%LOCALAPPDATA%\GrindPilot-data\backups\` (`03-account-state.md`, 3.1; `docs/build.md`, "Data").
- **A user may point their own backup tool at `backups\`** for a copy off the PC.
- **Never at the live `state.db`,** which the app has open, and **never at `test-library\`,** whose samples can show
  other players (`04-capture-and-readers.md`, 4.8).

## 9.7 Screenshot and recording tools

- **The capture tool and its folder are the user's own,** and both are settings. The app watches the folder the user
  names and only reads: it never changes, moves or deletes a screenshot or a recording.
- **The NVIDIA App is one example,** and the one the screens were sampled with (`04-capture-and-readers.md`, 4.1):
  PNG screenshots, saved with recordings in a folder for the game under `Videos\NVIDIA\`.
- **Known behaviours of the NVIDIA App:** its default screenshot key, Alt+F1, is also the game's Party & Friends key,
  so set the screenshot to another key; and it refuses to capture while Chrome shows protected content.
- **The app never calls the NVIDIA App** or any other capture tool. It reads only the files they save.
- **Chapter marks.** For each recording it reads, the app can write a small file in its data folder (not beside the
  video) listing events with their offsets: level-ups, unlocks, match results. A video tool that takes chapter marks
  can use it. The format is fixed in stage 9 (`10-build-stages.md`).
