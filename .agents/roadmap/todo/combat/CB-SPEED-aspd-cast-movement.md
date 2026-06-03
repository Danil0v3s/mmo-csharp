# CB-SPEED — ASPD / cast / movement-speed formula tail

> **Epic:** combat · **Status:** ❌ Not started · **Size:** S · **Player-visible:** yes
> **Depends on:** none · **Unlocks:** none

## The deliverable

> The remaining ASPD/cast/movement-speed early-branches + cast-lock match rAthena. **Combat last.**

## What this absorbs (archive)

- `_archive/todo/combat/COMBAT-105` — `status_calc_speed` early-return branches (freecast/ExceedBreak + mado gear).
- `_archive/todo/combat/COMBAT-106` — `status_calc_speed` Dancing-lesson song penalty + TF_MISS assassin speedup.
- `_archive/todo/combat/COMBAT-110` — movement cast-lock (block move while casting unless SA_FREECAST/LG_EXEEDBREAK).

## rAthena reference

- `rathena/src/map/status.cpp` — `status_calc_speed` (the slow/fast accumulator early branches),
  `status_calc_aspd`; `rathena/src/map/unit.cpp` — movement cast-lock.

## Scope

- [ ] `status_calc_speed` freecast/ExceedBreak + mado-gear early branches; Dancing-lesson +
      TF_MISS terms.
- [ ] Movement cast-lock (block move while casting unless the freecast SCs).

## Done criteria

- The cited movement-speed cases compute the rAthena value; a casting player can't walk unless
  SA_FREECAST/LG_EXEEDBREAK; the `Combat*Tests` pass.

## Test plan

- Extend the archived COMBAT-105/106/110 tests.

## Notes

- Builds on the landed `ComputeScSpeed`/`ComputeSkillAspdVal` (archive COMBAT-50/65). Combat-last.
