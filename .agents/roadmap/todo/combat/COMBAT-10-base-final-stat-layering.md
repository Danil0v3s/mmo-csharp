# COMBAT-10 — Base→final stat layering (equip param bonuses + job bonus + SC stat mods)

> **Epic:** Combat parity · **Status:** ❌ Not started · **Size:** L · **Player-visible:** yes
> **Depends on:** none · **Blocks:** COMBAT-01 param-stat criteria, COMBAT-09 (SC recalc ordering)
> **Filed by:** COMBAT-01 on 2026-06-01 (see "Why this exists").

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

- [ ] **Introduce a base-stat store.** Add base allocated stats to
      `PlayerEntity` (e.g. `BaseStr..BaseCrt`, mirroring rAthena `status.str`),
      OR make `PcBaseInputs` the sole base source and have callers read base.
- [ ] **Populate base** at enter (`NotifyActorInitHandler` from `ch.*`), on stat
      allocation (`StatusChangeHandler` increments **base**), and on GM/script
      `setstat` (`PlayerLifecycleHelpers.SetParam` writes **base**).
- [ ] **Route all recalc-input builders to read base**, not `player.Stats`:
      `ExpService.PcInputsFromCurrent`, `StatusChangeHandler.BuildInputs`,
      `StatusOpsService.CalcPc`, `JobChangeService.BuildInputs`,
      `LevelCommand`, `JobLevelCommand`.
- [ ] **`CalcPc` layering**: `s.Str = base + equipParam(bundle.Str) +
      jobBonus(class,jobLv) [+ SC, via COMBAT-09]`, for all 12 stats, then
      derive misc/atk/matk from the totals.
- [ ] **Apply `EquipBonusBundle.Str..Crt`** (already captured by COMBAT-01) in
      that layering — removes the COMBAT-01 boundary test
      `CalcPc_doesNotYetApplyParamStats_boundaryForCombat10`.
- [ ] **Job-bonus stats**: read the per-job per-joblevel stat bonus table
      (`JobStats` catalog / `job_stats` seed) and add into the totals (overlaps
      COMBAT-09's "no job bonus stats").
- [ ] **SC re-application ordering**: coordinate with COMBAT-09 so SC stat mods
      (Blessing/AGI-Up/Provoke±) layer on the final without being wiped — likely
      CalcPc re-applies active SCF_* SCs at the end, and the SC start/end no
      longer mutate `Stats.<stat>` directly.
- [ ] **Client stat window**: confirm `ZC_STATUS`/`ZC_PAR_CHANGE` still send the
      base stat (+ the bonus shown separately) the way the client expects.

## Done criteria

- `bonus bStr,10;` raises displayed STR by 10 and raises `s.Batk` via the renewal
  BaseAtk formula; `bonus bDex,10;` raises HIT by ~10 and ATK by the dex term.
- A level-up / SC apply / job-change after equipping a STR card does **not**
  double-count (STR stays base+10, not base+20).
- Blessing (+STR/INT/DEX) applied, then any recalc → the buff is **preserved**.
- Job-bonus stats apply per job + job level.
- COMBAT-01's `CalcPc_doesNotYetApplyParamStats_boundaryForCombat10` is updated
  to assert STR == base + equip.

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
