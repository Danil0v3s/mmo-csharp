# COMBAT-69 — SG_DEVIL max-job-level ASPD clause (Star Gladiator path)

> **Epic:** combat · **Status:** 🚧 In progress · **Size:** S · **Player-visible:** yes
> **Depends on:** COMBAT-50 (the skill-val ASPD seam) · **Blocks:** none
> **Filed by:** COMBAT-50 — the `|| pc_is_maxjoblv` half of the SG_DEVIL gate it could not cleanly resolve.

## Problem

COMBAT-50 implemented the SG_DEVIL ASPD `val` term (`+1 + lv`) for **Star Emperors**
(Taekwon 3rd-class) via `IsStarEmperor`. rAthena's full gate is
`(class & MAPID_THIRDMASK) == MAPID_STAR_EMPEROR || pc_is_maxjoblv(sd)` — so a **Star
Gladiator** (2nd-class) who has SG_DEVIL learned and is **at max job level** also gets the
bonus. That `|| pc_is_maxjoblv` path is not implemented (the per-class max-job-level isn't
cleanly reachable from `StatusCalcService.ComputeSkillAspdVal`, which only has `inputs.JobId`,
not the job aegis name `IJobStatsCacheService.GetMaxJobLevel` keys on).

## Current state (C#)

- `Map.Server/Status/StatusCalcService.cs:ComputeSkillAspdVal` — `SG_DEVIL` gated on
  `IsStarEmperor(pc)` only.
- `Map.Server/Status/JobStatsCacheService.cs:GetMaxJobLevel(string jobAegis)` — exists but is
  aegis-keyed; no jobId→aegis resolver is threaded into `StatusCalcService`.

## rAthena reference (source of truth)

- `status.cpp:2345` — `if ((skill_lv = pc_checkskill(sd, SG_DEVIL)) > 0 && ((sd->class_&MAPID_THIRDMASK) == MAPID_STAR_EMPEROR || pc_is_maxjoblv(sd))) val += 1 + skill_lv;`
- `pc.cpp pc_is_maxjoblv` — `sd->status.job_level >= pc_maxjoblv(sd)` (per-class job-level cap).

## Scope — every sub-system that must be touched

- [ ] Thread a per-class max-job-level resolver (jobId→aegis→`GetMaxJobLevel`, or a jobId-keyed
      accessor) into `StatusCalcService`/`ComputeSkillAspdVal`.
- [ ] Extend the SG_DEVIL gate to `IsStarEmperor(pc) || pc.JobLevel >= maxJobLevel`.

## Done criteria

- A Star Gladiator at max job level with SG_DEVIL learned gets `+1 + lv` ASPD val; below max
  job level, no bonus. Star Emperors keep the existing behavior.

## Test plan

- `Combat69SgDevilMaxJobTests`: Star Gladiator at max job → bonus; below max → none.

## Notes / gotchas

- COMBAT-50 already wires `ComputeSkillAspdVal` into the amotion formula — this only widens the
  SG_DEVIL gate + supplies the max-job-level input.
