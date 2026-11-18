# GrindPilot: rules for Claude Code sessions in this repo

GrindPilot is an unofficial companion app for Call of Duty: Modern Warfare 4 on Windows: camos, weapon levels,
challenges, prestiges, loadouts and blueprints, read only from the player's own screens. The spec is under
`docs/spec/`, and `docs/spec/00-index.md` is its first page; build sessions follow it and its stages in
`docs/spec/10-build-stages.md`, and don't re-decide what it settles. How to build, test and release, the
forbidden-call check and Velopack's patched helpers are in `docs/build.md`; read it before changing code.

A maintainer may keep their own instructions in a git-ignored `CLAUDE.local.md` beside this file (`docs/build.md`,
"Local-only files"). Lines marked `[Legal]` here and in the spec are fixed: never cross one, and ask the maintainer
when one no longer fits.

## Limits

- `[Legal]` Never read or write the game's memory, files or process, never automate input to it, and never inject an
  overlay into it.
- What the app may read: screenshots the player took, read as each file is saved; their own recordings, read after the
  game has closed; and the screen while they play (02-rules-and-limits.md, 2.2). Activision's position on all three is
  unverified, so never call any capture method safe or ban-safe, in the spec, the app, the README, a release or a
  session.
- `[Legal]` Only official sources for patch notes and game data. No third-party stat scrapers that work against
  Activision's terms.
- `[Legal]` Follow every site's robots.txt and terms when you research, and skip any site whose terms forbid automated
  access. This project does not read callofduty.com, activision.com, CODMunity, wzstats.gg, sym.gg, Fandom or game
  wikis. Activision's official posts are read from Steam's news feed.
- `[Legal]` Copy no code from a repo that has no open-source license, and put no game art and no third-party-compiled
  game data into this repo. Catalog entries keep facts (names, levels, counts) with their source, never copied
  passages of Activision's posts.
- `[Legal]` Call of Duty and Modern Warfare are Activision's trademarks. GrindPilot is unofficial and not affiliated
  with or endorsed by Activision; use the game's names only to say what the app works with, and no Activision logo or
  art.

## Images and recordings

- Claude never opens an image or a video frame here: not in the main session, not in a subagent, not as a pasted file.
  `.claude/settings.json` denies the Read tool on image and video files. Captures can hold other players' names.
- That rule can't see a shell command that reads a file's bytes. Don't use one to bring a picture into the session
  (no base64, no upload). If someone attaches an image to a message anyway, say plainly that it reached the session,
  repeat no name from it, don't use it as a sample, and ask for the screen in words instead.
- Captures are read on the player's own PC by local text reading. A session prints only numbers and game labels: a
  word that is not on the allowlist of game terms prints as `▢` with a number. Raw text read from a capture is never
  printed and never committed.
- Ask before any image or frame goes to an outside service, including an AI API.

## What never goes in this repo

Images, recordings, player names (the maintainer's own included), anyone's account values, raw text read from a
capture, secrets, and personal setup (machine names, user folders, home-lab or note-taking details). A secret is named
by its item name only, never shown in output, a command line or a transcript. Test data is invented
(`tests/fixtures/`); a maintainer's real confirmed values live outside the repo, in the data folder's `test-library\`.

## Working here

- Read `docs/build.md` before changing code, and keep its forbidden-call check, gitleaks and the tests green.
- Commits use the committer's GitHub noreply address, and merges are made locally (`docs/build.md`).
- Anything installed for a session stays in the git-ignored `.scratch\` and is removed at the end.
