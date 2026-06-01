# COMBAT-32 — Passive-skill absolute base-stat modifiers + Super Novice all-stat +10

> **Epic:** Combat parity · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
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

- [ ] Add a passive-skill base addend pass in `CalcPc` (or a helper fed into the
      `paramBase[]` accumulation) keyed off `PlayerEntity.LearnedSkills` +
      class mask + `die_counter` (needs a death-counter field if not present).
- [ ] Gate Super Novice on the MAPID upper-mask + job level + die_counter.
- [ ] Confirm each skill id exists in `SkillIds`; add any missing.
- [ ] These addends must layer idempotently (they're recomputed each CalcPc from
      skill levels, so they ride the same param-base snapshot — verify no double
      count via the COMBAT-10 delta path).

## Done criteria

- A Hunter with AC_OWL lv10 shows DEX +10 (and the derived Hit/BaseAtk rise).
- A job-70, never-died Super Novice shows +10 to all six base stats.
- BS_HILTBINDING / SA_DRAGONOLOGY / RA_RESEARCHTRAP / SU_POWEROFLAND apply their
  rAthena-exact amounts; idempotent across repeated recalc.

## Test plan

- Unit: CalcPc with a fake LearnedSkills set asserts each addend + idempotency.
- Unit: Super Novice gate (job<70 → no bonus; job≥70 die_counter>0 → no bonus).

## Notes / gotchas

- `die_counter` may not be modeled yet — add the field (mmo_charstatus has it) or
  gate conservatively (treat 0 until persisted) and note the limitation.
