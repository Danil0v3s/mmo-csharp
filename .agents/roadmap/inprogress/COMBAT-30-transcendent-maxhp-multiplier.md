# COMBAT-30 — Transcendent (JOBL_UPPER ×1.25) + Taekwon-ranker (×3) MaxHP/SP multiplier

> **Epic:** Combat parity · **Status:** 🚧 In progress · **Size:** S · **Player-visible:** yes
> **Depends on:** none · **Blocks:** none

## Problem

rAthena's renewal `status_calc_maxhp_pc` / `status_calc_maxsp_pc` multiply the job-base
HP/SP by **1.25 for transcendent classes** (`class_ & JOBL_UPPER`) and by **3 for a
Taekwon ranker**. The C# `StatusCalcService.CalcPc` applies neither, so a transcendent
character has ~20 % less HP/SP than rAthena. (Note: this is the *correct* reading of the
"renewal trait HP" line in the original COMBAT-09 ticket — rAthena's MaxHP formula has **no
STA term**; STA feeds Res, which is already computed in `CalcMisc`.)

## Current state (C#)

- `Map.Server/Status/StatusCalcService.cs` `CalcPc` — MaxHP = `base * (100+vit)/100`, then
  `(+ flat) * (100+rate)/100` (COMBAT-09). **No `JOBL_UPPER` ×1.25, no taekwon ×3.**
- `Map.Server/Status/JobAspdCacheService.cs` `JobAegisMapper` — maps only 1st-class +
  trans-1st (`*_High`) job ids; trans-2nd / 3rd / 4th are unmapped.
- `Map.Server/Entities/PlayerEntity.cs` — has `ClassMask` (rAthena `class_` bitmask) but no
  helper to test `JOBL_UPPER`.

## rAthena reference (source of truth)

- `status.cpp:3479-3483`:
  ```c
  if (sd.class_ & JOBL_UPPER)        dmax *= 1.25;
  else if (pc_is_taekwon_ranker(&sd)) dmax *= 3;
  ```
  applied AFTER the VIT scale and BEFORE the flat/rate equip bonuses. Same for MaxSP
  (`status_calc_maxsp_pc`).
- `JOBL_UPPER = 0x1000` (pc.hpp). `pc_is_taekwon_ranker` = Taekwon job at max job level with
  the ranker flag.

## Scope — every sub-system that must be touched

- [ ] Add a `JOBL_UPPER` test (use `PlayerEntity.ClassMask & 0x1000`, or a `JobAegisMapper`
      helper) and apply the ×1.25 multiplier to MaxHP **and** MaxSP after the VIT/INT scale,
      before the flat/rate equip fold.
- [ ] Add the Taekwon-ranker ×3 multiplier (gate on Taekwon job + max-job-level + ranker
      state; ranker state may need a small flag if not modeled — note, don't stub).
- [ ] Ensure `ClassMask` is populated for connected players (it is set from `ch.*`? verify;
      if not, wire it alongside `ClassId` in `NotifyActorInitHandler`).
- [ ] Map the transcendent job ids needed to exercise this (extend `JobAegisMapper` /
      `ClassMask` derivation) so the multiplier is reachable.

## Done criteria

- A transcendent character's MaxHP/MaxSP is 1.25× the same-stat non-trans value.
- A Taekwon ranker's MaxHP is 3× base.
- Non-trans characters are unchanged.

## Test plan

- Unit-test CalcPc with a JOBL_UPPER ClassMask → MaxHP/MaxSP ×1.25.
- Unit-test taekwon-ranker ×3.
- Regression: non-trans Novice unchanged.

## Notes / gotchas

- Multiplier order matters: VIT scale → ×1.25/×3 → +flat → ×rate (match rAthena exactly).
- If `ClassMask` / ranker state isn't populated for live players, that wiring is part of
  this ticket (don't leave the multiplier dormant behind unpopulated state).
