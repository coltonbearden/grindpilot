# 10. Build stages

Eleven stages. Each is sized for one Claude Code session, and each leaves the player with something to use. The
order puts the plainest input first (screenshots the player takes) and the two riskier inputs last.

Each stage below is the brief for one build session.

## 10.1 What every stage does

- **Starts by reading** `CLAUDE.md`, `docs/build.md`, `00-index.md`, this stage's entry, and the parts it names. It
  does not re-decide what they settle. When something no longer fits, it asks the maintainer.
- **Merges the green Dependabot pull requests first** (`docs/build.md`, "Build and test").
- **Never opens an image.** It works from masked views, and a maintainer confirms values on a local proof page.
- **Proves itself two ways:** CI green, and the local suite on a maintainer's PC showing no case that was right and
  is now wrong. A stage that changes a reader also adds the cases that prove the change.
- **Looks up libraries in current docs** and names the library and version beside each fact it relies on.
- **Ends with** a release that installs or updates, and a report of what changed and what a maintainer should check
  by hand.

## 10.2 The stages

### Stage 1: the app exists

- **Status:** built. How it works is in `docs/build.md`.
- **Scope:** the solution and its projects (`08-app-shape-and-stack.md`, 8.4). The account store with its three
  layers and its tests (`03-account-state.md`). The catalog, seeded from the official posts with every entry's
  source. The Avalonia window with tray, the "Inputs and status" page and a catalog browser. CI with the
  forbidden-call check, gitleaks and the headless window tests. Velopack install and self-update from the repo's
  public releases, with no token (8.6). The confirmed-values import, as code tested with invented data
  (`tests/fixtures/`): a new install starts empty.
- **The player can use:** an installed app that updates itself and starts with an empty account, plus a browser of
  the official facts: weapons, unlock levels, the camo ladder.
- **Proofs:** installs with no admin prompt; a new install starts with an empty account; a second release is picked
  up and applied on the next start; rebuilding the current state from observations matches; CI is green and fails
  when a forbidden call is added on a test branch.
- **From a maintainer:** run the installer once; click through one update.

### Stage 2: the first reader

- **Scope:** the reading pipeline with both engines (`04-capture-and-readers.md`, 4.4 to 4.7). The screenshot folder
  watcher. Profiles for the player level screen, the Prestige tab and the Challenges home. The review list. The
  test library, which is the maintainer's and stays outside the repo (4.8; `docs/build.md`, "The test library"),
  and the local suite. The telemetry-off test.
- **The player can use:** press the screenshot key on three screens and see their level, Prestige and today's
  dailies update, with anything doubtful waiting on the review list.
- **Proofs:** the maintainer's confirmed samples of those screens produce the confirmed values; no wrong value
  across every screenshot in the test library, counting unread separately; a screenshot of an unknown screen
  changes nothing; a file that never arrives is reported, not assumed.
- **From a maintainer:** fresh captures of the three screens; a pass through the review list; and a deliberate check
  of every seed value in the test library, picture beside value on the review list, before it becomes a golden
  case. Values first confirmed against a summary (4.9) are seen beside their own picture here.

### Stage 3: camos and weapons

- **Scope:** profiles for camo challenges, one weapon's camos, Mode Mastery, tracked camos and the weapon select
  list. The Camos and Weapons pages (`05-features/camos.md`, `weapon-leveling.md`). Style marks. Catalog harvest for
  Special camo names, each confirmed by eye before it is used.
- **The player can use:** the camo board with every weapon's next step, the class gates, the long counters and
  "closest to done"; and the weapon list with unlocks and prestige state.
- **Proofs:** the maintainer's confirmed samples of these screens produce the confirmed values; ladder-order and
  counts-add-up tests; no wrong value across the test library.
- **From a maintainer:** captures of each weapon class tab of the weapon list, and of the camo pages; confirmation
  of the harvested camo names.

### Stage 4: challenges and the session planner

- **Scope:** profiles for calling cards, Mastery Sets, one challenge and the tracked tab. The Challenges page. The
  session planner and its settings file (`06-session-planner.md`). The "Next session" page and the plan file.
- **The player can use:** a plan for the next session that stacks a weapon, camo steps, dailies and challenges,
  with the reason for each line.
- **Proofs:** the maintainer's confirmed samples of these screens produce the confirmed values; the planner's
  snapshot and property tests; the same state gives the same plan twice.
- **From a maintainer:** a review of the planner's weights and the style table; one session played from a plan,
  and what was wrong with it.

### Stage 5: loadouts and Gunsmith

- **Scope:** profiles for Create a Class, the selection screens (perks, equipment, field upgrades, killstreaks),
  the Gunsmith overview, an attachment list and its Details view. Catalog harvest for the names on those screens.
  Attachment coverage tracking. The Loadouts page (`05-features/loadouts.md`).
- **The player can use:** every loadout on one page with health marks, and per weapon a list of attachments with
  what each changes, as far as they have been captured.
- **Proofs:** the maintainer's confirmed samples of these screens produce the confirmed values; closed-list tests;
  a harvested name never appears until confirmed.
- **From a maintainer:** captures of each loadout; for the most-used weapons, each slot's list and its Details;
  confirmation of harvested names.

### Stage 6: blueprint building

- **Scope:** profiles for the builds screen and the import dialog; whatever the Gunny suggestion and Build Code
  export screens turn out to be. Saved builds, the blueprint inventory, Build Codes and the build planner with its
  goal table (`05-features/blueprint-building.md`). The Builds page.
- **The player can use:** their saved builds in one place with tags and codes; a proposed build for a goal from
  what they have unlocked, with reasons.
- **Proofs:** the maintainer's confirmed samples of these screens produce the confirmed values; legal-build and
  no-figure-no-score tests; a Build Code round-trips.
