# COMBAT-111 — Bespoke OnRecalc remainder: multi-field / +AspdRate / +primary / +trait / primary-coupled

> **Epic:** combat · **Status:** ❌ Not started · **Size:** L · **Player-visible:** yes
> **Depends on:** COMBAT-89 · **Blocks:** none
> **Filed by:** COMBAT-89 — it added OnRecalc to the 23 clean single-derived-field handlers; the
> multi-field and coupled groups remain (continuing the COMBAT-53 → 72 → 89 decomposition).

## Problem

`status_calc_pc_` re-folds every SCB_* each recalc; the remaining bespoke handlers still lack
`OnRecalc`, so their buff/debuff is wiped on the next CalcPc. COMBAT-89 finished the clean
single-derived-field batch; these groups need per-handler care:

1. **Multi-field (2+ reset-able derived fields)**: AquaplayOption (MatkMin/Max), Armorchange
   (Def/Mdef), BlastOption/ChillyAirOption/CoolerOption/Incmatkrate (MatkMin/Max),
   HeaterOption/PyrotechnicOption/TropicOption (WatkMin/Max), Bloodylust (Batk/Def/Def2), Fling
   (Def/Def2), Freeze/Neutralbarrier/Stone/StoneWall/Stonehardskin (Def/Mdef),
   Saturdaynightfever (Flee/Hit), Izayoi (Batk).
2. **+Trait stat** (CalcPc resets Patk/Smatk/Res/Mres too): DMachine (Def + Res),
   DragonicAura (Patk + Hit).
3. **+AspdRate** (re-apply the DERIVED part only; AspdRate is NOT reset): Fleet, Gloomyday,
   GoldeneFerse, HeatBarrel, Madnesscancel, Rushwindmill, Steelbody, Twohandquicken.
4. **+primary/pool** (re-apply the derived field only, NOT the primary): GtChange (Batk; +Agi),
   MoonComfort (Flee; +Dex/Luk), SunComfort (Def2; +Dex/Luk), Zangetsu (Batk; +Hp/Sp).
5. **+MaxHp** (derived axis here; MaxHp axis is COMBAT-73/90): Berserk, PowerOfGaia, Eqc,
   SolidSkinOption.
6. **Primary-coupled sub-class** (afterStart != recalc): Truesight (+5 all base stats), Curse
   (Luk=0) — reconcile primary-stat survival with the COMBAT-10 param-base delta before the
   derived OnRecalc.

(No-derived handlers — Cloaking, Flashcombo, Signumcrucis, Soul*, TelekinesisIntense,
WaterBarrier — need NO OnRecalc; they don't add to a reset-able stat.)

## Current state (C#)

- `Map.Server/Status/StatusEffectRegistry.cs` — the handlers above have `OnRecalc == null`.
- Reset-able fields (need OnRecalc): WatkMin/Max, MatkMin/Max, Hit, Flee, Flee2, Cri, Def, Def2,
  Mdef, Mdef2, Batk, **Patk, Smatk, Res, Mres** (CalcPc:148-151). NOT reset: AspdRate, primary, pools.

## rAthena reference (source of truth)

- `status.cpp status_calc_pc_` / `_batk` / `_def` / `_hit` / `_flee` / `_patk` / `_res` etc.

## Scope

- [ ] Add OnRecalc (re-apply reset-able derived/trait fields only) to each group above, in verifiable
      batches; verify each OnStart's exact field/val before editing (no blind bulk replace).
- [ ] Primary-coupled (Truesight/Curse): reconcile the primary-stat survival, then add the derived OnRecalc.

## Done criteria

- Every player-facing derived/trait-stat buff/debuff survives an equip/level recalc, idempotently.

## Test plan

- Extend Combat53BespokeRefoldTests rows per handler (multi-field + trait); primary-coupled facts.
