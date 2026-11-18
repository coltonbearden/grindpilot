# Building, testing and releasing GrindPilot

How the code in this repo is built, tested and released, for the build sessions that follow stage 1. The spec under
`docs/spec/` says what to build; this page says how the build works.

## What is where

| Folder | What | Depends on Windows |
|---|---|---|
| `src/Core` | The account store (SQLite, three layers), the catalog model, the state rules, the confirmed-values import, the data folder and its one-time move | No |
| `src/Readers` | Empty until stage 2 | No |
| `src/Windows` | The doors: game-running check, Credential Manager token store, update service, elevation check | Yes |
| `src/App` | The Avalonia window and tray; `GrindPilot.exe` | Yes |
| `src/Jobs` | `GrindPilot.Jobs.exe`: `check-store`, `check-feed`; the scheduled jobs from stage 8 | Yes |
| `tests/*.Tests` | One per project, plus the forbidden-call check's own tests | |
| `tests/Local` | The local suite: a maintainer's PC only (CI sets `CI=true`, which makes it build but not run) | |
| `tests/fixtures` | Invented test data: an import map and its confirmed values, made up, with no screenshot behind them | |
| `tools/ForbiddenCalls` | The forbidden-call check | |
| `tools/CatalogSeed` | Seeds part of `catalog/` from the official posts | |
| `tools/velopack` | The patch that removes Velopack's process scan, the reviewed source, the build script, Velopack's licence, the crate notices | |
| `tools/notices` | Writes `THIRD-PARTY-NOTICES.md` | |
| `catalog/` | The catalog, as data, every entry with its source | |

## Build and test

```
dotnet build GrindPilot.slnx -c Release
dotnet test GrindPilot.slnx -c Release
```

- The SDK is .NET 10 (`global.json` sets a floor of 10.0.100 and rolls forward), so Dependabot doesn't touch it.
- Every project keeps a `packages.lock.json`; CI restores with `--locked-mode`. After changing a package version, run
  `dotnet restore GrindPilot.slnx` and commit the changed lock files.
- Dependabot opens weekly pull requests for NuGet packages and GitHub Actions, with CI on each. **Each build session
  starts by merging the green ones** (a Velopack one only after its source is read, below). One that fails CI is
  fixed in that session or left open with the reason in its report.
- **Merge locally, never with GitHub's merge button or API.** A merge made on GitHub's side is authored with the
  merger's account email, which on a public repo publishes it. Fetch the pull request's branch and fast-forward
  `main` to it (or rebase it onto `main`), with the checkout's own git identity set to the committer's GitHub noreply
  address, then push; GitHub marks the pull request merged.
- `App` and `Jobs` (and `App.Tests`) are `win-x64`, so the locked restore and the self-contained publish agree.
- The headless window tests (`tests/App.Tests`) drive the real window with Avalonia's headless platform and Skia.
  They assert on the visual tree and on pixels counted in memory. **No frame is saved or opened** (`CLAUDE.md`,
  "Images and recordings"), and no baseline image is committed.

### The two-way proof every stage gives (10.1)

1. CI green on `main`.
2. The local suite on a maintainer's PC: `dotnet test GrindPilot.slnx -c Release` runs `tests/Local` too. A case that
   was right and is now wrong is a failure; from stage 2, an unread value is counted separately.

The local suite checks every catalog entry and fact against the post or confirmed screen it cites, and every import
pair in the test library (below). Cases that need the test library skip when it is absent, and say so.

### The test library

