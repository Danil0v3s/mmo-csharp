# COMBAT-12 — Magic skill ratio + constant pipeline (plugin `CalculateSkillRatio` for BF_MAGIC)

> **Epic:** Combat parity · **Status:** 🚧 In progress · **Size:** M · **Player-visible:** yes
> **Depends on:** none · **Blocks:** none
> **Filed by:** COMBAT-02 on 2026-06-01 (the magic-side analog it scoped out).

## Problem

COMBAT-02 made the **weapon** skill-ratio pipeline authoritative (plugin
`CalculateSkillRatio` + the new `CalculateSkillConstantAddition`, applied in
`WeaponSkillImpl.CastendDamageId`). The **magic** path has the same two-source
problem and does NOT consult the plugin's ratio hook:

- `SkillAttackService.CalcMagicDamage` / `IBattleCalculator.CalcMagicAttack` take a
  `ratePerLevel` from `skill_db.DamageRate` and never call the plugin's
  `CalculateSkillRatio`.
- Magic plugins that override `CalculateSkillRatio` therefore have a **dead** hook.
  Concrete: `Map.Server/Skills/Behaviors/Mage/SoulStrike.cs` overrides
  `CalculateSkillRatio` to add `+5*lv` vs Undead (rAthena MG_SOULSTRIKE), but
  `CastendDamageId` resolves damage via `MagicBoltHelper.PerHitDamage` and never
  applies that ratio — the anti-undead bonus does nothing.

## Current state (C#)

- `Map.Server/Skills/SkillAttackService.cs` — `CalcMagicDamage` (the BF_MAGIC path)
  uses `skill_db.DamageRate[lvl]`, not the plugin ratio.
- `Map.Server/Combat/BattleCalculator.cs` — `CalcMagicAttack(…, ratePerLevel)`.
- `Map.Server/Skills/Behaviors/Mage/SoulStrike.cs` — dead `CalculateSkillRatio`.
- `Map.Server/Skills/Behaviors/Mage/MagicBoltHelper.cs` — per-bolt magic damage.

## rAthena reference

- `battle.cpp:4590` `battle_calc_attack_skill_ratio` is shared by BF_WEAPON **and**
  BF_MAGIC (it's keyed on skill_id, not attack type). MG_SOULSTRIKE arm + the
  `battle_check_undead` `+5*skill_lv` branch live there.
- `battle.cpp:6606` `battle_calc_skill_constant_addition` likewise applies to magic
  (e.g. GS_MAGICALBULLET pre-renewal).

## Scope

- [ ] Route magic-skill damage through the plugin ratio hook: when a magic plugin
      exists for `skillId`, `CalcMagicAttack` (or the plugin's `CastendDamageId`)
      must apply `CalculateSkillRatio(100, …)` and `CalculateSkillConstantAddition`,
      not just `skill_db.DamageRate`.
- [ ] Fix `SoulStrike`: the undead `+5*lv` must actually scale the bolt damage.
- [ ] Guard test: a magic plugin's ratio is applied once (no DamageRate double-count).

## Done criteria

- Soul Strike lv10 vs an Undead target deals the rAthena MG_SOULSTRIKE damage
  including the `+5*lv` bonus; vs non-undead, no bonus.
- A magic skill with a plugin ratio is not also multiplied by `DamageRate`.

## Test plan

- SoulStrike vs undead vs non-undead, fixed magic swing, assert the ratio delta.
- No-double-count guard for the magic path.

## Notes

- Mirror COMBAT-02's `FixedSwingBattle` test approach but via `CalcMagicAttack`.
- Multi-hit (Soul Strike's `(lv+1)/2` bolts) is orthogonal — keep it.
