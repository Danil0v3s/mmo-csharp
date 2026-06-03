# FEATURE-16 — Fame-ranking subsystem + Taekwon-ranker population

> **Epic:** features · **Status:** ❌ Not started · **Size:** L · **Player-visible:** yes
> **Depends on:** none · **Blocks:** COMBAT-51 done-criterion (live Taekwon ×3)
> **Filed by:** COMBAT-51 — the fame-rank list `IsTaekwonRanker` reads from does not exist yet.

## Problem

`PlayerEntity.IsTaekwonRanker` is never populated, so no live Taekwon gets the ×3 MaxHP
(COMBAT-30 wired the multiplier; COMBAT-51 fixed the transcendent table). rAthena gates the ×3
on `pc_famerank(char_id, MAPID_TAEKWON)` — i.e. the character being in the top-N **fame ranking
list** for their class. There is no fame-ranking subsystem in the C# port: `IPlayerFameService`
only exposes `AddFame` (adds fame points), and there is no per-class top-N leaderboard, no
char-side persistence of it, and no map-side population of the ranker flag.

rAthena maintains fame lists for Blacksmith (forging), Alchemist (brewing), Taekwon (PvP kills),
and the chef/sage lists — surfaced via `CZ_REQ_RANKING` / `ZC_*RANK*` and consumed by
`pc_famerank`. This ticket builds that subsystem (Taekwon first, the others fall out).

## Current state (C#)

- `Map.Server/Entities/PlayerEntity.cs:IsTaekwonRanker` — bool, no writer.
- `Map.Server/Status/IPlayerFameService.cs` — only `AddFame(pc, count)`.
- `Map.Server/Status/StatusCalcService.cs:CalcPc` — applies ×3 when
  `JobId == TaekwonJobId && BaseLevel >= 90 && IsTaekwonRanker` (COMBAT-30; dormant).
- No fame-ranking table / repository / IPC / ranking packets.

## rAthena reference (source of truth)

- `pc.hpp` `pc_is_taekwon_ranker` macro: `(class_&MAPID_UPPERMASK)==MAPID_TAEKWON && base_level >= battle_config.taekwon_ranker_min_lv && pc_famerank(char_id, MAPID_TAEKWON)`.
- `pc.cpp` `pc_famerank` / `pc_addfame` + the `fame_list` arrays; `int_fame` / char-side ranking
  persistence; `clif_parse_ranklist` / `ZC_*RANK*` packets.

## Scope — every sub-system that must be touched

- [ ] Char-side fame-ranking persistence (per-class top-N list from the `fame` column) + repo.
- [ ] IPC: map↔char fame-rank query/update (load the rank list on enter; push on fame change).
- [ ] Map-side `IPlayerFameService` extension: `IsInFameRank(pc, class)` + populate
      `PlayerEntity.IsTaekwonRanker` on map enter and on fame change; recalc stats so the ×3 folds.
- [ ] Ranking packets (`CZ_REQ_RANKING` → `ZC_*RANK*`) so the client can open the ranking window.

## Done criteria

- A Taekwon on the live Taekwon fame rank (base level ≥ 90) gets the ×3 MaxHP end-to-end;
  dropping off the rank removes it on next recalc.
- The ranking window shows the top-N for each fame class.

## Test plan

- `Feature16FameRankTests`: a char in the Taekwon top-N → `IsTaekwonRanker` true → ×3; off-rank → false.

## Notes / gotchas

- COMBAT-30/51 already supply the ×3 multiplier + Taekwon class gate — this ticket only supplies
  the fame-rank list + population. Keep `battle_config.taekwon_ranker_min_lv` (default 90) configurable.