A maintainer's own confirmed values never enter the repo. They live in the data folder's `test-library\`
(`%LOCALAPPDATA%\GrindPilot-data\test-library\`, or under `GRINDPILOT_DATA`): `imports\<id>.json` (an import map) and
`expected\samples-<id>.json` (the confirmed values, by sample tag, with each screenshot's file name and SHA-256). The
local suite reads them there; CI and the unit tests use the invented pair in `tests/fixtures/`. From stage 2 the
golden cases go there too.

## Local-only files

Git ignores these, and they never leave the maintainer's PC:

- `CLAUDE.local.md`: a maintainer's own instructions for Claude Code sessions here. Claude Code loads it beside
  `CLAUDE.md`. Import nothing for this project from user-level Claude Code config.
- `.claude/settings.local.json`: per-machine permission rules (for example, for an MCP server only that PC has).
- `private/`: a maintainer's own scripts, such as a scan of commits for personal details before a push.

## The catalog

- `tools/CatalogSeed` writes `weapons.json`, `weapon-classes.json`, `tactical.json`, `lethal.json`,
  `field-upgrade.json`, `killstreak.json`, `perk.json`, `attachment-slots.json` and `sources.json` from P-LOAD's
  tables and weapon blocks and P-S1's weapon blocks: `dotnet run --project tools/CatalogSeed -- --repo .`. It reads
  Steam's news feed for app 1938090, announcements only, and keeps copies in the git-ignored `.scratch\posts\`. It
  keeps `sources.json`'s `screens` as they are.
- The other files (`camos.json`, `camo-groups.json`, `mastery-sets.json`, `calling-card-categories.json`,
  `challenges.json`, `attachments.json`, `rules.json`, `seasons.json`) are entered by hand.
- Every entry and fact names its sources: `P-…` (an official post) or `S…` (an in-game screen a maintainer confirmed by
  eye, listed in `sources.json`'s `screens` by the screen it shows; no picture is published). Where the name isn't
  written that way in the post, `quote` gives the exact words, kept to the few words that identify the fact, never a
  copied passage. A fact two posts state differently holds both values: a conflict.
- Checks: `tests/Core.Tests/CatalogFileTests.cs` in CI (shape, counts, the known conflicts, known source tags);
  `tests/Local/CatalogAgainstPostsTests.cs` on a maintainer's PC (every entry and fact against its source).
- Names on screens are matched with the look-alike rule of 4.6 (O/0, I/1/l, S/5, B/8). A read `l` may be an L or a
  misread I, so both are tried.

## The forbidden-call check

`tools/ForbiddenCalls` reads managed metadata (P/Invokes, references to `Process` members that open another
process, and run-time function lookup) and native import tables. It never runs anything it reads.

- **Our own assemblies** (`GrindPilot*.dll`) may make no call on `forbidden.json`'s list unless `allowed.json` names
  the exact call and why. It is empty.
- **Dependencies** (packages, Velopack's helpers, the packed release) may make one only when
  `dependency-review.json` names it with a reason. A new one fails CI until it is reviewed there.
- **Left out:** the .NET runtime's own files (read from the runtime pack in `GrindPilot.deps.json`) and the apphost.
  Where our code relies on a runtime call touching no other process, the runtime's source is read and cited: the
  game-running check's `Process.GetProcesses()` and `ProcessName` read one `NtQuerySystemInformation` snapshot and open
  no process (dotnet/runtime v10.0.0, `ProcessManager.Win32.cs` lines 290-294 and 341-355, `Process.cs` lines
  1099-1104, `Process.Windows.cs` lines 896-916; read 2026-11-17). Any other `Process` member needs the same reading
  first; most of them open the process, and `forbidden.json` lists those.
- **When a dependency is added or updated** and CI reports `NOT REVIEWED`: read that library's source for the call,
  and add an entry only if it can't touch another program. Run the check locally with `--write-review` for a skeleton.
- The list is 02-rules-and-limits.md, 2.4's: opening another process, reading or writing its memory, starting a thread
  in it, hooking or sending input, taking focus.

## Velopack's helpers

Velopack 1.2.161's own Windows helpers (`Setup.exe`, `Update.exe`) open every running process on each install,
update and uninstall, to find copies of the app by path (`src/bins/src/shared/util_windows.rs`, lines 46-60). That
would reach the game. So (a design decision of 2026-11-17):

- `tools/velopack/build-helpers.ps1` clones Velopack (MIT; its licence is `tools/velopack/LICENSE-velopack.txt`) at
  the tag `tools/velopack/source.json` names, checks the commit, applies `no-process-scan.patch`, builds `update`,
  `setup` and `stub` with a static C runtime, and puts them into a private copy of the `vpk` package at the same
  version. CI caches the result.
- The helpers that ship no longer import `QueryFullProcessImageNameW`; `dependency-review.json` lists what they do
  import, with reasons, so stock helpers can't slip back in unnoticed.
- With the scan gone, a running GrindPilot is no longer force-closed: the app exits itself before an update applies
  (Velopack's `--waitPid` on its own process), and the user closes it before an uninstall.
- **Taking a new Velopack version** (Dependabot proposes it): CI fails until `source.json` names it. Read the new
  version's source first: `force_stop_package` and every `OpenProcess` caller in `src/bins` and `src/lib-rust`. Update
  the patch if it no longer applies, `source.json` with the new tag, commit and date, and the crate notices (below).
- `vpk` is never installed on a maintainer's PC: CI fetches it.

## Third-party notices

`THIRD-PARTY-NOTICES.md` lists everything the installer and program folder carry beside GrindPilot, each with its
licence read from the package itself:

- **.NET packages and runtime packs:** `tools/notices/Write-Notices.ps1 -Publish <publish folder>` reads the publish
  folder's `GrindPilot.deps.json` and, for each package, its `.nuspec` (licence, copyright) and the licence and notice
  files inside it. A package that states MIT by expression only gets the MIT text with its own copyright line; one
  that states another licence by expression points to the standard text (`tools/notices/licenses/`, from SPDX's list).
- **The Rust crates in Velopack's helpers:** `tools/velopack/crate-notices.md`, written by
  `tools/notices/Write-CrateNotices.ps1 -VelopackSource <checkout>` from `cargo metadata` at the reviewed commit
  (normal and build dependencies of `velopack_bins`, never dev-dependencies). It is regenerated whenever `source.json`
  names a new commit; `Write-Notices.ps1` refuses a crate list made for another commit.
- The release workflow regenerates the file for exactly what each release ships, puts it in the package beside
  `LICENSE`, and attaches both to the release. CI regenerates it on every push and warns when the committed copy
  differs; refresh the committed copy in the session that changes a package.

## Releases

- **A tag makes a release:** `git tag v0.2.1 && git push origin v0.2.1`. Tag only after CI on that commit is green.
  `release.yml` builds, tests, packs, writes the notices, runs the forbidden-call check, then **installs and
  uninstalls on the runner before uploading**: the per-user install, the `HKCU` uninstall entry, a real start that
  must load the catalog and start with an empty account, and an uninstall that leaves `%LOCALAPPDATA%\GrindPilot-data\`
  alone. Then it publishes the release, and a read-only job downloads the package through the same GitHub source the
  app uses: with no token on a public repo, as every install reads it.
- **For a stable release after the first,** that job also installs the previous release, runs it twice in its
  window-less `--update-check` mode, and checks that the second start applied this version.
- **A tag with a hyphen** (`v0.2.1-rc.1`) becomes a pre-release, which the app's update check ignores.
- **The update repo:** the app reads the releases of the repo named by the `UpdateRepoUrl` build property
  (`Directory.Build.props`, default `https://github.com/coltonbearden/grindpilot`). The workflows set it to the repo
  they run in, so a fork's builds update from the fork's own releases. A private fork needs a read-only token, saved
  in Settings › Update token (Windows Credential Manager, `grindpilot-releases-token`); public releases need none.
