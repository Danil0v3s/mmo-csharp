# COMBAT-10 — Base→final stat layering (equip param bonuses + job bonus + SC stat mods)

> **Epic:** Combat parity · **Status:** ✅ Done (2026-06-01) · **Size:** L · **Player-visible:** yes
> **Depends on:** none · **Blocks:** COMBAT-01 param-stat criteria, COMBAT-09 (SC recalc ordering)
> **Filed by:** COMBAT-01 on 2026-06-01 (see "Why this exists").
>
> **Absorbed from COMBAT-09 (2026-06-01):** COMBAT-09 shipped the ASPD formula + MaxHP
> fold, but its axes 1 (SC stat re-fold ordering) and 2 (job-bonus stats) cannot be done
> without this ticket's base/final param split — the recalc-input builders read back the
> *conflated* `player.Stats.*`, so any additive job-bonus or SC fold double-counts on the
> next recalc. Both are already in this ticket's Scope (the job-bonus loader
> `IJobStatsCacheService.GetBonusSum` already exists — it just needs to be applied to the
> *base* total, not the conflated read-back). **This ticket now owns COMBAT-09's Done
> criteria 1 (AGI-Up survives recalc) and 2 (job-level-50 job-bonus stats).**

## Why this exists

COMBAT-01 made equip/card **flat-derived** bonuses (Hit/Flee/Cri/Batk/Matk/MaxHp/
MaxSp/Aspd) reach `CalcPc` idempotently. It could **not** safely apply the
**param-stat** bonuses (`bStr..bLuk`, `bPow..bCrt`) because of a missing
base/final stat separation:

- `BattleStats.Str` (`player.Stats.Str`) doubles as **both** the base allocated
  stat (written by `StatusChangeHandler` stat allocation) **and** the final
  battle stat. Every recalc caller reads `player.Stats.Str` back and feeds it to
  `CalcPc` (`ExpService.PcInputsFromCurrent`, `StatusChangeHandler.BuildInputs`,
  `StatusOpsService.CalcPc`, `JobChangeService.BuildInputs`, `LevelCommand`,
  `JobLevelCommand`, `NotifyActorInitHandler`).
- So folding `+bundle.Str` into `s.Str` would **double-count** on the next
  level-up / SC / job-change recalc.
- A naive base/final split would **wipe SC stat mods**: today Blessing's
  `target.Stats.Str += delta` (`StatusEffectRegistry.cs:118`) survives recalc
  *only because* base+SC are conflated in the read-back value. Feeding a true
  base (without SC) to `CalcPc` would erase the buff unless SCs are re-applied
  after the recalc — which is COMBAT-09's concern.

rAthena keeps these separate: `sd->status.str` (persisted base) vs
`sd->battle_status.str` (final), and `status_calc_pc_` rebuilds
`battle_status = base + param_bonus(card) + param_equip + job_bonus + SC`
(`status.cpp:4244-4266`). This ticket ports that layering.

## Current state (C#)

- `Map.Server/Entities/PlayerEntity.cs` — `Stats` (`BattleStats`) holds the only
  copy of Str..Luk/Pow..Crt; there is **no separate base store**.
- `Map.Server/Handlers/StatusChangeHandler.cs:39,63` — stat allocation writes
  the increment directly into `player.Stats.<stat>` (treating it as base).
- `Map.Server/Status/PlayerLifecycleHelpers.cs:73-78` — GM/script `setstat`
  writes `pc.Stats.<stat>` absolute.
- `Map.Server/Status/StatusEffectRegistry.cs:118-135` — SC stat mods do
  `Stats.Str += delta` / `-= delta` on start/end (Blessing, Increase AGI, etc.).
- `Map.Server/Status/StatusCalcService.cs:43-54` — `CalcPc` copies `inputs.Str`
  → `s.Str` (no equip/job/SC param layering).
- `Map.Server/Inventory/EquipBonusBundle.cs` — `Str..Crt` param fields **are
  already captured** by the extractor (COMBAT-01); just not applied.
- The 7 recalc-input builders listed above all read `player.Stats.<stat>`.

## rAthena reference

- `pc.cpp pc_bonus` `SP_STR..SP_LUK` → `indexed_bonus.param_bonus[]`.
- `status.cpp:4044-4045` snapshots `param_bonus` → `param_equip`, zeroes the
  card slot for the equip pass.
- `status.cpp:4244-4266` final stat = `base_status->str + status.str +
  param_bonus[STR] + param_equip[STR]`; then derives misc/atk/matk from it.
- job-bonus stats: `status.cpp status_calc_pc_` adds `job_bonus[class][joblevel]`
  per stat (the `+1 STR at job lv N` tables) — out of `JobStats` catalog.
- SC stat mods are re-applied each `status_calc_pc_` via the SCB_* flags
  (the COMBAT-09 ordering).

## Scope — every sub-system that must be touched

- [x] **Base-stat store** — ✅ new `PcBaseParams` (12 base allocated/trait stats,
      rAthena `status.str`..`crt`) + `PlayerEntity.BaseParams`; plus an internal
      `PlayerEntity.AppliedParamBase` snapshot + `ShiftFinalParam` helper.
- [x] **Populate base** — ✅ `NotifyActorInitHandler` hydrates `BaseParams` from
      `ch.*` at enter; `StatusChangeHandler` allocates into `BaseParams`;
      `PlayerLifecycleHelpers.SetParam` (GM setstat) + `TraitStatusUp` write base
      via `ShiftFinalParam`.
