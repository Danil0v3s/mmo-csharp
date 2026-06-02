# COMBAT-64 — bonus3/4/5 static forms + pc_sub_skillatk_bonus (defender reduction)

> **Epic:** Combat parity · **Status:** ✅ Done (2026-06-03) · **Size:** M · **Player-visible:** yes
> **Depends on:** COMBAT-44
> **Blocks:** none
> **Filed by:** COMBAT-44 — the bonus-tail pieces beyond SkillHeal + vanish.

## Problem

COMBAT-44 landed the per-skill `bSkillHeal` and the on-hit HP/SP vanish consumer.
Two bonus-tail pieces remain:

1. **General `bonus3`/`bonus4`/`bonus5` static-numeric forms.** The extractor's
   `Bonus2Indexed` regex only captures the 2-arg shape; the 3/4/5-arg shapes
   (`bonus3 bAddEff,Eff_Stun,500,ATF_SHORT;`, etc.) are unparsed.
2. **`pc_sub_skillatk_bonus`** — the DEFENDER's per-skill incoming-damage reduction
   (rAthena `bonus2 bSubSkill,sk,n` applied when the defender is hit by skill `sk`).

(race2 `bAddRace2`/`bSubRace2` + `bSubDefEle` are tracked on COMBAT-63.)

## Current state (C#)

- `Map.Server/Inventory/BonusScriptExtractor.cs` — `bonus` / `bonus2` only; no bonus3/4/5
  pass.
- `EquipBonusBundle` — `SkillAtk` (offensive per-skill) exists; no defender-side
  `SubSkillAtk` map.
- `WeaponSkillImpl.ComputeSkillDamage` / `CalcMagicAttack` apply `SkillAtk` post-DEF;
  no defender-side reduction.

## rAthena reference

- `pc.cpp` bonus3/4/5 SP_* parses; `battle.cpp pc_sub_skillatk_bonus` (defender
  per-skill reduction).

## Scope

- [x] bonus3/4/5 forms. **Re-scoped post-CONV-5:** the regex `BonusScriptExtractor` is retired
      — the live path is `ScriptedBonusHost.bonus3/4/5`, which already handles the major forms
      (`bAutoSpell{,WhenHit}` → autobonus, `bAddEff{,2,WhenHit}` → on-hit procs,
      `bAutoSpellOnSkill`). Rounded out the AddEff family by adding the 4-arg
      `bonus4 bAddEff/bAddEffWhenHit` (explicit duration `t`) to the host. The flat remainder
      (drops / vanish-race+flag / SetDefRace / StateNoRecover / AddEffOnSkill) each needs a
      separate subsystem. ➡️ Moved to COMBAT-83 (flag-matched AddEle/SubEle/SubRace also → COMBAT-82).
- [x] Defender `SubSkillAtk` (skillId→%) map + `bSubSkill` parse (shared `ApplyIndexedBonus`,
      so both the live host and the legacy regex get it) + apply `-n%` when the defender is hit
      by the matching skill, in the weapon path (`SkillImpl.ComputeSkillDamage`) and the magic
      path (`CalcMagicAttack`) — symmetric to the offensive `SkillAtk` (rAthena
      `pc_sub_skillatk_bonus`, battle.cpp:7873 `ATK_ADDRATE(-i)`).

## Done criteria

- ➡️ from COMBAT-44: bonus3 forms parse (live host: autospell/addeff; bonus4 AddEff added) ✅;
  a defender's per-skill damage reduction lowers incoming damage from the matching skill ✅.
  The flat bonus3/4/5 remainder ➡️ COMBAT-83.

## Test plan

- bSubSkill parse; defender sub-skillatk reduces a matching weapon + magic skill (and stacks
  with offensive SkillAtk); bonus4 bAddEff records a proc with duration. ✅
  Combat64SubSkillAndBonus4Tests (6).

## History

- 2026-06-03 · Shipped the defender per-skill reduction + rounded out the AddEff family.
  Added `EquipBonusBundle.SubSkillAtk` + `case "subskill"` in the shared `ApplyIndexedBonus`,
  and applied `-n%` in `SkillImpl.ComputeSkillDamage` (weapon) + `CalcMagicAttack` (magic),
  symmetric to the offensive `SkillAtk` (battle.cpp:7873). Added `bonus4 bAddEff/bAddEffWhenHit`
  (explicit duration) to the live `ScriptedBonusHost` — discovered the regex extractor is
  retired (CONV-5) and the host already covers the major bonus3/4/5 forms. Combat64SubSkill
  AndBonus4Tests (6); combat+inventory+skills suite 3178 green, full suite 4059 pass (1 fail =
  pre-existing INFRA-11 replay gate). Filed COMBAT-83 (flat bonus3/4/5 remainder — drops /
  vanish-race+flag / SetDefRace / StateNoRecover / AddEffOnSkill).
