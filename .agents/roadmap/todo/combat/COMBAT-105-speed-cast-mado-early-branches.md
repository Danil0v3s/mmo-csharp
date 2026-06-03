# COMBAT-105 — status_calc_speed early-return branches: freecast/ExceedBreak + mado gear

> **Epic:** combat · **Status:** ❌ Not started · **Size:** S · **Player-visible:** yes
> **Depends on:** COMBAT-84, COMBAT-70 · **Blocks:** none
> **Filed by:** COMBAT-84 — the speed tail SCs + the Hiding/ChaseWalk slow branch landed; the two
> early-return branches need live cast-state / mado-gear state not reachable from `CalcPc`.

## Problem

`status_calc_speed` (status.cpp:7787) has two early branches the C# `ComputeScSpeed` doesn't model:
1. **Mado gear** (`pc_ismadogear`): speed is unaffected by other SCs; instead
   `val = (NC_MADOLICENCE<5 ? 50-10*lic : -25) - (SC_ACCELERATION ? 25 : 0)`, `speed += speed*val/100`,
   and return.
2. **Freecast / Exceed Break** (`sd->ud.skilltimer != INVALID_TIMER && (SA_FREECAST || skill_id ==
   LG_EXEEDBREAK)`): `speed_rate = (LG_EXEEDBREAK ? 160-10*lv : 175-5*pc_checkskill(SA_FREECAST))`,
   replacing the whole slow/fast accumulator while casting.

`ComputeScSpeed` runs from `CalcPc` (a stat recalc) and has no access to the live cast skill id/level
(`ud.skilltimer`/`ud.skill_id`) nor the mado-gear-equipped flag.

## Current state (C#)

- `Map.Server/Status/StatusCalcService.cs:ComputeScSpeed` — no mado / freecast early branch.
- `Map.Server/Entities/PlayerEntity.cs` — no current-cast-skill field exposed to the stat calc;
  COMBAT-70 precomputed `FreecastAdelay` for the ASPD path but not the speed branch.

## rAthena reference (source of truth)

- `status.cpp:7793` (mado) / `status.cpp:7809` (freecast/exeedbreak).

## Scope

- [ ] Expose the live cast skill id/level + a mado-gear flag to the speed recalc (or recompute speed
      on cast-start/end), and add the two early branches (coordinate with COMBAT-70's cast-state seam).

## Done criteria

- While casting with SA_FREECAST learned, move speed = `150 * (175-5*lv)/100`; a mado-gear character's
  speed follows the NC_MADOLICENCE/SC_ACCELERATION formula and ignores other SCs.

## Test plan

- Freecast-while-casting speed; mado-gear speed with/without SC_ACCELERATION.
