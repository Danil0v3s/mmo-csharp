# COMBAT-09 — ASPD formula, job-bonus stats, MaxHP trait, SC-safe recalc ordering

> **Epic:** Combat parity · **Status:** 🚧 In progress · **Size:** L · **Player-visible:** yes
> **Depends on:** COMBAT-01 (flat stat / aspd-rate fields land on PcBaseInputs) · **Blocks:** none

## Problem

Four stat-derivation gaps in `StatusCalcService.CalcPc`:

1. **Recalc-mid-buff wipes SC stat deltas (ordering bug).** `CalcPc` hard-zeroes derived
   stats every call (`StatusCalcService.cs:66-78`) and rebuilds from base inputs only. If a
   recalc fires while an AGI-Up / Blessing / weapon-perfection SC is active, the SC's stat
   contribution vanishes until the SC happens to re-apply. rAthena re-folds every active SC
   inside `status_calc_pc_` via the `status_calc_str/agi/...` chain on every recalc.
2. **ASPD / amotion is a heuristic.** `Adelay = baseAmotion * 540 / 590`
   (`StatusCalcService.cs:122`) with a comment admitting it's a stopgap. No AGI/DEX
   `status_calc_aspd` formula, no `bAspdRate`, no dual-wield ASPD, no per-weapon ASPD beyond
   the single `job_aspd_db` base row.
3. **No job-bonus per-job/per-job-level stat bonuses.** rAthena adds
   `job_bonus[job_level-1][PARAM_*]` to the base stats; the C# port never applies them, so a
   high-job-level character is missing its stat bonuses entirely.
4. **MaxHP/SP ignore `bMaxHPrate` and renewal trait HP.** `CalcPc` MaxHP applies only the VIT
   scale (`StatusCalcService.cs:93-96`); no `MaxHpRate`/`FlatMaxHp` fold (overlaps COMBAT-01)
   and no STA-based trait HP for 4th jobs.

## Current state (C#)

- `Map.Server/Status/StatusCalcService.cs:38-130` `CalcPc` — copies `inputs.*` → `s.*`,
  zeroes `Hit/Flee/Cri/Batk/Def2/Mdef2/Patk/Smatk/Res/Mres/...` (`:66-78`), runs `CalcMisc`,
  computes MaxHP/SP (VIT/INT scale only), sets ASPD heuristic (`:113-123`). **No SC re-fold,
  no job_bonus, no aspd formula.**
- `Map.Server/Status/StatusCalcService.cs:114-122` — `baseAmotion = _jobAspd?.GetBaseAspdByJobId(
  jobId, weaponType) ?? 590`; `Adelay = baseAmotion*540/590`. Single base row; no AGI/DEX
  refinement, no dual-wield, no aspd-rate.
- `Map.Server/Status/StatusCalcService.cs:82-106` — MaxHP/SP from `IJobStatsCacheService`
  base × `(100+vit)/100`; no rate/flat/trait fold.
- `Map.Server/Status/IStatusCalcService.cs:68-94` — `PcBaseInputs` has `JobLevel` but no
  job-bonus stat array and no aspd-rate / flat-derived inputs.
- `StatusCalcService` constructor takes `IJobAspdCacheService?` + `IJobStatsCacheService?`
  (`:32`) but **not** an `IStatusChangeService` — so it cannot re-fold SCs even in principle.

## rAthena reference (source of truth)

Canonical: `status.cpp` (not split files).

- `status.cpp:4948` `status_calc_pc_(sd, opt)` — the master PC recalc. It (a) rebuilds base
  status, (b) **adds job bonuses** (`:4202-4211`):
  `const auto& bonus = job_info->job_bonus[sd->status.job_level-1]; base_status->str +=
  bonus[PARAM_STR];` (and agi/vit/int/dex/luk/…), (c) folds card/equip param bonuses
  (`:4244-4266`, COMBAT-01), then (d) the final stats pass through `status_calc_str/agi/...`
  which apply every active SC delta. So SCs are re-applied on every recalc — recalc is
  idempotent w.r.t. active buffs.
- `status.cpp:2310` `status_base_amotion_pc(sd, status)` — the renewal ASPD formula
  (confirmed by reading):
  ```c
  aspd = job->aspd_base[weapontype1];               // single weapon base
  // dual-wield: += aspd_base[weapontype2]/4 (when both hands differ)
  temp_aspd = dex*dex/5.0f + agi*agi*0.5f;          // (the /7 variant is a config branch)
  temp_aspd = sqrt(temp_aspd) * 0.25f + 196;
  aspd = (int)(temp_aspd + (status_calc_aspd(...,true)+val) * agi/200) - min(aspd,200);
  ```
  i.e. amotion is driven by AGI²/DEX² under a sqrt, plus the weapon base, plus
  `status_calc_aspd` SC/bonus contributions.
- `status.cpp:8006` `status_calc_aspd(bl, sc, fixed)` — sums SC aspd modifiers (e.g.
  Two-Hand Quicken, Adrenaline Rush, Berserk) and `sd->bonus.aspd_add` / `aspd_rate`.
