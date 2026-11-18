# 11. Open questions

What this spec doesn't know. Nothing here is guessed elsewhere: where a part depends on one of these, it says
"unknown" and points here.

## 11.1 For a player or a design decision to answer

| # | Question | Why it matters | Where |
|---|---|---|---|
| 1 | **Exactly what does a Classic Prestige reset on a weapon: its level, its attachments, its camos?** A player's report, not yet confirmed by an official post or a confirmed screen: weapons reset | The before-you-prestige checklist. The rest is read from screens either side of a Prestige | `05-features/prestiges.md` |
| 2 | **What is the Armory, and where is it?** No sample carries that word | Whether the app reads it at all | `04-capture-and-readers.md`, 4.2 |
| 3 | ~~Does the user own BlackCell?~~ **Settled as a setting:** whether the user owns BlackCell this season is set in the app, default not owned | How the dailies and the bonus are counted | `05-features/challenges.md` |
| 4 | **How many custom loadouts can a player have, and is there a limit?** | The Loadouts page | `05-features/loadouts.md` |
| 5 | **Where does the game show that today's reroll is used?** | Offering "reroll this one" only when it is still available | `05-features/challenges.md` |
| 6 | ~~What should a user's own backup take?~~ **Settled 2026-11-17:** the app's daily copies in `backups\`; not the live `state.db`, and not `test-library\` | The account store's only other copy is the app's own daily backup beside it | `03-account-state.md`, 3.1 |
| 7 | **The patch-note gap.** Tuning notes are only on callofduty.com, which nothing here reads. Leave it, or let the user paste notes into the app by hand when they care about one (on the later list, `01-scope.md`, 1.4)? | How much the patch watch can ever flag | `sources.md`, "What the official feed does not carry" |
| 8 | ~~The app's display name.~~ **Settled 2026-11-17:** GrindPilot. The repo and its folders use the name too, and the data folder is `GrindPilot-data` | Installer, window title, task titles | `00-index.md` |
| 9 | ~~Where the program installs.~~ **Settled 2026-11-17.** Velopack's uninstall deletes the whole `%LocalAppData%\{packId}` folder `[ctx7: /velopack/velopack.docs, "Uninstalling"]`, so with the program and its data in one folder an uninstall would delete the store, its backups and the test library. Design decision (2026-11-17): the program goes in `%LOCALAPPDATA%\GrindPilot\` (pack id `GrindPilot`); the data lives apart, in `%LOCALAPPDATA%\GrindPilot-data\` | Whether an uninstall or reinstall can lose the account store | `08-app-shape-and-stack.md`, 8.6 |

## 11.2 Unknown until a screen shows it

Each becomes known when a maintainer captures and confirms the screen in the stage named, and not before.

| Unknown | Stage |
|---|---|
| The Weapon Prestige Master cap. `[P-PROG]` names a camo at level 250 and states no cap | 3 |
| What the word `PRESTIGE` beside a weapon's `n /m` figure refers to: the weapon's own prestige, or the unlocks of the player's current Prestige | 3 |
| Three details left unsettled in the screens table: the loadout's name (`01 CUSTOM` or `01 CUSTOM CLASS`), and the change figures beside two stats | 2, 5 |
| Whether Mastery camo counters are per weapon or pooled | 3 |
| The Special camo challenges for each weapon | 3 |
| The Prestige Master tab once a player is past Prestige 10 | 7 |
| The Permanent Unlock Token screen, and the Prestige choice screen | 7 |
| The Gunny suggestion screen | 6 |
| Where a Build Code is shown for a saved build, and what a code looks like | 6 |
| How owned blueprints are laid out on the builds screen | 6 |
| Whether wildcards exist in Create a Class | 5 |
| Weekly challenges, DMZ dailies, and where event challenges are listed | 4 |
| Where the game shows its version, so captures can be tied to a patch | 2 |
| XP needed per player level and per weapon level | Never read directly; worked out from the user's own history if at all |

**Conflicts between official posts,** kept as conflicts until a confirmed screen settles them
(`03-account-state.md`, 3.5): six weapon unlock levels; the Throwing Knife at 48 or 49; Weapon Prestige resetting to
level 1 or to zero.

## 11.3 Unknown about Activision's rules

Set out in full in `02-rules-and-limits.md`, 2.3. In short:
- No official post read says anything for or against screenshots, recordings, screen reading or companion apps.
- Activision's policy pages were not read. A user can read them; they outrank this spec.
- How the anti-cheat treats a program that copies the screen is unknown, and so is whether such a copy comes back
  blank. Stage 10 finds out, only when the person running it turns live reading on.
- Whether any official stats API exists is unverified. None was found, which doesn't prove there is none. The app
  is designed to need none.
- Because Activision's position is unverified, whether to use the app, and which inputs, is the user's decision, at
  their own risk.

## 11.4 Technical questions for the stages

| Question | Stage |
|---|---|
| Which Windows interface copies the monitor: Windows.Graphics.Capture, whose border an unpackaged app may not be able to remove, or DXGI Desktop Duplication, whose docs were not read for this spec | 10 |
| ~~Avalonia's support policy.~~ **Settled 2026-11-17, Avalonia 12.1.6:** no lifecycle is published for the free releases. Security fixes go to the current 12.x only ("11.x and older: Not provided; 12.x (current stable): Provided") `[docs: github.com/AvaloniaUI/Avalonia, SECURITY.md]`; the five-year LTS for 12.0 is the paid Enterprise offer `[docs: avaloniaui.net/enterprise]`. So the app stays on the newest 12.x, which Dependabot proposes | 1 |
| ~~Whether Velopack verifies an update's signature.~~ **Settled 2026-11-17, Velopack 1.2.161:** it checks a download's size and SHA hash against the release feed and throws `ChecksumFailedException` on a mismatch; no signature check is documented, and signing in its docs is Authenticode for SmartScreen `[ctx7: /velopack/velopack.docs; docs: docs.velopack.io/integrating/overview]`. An update is trusted as far as GitHub's HTTPS and the repo's releases, which need no token (`08-app-shape-and-stack.md`, 8.6) | 1 |
| ~~How long CI takes.~~ **Measured 2026-11-17:** a CI run takes about 4 minutes once Velopack's helpers are cached (about 10 the first time), a release about 12 | 1 |
| ~~Whether Velopack's helpers touch other programs.~~ **Found 2026-11-17, Velopack 1.2.161:** `Setup.exe` and `Update.exe` open every running process on each install, update and uninstall (`src/bins/src/shared/util_windows.rs`, lines 46-60), with no setting to stop it. Design decision (2026-11-17): the helpers are built from Velopack's source with that scan removed (`docs/build.md`, "Velopack's helpers") | 1 |
| The license of the PP-OCR model files the second reader ships | 2 |
| ~~Who keeps Velopack's `vpk` tool current.~~ **Settled 2026-11-17:** nobody needs to. `vpk` runs only in CI, as a private copy of the `vpk` package at the Velopack package's version, carrying the patched helpers; it is never installed on a developer's PC (`docs/build.md`) | 1 |
| What happens if Smart App Control is switched on: the unsigned app would be blocked | 1, if it comes up |
| Other resolutions, aspect ratios, UI scales and languages. Only 1920×1080 English was sampled | Not planned |
| Whether the two readers stay as accurate on live frames as on files | 10 |

## 11.5 Noticed along the way, reported and not fixed here

- **The NVIDIA App** refuses to capture while Chrome shows protected content, and its Alt+F1 key collides with the
  game's Party & Friends key.
