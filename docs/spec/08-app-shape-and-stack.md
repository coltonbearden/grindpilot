# 8. App shape and stack

Design decision (2026-11-09): GrindPilot is one Windows 11 desktop app, not a set of scripts. The stack is judged
on the goals 8.3 uses: reliable and maintainable, polished, small beside a game, testable with nobody watching, and
straightforward to install.

Facts below were taken from current documentation on 2026-11-09. Each carries its source: `[ctx7: …]` is the
Context7 library it came from, `[docs: …]` the vendor page. The choice itself (8.3) is a design decision.

## 8.1 What the app has to do

- Show a dashboard in a normal window on a second monitor, sit in the tray, and optionally start at sign-in.
- Run beside a demanding game without costing it frames.
- Watch a folder for screenshots, read frames from recordings, and copy a monitor's picture from Windows.
- Use Windows' own text reader and a second, independent one.
- Keep a local SQLite store.
- Run two scheduled jobs.
- Install for one user without admin rights, and update itself from this repo's public releases.
- Be built and kept working mostly by Claude Code sessions, so its tests must be strong and run without a person.

## 8.2 The options, against current docs

| | .NET + Avalonia | .NET + WinUI 3 | .NET + WPF | Rust + Tauri 2 | Electron | Python + PySide6 |
|---|---|---|---|---|---|---|
| **Current release** | Avalonia 12.1.6; needs .NET 8 or later `[docs: github.com/AvaloniaUI/Avalonia releases]`, `[ctx7: /avaloniaui/avalonia-docs]` | Windows App SDK 2.6.5 of 2026-11-02; the 2.0 line is serviced to 2027-04-29 `[docs: learn.microsoft.com/windows/apps/windows-app-sdk/release-channels]` | Ships with .NET 10; Fluent styling "still in progress" `[docs: learn.microsoft.com/dotnet/desktop/wpf/whats-new/net100]` | 2.12.5; a 3.0 alpha exists `[docs: github.com/tauri-apps/tauri releases]` | 45.2.3; latest three majors supported, 45 ends about 2027-04 `[docs: releases.electronjs.org/schedule]` | 6.12.0 `[docs: pypi.org/project/PySide6]` |
| **Windows text reader and screen copy** | Through .NET's Windows target (`net10.0-windows10.0.26100.0`) `[docs: learn.microsoft.com/windows/apps/desktop/modernize/desktop-to-uwp-enhance]` | The same | The same | Through Microsoft's `windows` crate, which has an OCR sample `[ctx7: /microsoft/windows-rs]` | Needs a custom native add-on; no guidance found `[ctx7: /websites/electronjs]` | Through PyWinRT, a community project `[ctx7: /pywinrt/pywinrt]` |
| **Tray, monitors** | Tray icon and screens built in `[ctx7: /avaloniaui/avalonia-docs]` | No tray in Microsoft's docs; a community library `[docs: github.com/HavenDV/H.NotifyIcon]` | Borrowed from WinForms or the same community library | Tray in core; single-instance and autostart as plugins `[ctx7: /tauri-apps/tauri-docs]` | All built in `[ctx7: /websites/electronjs]` | Built in `[ctx7: /websites/doc_qt_io_qtforpython-6]` |
| **Process model beside a game** | One native process | One native process | One native process | A core process plus WebView2 processes `[docs: tauri.app/concept/process-model]` | Chromium's processes, one renderer per window `[ctx7: /websites/electronjs]` | One process with a Python runtime |
| **Tests without a person** | A headless test platform for xUnit and NUnit with simulated input and frame capture `[ctx7: /avaloniaui/avalonia-docs]` | A unit-test app template; UI tests through Appium, since WinAppDriver "is no longer under active development" `[ctx7: /websites/learn_microsoft_en-us_windows_apps]` | FlaUI over Windows UI Automation `[docs: github.com/FlaUI/FlaUI]` | A mock runtime, and WebDriver for end-to-end `[ctx7: /tauri-apps/tauri-docs]` | Playwright or WebdriverIO `[ctx7: /websites/electronjs]` | pytest-qt `[docs: pytest-qt.readthedocs.io]` |
| **Install and update** | Velopack | MSIX with an App Installer file on a web server, or Velopack if unpackaged `[ctx7: /websites/learn_microsoft_en-us_windows_apps]` | Velopack | Its own NSIS installer (per user, no admin) and updater plugin `[docs: tauri.app/distribute/windows-installer]` | electron-updater; a private GitHub source is "only for very special cases" `[ctx7: /websites/electron_build]` | Nothing in Qt's docs; Velopack has a Python library |
| **Second reader (ONNX Runtime)** | Official C# package `[docs: onnxruntime.ai/docs/install]` | The same | The same | No official binding; a community crate `[docs: onnxruntime.ai/docs/get-started/community-projects.html]` | Official Node package | Official Python package |
| **Local store** | Microsoft.Data.Sqlite, first party `[docs: learn.microsoft.com/dotnet/standard/data/sqlite]` | The same | The same | An official plugin | Node's built-in module is a release candidate `[docs: nodejs.org/api/sqlite.html]` | The standard library |

