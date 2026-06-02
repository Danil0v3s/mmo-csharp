# COMBAT-19 — Per-skill element resolution (magic/misc + endow overrides)

> **Epic:** Combat parity · **Status:** 🚧 In progress · **Size:** M · **Player-visible:** yes
> **Depends on:** none · **Blocks:** none
> **Filed by:** COMBAT-05 (axis 4).

## Problem

Magic and misc damage use the *caster's weapon element* instead of the skill's
declared element. A Fire Bolt cast with a neutral weapon resolves as Neutral vs the
target's element table — wrong. rAthena `battle_calc_element_damage` (battle.cpp:3781)
resolves the attack element from `skill_get_ele(skill_id, lv)`, the weapon element,
and SC overrides (endow / Pyroclastic). The defender-side `bSubEle` lookup added in
COMBAT-05 also uses the rh weapon element for magic/misc and inherits this gap.

## Current state (C#)

- `Map.Server/Combat/BattleCalculator.cs:CalcMagicAttack` / `CalcMiscAttack` — element
  = `s.WeaponElement` (comments admit the per-skill lookup "lands later").
- `Map.Server/Skills/SkillDefinition.cs` — `Element` IS loaded (the skill_db column is
  parsed), but `BattleCalculator` has no `ISkillDb` to read it from `skillId`.
- `Map.Server/Combat/BattleCardService.cs:CalcCardFix` — defender `SubEle` uses
  `ss.WeaponElement` (correct for weapon; approximate for magic/misc until this lands).

## rAthena reference

- `battle.cpp:3781` `battle_calc_element_damage` + `skill_get_ele`.
- Endow SCs (Fire/Water/Wind/Earth weapon, Aspersio, etc.) and Pyroclastic override
  the resolved element (SC-02/SC-11 set `Stats.WeaponElement` for the weapon endows).

## Scope

- [ ] Inject `ISkillDb` (optional) into `BattleCalculator`; resolve magic/misc element
      from `def.Element` (with `ELE_WEAPON` → weapon element, `ELE_ENDOWED` → endow SC,
      `ELE_RANDOM` → random).
- [ ] Use the resolved element in `ElementTable.GetRate` for magic/misc instead of
      `s.WeaponElement`.
- [ ] Thread the resolved attack element into `CalcCardFix` so the defender `SubEle`
      lookup matches (extend the signature or pass via a small context).

## Done criteria

- Fire Bolt resolves as Fire from skill_db regardless of weapon element; vs Water/Fire/
  Neutral defenders the element table rates apply; `bSubEle,Fire` resists it.

## Test plan

- Element resolution: Fire Bolt vs Water/Fire/Neutral → table rates.
- Endow override: Fire Weapon makes a neutral-weapon swing resolve Fire.