- **From a maintainer:** captures of the Gunny suggestion screen and of a Build Code being shown for a build; a
  review of the goal table; one proposed build built in the game, and how it felt.
- **Starts by** capturing those two screens. Design decision (2026-11-10): all four parts stay, as checked against
  the screens already sampled. If either new screen shows a part can't work as written, the stage asks the
  maintainer before building it.

### Stage 7: prestige

- **Scope:** the Prestige page: status, the before-you-prestige checklist, token advice and pace
  (`05-features/prestiges.md`). Manual entry for tokens held and spent. Observations either side of a Prestige, to
  pin down exactly what resets on a weapon. Known so far (2026-11-10): weapons reset, details unknown
  (`11-open-questions.md`, 11.1 #1).
- **The player can use:** a checklist before each Prestige that names what relocks and which loadouts break, and a
  token suggestion that follows the token rule (`05-features/prestiges.md`).
- **Proofs:** token advice never names a weapon; relock and broken-loadout tests; the weapons line shows "weapons
  reset, details unknown" until two observations exist.
- **From a maintainer:** tokens entered; captures of the token screen and the Prestige choice screen when next
  reached, without spending or choosing for the capture's sake.

### Stage 8: scheduled jobs, status and the optional integrations

- **Scope:** the `Jobs` program with the reader self-test and the rule-based patch watch (`07-ai-and-agents.md`, S1
  and S2), including the reader for Steam's news feed (7.4). Registering and removing the two Task Scheduler tasks
  for the signed-in user from Settings. The jobs' results on the "Inputs and status" page. The optional task export
  for the allowed tasks (`09-optional-integrations.md`, 9.2) and the optional monitor push (9.4), both off by
  default and configured only in Settings.
- **The player can use:** a red status when a game update breaks the readers, a flag when a patch note names one of
  their builds, and the Prestige checklist when level 55 is reached. With the integrations switched on, the last
  two also arrive as tasks in their own task list, and a failing job shows as down on their own monitor.
- **Proofs:** a forced failure in each job shows red on the status page and, against a test monitor, a down that
  clears on the next good run; a mangled copy of the feed gives "format changed" and red; a task is not exported
  twice for the same thing; with neither integration configured, the jobs send nothing but the feed read; neither
  job runs while the game is running; a job switched off, or the app uninstalled, leaves no scheduled task behind.
- **From a maintainer:** a test monitor and a test task target to prove both integrations against.

### Stage 9: recordings

- **Scope:** reading recordings after the game has closed (`04-capture-and-readers.md`, 4.10). Profiles for the
  after-match screens and the unlock notice. Events with offsets, and the chapter-marks file
  (`09-optional-integrations.md`, 9.7). The replay test tool with time-stamped expected events. Match history and
  pace per hour of play.
- **The player can use:** a list of what each recorded session unlocked and when, their own match history, and
  forecasts based on hours played.
- **Proofs:** the maintainer's test recordings replay to the events the maintainer confirms, with no miss and no
  false event in the every-frame mode; an unscored run is reported as unscored; no recording is changed, moved or
  deleted; nothing is read while the game runs.
- **From a maintainer:** confirmation of the expected events for the test recordings on a local proof page; one
  more recorded session.

### Stage 10: the live screen

- **Scope:** the monitor copy behind its one door, tried both ways (`04-capture-and-readers.md`, 4.11). The cheap
  gate. Live reading of the menu screens already profiled, the after-match screens and the unlock notice. The
  "not reading" state. The measurements in 4.12. One switch that turns live reading off.
- **The player can use:** a dashboard on a second monitor that follows their progress as they play, with no key
  press.
- **Proofs:** the measurements meet 4.12, or the stage reports them and leaves live reading off; the forbidden-call
  check is green; nothing is drawn on the game's monitor; live values agree with a screenshot of the same screen;
  the reader recovers after a resolution change and a minimise.
- **From a maintainer:** their go-ahead, in that session, before the reader first runs beside the game; two play
  sessions with it on and one with it off for the comparison, with no recording running during them.
- **Starts by** restating what is unverified about this input (`02-rules-and-limits.md`, 2.3), without calling it
  safe. Live reading is at the player's own risk, and stays off until they switch it on.

### Stage 11: the model features

- **Scope:** the session briefing, the patch-note matcher and the build explanation (`07-ai-and-agents.md`, A1 to
  A3), with the three caps and the checks that discard an answer naming things it wasn't given. The key, entered in
  Settings and kept in Credential Manager (`09-optional-integrations.md`, 9.5).
- **The player can use:** a written briefing for a session, an explanation of a proposed build, and patch flags for
  lines that name matching can't place, all with their own API key.
- **Proofs:** with the key removed, every feature still works without the model and nothing is sent to Anthropic;
  a prepared answer naming a weapon outside its input is discarded; the daily call limit stops calls; measured cost
  per call is reported against the estimates.
- **From a maintainer:** a Claude Console workspace with a monthly spend limit of their own choosing, and a key from
  it entered in Settings.

## 10.3 Why this order

- **Stages 1 to 7 use only screenshots the player takes.** That is the plainest input, and it covers the whole
  first version (`01-scope.md`).
- **Stage 8 comes once there are readers worth watching,** and before anything that runs on its own piles up.
- **Stages 9 and 10 add the two inputs decided on 2026-11-09** (`02-rules-and-limits.md`, 2.2), in order of how
  much they ask of the player and of the game.
- **Stage 11 is last** because nothing depends on it, by design.
- Stages 5 to 7 can swap among themselves if prestige help is wanted sooner, with one limit: the prestige checklist
  needs the loadout reader from stage 5.
