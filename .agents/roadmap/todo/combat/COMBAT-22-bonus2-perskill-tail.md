# COMBAT-22 — bonus2 per-skill + indexed long tail (skillatk/skillheal/castrate/ignore-def/magic-add-race/vanish)

> **Epic:** Combat parity · **Status:** ❌ Not started · **Size:** L · **Player-visible:** yes
> **Depends on:** COMBAT-06 · **Blocks:** none
> **Filed by:** COMBAT-06 (the bonus2 tail it scoped but didn't reach).

## Problem

`BonusScriptExtractor.ApplyIndexed` covers ~12 `bonus2` codes (add/sub race/ele/size/
class, drain, coma). The long tail is unparsed and unconsumed: `bSkillAtk` (per-skill
%), `bSkillHeal`/`bSkillHeal2`, per-skill `bVariableCastrate`/`bFixedCastrate`/
`bCastrate`, `bAddRace2`/`bSubRace2`, `bMagicAddRace`, `bIgnoreDefRate`/
`bIgnoreMdefRate` (race/class variants), `bSubDefEle`, `bHPVanishRate`/`bSPVanishRate`,
`bWeaponComaEle`, plus the general `bonus3`/`bonus4`/`bonus5` static-numeric forms.

## Current state (C#)

- `Map.Server/Inventory/BonusScriptExtractor.cs:ApplyIndexed` — the 12 covered codes.
- `Map.Server/Inventory/EquipBonusBundle.cs` — has Add/Sub arrays + Coma + AddEff; lacks
  `SkillAtk`/`SkillHeal` maps, per-skill cast-rate maps, `MagicAddRace[]`,
  `IgnoreDefRaceMask`, `SubDefEle[]`, `HpVanishRate`/`SpVanishRate`.

## rAthena reference

- `pc.cpp:4406` `pc_bonus2` arms; `pc.cpp:5048` `pc_bonus3`. Consumers in `battle.cpp`
  (skill_atk, magic_add_race, ignore_def, vanish), `skill.cpp` (per-skill cast rate),
  COMBAT-21's cardfix (sub_def_ele, magic_add_race).

## Scope

- [ ] Add bundle fields/maps: `SkillAtk` (skillId→%), `SkillHeal` (skillId→%), per-skill
      `Castrate`/`VarCastRate`/`FixCastRate` maps, `MagicAddRace[]`, `IgnoreDefRaceMask`,
      `SubDefEle[]`, `HpVanishRate`/`SpVanishRate`, `AddRace2`/`SubRace2`.
- [ ] Parse them in `ApplyIndexed` (+ a `bonus3` static regex for the 3-arg forms).
- [ ] Wire consumers: `SkillAtk[id]` in `WeaponSkillImpl`/magic calc; per-skill cast
      rates in COMBAT-07's timing; ignore-def/magic-add-race/sub-def-ele in COMBAT-21's
      cardfix; vanish on-hit in `DamageService`.

## Done criteria

- `bonus2 bSkillAtk,MG_FIREBOLT,20;` adds 20% to Fire Bolt only; `bonus2 bMagicAddRace,
  RC_Demon,15;` adds 15% magic vs demons; ignore-def / vanish each verified.

## Test plan

- Parser per code; consumer per code (skill-scoped atk, race-scoped magic, etc.).
