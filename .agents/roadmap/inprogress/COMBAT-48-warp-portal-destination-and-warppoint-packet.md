# COMBAT-48 — AL_WARP destination resolution + CZ_SELECT_WARPPOINT wiring

> **Epic:** Combat parity · **Status:** 🚧 In progress · **Size:** M · **Player-visible:** yes
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

- [ ] Persisted memo-point store (the 3 memo'd map+coords per PC) + accessor.
- [ ] `CastEndMap(AL_WARP, mapName)`: resolve the memo'd coords for `mapName`, gate on
      `nowarp`/`nowarpto`, `pc_setpos`.
- [ ] `CZ_SELECT_WARPPOINT` packet def + handler ([PacketHandler]) → calls `CastEndMap`
      with the picked map name; consume SP + after-cast delay as the normal cast-end path.

## Done criteria

- A player who memo'd prontera and casts Warp Portal → selects prontera → a portal that
  warps to the memo'd coords.
- The Teleport chooser pick from the live client routes into `CastEndMap`.

## Test plan

- `Combat48WarpTests`: memo a destination, CastEndMap(AL_WARP, that map) → setpos to memo
  coords; nowarp gate refuses; the CZ_SELECT_WARPPOINT handler dispatches to CastEndMap.

## Notes / gotchas

- Warp Portal actually places a ground unit at the chosen exit; the destination here is the
  portal's exit coords. Coordinate with the WarpPortal plugin (`Acolyte/WarpPortal.cs`).