- `main` can't be force-pushed or deleted (the `protect-main` ruleset). Tags aren't covered by it.
- **Installer:** unsigned. Windows SmartScreen may warn on first run; if Smart App Control is switched on, the app needs
  a trusted signature first.

### Updates on the player's PC

The app checks the update repo's releases when it starts and once a day while open, downloads in the background, and
applies the update at the next start, **never while the game is running** (Steam's `RunningAppID` or a process named
`cod` or `sp26-cod`). Velopack's own apply-at-start is switched off for that reason.

## Data

`%LOCALAPPDATA%\GrindPilot-data\`: `state.db` (the store), `backups\` (the newest 14 daily copies), `logs\` (14 days;
keys, outcomes, counts, never a value read from a screen or a token), `settings.json` (monitor and window place),
`test-library\` (a maintainer's own, above). The program itself is in `%LOCALAPPDATA%\GrindPilot\`, which an uninstall
deletes whole. Windows paths ignore case, so the data folder must never be spelled like the program folder.

**The one-time move.** Builds before 0.2.0 kept their data in `%LOCALAPPDATA%\mw4-grind\`. At start, before anything
opens a file, `DataFolderMove` (`src/Core/DataFolderMove.cs`) moves it, once, and only when `GrindPilot-data\`
doesn't exist yet: it copies into a temporary sibling, checks every file's SHA-256 against the original, renames the
copy into place with a `moved-from.txt` record, then renames the old folder to `mw4-grind.moved-<yyyyMMdd>`. A failed
copy or a hash that doesn't match removes the temporary copy, leaves the old folder exactly as it was, and that start
runs on the old folder; the next start tries again. It is skipped when `GRINDPILOT_DATA` is set. Its tests:
`tests/Core.Tests/DataFolderMoveTests.cs`.

`GrindPilot.Jobs check-store` rebuilds the current state from the observations on a copy of the store and says
whether it matches: `dotnet run --project src/Jobs -c Release -- check-store`.

## Secrets

gitleaks runs over the whole history in CI (the MIT tool, its newest release, checksum checked; gitleaks-action isn't
used because its license changed at v2). `.gitleaks.toml` adds one narrow allowance: dotted lower-case account key
names in the invented import map. Run `gitleaks git --staged` before every commit and stop on any finding. GitHub's
secret scanning and push protection are on for the repo.

## Held back, with the reason

| What | Held at | Why | Lift when |
|---|---|---|---|
| `xunit.v3`, `xunit.runner.visualstudio` | 3.x | Avalonia.Headless.XUnit 12.1.6 is built on xunit.v3 3.2.2 | Avalonia's headless package moves to 4.x |
| Velopack | 1.2.161 | Each version's helpers are rebuilt from source read for the process scan | Its source is read again (above) |
| The .NET SDK | not in Dependabot | `global.json` only sets a floor and rolls forward | never needed |
| Deleting `mw4-grind.moved-<date>` | not done | The move keeps the old folder until the new one has been in use for 14 days | a release 14 days or more after 0.2.0 |
