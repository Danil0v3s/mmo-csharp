# COMBAT-89 — Bespoke OnRecalc tail (~73 handlers) + primary-coupled sub-class

> **Epic:** Combat parity · **Status:** 🚧 In progress · **Size:** L · **Player-visible:** yes
> **Depends on:** COMBAT-72 (the verified batch + reaffirmed pattern) · **Blocks:** none
> **Filed by:** COMBAT-72 — it added `OnRecalc` to a 10-handler verified batch; the long tail and
> the primary-coupled handlers remain (continuing the COMBAT-53 → COMBAT-72 decomposition).

## Problem

`status_calc_pc_` re-folds every SCB_* contribution each recalc; the C# bespoke `Register`-ed
handlers must mirror that with `OnRecalc` (re-apply the snapshot to DERIVED fields only) or their
buff/debuff is wiped on the next `CalcPc` (equip/level/stat-alloc/job-change). COMBAT-53 did 7,
COMBAT-72 did 10 more (Humming/Fortune/Assumptio/Moonlitserenade/Whistle/Drumbattle/Impositio/
Echosong/Symphonyoflover/Adrenaline). **Two groups remain:**

1. **Pure-derived tail (~73)** — mechanical: re-apply the snapshot to the reset-able derived
   field(s), skipping AspdRate (NOT reset by CalcPc — re-applying double-counts) and primary
   stats. Each needs its OnStart inspected (which fields, which snapshot val).
2. **Primary-coupled handlers** (`Truesight` +5 all base stats; `Curse` Luk=0; etc.) — their
   PRIMARY-stat mutation interacts with the derived rebuild differently at start vs recalc, so a
   strict `afterStart == recalc` does not hold (COMBAT-53 reverted Truesight/Curse for this). They
   need the primary-stat survival reconciled with the COMBAT-10 param-base delta before the
   derived OnRecalc.

## Current state (C#)

- `Map.Server/Status/StatusEffectRegistry.cs` — the effective (last-`Register`) handler for ~73
  derived-stat types still has `OnRecalc == null`.
- Reset-able derived fields (need OnRecalc): WatkMin/Max, MatkMin/Max, Hit, Flee, Flee2, Cri,
  Def, Def2, Mdef, Mdef2, Batk. **NOT reset** (skip): AspdRate, primary stats. (Verified in
  `StatusCalcService.CalcPc`, COMBAT-72.)

## Remaining handlers (from COMBAT-72's affected list, minus the 17 done)

AquaplayOption, Armorchange, Aurablade, Berserk(+MaxHp→COMBAT-73), BlastOption, Bloodlust,
Bloodylust, Chattering, ChillyAirOption, Cloaking, CoolerOption, Curse\*, DMachine, DragonicAura,
Eqc(+MaxHp), Explosionspirits, Flashcombo, Fleet, Fling, Freeze, Gloomyday, GoldeneFerse,
Grooming, GtChange, Hallucinationwalk, HeatBarrel, HeaterOption, Illusiondoping, Incmatkrate,
Izayoi, Laudaramus, Laziness, Madnesscancel, MercHitup, MoonComfort, Neutralbarrier,
NpcHallucinationwalk, OveredBoost, Paralysis, PowerOfGaia(+MaxHp), Prestige, Pyroclastic,
PyrotechnicOption, Rushwindmill, Saturdaynightfever, ShieldspellAtk, Signumcrucis,
SolidSkinOption(+MaxHp), Soulenergy, Soulfairy, Soulfalcon, Soulgolem, Soulshadow, Steelbody,
Stone, StoneWall, Stonehardskin, Stripshield, Stripweapon, SunComfort, TelekinesisIntense,
TinderBreaker, TinderBreaker2, TropicOption, Truesight\*, Twohandquicken, Violentgale, Volcano,
WaterBarrier, WildWalk, Zangetsu, Zephyr.  (\* = primary-coupled.)

## rAthena reference (source of truth)

- `status.cpp` `status_calc_pc_` / `status_calc_batk` / `_def` / `_hit` / `_flee` etc.

## Scope — every sub-system that must be touched

- [ ] Add `OnRecalc` (snapshot → reset-able derived fields only) to every pure-derived handler
      above, in verifiable batches.
- [ ] For the primary-coupled handlers, reconcile primary-stat survival (COMBAT-10 delta) with the
      derived rebuild, then add the derived OnRecalc.

## Done criteria

- Every player-facing derived-stat buff/debuff survives an equip/level recalc, idempotently.
- Primary-coupled handlers: the derived fields depending on the primary stay consistent
  start↔recalc.

## Test plan

- Extend `Combat53BespokeRefoldTests` (survives + idempotent) per handler; add primary-coupled
  cases once that sub-class is handled.

## Notes / gotchas

- MaxHp/MaxSp-touching handlers (Berserk, PowerOfGaia, Eqc, SolidSkinOption) — the derived part is
  here, the MaxHp axis is COMBAT-73.
- Some types share near-identical bodies; verify each OnStart individually before batch-editing
  (a blind bulk replace is unsafe — the bodies differ in fields/vals).
