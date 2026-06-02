# COMBAT-19 — Per-skill element resolution (magic/misc + endow overrides)

> **Epic:** Combat parity · **Status:** ✅ Done (2026-06-02) · **Size:** M · **Player-visible:** yes
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

- [x] Resolve magic/misc element from `def.Element`. Made the dead-stub
      `BattleElementService.GetMagic/GetMiscElement` real (inject `ISkillDb`); added the
      `ELE_WEAPON`/`ELE_ENDOWED`/`ELE_RANDOM` sentinels to `BattleElement` (12/13/14) +
      `SkillDbLoader.ParseElement`. Injected optional `IBattleElementService` into
      `BattleCalculator` (null → legacy weapon-element fallback).
      ➡️ The per-skill bespoke override switch (Psychic Wave/Adoramus+Ancilla/Hell
      Inferno/dragon-breath/spiritcharm/arrow-element songs) moved to **COMBAT-41**.
- [x] Use the resolved element in `ElementTable.GetRate` for magic (`CalcMagicAttack`)
      and misc (`CalcMiscAttack`) instead of `s.WeaponElement`.
- [x] Thread the resolved element into `CalcCardFix` via a new optional
      `BattleElement? attackElement` param (weapon path passes null → rh weapon element)
      so the defender `bSubEle` lookup uses the magic/misc element.

## Done criteria

- Fire Bolt resolves as Fire from skill_db regardless of weapon element ✅; vs Water the
  element table rate applies (Fire→Water 90%) ✅; `bSubEle,Fire` resists it (−20%) ✅.

## Test plan

- Element resolution: Fire Bolt vs Water/Fire/Neutral → table rates. ✅

## History

- **2026-06-02** — inprogress→done. Magic/misc now resolve the attack element from
  skill_db: `BattleElementService` (was a dead Neutral stub) ports
  `battle_get_magic/misc_element` (declared element + ELE_WEAPON/ENDOWED/RANDOM
  sentinels, new on `BattleElement` + the loader); injected into `BattleCalculator`
  (optional, legacy fallback when null) and used in `CalcMagicAttack`/`CalcMiscAttack`;
  the resolved element threads into `CalcCardFix` (new `attackElement` arg) for the
  defender `bSubEle` resist. `Combat19SkillElementTests` (9) green; unit suite 3788 (the
  1 fail is the pre-existing INFRA-11 replay gate). Filed COMBAT-41 (per-skill bespoke
  element overrides).
- Endow override: Fire Weapon makes a neutral-weapon swing resolve Fire.
