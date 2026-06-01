# COMBAT-06 — pc_bonus / bonus2 / bonus3 script coverage + consumers

> **Epic:** Combat parity · **Status:** 🚧 In progress · **Size:** XL · **Player-visible:** yes
> **Depends on:** COMBAT-01 (param/flat consumers), COMBAT-05 (defensive cardfix consumers)
> · **Blocks:** none

## Problem

The item-script bonus engine recognizes only ~29 of rAthena's ~300 `SP_*` bonus codes, and
several of the parsed ones have no consumer. A huge swath of card/equip effects therefore
silently do nothing: `bDef/bMdef/bDefRate`, `bAtkRate/bMatkRate`, `bSpeedRate`, `bHealPower`,
`bUnbreakable`, `bNoCastCancel`, and most `bonus2` codes (`bAddRace2`, `bMagicAddRace`,
`bIgnoreDefRate`, `bSkillAtk`, `bSkillHeal`, `bCastrate`, `bSubDefEle`, `bHPVanishRate`, …).
The 1-argument flag form (`bonus bNoCastCancel;`) isn't even parsed.

## Current state (C#)

- `Map.Server/Inventory/BonusScriptExtractor.cs:55-57` — `BonusFlat` regex requires
  `bonus bKey, N;` (a value). The **1-arg flag form** `bonus bNoCastCancel;` has no value and
  is not matched → dropped.
- `Map.Server/Inventory/BonusScriptExtractor.cs:99-125` — `ApplyFlat`: **17** keys
  (atk, matk, critical, hit, flee, aspd, aspdrate, maxhp, maxsp, maxhprate, maxsprate,
  longatkrate, shortatkrate, critatkrate, variablecastrate, fixedcastrate, delayrate).
  Missing: `str/agi/vit/int/dex/luk` (COMBAT-01), `def/def2/mdef/mdef2/defrate/mdefrate`,
  `atkrate/matkrate`, `speedrate/speed`, `healpower/healpower2`, `unbreakable*`,
  `nocastcancel`, `castrate`, `usesprate`, `hprecovrate/sprecovrate`, `addmaxweight`, etc.
- `Map.Server/Inventory/BonusScriptExtractor.cs:127-148` — `ApplyIndexed`: **12** keys
  (addrace, subrace, addele, subele, addsize, subsize, addclass, subclass, hpdrainrate,
  spdrainrate, comaclass, comarace). Missing the long `bonus2` tail: `addrace2/subrace2`,
  `magicaddrace/magicsubdefele`, `ignoredefrate/ignoremdefrate`, `skillatk/skillheal/
  skillheal2`, `castrate (per-skill)`, `subdefele`, `hpvanishrate/spvanishrate`,
  `weaponcomaele`, `addeff(2)`, `addeffwhenhit`, etc.
- `bonus3`/`bonus4`/`bonus5`: minimal — only `bAddEff`/`bAddEffWhenHit` land via TS hooks
  (`EquipBonusBundle.cs:104-105, 150`); no general 3/4/5-arg regex coverage.
- **Even parsed flat keys mostly have no consumer** (overlaps COMBAT-01): `FlatHit/FlatFlee/
  FlatCritical/FlatAtk/FlatMaxHp/MaxHpRate/FlatAspd` are written but `StatusCalcService.CalcPc`
  doesn't read them.

## rAthena reference (source of truth)

Canonical: `pc.cpp` switch functions (not split files).

- `pc.cpp:3644` `void pc_bonus(sd, type, val)` — the master `switch(type)` for 1-value bonuses.
  Confirmed arms: `SP_STR..SP_LUK` (`:3653`, `param_bonus[type-SP_STR] += val`),
  `SP_POW..SP_CRT`, `SP_MATK_RATE` (`:3932`, `sd->matk_rate += val`),
  `SP_ATK_RATE` (`:3957`, `sd->bonus.atk_rate += val`), `SP_MAGIC_ATK_DEF`,
  `SP_IGNORE_DEF_ELE/RACE/CLASS` (`:3936-3956`, set `right_weapon.ignore_def_* |= 1<<val`),
  `SP_DEF/SP_MDEF/SP_DEF_RATE/...`, `SP_SPEED_RATE`, `SP_HEAL_POWER`, `SP_UNBREAKABLE*`,
  `SP_NO_CAST_CANCEL` (flag form), etc. Many gate on `sd->state.lr_flag` (left/arrow).
- `pc.cpp:4406` `void pc_bonus2(sd, type, type2, val)` — 2-arg: `SP_ADDRACE2`,
  `SP_MAGIC_ADDRACE`, `SP_IGNORE_DEF_RATE_RACE`, `SP_SKILL_ATK`, `SP_SKILL_HEAL`,
  `SP_VARCASTRATE`/`SP_FIXCASTRATE` (per-skill), `SP_SUB_DEF_ELE`, `SP_HP_VANISH_RATE`, …
- `pc.cpp:5048` `void pc_bonus3(sd, type, type2, type3, val)` — 3-arg: `SP_ADDEFF`,
  `SP_ADD_DAMAGE_BY_CLASS`, etc. (permille rate semantics — `EquipBonusBundle.cs:150`).
