# COMBAT-09 — ASPD formula, job-bonus stats, MaxHP trait, SC-safe recalc ordering

> **Epic:** Combat parity · **Status:** ✅ Done (2026-06-01) · **Size:** L · **Player-visible:** yes
> **Depends on:** COMBAT-01 (flat stat / aspd-rate fields land on PcBaseInputs) · **Blocks:** none
>
> **Shipped:** axis 3 (renewal `status_base_amotion_pc` ASPD formula — the AGI/DEX
> `sqrt` curve replacing the broken `*540/590` heuristic, which was setting amotion ≈
> the raw job-ASPD base when the cache was wired) + axis 4 (MaxHP/SP fold order fix:
> flat-before-rate, matching `status_calc_maxhp_pc`).
>
> **Scope correction discovered during implementation** (see History): axes 1 (SC
> stat re-fold) and 2 (job-bonus stats) **cannot** be done before COMBAT-10's
> base/final param split — the recalc-input builders read back the *conflated*
> `player.Stats.*`, so any additive job/SC fold double-counts on the next recalc.
> COMBAT-10 already scopes both (its Scope lines 79-85). They are ➡️ **Moved to
> COMBAT-10**. The SC/skill ASPD contributions (`status_calc_aspd`), the dual-wield/
> shield ASPD base terms, and the transcendent MaxHP ×1.25 multiplier are filed as
> COMBAT-28 / COMBAT-29 / COMBAT-30. (The ticket's "STA-based trait HP" premise is a
> misread — rAthena `status_calc_maxhp_pc` has no STA term; STA → Res, already done.)

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

- [ ] **SC-safe recalc.** ➡️ **Moved to COMBAT-10.** A correct re-fold needs the base/final
      param split (else the conflated read-back already "preserves" param-stat SC deltas by
      baking them into base, and derived-stat SC deltas are wiped by `CalcMisc`). COMBAT-10
      owns the split + the SC re-application ordering (its Scope lines 82-85). *(The SC ASPD
      contributions — Two-Hand Quicken etc. — are re-fold-safe without the split and are
      tracked separately in **COMBAT-28**.)*
- [ ] **Job-bonus stats.** ➡️ **Moved to COMBAT-10.** `IJobStatsCacheService.GetBonusSum`
      already sums `job_bonus[1..jobLevel]`, but folding it into `s.Str/...` in `CalcPc`
      double-counts because the recalc-input builders read back the conflated `player.Stats.*`
      — it must be added to the *base* total, which only exists after COMBAT-10's split.
- [x] **ASPD formula.** ✅ Replaced the `*540/590` heuristic with the renewal
      `status_base_amotion_pc` (`RenewalPcAmotion`): `sqrt(dex²/divisor + agi²/2)·0.25 + 196`
      (divisor 7 for ranged weapon types, else 5), minus `min(aspd_base, 200)`; the RE
      %-modifier from `bAspdRate` (`aspd_rate2`); `2000 − aspd·10` conversion; `aspd_add`
      (`bAspd`) flat; cap `[95, 4000]`. `Adelay = 2·amotion`; `Dmotion = cap(800 − 4·agi,
      400, 800)`. `JobId`/`WeaponType` now threaded through the 3 `CalcPc` call sites.
      *(SC/skill ASPD `val` terms + FREECAST + fix_aspd ➡️ **COMBAT-28**; dual-wield/shield
      base term ➡️ **COMBAT-29**.)*
- [x] **MaxHP/SP fold.** ✅ Fixed the fold ORDER to rAthena `status_calc_maxhp_pc`:
      `(base + flat) · (100+rate)/100` (flat-before-rate; the prior COMBAT-01 order was
      inverted). *(There is no "STA trait HP" — rAthena's MaxHP has no STA term. The real
      missing multiplier is transcendent ×1.25 / taekwon ×3 ➡️ **COMBAT-30**.)*
- [x] **No DB migration.** Confirmed `job_aspd_db` (raw per-weapon ASPD base) +
      `IJobAspdCacheService` already expose what the formula needs.
- [x] **Stat-window broadcast** — unchanged; the recalc push path already re-sends motion.

## Done criteria

- ➡️ **Moved to COMBAT-10:** Recalc while AGI-Up active does NOT drop the bonus.
- ➡️ **Moved to COMBAT-10:** A job-level-50 character shows correct `job_bonus` stat additions.
- ✅ ASPD: a high-AGI character has measurably lower amotion than a low-AGI one, following the
  `sqrt(dex²/5 + agi²/2)` curve; `bonus bAspdRate,10;` lowers it further. *(Dual-wield `/4`
  second-weapon term ➡️ **COMBAT-29**.)*
- ✅ MaxHP reflects `bMaxHPrate` + flat in the correct (flat-then-rate) order. *(Transcendent
  ×1.25 / taekwon ×3 multiplier ➡️ **COMBAT-30**; rAthena MaxHP has no STA trait term.)*

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

## History

- 2026-06-01 · Shipped axes 3+4. **Axis 3 (ASPD):** replaced the `*540/590` heuristic with
  the renewal `status_base_amotion_pc` formula (`StatusCalcService.RenewalPcAmotion` /
  `RenewalPcDmotion`): AGI/DEX `sqrt` curve (divisor 7 ranged / 5 melee), `min(aspd_base,200)`,
  RE %-modifier from `bAspdRate`, `2000−aspd·10` conversion, `bAspd` flat add, cap `[95,4000]`,
  `adelay=2·amotion`, `dmotion=cap(800−4·agi,400,800)`. Threaded `JobId`/`WeaponType` into the
  3 `CalcPc` call sites (`EquipService`, `StatusOpsService`, `NotifyActorInitHandler` — which
  now also sets `player.ClassId`). Note: `job_aspd_db.base_delay_ms` actually stores the raw
  per-weapon ASPD base (40-65), so the old `s.Amotion = base` set amotion≈40ms when the cache
  was wired — this fixes that. **Axis 4 (MaxHP):** fixed the fold order to flat-before-rate
  (`(base+flat)·(100+rate)/100`) per `status_calc_maxhp_pc`. **Discovered & corrected** that
  axes 1 (SC re-fold) + 2 (job-bonus) require COMBAT-10's base/final split (read-back
  conflation double-counts) → moved to COMBAT-10 (already scoped there). New `Combat09AspdTests`
  (13) + updated 3 `Combat01EquipBonusTests` baselines. Suite 3656 green. Follow-ups filed:
  COMBAT-28 (SC/skill ASPD terms), COMBAT-29 (dual-wield/shield base), COMBAT-30 (trans ×1.25
  MaxHP). The original "STA trait HP" premise was a misread (rAthena MaxHP has no STA term).
