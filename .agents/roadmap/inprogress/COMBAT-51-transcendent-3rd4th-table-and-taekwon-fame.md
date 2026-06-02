# COMBAT-51 — Transcendent 3rd/4th job table + Taekwon-ranker fame population

> **Epic:** Combat parity · **Status:** 🚧 In progress · **Size:** M · **Player-visible:** yes
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

- [ ] Replace the `4001-4022` band with the full transcendent job-id set (trans 1st/2nd +
      the trans-3rd/4th classes), or derive JOBL_UPPER from a complete `pc_jobid2mapid` port.
- [ ] Wire the fame-ranking population (char-side fame list → `IsTaekwonRanker` on map enter /
      fame change) so a ranked Taekwon actually gets the ×3.

## Done criteria

- ➡️ from COMBAT-30: a transcendent 3rd/4th-class character gets the ×1.25 MaxHP/SP.
- A Taekwon on the live Taekwon fame rank (base level ≥ 90) gets the ×3.

## Test plan

- `Combat51TranscendentTableTests`: a trans-3rd job id → ×1.25; a fame-ranked Taekwon → ×3.

## Notes / gotchas

- The ×3 for SP follows the HP block in rAthena (status_calc_maxsp_pc) — keep both in sync.
