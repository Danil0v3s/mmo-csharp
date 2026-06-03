# COMBAT-102 — bSetDefRace / bSetMDefRace (set target DEF/MDEF vs race proc)

> **Epic:** combat · **Status:** ❌ Not started · **Size:** S · **Player-visible:** yes
> **Depends on:** COMBAT-83 · **Blocks:** none · **Filed by:** COMBAT-83.

## Problem

`bonus4 bSetDefRace, r, rate, val` / `bSetMDefRace`: when attacking race r, with `rate`/100 chance,
treat the target's DEF (or MDEF) as `val` for that hit (rAthena `right_weapon.def_ratio_atk`-adjacent
set-def). The live host skips it; the C# DEF/MDEF reduction uses the raw target DEF.

## Current state (C#)

- `Map.Server/Inventory/EquipBonusBundle.cs` — no SetDefRace/SetMDefRace list.
- `Map.Server/Combat/BattleCalculator.cs:ComputeHandDamage` — uses raw `t.Def`; `CalcMagicAttack`
  raw MDEF.

## rAthena reference (source of truth)

- `pc.cpp` SP_SET_DEF_RACE / SP_SET_MDEF_RACE arms; `battle.cpp` the def-override consumer.

## Scope

- [ ] Add the per-race set-def/set-mdef list (race, rate, value) to the bundle + parse the bonus4 forms.
- [ ] In ComputeHandDamage (DEF) / CalcMagicAttack (MDEF), when attacking the matching race and the
      `rate` roll fires, override def/mdef to `val` before the reduction.

## Done criteria

- A bSetDefRace card with rate 1000 sets a matching-race target's DEF to `val` for the hit.

## Test plan

- Guaranteed-rate set-def override vs a matching-race target.
