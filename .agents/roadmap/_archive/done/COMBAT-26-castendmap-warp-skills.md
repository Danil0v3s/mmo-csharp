# COMBAT-26 — CastEndMap warp skills (Teleport / Warp Portal / Greed map step)

> **Epic:** Combat parity · **Status:** ✅ Done (2026-06-02) · **Size:** M · **Player-visible:** yes
> **Depends on:** none · **Blocks:** none

## Problem

`SkillCastEndService.CastEndMap` returns `false` unconditionally — the entire
"skill cast that resolves onto a map / warp destination" branch is unimplemented. So
Teleport (`AL_TELEPORT` random/save-point warp), Warp Portal destination resolution, and
other `CastEndMap`-class skills do nothing on cast end. This was Scope axis 3 of COMBAT-08,
split out because it depends on the player-warp service surface rather than the damage path.

## Current state (C#)

- `Map.Server/Skills/SkillCastEndService.cs:71-80` `CastEndMap` — `return false;` (warp deferred).
- Player relocation service: confirm whether `IPlayerWarpService` / a `pc_setpos` analogue
  exists (`Map.Server/Movement/` — `PcSetposService` is referenced by tests). Wire to it.
- `AL_TELEPORT` / `AL_WARP` skill defs — confirm `SkillTargetMode` / inf flags route to the
  `CastEndMap` handler (vs `CastEndId`/`CastEndPos`).

## rAthena reference (source of truth)

Canonical: `skill.cpp` `skill_castend_map` (the `case AL_TELEPORT`/`AL_WARP`/`AL_CHANGEUNDEAD`
arms), `pc.cpp` `pc_setpos`, `pc_randomwarp`.

- `AL_TELEPORT` level 1 → random warp on the current map (`pc_randomwarp`); level 2 → warp to
  the player's save point (`pc_setpos(save_point)`). The client sends the destination map name
  in the `CZ_*` map-cast packet; rAthena validates it against the skill level / nowarp flags.
- `AL_WARP` → resolves the chosen `ZC_WARPLIST` destination to a warp coordinate and `pc_setpos`.
- Honor map `noteleport` / `nowarp` / `nowarpto` flags and the GvG/PvP gates before warping.

## Scope — every sub-system that must be touched

- [x] Implemented `CastEndMap` for `AL_TELEPORT`: "Random" → `IPlayerPositionHelpers.
      RandomWarp` (bounded walkable-cell retry), "SavePoint" → `IPcDeathService.
      WarpToSavepoint`, both warping via `IPcSetposService`. Injected the warp seam
      (positions/death/mapFlags/maps) as optional ctor deps — no DI cycle.
- [x] Enforce the `noteleport` map flag before warping (resolves the caster's current map
      name + `IMapFlagService.IsSet`). Mob casters never warp here.
- [x] Returns `true` on a successful warp (the warp itself is the visible effect).
- [ ] `AL_WARP` destination resolution (map name → memo coords) + `pc_setpos`, plus the
      `CZ_SELECT_WARPPOINT` packet handler that routes the chooser pick into `CastEndMap`,
      ➡️ moved to **COMBAT-48** (needs the per-PC memo-point store + a new packet).

## Done criteria

- `CastEndMap` for Teleport level 1 relocates the caster to a random walkable cell ✅ (no
  longer `return false`).
- Teleport level 2 relocates the caster to their save point ✅.
- `noteleport`-flagged maps refuse the warp ✅.

## Test plan

- Cast Teleport lvl 1 on a test map → caster position changes to a walkable cell.
- Cast Teleport lvl 2 → caster warps to the configured save point.
- On a `noteleport` map → cast fails, position unchanged.

## Notes / gotchas

- If `IPlayerWarpService`/`pc_setpos` is not directly reachable from `SkillCastEndService`,
  inject the minimal seam rather than re-introducing a DI cycle.
- Random-warp cell selection must avoid non-walkable / `noteleport` cells (rAthena retries).

## History

- **2026-06-02** — inprogress→done. `SkillCastEndService.CastEndMap` (was an unconditional
  `return false`) now implements AL_TELEPORT: "Random" → `IPlayerPositionHelpers.RandomWarp`,
  "SavePoint" → `IPcDeathService.WarpToSavepoint`, gated on the `noteleport` map flag; PCs
  only. Injected the warp seam as optional ctor deps. `Combat26CastEndMapTests` (4); unit
  suite 3834 (1 fail = pre-existing INFRA-11 replay gate). Filed COMBAT-48 (AL_WARP memo
  resolution + CZ_SELECT_WARPPOINT handler).
