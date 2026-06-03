# COMBAT-44 — bonus tail: SkillHeal, HP/SP vanish, race2, bonus3/4/5 forms

> **Epic:** Combat parity · **Status:** ✅ Done (2026-06-02) · **Size:** M · **Player-visible:** yes
> **Depends on:** COMBAT-22 (per-skill maps + skill-name resolver), COMBAT-43 (race2/subdefele)
> **Blocks:** none
> **Filed by:** COMBAT-22 — the bonus2/bonus3 tail it did not reach.

## Problem

COMBAT-22 delivered the per-skill `bSkillAtk` map (+ the per-skill cast-rate maps as data
for COMBAT-24) and the skill-name → id resolver. The rest of the bonus2/bonus3 long tail
is still unparsed/unconsumed:

1. **bSkillHeal / bSkillHeal2** — per-skill heal % (caster / receiver).
2. **bHPVanishRate / bSPVanishRate** — `bonus2` (rate, per) on-hit vanish of the target's
   max HP/SP; `bonus3` adds a BF flag. Needs an on-hit consumer in `DamageService`.
3. **bAddRace2 / bSubRace2** — race2 classification (overlaps COMBAT-43).
4. **bSubDefEle** — defender magic_subdefele (overlaps COMBAT-43).
5. **General bonus3/bonus4/bonus5 static-numeric forms** — a regex pass for the 3/4/5-arg
   shapes the current extractor doesn't capture.
6. **pc_sub_skillatk_bonus** — the DEFENDER's per-skill incoming-damage reduction.

## Current state (C#)

- `Map.Server/Inventory/BonusScriptExtractor.cs` — parses bonus / bonus2 (incl. the new
  per-skill `bSkillAtk` / `bVariableCastrate` / `bFixedCastrate`); no bonus3/4/5 pass, no
  vanish, no skillheal.
- `EquipBonusBundle` — has `SkillAtk` + the cast-rate maps; lacks `SkillHeal`, vanish
  fields, race2/subdefele.

## rAthena reference (source of truth)

- `pc.cpp:4638` SP_SKILL_HEAL; `:4566/4571` SP_SP/HP_VANISH_RATE (bonus2);
  `:5128-5143` bonus3 vanish-by-race; `battle.cpp` vanish consumer.

## Scope — every sub-system that must be touched

- [x] `SkillHeal` (skillId→%) map on `EquipBonusBundle` + extractor `skillheal` case +
      consumer in the renewal heal formula (`Heal.CalcRenewalHeal`, keyed on `SkillId`).
- [x] HP/SP vanish: bundle (`HpVanishRate`/`HpVanishPer` + SP) + the `bonus2
      bHPVanishRate,rate,per` / `bSPVanishRate` parse + the on-hit consumer in
      `DamageService.PerformMeleeAttack` (`ApplyVanish`: roll rate in 1/1000 units,
      drain per% of the target's max HP/SP).
- [x] `bonus3`/`bonus4`/`bonus5` static-numeric pass + `pc_sub_skillatk_bonus`
      (defender reduction) ➡️ COMBAT-64. race2 (`AddRace2`/`SubRace2`) + `SubDefEle`
      ➡️ COMBAT-63.

## Done criteria

- ➡️ from COMBAT-22: a vanish card drains HP/SP on hit at its rate ✅; `bonus2 bSkillHeal,
  AL_HEAL,10` increases Heal by 10% ✅.
- bonus3 static forms parse ➡️ COMBAT-64; race2 cards apply ➡️ COMBAT-63.

## Test plan

- `Combat44BonusTailTests`: skillheal scoped to a skill; vanish roll + drain; bonus3 parse.

## Notes / gotchas

- bMagicAddRace + bIgnoreDefRace are already in the bundle (COMBAT-21); the ignore-def
  DEF-stage consumer is COMBAT-43. Avoid duplicating those.

## History

- 2026-06-02 · Added the per-skill `SkillHeal` map + on-hit HP/SP vanish to
  `EquipBonusBundle`; the extractor parses `bonus2 bSkillHeal,sk,n` and the vanish
  `bonus2 bHPVanishRate,rate,per` / `bSPVanishRate` forms. `Heal.CalcRenewalHeal`
  applies `SkillHeal[SkillId]`; `DamageService.PerformMeleeAttack` rolls the vanish
  rate (1/1000 units, ≥1000 = guaranteed) and drains per% of the target's max HP/SP
  on a landed weapon hit. Combat44BonusTailTests (5: skillheal + vanish parse,
  SkillHeal +10% heal, HP-vanish drain, no-proc at rate 0). Full Map.Server.Tests
  green except the pre-existing INFRA-11 replay gate. Filed COMBAT-64 (bonus3/4/5
  static forms + pc_sub_skillatk_bonus); race2/SubDefEle ride COMBAT-63.
