# GrindPilot

An unofficial companion app for **Call of Duty: Modern Warfare 4** on Windows. It keeps track of the grind (camos,
weapon levels, challenges, prestiges, loadouts and blueprints) and reads it only from your own screens, on your own
PC.

> **Unofficial.** GrindPilot is not affiliated with, endorsed by, or sponsored by Activision. Call of Duty and Modern
> Warfare are trademarks of Activision Publishing, Inc.; their names are used here only to say what the app works
> with. GrindPilot contains no Activision logo, art or game files.

## What it does today

GrindPilot is early (the first of the build stages in [`docs/spec/10-build-stages.md`](docs/spec/10-build-stages.md)):

- **Your account:** a local store of what is known about your account, every value with how sure it is and where it
  came from. A new install starts empty; the readers that fill it arrive in the next stages.
- **Catalog:** a browser of the game's facts (weapons, camos, perks, rules) as Activision's official posts state
  them, each with its source, and with conflicts between posts kept as conflicts.
- **Inputs and status:** what is switched on, the store's health, and updates.
- **Updates:** it updates itself from this repo's releases, and never while the game is running.

## What it never does

- It never reads or writes the game's memory, files or process, never sends the game input, and never draws over it.
  A check in CI fails any build whose code, or whose dependencies, call anything that could open another program
  ([`docs/build.md`](docs/build.md), "The forbidden-call check").
- It sends nothing about you anywhere: no telemetry, no account, no sign-in. Its only network traffic is the update
  check against this repo's GitHub releases.
- Screenshots, recordings and anything read from them stay on your PC.

**No promise about your account.** Activision's rules on third-party tools are Activision's to write and apply, and
this project makes no claim that using GrindPilot, or any way of capturing the screen, is allowed or safe for an
account. Read Activision's [Call of Duty Security and Enforcement
Policy](https://support.activision.com/articles/call-of-duty-security-and-enforcement-policy) and decide for yourself.
Use GrindPilot at your own risk.

## Install

1. Download `GrindPilot-win-Setup.exe` from the [latest release](https://github.com/coltonbearden/grindpilot/releases/latest).
2. Run it. It installs for your Windows user only, with no administrator prompt, into
   `%LOCALAPPDATA%\GrindPilot\`, and adds a Start menu entry.
3. The installer isn't code-signed, so Windows SmartScreen may warn about an unknown publisher (More info › Run
   anyway). Each release lists its SHA-256 hashes, and each release's build log is public.

Your data lives in `%LOCALAPPDATA%\GrindPilot-data\` (the account store, its daily backups, logs and settings). An
uninstall (Windows Settings › Apps) removes the program and leaves your data; delete that folder yourself to remove it.

## Build from source

Windows 10 or 11 and the .NET 10 SDK:

```
dotnet build GrindPilot.slnx -c Release
dotnet test GrindPilot.slnx -c Release
```

How the build, tests, releases and Velopack's patched helpers work: [`docs/build.md`](docs/build.md). What the app is
meant to become: the spec, starting at [`docs/spec/00-index.md`](docs/spec/00-index.md). Rules for Claude Code sessions
in this repo: [`CLAUDE.md`](CLAUDE.md).

## License

MIT, see [`LICENSE`](LICENSE). The software GrindPilot ships with is listed, with each licence, in
[`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md). Security issues: [`SECURITY.md`](SECURITY.md).
