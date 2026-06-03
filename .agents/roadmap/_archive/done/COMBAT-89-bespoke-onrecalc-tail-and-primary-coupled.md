# COMBAT-89 — Bespoke OnRecalc tail (~73 handlers) + primary-coupled sub-class

> **Epic:** Combat parity · **Status:** ✅ Done (2026-06-03) · **Size:** L · **Player-visible:** yes
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

- [x] Added `OnRecalc` to the **23 clean single-derived-field handlers** (Batk/Cri/Def/Def2/Flee/Hit,
      buffs + debuffs): Aurablade, Chattering, Bloodlust, Pyroclastic, ShieldspellAtk, Volcano,
      Explosionspirits, Laudaramus, Grooming, Hallucinationwalk, NpcHallucinationwalk, OveredBoost,
      Violentgale, Zephyr, MercHitup, Prestige, Illusiondoping, Laziness, Paralysis, Stripshield,
      Stripweapon, TinderBreaker, TinderBreaker2 — each re-applies its exact OnStart derived op.
- [ ] ➡️ The multi-field / +AspdRate / +primary/pool / +trait (Patk/Res) / +MaxHp / primary-coupled
      (Truesight/Curse) groups are **COMBAT-111** (each needs per-handler care beyond the clean batch).

## Done criteria

- ✅ The 23 clean single-derived-field buffs/debuffs survive an equip/level recalc idempotently
  (Combat53BespokeRefoldTests +23 rows). ➡️ The remaining ~50 handlers (multi-field/coupled) +
  the primary-coupled sub-class are **COMBAT-111**.

## Test plan

- Extend `Combat53BespokeRefoldTests` (survives + idempotent) per handler; add primary-coupled
  cases once that sub-class is handled.

## Notes / gotchas

- MaxHp/MaxSp-touching handlers (Berserk, PowerOfGaia, Eqc, SolidSkinOption) — the derived part is
  here, the MaxHp axis is COMBAT-73.
- Some types share near-identical bodies; verify each OnStart individually before batch-editing
  (a blind bulk replace is unsafe — the bodies differ in fields/vals).

## History

- 2026-06-03 — Added `OnRecalc` to the 23 clean single-derived-field bespoke handlers
  (Batk/Cri/Def/Def2/Flee/Hit; buffs + debuffs), each re-applying its exact OnStart op so the
  buff/debuff survives a CalcPc recalc idempotently (continuing COMBAT-53→72). Caught + fixed a
  duplicate Explosionspirits registration (last-wins shadowed the first edit; moved the OnRecalc to
  the active one). Combat53BespokeRefoldTests +23 rows (+ a Def2 reader case); full suite 4206 pass
  (1 fail = pre-existing INFRA-11 replay gate; the line-4389 TODO is pre-existing, not from this change).
  Filed COMBAT-111 for the multi-field / +AspdRate / +primary / +trait(Patk/Res) / +MaxHp /
  primary-coupled (Truesight/Curse) remainder.
