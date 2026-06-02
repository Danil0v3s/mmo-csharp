# COMBAT-48 — AL_WARP destination resolution + CZ_SELECT_WARPPOINT wiring

> **Epic:** Combat parity · **Status:** ✅ Done (2026-06-02) · **Size:** M · **Player-visible:** yes
> **Depends on:** COMBAT-26 (CastEndMap AL_TELEPORT + the warp-service seam)
> **Blocks:** none
> **Filed by:** COMBAT-26 — the AL_WARP + packet-flow parts it deferred.

## Problem

COMBAT-26 implemented `CastEndMap` for AL_TELEPORT (Random / SavePoint + noteleport gate).
Two pieces remain so warps are reachable from the live client and Warp Portal works:

1. **AL_WARP destination resolution.** Warp Portal lets the caster pick one of their memo'd
   map destinations; `CastEndMap(AL_WARP, mapName)` must resolve that name to the memo'd
   coordinates and `pc_setpos` there (honoring `nowarp`/`nowarpto`).
2. **CZ_SELECT_WARPPOINT handler.** The Teleport/Warp chooser (`ZC_WARPLIST`) is sent, but
   no client→server handler routes the player's pick into `CastEndMap`. Today
   `CastEndMap` has no packet caller, so the warp flow only works via direct/test invocation.

## Current state (C#)

- `Map.Server/Skills/SkillCastEndService.cs:CastEndMap` — AL_TELEPORT done (COMBAT-26);
  the `default:` arm logs + returns false for AL_WARP.
- `Map.Server/Movement/IPlayerPositionHelpers.cs:Memo` — memo slot setter exists; no
  per-PC memo-destination store / accessor surfaced for the warp resolution.
- No `CZ_SELECT_WARPPOINT` packet handler under `Map.Server/Handlers`.

## rAthena reference (source of truth)

- `skill.cpp` `skill_castend_map` `case AL_WARP`; `clif.cpp` `clif_parse_SelectWarpPoint`;
  `pc.cpp` `pc_memo` + `pc_setpos` against `sd->status.memo_point[]`.

## Scope — every sub-system that must be touched

- [x] Persisted memo-point store (the 3 memo'd map+coords per PC) + accessor.
      `MapSessionData.MemoPoints` loaded from the `memo` table in
      `PlayerStateService.LoadAsync`, hydrated onto `PlayerEntity.MemoPoints` in
      `NotifyActorInitHandler`, upserted back in `PlayerStateService.SaveAsync`.
- [x] `CastEndMap(AL_WARP, mapName)`: resolve the memo'd coords for `mapName` among the
      level-gated [save_point, memo[0..2]] list (lv capped 4), gate on `nowarp`(source)/
      `nowarpto`(dest) via new `MapFlag.NoWarp`/`NoWarpTo`, `pc_setpos` (or savepoint for the
      "SavePoint" sentinel). ➡️ Real Warp Portal **ground-unit placement** (rAthena places a
      portal, not a direct caster warp) moved to **COMBAT-67**.
- [x] `CZ_SELECT_WARPPOINT` (0x011b) packet def + `SelectWarpPointHandler` ([PacketHandler]) →
      calls `CastEndMap` with the picked map name. ➡️ SP consume + after-cast delay are applied
      by the normal cast flow (StartCast) in this port; rAthena's deferred consume
      (`SKILL_NOCONSUME_REQ`) + **cancel-refund** moved to **COMBAT-67**.

## Done criteria

- A player who memo'd prontera and casts Warp Portal → selects prontera → warps to the memo'd
  coords. ✅ (direct `pc_setpos`; the shareable-portal form ➡️ COMBAT-67)
- The Teleport/Warp chooser pick from the live client routes into `CastEndMap`. ✅

## History

- 2026-06-02 — Implemented AL_WARP destination resolution + the chooser-answer packet.
  Added `CZ_SELECT_WARPPOINT` (0x011b) + `SelectWarpPointHandler`; `SkillCastEndService.CastEndMap`
  AL_WARP branch (level-gated memo/savepoint resolution, "cancel" abort, `MapFlag.NoWarp`/
  `NoWarpTo` gates, `IPcSetposService.Setpos`); persisted memo points end-to-end via
  `PlayerStateService` ↔ `memo` table (load → `MapSessionData.MemoPoints` → `PlayerEntity.MemoPoints`
  → upsert). Tests: `Combat48WarpTests` (8, green). Map.Server 0 errors; suite 3950 pass (1 fail =
  pre-existing INFRA-11 replay gate). Filed COMBAT-67 (real Warp Portal ground unit + deferred
  consume/cancel-refund + pc_memo set-path & `CZ_REMEMBER_WARPPOINT`).

## Test plan

- `Combat48WarpTests`: memo a destination, CastEndMap(AL_WARP, that map) → setpos to memo
  coords; nowarp gate refuses; the CZ_SELECT_WARPPOINT handler dispatches to CastEndMap.

## Notes / gotchas

- Warp Portal actually places a ground unit at the chosen exit; the destination here is the
  portal's exit coords. Coordinate with the WarpPortal plugin (`Acolyte/WarpPortal.cs`).
