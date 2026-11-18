# 5.5 Loadouts

What each of the user's custom loadouts holds, what it depends on, and whether it still works.

## What the game does

- **Create a Class** opens at player level 4 `[P-LOAD]`. Before that only default loadouts exist.
- **A loadout holds** an operator, a primary, a secondary, a melee weapon, a tactical, a lethal, a field upgrade,
  three perks and killstreaks `[S39]`. Operator and killstreaks are set per loadout `[P-LOAD]`.
- **Perks** `[P-LOAD]`: three of eighteen, six per slot. The perk screen shows them in rows `PERK 1` to `PERK 3`
  `[S65]`. Overkill, a Perk 1 choice, swaps the secondary for a second primary `[P-LOAD]`.
- **Pools at launch** `[P-LOAD]`: 9 tacticals, 11 lethals, 12 field upgrades, 17 killstreaks costing 4 to 12 kills.
  The killstreak screen shows each one's cost (`n KILLS`) and, for a locked one, its level (`UNLOCK AT LEVEL n`)
  `[S66]`.
- **Apply to all.** Perk and killstreak screens offer `APPLY TO ALL LOADOUTS` `[S65]`, `[S66]`.
- **Weapons in a loadout** show their class, name and progress figure (`PRESTIGE n /m`) `[S39]`.
- **Launchers and melee weapons** take appearance changes only, no Gunsmith `[P-LOAD]`.

**Not known:** how many custom loadouts there are at launch (the August beta had up to 12 `[P-BETA]`), whether
wildcards exist, and the full lists of perks, equipment, field upgrades and killstreaks. `[unknown]` The lists fill
in from the user's own screens.

## What the user sees

- **Every custom loadout** on one page: name, weapons with their builds, equipment, perks, field upgrade,
  killstreaks.
- **Health marks** on a loadout:
  - an item that is locked right now;
  - an item that the next Classic Prestige relocks (`prestiges.md`);
  - a weapon whose build uses an attachment that a Weapon Prestige would relock;
  - a build that an official patch note mentions (the patch watch, `07-ai-and-agents.md`).
- **What a loadout is for:** a goal the user sets, such as "AR headshots" or "lane holding". The session planner
  uses it.
- **Changes over time:** when a loadout was last seen, and what changed since the capture before.

## What the user does

- Captures the Create a Class screen with a loadout selected, one screenshot per loadout.
- Names a loadout's goal in the app.
- Asks for a loadout suggestion for a goal. The app answers from items the user has unlocked, and shows the
  reasons. The user builds it in the game; the app changes nothing in the game.

## Data and where it comes from

| Data | Source |
|---|---|
| Loadout structure, perk slots, pools | `[P-LOAD]`, `[S39]`, `[S65]`, `[S66]` |
| The user's loadouts' contents | The Create a Class screen `[S39]` |
| Names of perks, equipment, field upgrades, killstreaks and operators | A few from `[P-LOAD]`; the rest harvested from the user's selection screens (`04-capture-and-readers.md`, 4.2: equipment and perks, operators) and confirmed by the user |
| Lock levels | Lock lines on those screens `[S66]`, and the level screen `[S02]` |
| What each perk or item does | The description shown on its selection screen, kept as the user's own notes. No third-party descriptions |

## Tests

- **Golden cases:** the confirmed-values set (`04-capture-and-readers.md`, 4.9) for the Loadouts and Equipment and
  perks screens produces its confirmed values.
- **Closed lists:** a perk or item name outside the catalog is unread and becomes a candidate; it never appears in a
  loadout until confirmed.
- **Health marks:** each mark is produced by exactly the condition above and by nothing else.
- **Suggestions use only unlocked items.** A suggestion that would need a locked item is rejected by the test, not
  just flagged.
- **Overkill:** a loadout with Overkill accepts a primary in the secondary slot; one without it does not.