- [x] **Recalc-input builders read base** — ✅ `ExpService.PcInputsFromCurrent`,
      `StatusChangeHandler.BuildInputs`, `StatusOpsService.CalcPc` (also fixed its
      latent Pow..Crt=0 bug), `JobChangeService.BuildInputs`, `LevelCommand`,
      `JobLevelCommand`, `EquipService` all read `BaseParams` + thread JobId/WeaponType.
- [x] **`CalcPc` layering** — ✅ `s[i] = base + equipParam(bundle) + jobBonus(class,jobLv)`
      for all 12 stats via a delta-vs-snapshot fold; misc/atk/matk/maxhp/aspd then
      derive from the final (post-fold) stats (status.cpp:4205-4266).
- [x] **Apply `EquipBonusBundle.Str..Crt`** — ✅ folded in the layering; COMBAT-01
      boundary test now asserts STR == base + equip.
- [x] **Job-bonus stats** — ✅ `IJobStatsCacheService.GetBonusSum(aegis, jobLevel)`
      added per stat (job_bonus_stats_db).
- [x] **SC re-application (primary)** — ✅ the param-base delta snapshot preserves
      primary-stat SC mods (Blessing/AGI-Up) across recalc with no SC-handler
      change. Derived-stat SC re-fold (Angelus Def2, Provoke Batk%) ➡️ **COMBAT-33**.
- [x] **Client stat window** — ✅ stat-alloc `ZC_PAR_CHANGE` now sends the BASE
      value; `ReadParam(SP_STR..LUK)` returns base (rAthena `pc_readparam`).

## Done criteria

- ✅ `bonus bStr,10;` → STR base+10 + `s.Batk` rises (renewal BaseAtk); `bonus bDex,10;`
  → HIT +10 + ATK dex term (`Combat10BaseFinalLayeringTests.EquipParam_*`).
- ✅ Level-up / SC / job-change / card-swap after a STR card does **not** double-count
  (`EquipParam_isIdempotent_*`, `_changingCard_appliesDeltaNotSum`).
- ✅ Blessing-style (+STR) applied, then any recalc → buff preserved, coexists with
  equip param, reverts cleanly (`ScStatMod_survivesRecalc`).
- ✅ Job-bonus stats apply per job + job level (`JobBonus_appliesPerJobAndLevel`).
- ✅ COMBAT-01 boundary test updated → STR == base + equip (`CalcPc_appliesEquipParamStats_combat10`).
- ✅ Novice-Lv1 no-gear baseline unchanged (`StatusCalcServiceTests.CalcPc_NoviceLv1_MatchesCaptureBaseline`).

### Out of scope (filed as follow-ups)

- ➡️ **COMBAT-32** — passive-skill absolute base addends (HILTBINDING/OWL/DRAGONOLOGY/
  RESEARCHTRAP/POWEROFLAND) + Super Novice all-stat +10 (status.cpp:4221-4242).
- ➡️ **COMBAT-33** — derived-stat SC re-fold on recalc (Angelus Def2, Provoke Batk%);
  primary-stat SCs already preserved here.
- ➡️ **COMBAT-31** — pre-existing DI cycle (DamageService↔ExpService↔StatusChangeService)
  blocks Map.Server boot + the `PacketReplayTests` integration harness; reproduces on a
  clean tree (SC-04 edge), unrelated to this ticket. The Novice-baseline regression is
  independently guarded by the passing `StatusCalcServiceTests` unit test.

## Test plan

- Unit: equip-param STR fold + idempotency across repeated `CalcPc`.
- Unit: SC stat mod survives a recalc (Blessing then equip-recalc).
- Unit: job-bonus stat applied for a known (job, jobLevel).
- Regression: the full Novice-Lv1 baseline with no gear is unchanged.

## Notes / gotchas

- This is the shared prerequisite for **COMBAT-09** (SC recalc ordering) — do
  them together or land this first. The `param_bonus` vs `param_equip` split in
  rAthena exists only because SC_CONCENTRATE reads the card-only slot
  (`status.cpp:4751-4752`); a single combined base-delta is fine until that SC
  ports.
- With no gear/SC the totals must equal today's values so the replay baseline +
  `StatusCalcServiceTests` stay green.

## History

- 2026-06-01 · Ported the rAthena base→final param layering (status.cpp:4205-4266).
  New PcBaseParams (persisted base allocated/trait stats) on PlayerEntity; CalcPc now
  folds `base + equip param (EquipBonusBundle) + job bonus (GetBonusSum)` into the 12
  primary/trait stats via a delta-vs-snapshot (AppliedParamBase) so it's idempotent
  AND primary-stat SC mods (Blessing/AGI-Up) survive recalc; misc/atk/matk/maxhp/aspd
  now derive from the final (post-fold) stats. All 7 recalc-input builders read
  BaseParams (fixing StatusOpsService's Pow..Crt=0 wipe); enter/alloc/setstat/trait-up
  write base; ZC_PAR_CHANGE + ReadParam return base. Combat10BaseFinalLayeringTests (7)
  + updated COMBAT-01 boundary test; unit suite 3732 green (replay integration harness
  blocked by the pre-existing COMBAT-31 DI cycle, unrelated). Filed COMBAT-31 (DI cycle),
  COMBAT-32 (passive-skill base addends + SuperNovice +10), COMBAT-33 (derived-stat SC re-fold).