**Not found in any of the docs:** a published idle memory or CPU figure for any option. Footprint is measured on a
maintainer's PC beside the game, not claimed (`04-capture-and-readers.md`, 4.12).

## 8.3 The choice

Design decision: **C# on .NET 10 with Avalonia for the window, Velopack for install and update.**

Why, by the goals at the top of this page:

- **Reliable and maintainable.** .NET 10 is the current long-term release, supported to 2028-11-14
  `[docs: dotnet.microsoft.com/platform/support/policy/dotnet-core]`. One language covers the window, the readers,
  the store and the jobs.
- **The two Windows features the app lives on are first-class in .NET.** Windows' text reader and screen copy are
  reached through .NET's own Windows target, with no extra layer to keep working.
- **Tests that run with nobody watching.** Avalonia's headless platform lets a build session and CI drive the
  window and compare rendered frames with no desktop. Of the three .NET options it is the only one documented to do
  that. This matters most for an app agents will maintain.
- **Tray and monitor placement are built in,** where WinUI 3 needs a community library for the tray.
- **Polished.** Avalonia documents a Fluent theme (`FluentTheme`, from its `Avalonia.Themes.Fluent` package) with
  light and dark variants that can follow the system `[ctx7: /avaloniaui/avalonia-docs]`. How close it looks to a
  native Windows 11 app is judged by eye in stage 1, not a documented fact.
- **Small beside a game.** One native process, no browser engine.
- **Straightforward installation.** Velopack's installer is one click, per user, into `%LocalAppData%`, and "does
  not require elevation" `[ctx7: /velopack/velopack.docs]`.

**What was given up:**
- **WinUI 3** has the most native look and is Microsoft's own. It lost on tests without a desktop, the missing tray,
  a 12-month servicing window per release line, and a packaging path (MSIX) whose update file needs a web server.
  It is the fallback if Avalonia disappoints on polish.
- **WPF** is the most settled, but its Windows 11 styling is unfinished.
- **Tauri and Electron** bring a browser engine's processes to a machine that is running a game, and put a layer
  between the app and Windows' text reader.
- **Python with PySide6** has the weakest install and update story, which ranks high among the goals above.

**What documentation didn't settle** went to `11-open-questions.md`: Avalonia's support policy, which wasn't found
when the stack was chosen and was settled in stage 1 (11.4).

## 8.4 The shape

One repo, one solution:

| Project | Holds | Depends on Windows? |
|---|---|---|
| `Core` | The account store, the catalog, field rules, the planner, the build planner | No. Pure .NET, so every rule is testable anywhere |
| `Readers` | The pipeline, screen profiles, the two reader engines behind one interface, the review list | Only through the doors below |
| `Windows` | The doors, one small class each: the screenshot folder watcher, Windows' text reader, the monitor copy, the game-running check, Credential Manager | Yes |
| `App` | The Avalonia window and tray | Through `Windows` |
| `Jobs` | A small console program the scheduled tasks run: self-test and patch watch | Through `Windows` |
| `Tests.*` | One per project, plus the local suite that needs the test library | The local suite only |

