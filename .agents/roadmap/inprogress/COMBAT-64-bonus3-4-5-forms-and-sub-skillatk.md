# COMBAT-64 — bonus3/4/5 static forms + pc_sub_skillatk_bonus (defender reduction)

> **Epic:** Combat parity · **Status:** 🚧 In progress · **Size:** M · **Player-visible:** yes
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

- [ ] Add a `bonus3`/`bonus4`/`bonus5` static-numeric regex pass to the extractor for
      the common multi-arg shapes (start with the numeric ones; flag-form args map to
      enum constants).
- [ ] Add a defender `SubSkillAtk` (skillId→%) map + extractor parse + apply the
      reduction when the defender is hit by the matching skill.

## Done criteria

- ➡️ from COMBAT-44: bonus3 static forms parse; a defender's per-skill damage reduction
  lowers incoming damage from the matching skill.

## Test plan

- bonus3 parse; defender sub-skillatk reduces a matching skill's damage.
