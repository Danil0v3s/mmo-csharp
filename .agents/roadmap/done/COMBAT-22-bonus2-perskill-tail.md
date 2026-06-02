# COMBAT-22 — bonus2 per-skill + indexed long tail (skillatk/skillheal/castrate/ignore-def/magic-add-race/vanish)

> **Epic:** Combat parity · **Status:** ✅ Done (2026-06-02) · **Size:** L · **Player-visible:** yes
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

- [x] Bundle maps: `SkillAtk` (skillId→%) + per-skill `SkillVarCastrate`/
      `SkillFixCastrate` (stored INVERSED like rAthena, data for COMBAT-24) added to
      `EquipBonusBundle` + `Reset()`. `MagicAddRace[]`/`IgnoreDefRace` already landed in
      COMBAT-21. ➡️ `SkillHeal` / `HpVanishRate` / `SubDefEle` / `AddRace2`/`SubRace2`
      moved to **COMBAT-44**.
- [x] Parse in `ApplyIndexed`: `bSkillAtk` / `bVariableCastrate` / `bFixedCastrate` with a
      reflection-built skill-name→id resolver (constant `MG_FIREBOLT`, quoted
      `"MG_FIREBOLT"`, or raw numeric id; unknown names skipped). Widened the bonus2 regex
      to accept the optional quotes + numeric idx. ➡️ the `bonus3`/4/5 static pass moved
      to **COMBAT-44**.
- [x] Wire `SkillAtk[id]`: weapon-skill lane in `WeaponSkillImpl.ComputeSkillDamage`
      (after ratio/constant) + magic lane in `BattleCalculator.CalcMagicAttack` (after
      MDEF, before cardfix). Per-skill cast rates → consumed by COMBAT-24; ignore-def →
      COMBAT-43; vanish → **COMBAT-44**.

## Done criteria

- `bonus2 bSkillAtk,MG_FIREBOLT,20;` adds 20% to Fire Bolt only ✅ (Cold Bolt unchanged);
  `bonus2 bMagicAddRace,RC_Demon,15;` adds 15% magic vs demons ✅ (COMBAT-21). ➡️
  ignore-def consumer (COMBAT-43) / vanish (COMBAT-44).

## Test plan

- Parser per code ✅ (name/quoted/numeric/unknown, inversed castrate); consumer per code
  ✅ (skill-scoped weapon + magic atk).

## History

- **2026-06-02** — inprogress→done. Per-skill `bSkillAtk` map (skillId→%) with a
  reflection-built skill-name resolver (name / quoted / numeric forms; bonus2 regex
  widened) feeds the weapon lane (`ComputeSkillDamage`) and magic lane (`CalcMagicAttack`)
  post-DEF; per-skill `bVariableCastrate`/`bFixedCastrate` stored inversed as data for
  COMBAT-24. `Combat22SkillAtkTests` (6); unit suite 3814 (1 fail = pre-existing INFRA-11
  replay gate). Filed COMBAT-44 (SkillHeal + HP/SP vanish + race2 + bonus3/4/5 + sub-skillatk).