- The consumers live across `battle.cpp` (cardfix, atk_rate, matk_rate, skill_atk,
  ignore_def), `status.cpp` (def/mdef/speed/heal/maxhp_rate folds in `status_calc_pc_`), and
  `skill.cpp` (per-skill castrate, nocastcancel).

## Scope — every sub-system that must be touched

- [ ] **Parse the 1-arg flag form.** Add a regex `bonus\s+b(?<key>[A-Za-z]+)\s*;` and route
      flag bonuses (`bNoCastCancel`, `bUnbreakableArmor`, `bIntravision`, …) to a new
      `ApplyFlag(bundle, key)` (set boolean fields on the bundle).
- [ ] **Expand `ApplyFlat`** to the full `pc_bonus` single-value table: at minimum
      `str/agi/vit/int/dex/luk` (COMBAT-01), `def/def2/mdef/mdef2/defrate/mdefrate`,
      `atkrate/matkrate`, `speedrate`, `healpower/healpower2`, `usesprate`, `hprecovrate/
      sprecovrate`, `criticalrate`, `addmaxweight`. Add fields to `EquipBonusBundle`.
- [ ] **Expand `ApplyIndexed`** to the `bonus2` tail listed above; add bundle arrays/maps:
      `MagicAddRace[]`, `IgnoreDefRaceMask`, `SubDefEle[]`, `HpVanishRate`, `SkillAtk` map
      (skillId→%), `SkillHeal` map, per-skill `Castrate`/`VarCastRate`/`FixCastRate` maps.
- [ ] **Add a `bonus3`/`bonus4`/`bonus5` regex** for the static-numeric forms and route the
      common codes (`bAddEff` already TS-handled; add `bAddDamageByClass`, `bSPDrainValRace`,
      etc.). Keep "miss not lie": unknown codes leave slots at 0.
- [ ] **Wire consumers** (the bulk of the work — a parsed bonus that nothing reads is still a
      no-op):
  - `atkrate` → `BattleCardService.CalcCardFix` or the renewal pre-ratio atk-percent
    (`battle.cpp:4604` `battle_get_atkpercent`).
  - `matkrate` → `BattleCalculator.CalcMagicAttack` (`:298`).
  - `def/mdef/defrate/mdefrate` → `StatusCalcService.CalcPc` (`s.Def/s.Mdef` post-equip).
  - `speedrate` → `StatusCalcService` speed (`s.Speed`, `:113`).
  - `healpower` → the heal skill plugins (Heal/Sanctuary) damage/heal calc.
  - `skillatk[skillId]` → applied in `WeaponSkillImpl.CastendDamageId` / magic skill calc.
  - `ignoredefrace/ignoredefele` → COMBAT-05 cardfix/def stage.
  - `subdefele` → COMBAT-05 defender cardfix.
  - `nocastcancel` → COMBAT-08 cast-interrupt gate.
  - per-skill `castrate`/`varcastrate`/`fixcastrate` → COMBAT-07 cast timing.
- [ ] **No DB migration** (item scripts are the source). The TS-hook path
      (`ScriptedBonusHost`) must also gain `ApplyFlag` / extended `ApplyFlat` routing so
      JS-translated scripts share the table (`BonusScriptExtractor.ApplyFlatBonus` is the
      shared entry, `:89`).

## Done criteria

- Count distinct `SP_*` codes that have **both** a parser case **and** a live consumer; target
  ≥ ~120 (the codes that matter for renewal PvE/PvP: param, def/mdef, atk/matk rate, skill
  atk, sub/add race/ele/size/class, ignore-def, heal power, cast rates, nocastcancel,
  hp/sp drain/vanish, addeff). Document the count in the PR.
- Representative checks: `bonus bAtkRate,10;` adds 10% to weapon damage; `bonus bMatkRate,10;`
  to magic; `bonus2 bSkillAtk,MG_FIREBOLT,20;` adds 20% to Fire Bolt only; `bonus bDefRate,-50;`
  halves def; `bonus bHealPower,30;` boosts Heal output; `bonus bNoCastCancel;` makes casts
  uninterruptible (with COMBAT-08).
- The 1-arg flag form parses (no longer dropped).

## Test plan

- Parser unit tests per new code: feed the script string, assert the bundle field.
- Consumer unit tests: each wired bonus changes its target number by the expected amount in
  isolation (atkrate→damage, defrate→def, skillatk→that skill only, healpower→heal amount).
- A coverage test that asserts the parser handles N representative real card scripts pulled
  from `item_db` without dropping recognized codes.
- Manual: equip a "+20% damage vs <skill>" or atk-rate card and observe the combat-log delta.

## Notes / gotchas

- This ticket is the umbrella for "make item scripts actually work"; COMBAT-01 and COMBAT-05
  are carve-outs (param stats; defensive cardfix). Implement those first, then this fills the
  long tail. Avoid re-doing their fields.
- Many `pc_bonus` arms gate on `sd->state.lr_flag` (left-hand weapon / arrow context). The
  regex extractor has no notion of left-hand context — most cards are armor/headgear (right /
  none), so default to the `LR_FLAG_NONE` behavior. Note left-hand weapon cards as a known
  approximation.
- Keep the regex "static numbers only" contract (`BonusScriptExtractor.cs:14-18`): dynamic
  `getrefine()*N`, `if(BaseLevel>…)` still belong to the TS-hook path, not the regex.
