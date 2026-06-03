# COMBAT-69 — SG_DEVIL max-job-level ASPD clause (Star Gladiator path)

> **Epic:** combat · **Status:** ✅ Done (2026-06-03) · **Size:** S · **Player-visible:** yes
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

- [x] Threaded the per-class job-level cap into `ComputeSkillAspdVal` via a new `int maxJobLevel`
      param, computed at the `CalcPc` call site from `JobAegisMapper.AegisByJobId(inputs.JobId)` →
      `IJobStatsCacheService.GetMaxJobLevel` (the jobId→aegis resolver already existed in the file;
      the ticket's "not reachable" note was stale). `maxJobLevel` defaults 0 so callers without a
      job-stats cache keep the Star-Emperor-only behavior.
- [x] Extended the SG_DEVIL gate to `IsStarEmperor(pc) || (maxJobLevel > 0 && pc.JobLevel >= maxJobLevel)`.

## Done criteria

- A Star Gladiator at max job level with SG_DEVIL learned gets `+1 + lv` ASPD val; below max
  job level, no bonus. Star Emperors keep the existing behavior. ✅ Combat69SgDevilMaxJobTests (3).

## Test plan

- `Combat69SgDevilMaxJobTests`: Star Gladiator at max job → bonus; below max → none.

## Notes / gotchas

- COMBAT-50 already wires `ComputeSkillAspdVal` into the amotion formula — this only widens the
  SG_DEVIL gate + supplies the max-job-level input.

## History

- 2026-06-03 · Widened the SG_DEVIL ASPD gate with the `|| pc_is_maxjoblv` half. Added a
  `maxJobLevel` param to `StatusCalcService.ComputeSkillAspdVal` (default 0 = backward-compatible),
  computed at the `CalcPc` call site via `JobAegisMapper.AegisByJobId(inputs.JobId)` →
  `GetMaxJobLevel`; the gate is now `IsStarEmperor(pc) || (maxJobLevel > 0 && pc.JobLevel >=
  maxJobLevel)`. The jobId→aegis resolver already existed in the file (ticket's "not reachable"
  premise was stale). Combat69SgDevilMaxJobTests (3); Status+Combat suite 801 green, full suite
  4088 pass (1 fail = pre-existing INFRA-11 replay gate). No follow-ups.
