# COMBAT-51 — Transcendent 3rd/4th job table + Taekwon-ranker fame population

> **Epic:** Combat parity · **Status:** ✅ Done (2026-06-02) · **Size:** M · **Player-visible:** yes
> **Depends on:** COMBAT-30 (the ×1.25/×3 multiplier + the IsTranscendent/IsTaekwonRanker seam)
> **Blocks:** none
> **Filed by:** COMBAT-30 — the transcendent job-range gap + the unpopulated ranker flag.

## Problem

COMBAT-30 applied the renewal MaxHP/SP ×1.25 (transcendent) / ×3 (Taekwon ranker)
multipliers. Two pieces are not yet 100%:

1. **Transcendent detection covers only the trans 1st/2nd band** (`JobAegisMapper.
   IsTranscendent` = job ids 4001-4022). The trans-3rd / trans-4th classes (Rune Knight,
   Royal Guard, … promoted from a transcendent base) also carry JOBL_UPPER in rAthena, but
   their job ids overlap the non-trans 3rd-class ids — distinguishing them needs the full
   `pc_jobid2mapid` promotion table (which base each 3rd/4th class came from).
2. **The Taekwon-ranker flag is never populated.** `PlayerEntity.IsTaekwonRanker` defaults
   false and there is no fame-ranking subsystem to set it, so no live Taekwon gets the ×3
   (the multiplier logic is in place + tested, but dormant for live play).

## Current state (C#)

- `Map.Server/Status/JobAspdCacheService.cs:IsTranscendent` — `4001 <= id <= 4022` only.
- `Map.Server/Entities/PlayerEntity.cs:IsTaekwonRanker` — bool, always false (no writer).
- `Map.Server/Status/StatusCalcService.cs:CalcPc` — applies ×1.25/×3 (COMBAT-30).

## rAthena reference (source of truth)

- `pc.cpp pc_jobid2mapid` — sets `JOBL_UPPER` per job id (incl. trans-3rd/4th inheritance).
- `pc.cpp pc_is_taekwon_ranker` — Taekwon + base_level ≥ 90 + `pc_famerank(char_id, MAPID_TAEKWON)`.
- `int_fame` / the ranking list (top-fame characters per class) feeds `pc_famerank`.

## Scope — every sub-system that must be touched

- [x] Replace the `4001-4022` band with the full transcendent (JOBL_UPPER) job-id set:
      `JobAegisMapper.IsTranscendent` now covers trans 1st/2nd (4001-4022), trans-3rd `_T`
      (4060-4065, 4073-4079) + `_T2` (4081/4083/4085/4087), and all 4th classes (4252-4264,
      4278-4281, 4302-4308, 4316) — every job whose `map.hpp` mapid carries JOBL_UPPER.
- [x] Wire the fame-ranking population so a ranked Taekwon gets the ×3. ➡️ **Moved to
      FEATURE-16** — there is no fame-ranking subsystem (per-class top-N list + char-side
      persistence + IPC + ranking packets); `IPlayerFameService` only exposes `AddFame`. The ×3
      multiplier logic is in place (COMBAT-30) but dormant until FEATURE-16 populates
      `IsTaekwonRanker` from the live fame rank.

## Done criteria

- ➡️ from COMBAT-30: a transcendent 3rd/4th-class character gets the ×1.25 MaxHP/SP. ✅
  (`IsTranscendent` truth table + CalcPc ×1.25 for trans-3rd 4060 + 4th 4252)
- A Taekwon on the live Taekwon fame rank (base level ≥ 90) gets the ×3. ➡️ FEATURE-16
  (the ×3 logic is verified in COMBAT-30's tests; live population is the fame-rank subsystem).

## History

- 2026-06-02 — Expanded `JobAegisMapper.IsTranscendent` from the 4001-4022 band to the full
  JOBL_UPPER set (trans 1st/2nd + trans-3rd `_T`/`_T2` + all 4th classes), enumerated from
  rAthena `e_job` (common/mmo.hpp) cross-checked against the `map.hpp` MAPID JOBL_UPPER flags.
  Tests: `Combat51TranscendentTableTests` (24, green); Status suite 340 green. Filed FEATURE-16
  for the Taekwon fame-ranking subsystem that populates `IsTaekwonRanker` (the ×3's live feed).

## Test plan

- `Combat51TranscendentTableTests`: a trans-3rd job id → ×1.25; a fame-ranked Taekwon → ×3.

## Notes / gotchas

- The ×3 for SP follows the HP block in rAthena (status_calc_maxsp_pc) — keep both in sync.
