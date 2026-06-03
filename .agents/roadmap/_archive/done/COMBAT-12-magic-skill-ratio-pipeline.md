# COMBAT-12 — Magic skill ratio + constant pipeline (plugin `CalculateSkillRatio` for BF_MAGIC)

> **Epic:** Combat parity · **Status:** ✅ Done (2026-06-01) · **Size:** M · **Player-visible:** yes
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

- [x] **Route magic damage through the plugin ratio hook** — ✅ `CalcMagicDamage`
      now consults `_behaviors.Get(skillId)`: when the magic plugin overrides
      `CalculateSkillRatio`, that ratio REPLACES `skill_db.DamageRate` (applied
      once, no double-count) and its `CalculateSkillConstantAddition` is threaded
      as the new `CalcMagicAttack(..., constantAddition)` (rAthena ATK_ADD,
      applied after the ratio, before element/MDEF). A reflection gate
      (`OverridesMagicRatio`, cached per type) keeps the ~40 magic plugins that
      rely on `DamageRate` (don't override the ratio) on the fallback — no
      regression. This also revives `AL_HOLYLIGHT`'s `+25` ratio (same dead-hook).
- [x] **Fix `SoulStrike`** — ✅ each bolt now routes through
      `ctx.SkillAttack.SkillAttack(BF_MAGIC,…)` (its own documented rAthena call)
      instead of a raw MATK midpoint, so the overridden ratio (`+5*lv` vs undead)
      lands. A manual-ratio fallback covers rigs without the attack service.
      Added MG_SOULSTRIKE to the `SkillDb` fallback seed (was DB-only).
- [x] **Guard test** — ✅ `Combat12MagicRatioTests` asserts the plugin ratio is
      applied exactly once (equals a single `CalcMagicAttack(rate)`, ≠ the
      `rate×DamageRate` product) and that a non-overriding plugin keeps DamageRate.

## Done criteria

- ✅ Soul Strike lv10 vs an Undead target gets the `+5*lv` (ratio 150 vs 100) and
  none vs non-undead (`SoulStrike_UndeadTarget_*`, `_lv2_*`). *(Soul Strike's
  Ghost element is still resolved via the caster's weapon element — the per-skill
  magic element lookup is **COMBAT-19**, already filed; the `+5*lv` ratio this
  ticket owns is exact.)*
- ✅ A magic skill with a plugin ratio is not also multiplied by `DamageRate`
  (`MagicPluginRatio_AppliedOnce_NotMultipliedByDamageRate`).

## Test plan

- SoulStrike vs undead vs non-undead, fixed magic swing, assert the ratio delta.
- No-double-count guard for the magic path.

## Notes

- Mirror COMBAT-02's `FixedSwingBattle` test approach but via `CalcMagicAttack`.
- Multi-hit (Soul Strike's `(lv+1)/2` bolts) is orthogonal — keep it.

## History

- 2026-06-01 · Made the magic path honor the plugin skill-ratio. `CalcMagicDamage`
  now uses a magic plugin's `CalculateSkillRatio` (+ `CalculateSkillConstantAddition`)
  as the authority, replacing `skill_db.DamageRate` (gated by a cached reflection
  check so the ~40 DamageRate-reliant magic plugins are untouched); threaded an
  `ATK_ADD` constant param into `CalcMagicAttack`. Fixed SoulStrike to route each
  bolt through `SkillAttack(BF_MAGIC)` so its `+5*lv` vs-undead bonus (a dead hook)
  now lands; also revives AL_HOLYLIGHT's +25. Added MG_SOULSTRIKE to the SkillDb
  fallback seed. Combat12MagicRatioTests (4); unit suite 3736 green. Soul Strike's
  Ghost element still resolves via caster weapon element → COMBAT-19 (existing).
