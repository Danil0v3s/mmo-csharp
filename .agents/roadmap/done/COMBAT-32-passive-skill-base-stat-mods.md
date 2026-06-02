# COMBAT-32 — Passive-skill absolute base-stat modifiers + Super Novice all-stat +10

> **Epic:** Combat parity · **Status:** ✅ Done (2026-06-02) · **Size:** M · **Player-visible:** yes
> **Depends on:** COMBAT-10 (base→final stat layering) · **Blocks:** none
> **Filed by:** COMBAT-10 on 2026-06-01 (the base-stat layering it ported skips these extra base addends).

## Problem

rAthena `status_calc_pc_` adds several **absolute base-stat bonuses** to
`base_status` *before* the card/equip/SC layering (status.cpp:4221-4242).
COMBAT-10 ported the base + equip param + job-bonus layering but does **not**
apply these passive-skill / Super-Novice base addends, so e.g. a Hunter with
`AC_OWL` gets no +DEX, and a job-70 Super Novice gets no +10 all-stats.

## Current state (C#)

- `Map.Server/Status/StatusCalcService.cs` `CalcPc` — layers
  `base + equip_param + job_bonus` only; no passive-skill or Super-Novice addend.
- `PlayerEntity.LearnedSkills` already holds the skill levels needed to gate these.

## rAthena reference (source of truth)

`status.cpp:4221-4242` (after the job_bonus block, before the param fold):

- Super Novice (`MAPID_SUPER_NOVICE`, job_level ≥ 70 OR `JOBL_THIRD`, `die_counter == 0`):
  `str/agi/vit/int/dex/luk += 10`.
- `BS_HILTBINDING > 0` → `str += 1`.
- `SA_DRAGONOLOGY` lv → `int += (lv+1)/2`.
- `AC_OWL` lv → `dex += lv`.
- `RA_RESEARCHTRAP` lv → `int += lv`.
- `SU_POWEROFLAND > 0` → `int += 20`.

These add to `base_status` (so they flow into the final stat AND its derived
hit/atk/matk), but are NOT part of the persisted allocated base.

## Scope — every sub-system that must be touched

- [x] Added `StatusCalcService.ApplyPassiveBaseStatAddends` — folds the addends into
      the `paramBase[]` span (after job/equip bonus, before the delta-fold), keyed off
      `PlayerEntity.LearnedSkills` + `inputs.JobId/JobLevel` + `player.DieCounter`.
- [x] Gated Super Novice on the super-novice job-id set (23/4045/4190/4191 — the C#
      `MapidClass.Upper` is overloaded like COMBAT-30, so a mask test is unreliable) +
      `JobLevel >= 70` + `DieCounter == 0`. Added the `PlayerEntity.DieCounter` field.
- [x] Added the missing `SkillIds` constants: BS_HILTBINDING (105), SA_DRAGONOLOGY
      (284), RA_RESEARCHTRAP (2248), SU_POWEROFLAND (5025); AC_OWL (43) already present.
- [x] Idempotent via the COMBAT-10 delta path — verified by
      `Addends_are_idempotent_across_repeated_recalc`.

## Done criteria

- A Hunter with AC_OWL lv10 shows DEX +10 (and the derived Hit/BaseAtk rise). ✅
- A job-70, never-died Super Novice shows +10 to all six base stats. ✅
- BS_HILTBINDING / SA_DRAGONOLOGY / RA_RESEARCHTRAP / SU_POWEROFLAND apply their
  rAthena-exact amounts; idempotent across repeated recalc. ✅
- The Super Novice +10 is permanently lost after the first death (die_counter
  populated + persisted). ➡️ Moved to COMBAT-52 — the `DieCounter` field exists and
  the gate reads it, but the death-increment + char-DB persistence is not yet wired
  (the field defaults 0 = "never died" until then).

## Test plan

- Unit: CalcPc with a fake LearnedSkills set asserts each addend + idempotency.
- Unit: Super Novice gate (job<70 → no bonus; job≥70 die_counter>0 → no bonus).

## Notes / gotchas

- `die_counter` may not be modeled yet — add the field (mmo_charstatus has it) or
  gate conservatively (treat 0 until persisted) and note the limitation.

## History

- 2026-06-02 · Added `ApplyPassiveBaseStatAddends` to `StatusCalcService.CalcPc` —
  folds the rAthena status.cpp:4221-4241 absolute base-stat addends (Super Novice
  all-stat +10 gated on the super-novice job-id set + joblv≥70 + die_counter==0;
  BS_HILTBINDING +1 STR; SA_DRAGONOLOGY +(lv+1)/2 INT; AC_OWL +lv DEX; RA_RESEARCHTRAP
  +lv INT; SU_POWEROFLAND +20 INT) into the `paramBase[]` span so they ride the
  COMBAT-10 idempotent delta-fold. Added the four missing `SkillIds` constants + the
  `PlayerEntity.DieCounter` field. Combat32PassiveBaseStatTests (9) green; full
  Map.Server.Tests green except the pre-existing INFRA-11 replay gate. Filed COMBAT-52
  for the die_counter death-increment + persistence wiring.
