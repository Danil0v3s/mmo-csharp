# COMBAT-26 — CastEndMap warp skills (Teleport / Warp Portal / Greed map step)

> **Epic:** Combat parity · **Status:** 🚧 In progress · **Size:** M · **Player-visible:** yes
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

- [ ] Implement `CastEndMap` for `AL_TELEPORT`: level 1 random warp on the current map, level 2
      save-point warp, via the player-warp service. Consume SP / apply after-cast delay as the
      normal cast-end path does.
- [ ] Implement the `AL_WARP` destination resolution (map name → coordinates) + `pc_setpos`.
- [ ] Enforce map flags (`noteleport`, `nowarp`, `nowarpto`) and PvP/GvG gates.
- [ ] Return `true` on a successful warp; emit any required ZC packets (the warp itself is the
      visible effect).

## Done criteria

- `CastEndMap` for Teleport level 1 relocates the caster to a random walkable cell on the
  current map (no longer `return false`).
- Teleport level 2 relocates the caster to their save point.
- `noteleport`-flagged maps refuse the warp with the rAthena failure path.

## Test plan

- Cast Teleport lvl 1 on a test map → caster position changes to a walkable cell.
- Cast Teleport lvl 2 → caster warps to the configured save point.
- On a `noteleport` map → cast fails, position unchanged.

## Notes / gotchas

- If `IPlayerWarpService`/`pc_setpos` is not directly reachable from `SkillCastEndService`,
  inject the minimal seam rather than re-introducing a DI cycle.
- Random-warp cell selection must avoid non-walkable / `noteleport` cells (rAthena retries).
