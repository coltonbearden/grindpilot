# 4. Capture and readers

How a picture of the game becomes a value in the account state, and how everything else in the picture stays out.

**The rule this part enforces:** a value that fails its check is stored as *unread*, never guessed
(`03-account-state.md`).

## 4.1 The three inputs

Design decision (2026-11-09): the app may read all three. No input is called safe; see `02-rules-and-limits.md`.

| Input | What it is | When it is read | Since |
|---|---|---|---|
| **Screenshot files** | Image files the user takes with their own screenshot tool, in a folder the user sets in the app (the NVIDIA App's default capture folder is one example) | As each file lands, also while the game runs (design decision, 2026-11-09: as each one is saved) | Build stage 2 |
| **Recordings** | Video files the user records with their own tool, in a folder the user sets | Only after the game has closed | Build stage 9 |
| **The live screen** | A copy of the monitor the game is on, taken from Windows | While the user plays | Build stage 10; off until the user turns it on, at their own risk |

The capture tool, its key and its folder are the user's choice; the app's settings name the folder, and the app only
reads files from it. It never calls a capture tool (`09-optional-integrations.md`, 9.7). What a sample session on
2026-11-10 showed about capture tools:
- **The samples** were PNG screenshots and recordings at 1920×1080, taken with the NVIDIA App.
- **A screenshot key the game also uses is not usable.** In the sample session one such key opened the game's Party &
  Friends panel, which covers the screen with friends' names. The user picks a key the game doesn't take.
- **A capture tool can refuse to capture.** The NVIDIA App refused while a browser showed protected content, and said
  so in its own notice. The app must treat "no file arrived" as normal and say so, not assume a capture happened.

## 4.2 The screens the app reads

Each row is a screen the app knows, from sample screens captured on 2026-11-10. "Shows" lists what the two readers
found there. Names in capitals are the game's own labels; `n`, `m`, `a/b`, `<WEAPON>` and `<CLASS>` stand for what
differs from player to player. The last column gives the source tags (`sources.md`) the rest of the spec cites for
that screen; the samples behind them are not published.

| Screen | How the game labels it | Shows | Source tags |
|---|---|---|---|
| **Player level** | `CAREER /` › `PROGRESSION`, tab `MULTIPLAYER LEVEL UNLOCKS` (other tabs: `DMZ LEVEL UNLOCKS`, `PRESTIGE`, `PRESTIGE MASTER`) | The prestige track by name, the Prestige number, an XP boost label, a row of level tiles, the items each level unlocks with their type, and the current level card | S02 |
| **Prestige** | `PROGRESSION`, tab `PRESTIGE` | Ten tiles, `PRESTIGE 1` to `PRESTIGE 10`, each with its track and a status (`COMPLETED`, `IN PROGRESS`, or none) | S03 |
| **Challenges home** | `CAREER /` › `CHALLENGES` | A `CAMOS` tile (status, next camo, requirement), a `CALLING CARDS` tile (status, challenge, progress), and `DAILY CHALLENGES`: a countdown, one row per daily (text, progress, XP) and a `BONUS` row for completing three | S04 |
| **Camo challenges** | `CAREER / CHALLENGES /` › `CAMO CHALLENGES`, tab `WEAPON MASTERY` | Nine weapon-class tabs, a grid of weapons, and for the selected weapon: `n/15 CAMOS`, the next camo and its category, its challenge with progress, and the `NEXT MASTERY` requirement with progress | S05 |
| **Mode mastery** | `CAMO CHALLENGES`, tab `MODE MASTERY` | Three columns (Multiplayer, DMZ, Warzone) of three Mastery camos each, and the selected one's requirement and progress | S14 |
| **Tracked camos** | `CAMO CHALLENGES`, tab `TRACKED & NEAR COMPLETE` | A `NEAR COMPLETE` list of weapons, the selected entry's camo category and requirement, and `TRACK NEXT CAMO [n/9]` | S15 |
| **One weapon's camos** | `CAREER / CHALLENGES / CAMO CHALLENGES /`, tabs `<WEAPON> PROGRESSION` and `MODE MASTERY` | Four groups with counts: `MILITARY [n/8]`, `SPECIAL [n/3]`, `COMPLETIONIST [n/4]`, `PRESTIGE [n/4]`. For the selected camo: its name, group, requirement with progress, and challenge with progress | S19, S22, S25, S26 |
| **Calling cards** | `CAREER / CHALLENGES /` › `CALLING CARDS`, tabs `REGULAR`, `ELITE`, `PRESTIGE`, `TRACKED & NEAR COMPLETE` | Categories (`MULTIPLAYER MASTERY`, `DMZ MASTERY`, `SHARED MODE MASTERY`, `WZ MODE MASTERY`, `GAME MASTERY`), each category's `MASTERY SETS` with `n/6`, and each set's requirement | S46, S47 |
| **One calling-card challenge** | inside a Mastery Set | The challenge name, `TIERED CHALLENGE`, tiers done (`n/m COMPLETED`), the requirement, progress (`a/b`) and `ADD TO TRACKED [n/9]` | S48 |
| **Loadouts** | `CREATE A CLASS` | The loadout list (numbered custom slots) and, for one loadout: operator, primary, secondary, melee, tactical, lethal, field upgrade, perks and killstreaks with their kill costs. Each weapon line shows its class, name and a progress figure | S39 |
| **Weapon select** | `CREATE A CLASS / EDIT CUSTOM CLASS /` › `SELECT PRIMARY WEAPON` (or `SECONDARY`) | Class tabs; per weapon its name, a progress figure (`PRESTIGE n /m`, or `PRESTIGE MASTER n`), a `BUILDS` marker and attachment pips; for the selected weapon a description and its stats | S44 |
| **Builds** | `… / SELECT PRIMARY WEAPON /` › `<WEAPON> BUILDS` | `+ CREATE NEW BUILD`, the saved builds by name, the selected build's stats with their changes, and the actions `FIRING RANGE`, `PREVIEW WEAPON`, `EQUIP WEAPON AND OPEN GUNSMITH`, `TOGGLE DETAILS` | S70, S71 |
| **Gunsmith** | `CREATE A CLASS / EDIT CUSTOM CLASS /` › `<WEAPON>`, tabs `GUNSMITH`, `APPEARANCE`, `PROGRESSION` | The weapon class, `PRESTIGE n /m UNLOCKS`, `ATTACHMENTS n/5`, an `APEX` slot (and `BLOCKED BY APEX` on a slot it blocks), the fitted attachments by name and slot, stats with their changes, a Gunny control and `SELECT BUILD` | S52, S72, S73 |
| **Attachment list** | `… / <WEAPON> /` › `SELECT MUZZLE` (one per slot) | The list for that slot starting with `NONE`, a count (`n/m`), a `SHARED ATTACHMENT` flag, a lock line (`EARN n MORE WEAPON UNLOCKS USING THE <WEAPON>`), `PROS` and `CONS`, and stats with their changes | S50 |
| **Details** | the same screen after `TOGGLE DETAILS` | Exact numbers: damage by range with multipliers, fire rate, aim-down-sight in and out times, sprint-to-fire time, reload time (all in ms), bullet penetration (in inches), flinch and movement figures | S51 |
| **Appearance** | Gunsmith tab `APPEARANCE` | Slots for `CAMO`, `RETICLE` and cosmetic items; `RETICLE SELECT` has tabs `PROGRESSION` and `SHARED` | none |
| **Camo select** | `CREATE A CLASS / … / CAMO SELECT /` | The same four groups as the camo page, plus `SHARED` and a further group | none |
| **Equipment and perks** | `SELECT TACTICAL`, `SELECT FIELD UPGRADE`, `SELECT PERK`, `SELECT KILLSTREAK` | The items by name; perks in rows `PERK 1` to `PERK 3`; a count (`n/m`); the killstreak's cost (`n KILLS`); a lock line for a locked item (`UNLOCK AT LEVEL n`); `APPLY TO ALL LOADOUTS` | S65, S66 |
| **Operators** | `SELECT OPERATOR` | The operators and a count (`n/m`) | none |
| **Import a build** | dialog `IMPORT WEAPON BUILD` | A line asking for a Build Code, and `IMPORT WEAPON BUILD` / `CANCEL` | S75 |

**Seen so far only in recordings:** the main menu, the Prestige rewards track, the Career summary and combat
record, recent matches (mode, result, weapon), the Profile, the lobby with its dailies; during a match the timer,
objective prompts, score events (`n XP KILL`) and an unlock notice (`… UNLOCKED`); after a match the result with
its mode, `BEST PLAY`, the scoreboard, and a progression screen showing the level before and after, what was
unlocked and the XP earned.

**Not among the samples:**
- **Armory.** No sample carries that word. Either its screen is titled differently or it wasn't captured. `[unknown]`
- **Permanent Unlock Token screen.** Not sampled yet. `[unknown]`
- **The Gunny suggestion screen itself and the export side of Build Codes.** The Gunsmith shows a Gunny control
  and the import dialog exists; what they look like when used is not read yet. `[unknown]`

## 4.3 What the trial found

A throwaway trial on 2026-11-10 ran two local OCR engines over a set of 1080p screenshots and over frames pulled from
recordings. It answers one question: which fields can be read at 1920×1080. It does not choose the app's stack.

| | Windows OCR (`Windows.Media.Ocr`) | RapidOCR (PP-OCR models on ONNX Runtime, CPU) |
|---|---|---|
| Library facts | The trial used the `en-US` recognizer; `MaxImageDimension` is 10000; returns lines and words with boxes and **no confidence** | The version tried (3.10.0) ships its default detection, direction and recognition models inside the package, so nothing is downloaded; returns lines with a **confidence** each |
| Time per full screen, on the trial PC | About 60 ms at native size, about 180 ms on a 2× enlargement | About 540 ms (range 360 to 1,240 ms) |
| Typical slips | Small digits: a number beside a word on a tile lost its number; short counts and small XP figures were missed at native size and found at 2× | Missed a large digit after a word; read one number reversed; joins neighbouring cells into one line |
| Video frames | 46 words per frame (median) | 65 words per frame (median); screenshots gave 50 |

**Findings the design rests on:**
1. **Neither engine is enough alone.** On the fields checked against a confirmed-values set (4.9), all three
   readings (Windows OCR at native size and at 2×, and RapidOCR) agreed on fewer than two thirds. Each engine got
   fields the other lost.
2. **Agreement was never wrong.** Where Windows OCR at 2× and RapidOCR returned exactly the same words, the value
   matched the confirmed one every time. Where they differed, often both were right and one had picked up a
   neighbouring cell; at least one reader was right on all but a few fields.
3. **Enlarging helps Windows OCR** find small text, at about three times the cost. Of the three readings, RapidOCR
   read the most fields in full and Windows OCR at native size the fewest.
4. **Recorded frames read about as well as screenshots** at the bitrate tried (1080p, 60 fps, about 16 Mbps).
5. **Free reading of a whole frame is how names leak.** Menu screens are mostly labels and numbers. Match frames
   are full of other players' names and account numbers. So the app reads fixed regions of recognised screens and
   nothing else (4.6).

**What this trial is not.** It read whole screens and picked fields out with hand-placed boxes. The app reads
cut-out regions enlarged to a fixed text height (4.4), which should do better on small text. Stage 2 proves that
against a confirmed-values set; until then these findings are the floor, not the claim. What such a set is, and why
none of its values appear here, is in 4.9.

## 4.4 The reading pipeline

Every input goes through the same steps. Each step can stop the frame; a stopped frame changes nothing.

1. **Take a frame.** A screenshot file, a frame of a recording, or a copy of the live screen.
2. **Gate it cheaply.** Before any text reading, a fast check on a few small areas (the header strip and the
   breadcrumb) decides whether this could be a known screen. Most live frames stop here.
3. **Recognise the screen.** The header and breadcrumb are read and matched against the screen profiles (4.5).
   All of a profile's anchors must match. No match means "not a known screen": the frame is dropped, and only a
   count of such frames is kept.
4. **Read the profile's fields, and only those.** Each field is a region of the frame. The region is cut out,
   enlarged to a fixed text height, and read by **both** engines.
5. **Check each field.** Its own rule first (4.7), then rules across fields of the same screen.
6. **Decide.**
   - Both engines agree and the rules pass: the value is stored as **read**.
   - They disagree, but exactly one reading passes the rules: that one is stored as **read, single engine**.
   - Otherwise the field is stored as **unread**, and the cut-out region goes to the review list.
7. **Never combine readings from different frames.** A second frame can confirm or replace a value. It can't be
   stitched together with the first.
8. **Store an observation** (`03-account-state.md`), carrying the capture it came from, both readings and the
   outcome.

**The review list** holds unread and doubtful fields for the user to settle. It shows the cut-out region and both
readings; the user picks or types the value, and it is stored as **confirmed** (`03-account-state.md`). A confirmed
value becomes a case in the local test library (4.8). The list is capped (200 entries, oldest dropped first) and its
images never leave the user's PC.

## 4.5 Screen profiles

A screen is described by data, not code, so that a game update means editing a profile and proving it against the
test library.

A profile holds:
- **A name and version**, and the game version it was last proven on.
- **Anchors:** regions with the words that must be there (for example the title `CAMO CHALLENGES` and the tab
  `WEAPON MASTERY`). One wrong letter is tolerated in a word of six or more letters; none in a shorter one.
- **Fields:** a key (`player.level`), a region in fractions of the frame, a type (4.7), and the rule it must pass.
- **Repeating blocks:** a grid or a list is one block description plus its step, not one field per cell.
- **States:** how the same screen looks locked, completed or in progress, where that changes the regions.
- **Never-read areas:** regions the profile rules out by name, such as the player card.

Profiles cover 1920×1080 at the UI scale and language of the samples (English). Another resolution, aspect ratio,
UI scale or language needs its own samples before it is supported; an enlarged 1080p frame is not a substitute.

## 4.6 Keeping other players out of everything

- **Regions only.** No reader ever runs over a whole frame in the app. A region is read only if a profile lists
  it, and no profile lists the player card, the party or friends panel, the kill feed, the scoreboard's name
  column, text chat or the watermark line.
- **Closed lists for names of things.** A weapon, camo, attachment, perk, operator, mode or map is accepted only
  if it matches the catalog (`03-account-state.md`). Look-alike characters count as equal (`O`/`0`, `I`/`1`/`l`,
  `S`/`5`, `B`/`8`); any other difference is a non-match and the field is unread.
- **Numbers that are not progress are refused.** A `#` followed by digits, or seven or more digits in a row, is an
  account or session number and is never stored, logged or shown.
- **Nothing raw is kept.** The app stores field values and outcomes. It does not store the text of a frame. Frames
  and cut-out regions exist in memory, in the review list and in the local test library, and nowhere else.
- **The player's own name** is never read. No feature needs it.
- **Logs** carry field keys, outcomes and timings, never values from free text.
- **Build sessions** follow `CLAUDE.md`: no image is opened, and a session prints only the masked view.

## 4.7 Field types and their rules

Every number in the "Looks like" column is invented; the names and status words are the game's own.

| Type | Looks like | Rule |
|---|---|---|
| **Count pair** | `12/40`, `[3/8]`, `1200 /1500` | Two whole numbers, first ≤ second, second inside the range the profile gives |
| **Level** | `LEVEL 12`, `PRESTIGE 2` | Whole number inside the known range (player level 1 to 55; Prestige 1 to 10) `[P-PROG]` |
| **Weapon progress** | `PRESTIGE 20 /60`, `PRESTIGE MASTER 12` | First ≤ second; the second matches the weapon's unlock count once that is known |
| **Stat with change** | `50 +5`, `38 -4`, `41` | A whole number, then an optional signed number |
| **Time** | `300 MS` | A whole number and the unit the profile expects |
| **XP** | `1000`, `5000` | A whole number inside the profile's range |
| **Name** | `GOLD` (a camo) | A match in the catalog's closed list for that kind of thing |
| **Status word** | `COMPLETED`, `IN PROGRESS`, `EQUIPPED` | One of the words the profile lists |
| **Sentence** | `Get 40 Headshots against Players` | Matches one of the known challenge patterns, with its number read as a number |
| **Countdown** | `9H 20M` | Hours and minutes inside 0 to 24 hours |

Rules across fields, where a screen allows them: group counts on a camo page add up to the weapon's `n/15`; a
tier count never exceeds its total; a value that is lower than the last confirmed value for the same key is held
for review unless something explains it (a Prestige, a new season).

**The game's own counters can be wrong.** The readers report what the screen shows. The app says "as shown in
game", not "true".

## 4.8 The test library

Confirmed values from real screens are the app's local tests. The tests in the repo use invented data.

- **Where:** `%LOCALAPPDATA%\GrindPilot-data\test-library\`, on the PC that took the screens. Images and frames stay
  there. Nothing from it is committed, because samples can show other players and the player's own account values.
- **What a case is:** an image file, its sha256, the screen it is, and the confirmed value of each field.
- **What is committed:** nothing from the library. Its confirmed values stay in the library, in
  `test-library\expected\samples-<id>.json`, with their import map in `test-library\imports\<id>.json` (4.9). The
  repo's tests use invented values of the same shape, with no screenshot behind them, in `tests/fixtures/`.
- **Seeded from:** a maintainer's own captures and the values confirmed for them (4.9).
- **How it grows:** every value confirmed in the review list adds a case. A screen that changes after a game
  update gets new cases; old ones are kept, marked with the game version they belong to.
- **On the PC that holds the library:** the full suite replays every case through the real pipeline. A case passes
  when every field matches or is unread. A wrong value is a failure; an unread one is counted separately. Cases
  that need the library skip when it is absent, and say so.
- **In CI, where the images don't exist:** the same code runs on synthetic frames drawn by the tests (text of the
  right size on a dark background), on the invented data in `tests/fixtures/`, and on the rules and the catalog.
  CI never claims to prove real screens.
- **Recordings:** a replay case is a video, a list of expected events each with a time window, and a mode that
  reads every frame so the result repeats. A run with no expected events is reported as **unscored**, never as a
  pass.

## 4.9 The screens table

A **confirmed-values set** is what the readers are measured against on real screens (4.3, 4.8). A maintainer makes
one from their own screenshots; none is published.

- **How it is made.** The readers' best value for each field of each sample screen is put in a one-line summary per
  screen. The maintainer checks each summary against the picture by eye, field by field, and confirms or corrects
  each value. A value the summary only implies or paraphrases, and a detail left unsettled, is marked as such.
- **How strong it is.** A confirmation against a summary is weaker than one value beside one picture. So stage 2
  puts each value through the review list (4.4), picture beside value, before it becomes a golden test case.
- **What it holds.** Values that are numbers and game labels only, keyed by sample tag and field: no player name,
  no text of a frame. Beside them it records each screenshot's file name and sha256, which is one more reason it
  stays local.
- **Where it lives.** Outside the repo, in the data folder: `test-library\expected\samples-<id>.json` (the confirmed
  values) and `test-library\imports\<id>.json` (the import map). No picture, file name or hash from it is
  published, and no account value from it appears in this part or in `05-features/`. The repo's tests use an
  invented set of the same shape in `tests/fixtures/`.
- **How a reader is scored against it.** Per field: **yes** (it read every confirmed word), **part** (it missed
  some and read nothing else in their place) or **no** (it read something different).

Design decision (2026-11-10): the first set was accepted as matching its pictures, in the sense above and no
stronger one.

## 4.10 Reading recordings (build stage 9)

- **Only after the game has closed**, known from the game-running check (`09-optional-integrations.md`, 9.3).
- Frames are pulled at a low rate (start at one every two seconds) and go through the same pipeline.
- A recording yields **events with times**: a level-up, an unlock, a match result. Times are offsets into the file,
  so they can serve as chapter marks for the user's own videos (`09-optional-integrations.md`, 9.7).
- The recording itself is never changed, moved or deleted by the app.

## 4.11 Reading the live screen (build stage 10)

What the live reader does, and what it never does:

- **It takes the picture from Windows, not from the game.** It copies the monitor the game is shown on, through a
  Windows screen-capture interface. It never opens the game's process, reads its memory or files, loads anything
  into it, or draws on top of it.
- **It finds out that the game is running** from Steam's running-app value and the names in the process list
  (`09-optional-integrations.md`, 9.3). It asks nothing of the game's process beyond its name.
- **It sends no input** to the game or to anything else, and it never brings itself to the front.
- **The dashboard is a normal window on another monitor.** There is no overlay.
- **It stays small:** below-normal priority, a few frames a second at most, one reused frame buffer, and the cheap
  gate (step 2) before any reading.
- **It expects to break quietly** when the game's window changes (the splash screen, a resolution change, a
  minimise). It checks every few seconds that frames are arriving at the expected size, and shows "not reading"
  when they are not.

Two Windows interfaces can copy a monitor. Which one the app uses is decided by the first live stage, on a test PC:

| | Windows.Graphics.Capture | DXGI Desktop Duplication |
|---|---|---|
| Documented | Capturing a monitor from its handle needs Windows 10 version 1903 or later. Windows draws a yellow border round what is captured; removing it needs user consent and a capability in a package manifest. `[docs: learn.microsoft.com, Windows.Graphics.Capture]` | Not read for this spec. `[unknown]` |
| Open for that stage | Whether an app installed without a package can remove the border | Whether it works with the game in its usual display mode |

**What is not known and must be tested in that stage, only on the maintainer's explicit go-ahead and at their own
risk:** how the game's anti-cheat treats a program that copies the screen, and whether the copy comes back blank.
`02-rules-and-limits.md` sets out what is documented and what is not.

## 4.12 What it may cost the game

No published figure exists for any of this, so the stages measure it on a test PC:

- Reading a screenshot file: under one second end to end, at below-normal priority.
- The live reader: no measurable change in the game's frame rate, checked with the game's own counter over the
  same match type with the reader on and off; under 300 MB of memory; under 2% of one CPU core averaged over a
  match.
- A stage that misses these numbers reports them and stops short of turning the feature on.
