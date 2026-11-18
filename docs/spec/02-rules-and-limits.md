# 2. Rules and limits

What the app may never do, how capture was decided, and what is and isn't known about Activision's rules.

## 2.1 The limits

These are fixed. A build session does not weigh them against convenience.

- `[Legal, 2026-11-01]` Never read or write the game's memory, files or process, never automate input to it, and
  never inject an overlay into it.
- `[Legal, 2026-11-01]` Only official sources for patch notes and game data. No third-party stat scrapers that work
  against Activision's terms.
- `[Legal, 2026-11-01]` Follow every site's robots.txt and terms when researching, and skip any site whose terms
  forbid automated access.
- `[Legal]` Copy no code from a repo that has no open-source license, and put no game art and no
  third-party-compiled game data into this repo.
- Design decision (2026-11-09): Claude never opens an image or a video frame.
- Ask the user before any image or frame goes to an outside service, including an AI API.

## 2.2 The capture decision

Design decision (2026-11-09): the app may read the screen while the game is played, as well as screenshots and
recordings.

Three options were weighed: screenshots only; also recordings after the game closes; also the screen while playing.
The third was chosen, which includes the first two. The same day, screenshots were set to be read as each one is
saved, so the app may run while the game runs.

This replaced an earlier rule: only screenshots, read after the fact.

**How the choice was made:** no option was marked as recommended, and none was described as safe. That stays true
everywhere: the spec, the app's own text and every session describe capture as the user's choice, made at their own
risk, with Activision's position unverified.

## 2.3 What Activision documents, and what it doesn't

Read on 2026-11-09 from Activision's own posts in Steam's feed (`sources.md`).

| | What the posts say |
|---|---|
| **Documented** | RICOCHET Anti-Cheat uses server-side detection, hardware-based protections with "TPM 2.0 and Secure Boot integration", investigations and legal action `[P-TRUST]` |
| **Documented** | "TPM 2.0 and Secure Boot are required to play" Modern Warfare 4 on PC, and "Other security measures may be enforced" `[P-TRUST]` |
| **Documented, in an older post about Black Ops 7** | Two input devices, Cronus Zen and XIM Matrix, are "not permitted in Call of Duty", and detection there looks at input behaviour `[P-RICO]` |
| **Not stated in any post read** | Anything about third-party software in general, overlays, screen capture, screen reading, software macros or companion apps; any list of allowed tools; any penalty for a specific tool |
| **Not stated in any post read** | Whether RICOCHET uses a kernel driver on PC for this game |

**Not read at all.** Activision's Security and Enforcement Policy and its terms of use are published on
Activision's own sites, which this project does not read automatically. This spec does not summarise them. A user
can read them, and what they say outranks anything here.

**Other people's statements are not Activision's permission.**
- CODMunity's privacy page says its app reads no memory, injects nothing and uses no driver (as reported on
  2026-11-09; not re-read for this spec). That describes CODMunity's app, not what Activision allows.
- Two public tracker projects copy the screen through Windows while the game runs and say the same of themselves.
  One of them adds that Activision has made no commitment and that use is at the user's own risk.

**So, plainly:** nothing read for this spec says a program that reads screenshots, recordings or the screen is
allowed, and nothing says it is forbidden. Whether Activision detects or acts on any of the three is unknown. The
account at stake is the user's own, and whether to use the app, and which inputs, is the user's decision, at their
own risk.

## 2.4 What the app does and never does

| The app may | The app never |
|---|---|
| Read screenshot and recording files the user made, from the NVIDIA App's capture folder | Read or write any file under the game's install folder, Steam's library, or `Documents\Call of Duty` |
| Copy the picture of a monitor from Windows | Open the game's process, read or write its memory, or load anything into it |
| Read Steam's running-app value and the names in the process list, to know the game is running | Ask anything of the game's process beyond its name |
| Show its own window on another monitor | Draw over the game, stay on top of it, or take focus from it |
| Display a build or a Build Code for the user to enter | Send a key press, a click, a paste or any other input, to the game or to anything else |
| Read news from official posts | Fetch callofduty.com, activision.com, or any stat or guide site |
| Add tasks through the optional task export, and push job status through the optional monitor push, once the user has set them up (`09-optional-integrations.md`) | Send a screenshot, a frame, a player name or an account number anywhere |

**How this is held in code, not just in words:**
- **One door for each risky thing.** File reads go through one component that refuses the forbidden folders.
  Screen copying, the process list and the registry value each live in one small component.
- **A forbidden-call check in CI.** The build fails if the code references the Windows calls for opening another
  process, reading or writing its memory, starting a thread in it, installing an input hook, or sending input. The
  list lives beside the check and is reviewed when a dependency is added.
- **Dependencies are checked for the same calls and for network traffic of their own** before they are added. A
  reader library that sends telemetry has it switched off before first use, and a test proves it.
- **Live reading is off until the user turns it on.** In the stage that builds it, it first runs only when the person
  running that session turns it on. One switch turns every input off. The dashboard always shows which inputs are
  on.

## 2.5 Research rules for sessions

- Official game facts come from Steam's feed for the game. A fact from anywhere else is not a fact here.
- The public repos in `sources.md` were read for approach only. No code and no data came from them, and none may.
- A library's behaviour is taken from its current documentation, with the library and version named beside the
  fact, not from memory.

## 2.6 Standing statements

- This project is unofficial and is not affiliated with or endorsed by Activision.
- Activision's position on a program like this is unverified. Using it is at the user's own risk.
- Game names are their owners' trademarks. The repo holds no game art.
