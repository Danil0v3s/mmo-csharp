# COMBAT-67 — Warp Portal ground-unit placement + deferred consume + pc_memo set-path

> **Epic:** combat · **Status:** ✅ Done (2026-06-03) · **Size:** L · **Player-visible:** yes
> **Depends on:** COMBAT-48 (AL_WARP destination resolution + CZ_SELECT_WARPPOINT) · **Blocks:** none
> **Filed by:** COMBAT-48 — the rAthena-faithful parts it intentionally simplified.

## Problem

COMBAT-48 wired AL_WARP destination resolution + the CZ_SELECT_WARPPOINT handler, but
took three deliberate simplifications away from rAthena that are each a real,
player-visible divergence:

1. **Direct warp instead of a portal.** rAthena AL_WARP does NOT teleport the caster — it
   places a **Warp Portal ground unit** (`skill_unitsetting`) at the cast cell whose exit is
   the chosen destination; anyone who steps on the portal is warped. COMBAT-48 instead calls
   `pc_setpos` on the caster directly. So today casting Warp Portal teleports the caster (like
   Teleport) rather than creating a shareable portal.
2. **No deferred-consume / cancel-refund.** rAthena marks the cast `SKILL_NOCONSUME_REQ` and
   consumes SP + the Blue Gemstone only at selection (`skill_castend_map` →
   `skill_consume_requirement(...,2)`); picking **"cancel"** refunds (nothing was consumed).
   The C# port consumes SP at cast (`StartCast`/`StartCastAt`), so cancelling the chooser still
   costs SP, and the gemstone is never consumed at selection.
3. **pc_memo set-path is not parity + unreachable from the client.** `PlayerPositionHelpers.Memo`
   picks the first *empty* slot instead of rAthena's insert-at-slot-0 `memmove` shift, and skips
   the `MF_NOMEMO`/`MF_NOWARPTO`/instance/AL_WARP-level gates. There is also no
   `CZ_REMEMBER_WARPPOINT` (0x011d) handler, so a normal client can't memo a point at all
   (only the GM `@memo` path / tests set memo slots today).

## Current state (C#)

- `Map.Server/Skills/SkillCastEndService.cs:CastEndMap` `case AL_WARP` — resolves the memo
  destination + nowarp/nowarpto gate, then `IPcSetposService.Setpos(caster, …)` (direct warp).
  Carries a NOTE pointing here.
- `Map.Server/Skills/Behaviors/Acolyte/WarpPortal.cs` — `CastendPos2` sends `ZC_WARPLIST` and
  marks no-consume; it does NOT place a portal unit (no Warp Portal `ISkillUnitTickHandler`).
- `Map.Server/Skills/Units/Handlers/` — no AL_WARP / Warp Portal unit handler.
- `Map.Server/Movement/PlayerPositionHelpers.cs:Memo` — first-empty-slot writer; no gates.
- No `CZ_REMEMBER_WARPPOINT` packet/handler under `Core.Server/Packets/In/CZ` + `Map.Server/Handlers`.

## rAthena reference (source of truth)

- `skill.cpp` `skill_castend_map` `case AL_WARP` (records dest on `group->val2/val3`, creates the
  unit via `skill_unitsetting`); the portal-step warp is in `skill_unit_onplace` /
  `skill_unitsetting` `UNT_WARP_ACTIVE`/`UNT_WARP_WAITING` handling.
- `skill.cpp` `skill_consume_requirement(sd, menuskill_id, lv, 2)` + the `SKILL_NOCONSUME_REQ`
  flag on the AL_WARP cast; `"cancel"` path in `skill_castend_map`.
- `pc.cpp` `pc_memo` (mapflag gates + `memmove` insert-at-0); `clif.cpp` `clif_parse_RequestMemo`
  (`CZ_REMEMBER_WARPPOINT` 0x011d) → `pc_memo(sd,-1)`.
- Monolithic-switch caveat: canonical source is `skill.cpp`/`pc.cpp` switch arms (no
  `rathena-fork/src/map/skills/...` split files in this checkout).

## Scope — every sub-system that must be touched

- [x] Warp Portal `ISkillUnitTickHandler` (`WarpPortalUnit`): single-cell placeable unit storing
      the exit on the group (`SkillUnitGroup.DestMap`/`DestX`/`DestY`); its `OnPlace` warps any
      player stepper (incl. the caster, not mobs) via injected `IPcSetposService`. Placed from the
      AL_WARP selection path (`CastEndMap`) instead of the direct `Setpos`. `OnPlace` gained a
      `SkillUnitGroup group` param so the handler can read the per-group exit. Duration
      `5000+5000*lv`, radius 0, registered in DI.
- [ ] Defer SP/gemstone consume to selection + cancel-refund. ➡️ Moved to COMBAT-86 — a
      cast-pipeline (`StartCast` no-consume + `skill_consume_requirement` at selection) change,
      separable from the portal/memo work.
- [x] `pc_memo` parity in `PlayerPositionHelpers.Memo` (rAthena pc.cpp:7098): `MF_NOMEMO`/
      `MF_NOWARPTO` gates + the AL_WARP level gate (`skill<2 || skill-2<pos`) + dedup +
      insert-at-slot-0 `memmove` shift. (Instance gate is a no-op until instances port — noted.)
- [x] `CZ_REMEMBER_WARPPOINT` (0x011d) packet def + `RememberWarpPointHandler` `[PacketHandler]`
      → `Memo(pc, -1)`.

## Done criteria

- Casting Warp Portal creates a portal entity that warps a player who steps on it to the chosen
  memo destination (not the caster directly) ✅.
- A client `CZ_REMEMBER_WARPPOINT` on a legal map memos the current cell at slot 0 (shifting the
  list); a `nomemo` map refuses ✅.
- Cancelling the chooser costs no SP / gemstone; a successful pick consumes both once ➡️ COMBAT-86.
- No `// TODO` / `data-pending` / log-only no-op in the touched files ✅.

## Test plan

- `Combat67WarpPortalUnitTests`: place a Warp Portal unit with a destination; a stepper is
  warped there; the caster is not moved by the cast itself.
- `Combat67MemoTests`: `CZ_REMEMBER_WARPPOINT` memos slot 0 with shift; `nomemo` refuses;
  cancel refunds SP.

## Notes / gotchas

- COMBAT-48 already persists memo points (load/save via `PlayerStateService` ↔ `memo` table) and
  resolves the destination + nowarp/nowarpto gates — reuse those; this ticket is the portal
  entity + consume timing + the memo-SET half.

## History

- 2026-06-03 · Shipped the Warp Portal ground unit + the pc_memo set-path. AL_WARP now places a
  `WarpPortalUnit` at the cast cell (destination stamped on `SkillUnitGroup.DestMap`/`DestX`/
  `DestY`) instead of warping the caster directly; the unit's `OnPlace` (now passed the group)
  warps any player stepper via `IPcSetposService`. Rewrote `PlayerPositionHelpers.Memo` to
  rAthena pc_memo (NoMemo/NoWarpTo + AL_WARP level gates + dedup + insert-at-0 shift) and added
  `CZ_REMEMBER_WARPPOINT` (0x011d) + `RememberWarpPointHandler` → `Memo(pc,-1)`. Updated the
  COMBAT-48 success test to the portal model + the COMBAT-55 OnPlace call to the new signature.
  Combat67WarpPortalMemoTests (8); skills+combat+movement suite 3196 green, full suite 4083 pass
  (1 fail = pre-existing INFRA-11 replay gate), Core.Server.Tests 87 green. Filed COMBAT-86
  (deferred requirement-consume + cancel-refund — a cast-pipeline change).