- MaxHP renewal trait: `status_calc_maxhpsp_pc` folds `maxhp_rate` (`apply_rate`) and
  `bonus.maxhp`, and 4th-job trait HP scales with STA.

## Scope — every sub-system that must be touched

- [ ] **SC-safe recalc.** Inject `IStatusChangeService?` into `StatusCalcService` (optional, to
      keep test ctors working). After the base+job+equip stat build, re-fold every active SC
      stat delta (the C# analogue of `status_calc_str/agi/...`): for each active stat-affecting
      SC on the player, add its `Val`-derived stat bonus to `s.Str/Agi/...` **before**
      `CalcMisc` derives Hit/Flee/etc. This makes a mid-buff recalc preserve buff deltas.
      (If a `StatusChangeService.RecalcStatBonuses(player)` helper exists, call it; otherwise
      iterate the active SC list and apply the documented stat deltas.)
- [ ] **Job-bonus stats.** Add `int[] JobBonusStats` (PARAM order) to `PcBaseInputs`, loaded by
      `IJobStatsCacheService` from `job_db` `job_bonus[jobLevel-1]`. Add a loader method
      `GetJobBonusStats(jobAegis, jobLevel)`. In `CalcPc`, add them to `s.Str/Agi/...` right
      after copying base inputs (before SC re-fold).
- [ ] **ASPD formula.** Replace the `*540/590` heuristic (`StatusCalcService.cs:122`) with
      `status_base_amotion_pc`: `temp = dex*dex/5.0 + agi*agi*0.5; temp = sqrt(temp)*0.25 + 196;`
      combine with the `job_aspd` weapon base + dual-wield `+ secondWeaponBase/4`, apply
      `aspd_rate`/`aspd_add` (`bundle.FlatAspdRate`/`FlatAspd` from COMBAT-01) and SC aspd
      mods. Set `s.Amotion` from the result; `s.Adelay = 2*amotion - dmotion` (renewal default
      `2*amotion`); keep the `IJobAspdCacheService` base row as the per-weapon `aspd_base`
      source.
- [ ] **MaxHP/SP fold.** Apply `MaxHpRate` (percent) + `FlatMaxHp` (flat) from the bundle
      (COMBAT-01) and add the renewal STA-based trait HP for 4th jobs. Re-clamp current HP/SP.
- [ ] **No DB migration** if `job_db` already carries `job_bonus` and `job_aspd` aspd_base per
      weapon (verify `IJobStatsCacheService`/`IJobAspdCacheService` loaders expose them);
      otherwise extend the loaders.
- [ ] **Stat-window broadcast** already runs after `CalcPc` (no change), but confirm ASPD
      change reaches the client (`ZC_ATTACK_RANGE`/`ZC_PAR_CHANGE` for aspd) — the recalc push
      path must include the aspd parameter.

## Done criteria

- Recalculating stats (e.g. on equip change) while AGI-Up is active does NOT drop the AGI-Up
  bonus — `s.Agi` after recalc equals base + job + equip + SC, matching pre-recalc.
- A job-level-50 character shows the correct `job_bonus` stat additions for its class
  (compare to rAthena `job_db` for one class).
- ASPD: a high-AGI character has measurably lower amotion than a low-AGI one, following the
  `sqrt(dex²/5 + agi²/2)` curve; `bonus bAspdRate,10;` lowers it further; dual-wield raises
  base amotion per the `/4` second-weapon term.
- MaxHP reflects `bMaxHPrate` + flat + (4th job) STA trait HP.

## Test plan

- Unit-test recalc idempotency: apply a stat SC, snapshot `s.Agi`, call `CalcPc` again, assert
  unchanged (regression for the ordering bug).
- Unit-test `status_base_amotion_pc` against hand-computed values for several (agi,dex,weapon)
  triples; assert exact amotion.
- Unit-test job-bonus application for one class at several job levels.
- Unit-test MaxHP with rate+flat+trait combinations.
- Manual: equip a weapon while under Two-Hand Quicken — ASPD should not reset to the unbuffed
  value; high-AGI assassin attacks visibly faster than a low-AGI one.

## Notes / gotchas

- The recalc-ordering fix is the highest-value item: today *any* recalc during a buff is a
  silent stat loss. Implement SC re-fold even if the full ASPD formula slips — they're
  separable.
- rAthena has a config branch for the `dex²/7` vs `dex²/5` ASPD coefficient
  (`status.cpp:2336-2342`); read `battle_config` to pick the right divisor rather than
  hardcoding.
- `IJobAspdCacheService.GetBaseAspdByJobId(jobId, weaponType)` already returns a per-weapon
  base (`StatusCalcService.cs:114`) — reuse it for `aspd_base[weapontype1]`; you only need the
  second-weapon row for dual-wield.
- Keep the optional-DI pattern: tests construct `StatusCalcService` with the default ctor; the
  SC re-fold and job-bonus must degrade gracefully (no-op) when the services are null, leaving
  the captured Novice fallback intact.
- `Adelay` renewal default is `2*amotion - dmotion` (commonly `2*amotion` with dmotion folded
  elsewhere); the current `*540/590` ratio is wrong — replace it, don't scale it.
