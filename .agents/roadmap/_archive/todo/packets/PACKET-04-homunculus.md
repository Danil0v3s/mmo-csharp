# PACKET-04-homunculus — Homunculus client→map packet bridge

> **Epic:** Packet bridge · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** FEATURE-homunculus (HomunculusService + Intif homun RPCs exist) · **Blocks:** none

## Problem

`Map.Server/Homunculus/HomunculusService.cs` implements the full homunculus surface
(`Call`, `CreateRequest`, `Food`, `Delete`, `Vaporize`, `Evolution`, `Mutate`, `SkillUp`,
`ChangeName`/`ChangeNameAck`, `Menu`, …) and `IIntifService` has `HomunculusCreate`,
`HomunculusRequest`, `HomunculusSave`, `HomunculusDelete`. But **no client→map homunculus
packet is wired**. A player cannot feed/delete the homunculus, level its skills, rename it, or
command it to move to the owner / to a position.

## Current state (C#)

- No handler exists for any homunculus packet.
- `Map.Server/Homunculus/IHomunculusService.cs` — `Food(master)`, `Delete(master)`,
  `Vaporize(master, flag)`, `SkillUp(master, skillId)`, `ChangeName(master, newName)`,
  `ChangeNameAck(master, ok)`, `Menu(master, choice)`, `Call(master)`, `CreateRequest(master, classId)`,
  `Evolution`, `Mutate`, plus skill-tree `CalcSkillTree` / `SkillTreeGetMax`.
- `Map.Server/Services/Intif/IIntifService.cs:91-94` — `HomunculusCreate(accountId, data)`,
  `HomunculusRequest(accountId, homunId)`, `HomunculusSave(data)`, `HomunculusDelete(homunId)`.

## rAthena reference (source of truth)

`rathena/src/map/clif.cpp` parse functions to port:

- `clif_parse_HomMenu` → `hom_menu` (`HomunculusService.Menu`). Menu byte: rAthena
  `enum e_hom_menu` — 0=status (`HOM_INFO`), 1=feed (`HOM_FEED`), 2=delete/intimacy
  (`HOM_DELETE`). Delete sends a confirm popup first.
- `clif_parse_HomMoveToMaster` (`CZ_REQUEST_MOVETOOWNER`) → `unit_walktobl` toward the master.
- `clif_parse_HomMoveTo` (`CZ_REQUEST_MOVENPC` / move-to-position) → `unit_walktoxy` for the homun.
- `clif_parse_HomAttack` → homunculus attack command → `unit_attack`.
- `clif_parse_ChangeHomunculusName` → `hom_change_name` (`ChangeName`).
- Homunculus skill-up: the normal skill-up uses the homun variant of the skill-level-up packet —
  confirm whether it is `CZ_UPGRADE_SKILLLEVEL` with a homun flag or a dedicated packet in the
  target PACKETVER (read `clif_packetdb.hpp`).

ZC responses: `ZC_PROPERTY_HOMUN` (homun status panel: hp/sp/exp/intimacy/hunger/skills),
`ZC_CHANGE_HOM_INFO` (incremental stat update), `ZC_FEED_MER_BTN` / feed result,
`ZC_HO_PAR_CHANGE` (param change), homun skill list `ZC_HOSKILLINFO_LIST`,
delete/vaporize ack. **Read `clif_packetdb.hpp` for ids.**

## Scope — every sub-system that must be touched

- [ ] **In packets** (`Core.Server/Packets/In/CZ/`):
  - [ ] `CZ_HOMUNCULUS_MENU` (`clif_parse_HomMenu`) — `<type>.W <command>.B` (feed/delete/info).
  - [ ] `CZ_REQUEST_MOVETOOWNER` (`clif_parse_HomMoveToMaster`) — `<id>.L`.
  - [ ] `CZ_REQUEST_MOVENPC` (`clif_parse_HomMoveTo`) — `<id>.L <x>.W <y>.W` (or packed pos).
  - [ ] `CZ_REQUEST_ACTNPC` / homun attack (`clif_parse_HomAttack`) — `<id>.L <target>.L <action>.B`.
  - [ ] `CZ_RENAME_MER` / homun rename (`clif_parse_ChangeHomunculusName`) — `<name>.24B`.
  - [ ] Homun skill-up packet — confirm id; drives `IHomunculusService.SkillUp`.
- [ ] **Out packets** (`Core.Server/Packets/Out/ZC/`): `ZC_PROPERTY_HOMUN`, `ZC_CHANGE_HOM_INFO`,
      `ZC_HOSKILLINFO_LIST`, feed-result, delete/vaporize ack, rename ack.
- [ ] **PacketHeader.cs** + **appsettings.packets.json** (rename + move are partially var/fixed).
- [ ] **Handlers** (`Map.Server/Handlers/Homunculus/`):
  - [ ] `HomMenuHandler` → dispatch on command byte: info→`ZC_PROPERTY_HOMUN`; feed→`Food` +
        feed-result; delete→confirm + `Delete`/`Vaporize` + `IIntifService.HomunculusDelete`/`Save`.
  - [ ] `HomMoveToMasterHandler` → `unit_walktobl`-equivalent on the homun entity (move toward master).
  - [ ] `HomMoveHandler` → walk the homun to x/y.
  - [ ] `HomAttackHandler` → homun attack target.
  - [ ] `HomRenameHandler` → `ChangeName` + `ChangeNameAck`.
  - [ ] `HomSkillUpHandler` → `IHomunculusService.SkillUp` + refresh `ZC_HOSKILLINFO_LIST`.
- [ ] No new char-side RPC — homun persistence RPCs exist.

## Done criteria

- Feed updates hunger/intimacy and refreshes the panel; delete shows the confirm flow and on
  confirm vaporizes/deletes per rAthena `hom_menu` (delete requires intimacy threshold).
- Move-to-owner walks the homun to its master; move-to-position walks it to the cell.
- Skill-up spends a homun skill point and updates the skill list; rejects if no points / max level.
- Rename is one-shot and rejects a second attempt (rAthena rename flag).
- No stub, no `// TODO`.

## Test plan

- Handler tests pinning: feed when full caps correctly; delete blocked below intimacy threshold;
  skill-up rejected at max level / zero points; rename rejected on second call.
- Manual: vaporize via menu, re-call, feed, move-to-owner, level a homun skill.

## Notes / gotchas

- The homunculus is a separate `Entity` owned by the player; movement/attack packets target the
  homun's entity id, not the player's. Resolve via the master→homun link.
- Delete (intimacy reset) vs Vaporize (rest) are different `Menu`/`Vaporize(flag)` paths — keep
  them distinct.
- Homun skill-up may share the player skill-up id with a flag; confirm before reusing the existing
  `UpgradeSkillHandler` — prefer a dedicated handler to avoid breaking the player path.
