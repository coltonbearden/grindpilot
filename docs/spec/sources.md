# Sources

Every game fact in this spec carries one of three tags:

- **`[P-…]`** an official Activision post, listed below with its URL and date.
- **`[S…]`** an in-game screen from a maintainer's samples (below), cited by its tag and named by the screen it
  shows. No picture or file name is published.
- **`[unknown]`** nobody official says, and no confirmed screen shows it yet. The app treats it as unknown, never as
  a guess.

A fact that two official posts state differently is recorded as a **conflict** with both tags.

## Official posts

All were read on 2026-11-09 through Steam's public news API for app 1938090, the feed Activision itself publishes.
Nothing was read from callofduty.com, activision.com or any third-party site.

| Tag | Date | Title | URL |
|---|---|---|---|
| `P-PROG` | 2026-10-19 | Modern Warfare 4 Progression and Prestige | https://store.steampowered.com/news/app/1938090/view/671757096278033531 |
| `P-LOAD` | 2026-10-19 | Modern Warfare 4 Operators, Weapons, Equipment, and Multiplayer Loadout Intel | https://store.steampowered.com/news/app/1938090/view/671757096278033470 |
| `P-S1` | 2026-10-28 | Call of Duty: Modern Warfare 4 and Warzone - Season 1 | https://store.steampowered.com/news/app/1938090/view/677387230220656499 |
| `P-WZ` | 2026-11-02 | Call of Duty: Warzone Season 1: Everything You Need to Know | https://store.steampowered.com/news/app/1938090/view/717920261052041976 |
| `P-BP` | 2026-11-05 | Modern Warfare 4 Season 1 Battle Pass System, BlackCell, and New Store Bundles | https://store.steampowered.com/news/app/1938090/view/717920898634482445 |
| `P-LIVE` | 2026-10-23 | Call of Duty: Modern Warfare 4 is Live Worldwide | https://store.steampowered.com/news/app/1938090/view/677387230220654226 |
| `P-DMZ` | 2026-10-09 | DMZ Deep Dive, Part 2: How to Play, FOB Full Intel | https://steamstore-a.akamaihd.net/news/externalpost/steam_community_announcements/1846018067933883 |
| `P-TRUST` | 2026-09-29 | Player Trust (RICOCHET Anti-Cheat, TPM 2.0 and Secure Boot) | https://steamstore-a.akamaihd.net/news/externalpost/steam_community_announcements/1844751498234560 |
| `P-RICO` | 2026-02-04 | RICOCHET Anti-Cheat, Season 02 (a Black Ops 7-era post on the same app) | https://steamstore-a.akamaihd.net/news/externalpost/steam_community_announcements/1823825466495150 |
| `P-LEGACY` | 2026-09-24 | Operator Skin Toggle and legacy weapons in Warzone | https://steamstore-a.akamaihd.net/news/externalpost/steam_community_announcements/1844751498222077 |
| `P-BETA` | 2026-08-27 | Beta intel (Gunny, loadouts). Describes the August beta and may not match launch | https://steamstore-a.akamaihd.net/news/externalpost/steam_community_announcements/1842212951300932 |

**What the official feed does not carry:**
- **No patch notes.** From launch (2026-10-23) to 2026-11-09 Steam's feed has no patch-notes or hotfix post. `P-WZ`
  points readers to "the Call of Duty Blog" for them, which is callofduty.com and off limits here.
- **Nothing on third-party tools.** No post says anything for or against overlays, screen capture, screen reading
  or companion apps (`02-rules-and-limits.md`).
- **No stats API.** No post mentions a public stats API, an account-stats page or an official companion app.

## In-game screens

In-game screens are cited as S-tags (`S01`, `S02`, …). They are 1920×1080 English screenshots a maintainer took with
the NVIDIA App on 2026-11-10. Each is named by the screen it shows:
in `04-capture-and-readers.md`, 4.2, for the spec, and in `catalog/sources.json`'s `"screens"` for the screens a
maintainer confirmed by eye that the catalog cites. No picture or file name is published, and none is in this repo:
the maintainer keeps the samples and their confirmed values outside the repo, in the data folder's `test-library\`
(`%LOCALAPPDATA%\GrindPilot-data\test-library\`; `04-capture-and-readers.md`, 4.8). The tests in the repo use
invented data in `tests/fixtures/`.

Only the maintainer who took them has viewed them. They were read by local text reading, and Claude saw only numbers
and allowlisted game words.

- **Confirmed by eye on 2026-11-10:** the readers' values on these samples were checked against the pictures, in the
  way and with the strength `04-capture-and-readers.md`, 4.9, describes: S02, S03, S04, S05, S15, S19, S22, S39,
  S44, S46, S48, S50, S51, S65, S66, S71, S73 and S75. Stage 2 re-checks each value on the review list before using
  it as a test.
- **Read, not confirmed:** a fact tagged with any other sample was read by the two readers and not checked by eye. A
  build stage confirms it before relying on it.

## Not sources

- CODMunity, wzstats.gg, sym.gg, Fandom and the public tracker repos looked at while the spec was written. None of
  their data is used.
