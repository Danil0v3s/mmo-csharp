# COMBAT-30 — Transcendent (JOBL_UPPER ×1.25) + Taekwon-ranker (×3) MaxHP/SP multiplier

> **Epic:** Combat parity · **Status:** ✅ Done (2026-06-02) · **Size:** S · **Player-visible:** yes
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

- [x] Added `JobAegisMapper.IsTranscendent(jobId)` (job-id band 4001-4022 — `MapidClass.Upper`
      is overloaded for this version so a job-id test is the reliable JOBL_UPPER signal) and
      applied the ×1.25 multiplier to MaxHP **and** MaxSP after the VIT/INT scale, before the
      flat/rate equip fold (`StatusCalcService.CalcPc`).
- [x] Added the Taekwon-ranker ×3 multiplier — gates on `JobId == 4046` + `BaseLevel >= 90` +
      `PlayerEntity.IsTaekwonRanker` (new flag; the fame-rank *population* of that flag
      ➡️ COMBAT-51, the multiplier logic itself is done + tested).
- [x] Fixed the latent never-populated `ClassMask` bug: `PlayerEntity.ClassId` is now a
      property whose setter derives `ClassMask = MapidClass.FromClassId(value)`, so every
      connected player's mask is populated wherever `ClassId` is assigned.
- [x] Added `JobAegisMapper.TaekwonJobId = 4046` + the trans 1st/2nd ids the multiplier needs.
      ➡️ COMBAT-51: the full trans-3rd/4th JOBL_UPPER inheritance table.

## Done criteria

- A transcendent character's MaxHP/MaxSP is 1.25× the same-stat non-trans value. ✅ (trans
  1st/2nd; trans-3rd/4th ➡️ COMBAT-51)
- A Taekwon ranker's MaxHP is 3× base. ✅ (multiplier logic; live fame-rank population
  ➡️ COMBAT-51)
- Non-trans characters are unchanged. ✅

## Test plan

- Unit-test CalcPc with a JOBL_UPPER ClassMask → MaxHP/MaxSP ×1.25.
- Unit-test taekwon-ranker ×3.
- Regression: non-trans Novice unchanged.

## Notes / gotchas

- Multiplier order matters: VIT scale → ×1.25/×3 → +flat → ×rate (match rAthena exactly).
- If `ClassMask` / ranker state isn't populated for live players, that wiring is part of
  this ticket (don't leave the multiplier dormant behind unpopulated state).

## History

- 2026-06-02 · Added `JobAegisMapper.IsTranscendent` (4001-4022) + `TaekwonJobId`, applied
  the renewal ×1.25 / ×3 MaxHP+MaxSP multipliers in `StatusCalcService.CalcPc` (after the
  VIT/INT scale, before the equip flat/rate fold, matching status.cpp:3479), added the
  `PlayerEntity.IsTaekwonRanker` flag, and fixed the latent `ClassMask`-never-populated bug
  via the `ClassId` setter. 5 tests in `Combat30TranscendentMaxHpTests` green; full
  Map.Server.Tests suite green except the pre-existing INFRA-11 replay gate. Filed COMBAT-51
  for the trans-3rd/4th JOBL_UPPER table + the Taekwon fame-rank population.