Data that isn't code: `catalog\` (game things, each with its source), `profiles\` (screen profiles), and
`tests\fixtures\` (invented test data). A maintainer's own confirmed values never enter the repo: they stay in the
data folder's `test-library\` (`docs/build.md`, "The test library").

**The window's pages:** Next session; Camos; Weapons; Challenges; Prestige; Loadouts; Builds; Review list; Inputs
and status; Settings. "Inputs and status" always shows which inputs are on, the last capture read, both jobs'
last results and the app's version.

**It opens on the monitor the user picks,** remembered between runs, and never on the monitor the game is on unless
the user drags it there.

## 8.5 The two readers

- **Windows' text reader** through .NET's Windows target. It returns words with boxes and no confidence
  `[docs: learn.microsoft.com/uwp/api/windows.media.ocr.ocrword]`. It reads in English (`en-US`), the only language
  sampled (`11-open-questions.md`, 11.4).
- **A PP-OCR recognition model on ONNX Runtime,** CPU only, through Microsoft's official C# package. Because the
  app reads fixed regions (`04-capture-and-readers.md`, 4.4), it needs only the recognition model, not the text
  detector: a region is resized to the model's input height, run, and decoded. The trial used the models that ship
  inside RapidOCR 3.10.0 `[ctx7: /websites/rapidai_github_io_rapidocrdocs]`.
- **Before the model files are shipped,** the stage that adds them confirms their license from the publisher and
  records it. `[unknown]` until then.
- **ONNX Runtime's telemetry is switched off before the first session is created,** and a test proves the switch
  is called.
- **GPU is not used.** The game needs it, and ONNX Runtime's DirectML path is in "sustained engineering"
  `[docs: onnxruntime.ai/docs/execution-providers/DirectML-ExecutionProvider.html]`.

## 8.6 Install and update

- **Install:** one `Setup.exe` built by Velopack, per user, no admin, into `%LOCALAPPDATA%\GrindPilot\` (pack id
  `GrindPilot`). The account store, its backups, logs, settings and the test library live in a separate folder,
  `%LOCALAPPDATA%\GrindPilot-data\`, because an update replaces the program's `current` folder and an uninstall
  deletes the whole `%LocalAppData%\{packId}` folder
  `[ctx7: /velopack/velopack.docs, "Uninstalling", "Preserving Files"]`. Windows paths ignore case, so the data
  folder is never spelled like the program folder. Design decision (2026-11-17): the program and the data each get a
  folder of their own (`11-open-questions.md`, 11.1 #9). Data from builds before 0.2.0, kept under an older folder
  name, is moved once at start (`docs/build.md`, "Data").
- **Update:** the app checks the update repo's GitHub Releases when it starts, and once a day while open. It
  downloads in the background and applies the update the next time it starts. **It never updates while the game is
  running** (`09-optional-integrations.md`, 9.3). Velopack's own apply-at-start is switched off
  (`SetAutoApplyOnStartup(false)`), because it would run before that check. A pre-release (a tag with a hyphen) is
  ignored.
- **The update repo** is the one the `UpdateRepoUrl` build property names (`docs/build.md`, "Releases"). The
  workflows set it to the repo they run in, so a fork's builds update from the fork's own releases. Public releases
  need no token. Velopack's GitHub source takes an access token, "required for private repositories"
  `[ctx7: /velopack/velopack.docs]`, so only a private fork needs one: a read-only token saved in Settings › Update
  token, which the app keeps in Windows Credential Manager as `grindpilot-releases-token`.
- **Velopack's helpers are built from its source with one step removed.** Velopack 1.2.161's `Setup.exe` and
  `Update.exe` open every running process on each install, update and uninstall, to find copies of the app
  (`src/bins/src/shared/util_windows.rs`, lines 46-60), which would reach the game. Design decision (2026-11-17): CI
  builds them from Velopack's MIT source with that scan removed and packs with them (`docs/build.md`, "Velopack's
  helpers"). A running GrindPilot is then not force-closed: the app exits itself before an update applies, and the
  user closes it before an uninstall.
- **Signing:** none at first. The installer is unsigned, so Windows SmartScreen may warn on the first download or
  run. Vendor docs disagree about what removes that warning; Microsoft says reputation "accumulates over time"
  `[docs: learn.microsoft.com/windows/apps/package-and-deploy/smartscreen-reputation]`. If Smart App Control is
  switched on, the app needs a trusted signature first.
- **Who keeps it current:** the app's own updater. The "Inputs and status" page shows the installed version and the
  newest release, and S1's status line carries both, so a stuck update shows (`07-ai-and-agents.md`, 7.4).
- **Start at sign-in** is a setting, off by default.

## 8.7 Building and testing

The details are in `docs/build.md`. In short:

- **CI:** GitHub Actions on a Windows runner, on every push: a locked restore, the build, the unit tests, the
  headless window tests, the forbidden-call check, the telemetry-off test once a reader library exists (stage 2),
  gitleaks over the whole history, and the third-party notices, regenerated with a warning when the committed copy
  differs. Velopack's patched helpers are built from source and cached.
- **Releases:** a tag on a commit whose CI is green builds the Velopack package, runs the forbidden-call check on
  it, installs and uninstalls it on the runner, and publishes it to the repo's Releases. A read-only job then
  downloads it through the same GitHub source the app uses, with no token. A tag with a hyphen becomes a
  pre-release, which the app's update check ignores.
- **The local suite** runs only on a maintainer's PC, against the test library: CI builds it but doesn't run it. It
  runs before any release, and nightly as S1 where that job is on.
- **Proof for a stage** is always both: CI green, and the local suite showing no case that was right and is now
  wrong.
- **Dependencies:** restored from lock files so a build repeats. Dependabot proposes updates
  (`07-ai-and-agents.md`, B4): it reads a `dependabot.yml`, watches the ecosystems named there, which include
  `nuget`, and opens pull requests on a schedule `[ctx7: /github/docs]`. Merges are made locally, never on GitHub
  (`docs/build.md`, "Build and test"). Nothing is held at an old version without a written reason in the repo.

## 8.8 Tools a contributor needs

- **The .NET 10 SDK** (and git), on Windows 10 or 11. Nothing else is installed to build, test or pack: `vpk` and
  Velopack's patched helpers are fetched and built by CI, never installed on a contributor's PC (`docs/build.md`,
  "Velopack's helpers").
- **Before each commit,** `docs/build.md` also asks for a `gitleaks git --staged` run; CI runs gitleaks over the
  whole history either way.
- **A tool a later stage needs** (for example, to pull frames from recordings in stage 9) is named by that stage,
  kept in the git-ignored `.scratch\`, and removed at the end of the session (`CLAUDE.md`, "Working here").
