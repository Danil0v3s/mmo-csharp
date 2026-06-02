# COMBAT-47 — Land Protector place-gate + skill-path ground-unit intercept

> **Epic:** Combat parity · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** COMBAT-25 (Safety Wall/Pneuma block + the SkillUnitGroup pool)
> **Blocks:** none
> **Filed by:** COMBAT-25 — the Land Protector gate (needs UF_NOLP) + the skill-attack intercept.

## Problem

COMBAT-25 wired the Safety Wall (melee) + Pneuma (ranged) blocks into the AUTO-ATTACK
path (`DamageService.PerformMeleeAttack`). Two pieces remain:

1. **Land Protector place-gate.** A hostile ground-unit *skill* (Storm Gust, Meteor,
   traps) must not place/tick on a `SA_LANDPROTECTOR` cell. This needs the rAthena
   `UF_NOLP` unit-flag (skills WITH it ignore Land Protector) which is not modeled on
   `SkillUnitFlag` / loaded into `SkillDefinition.UnitFlags`.
2. **Skill-path intercept.** Safety Wall / Pneuma only block the auto-attack swing today.
   A melee/ranged SKILL (e.g. Bash on a Safety Wall cell) is not intercepted because the
   skill damage funnel (`SkillAttackService` → `ApplyDamage`) carries no BF_SHORT/BF_LONG
   lane.

## Current state (C#)

- `Map.Server/Combat/DamageService.cs:TryGroundUnitBlock` — called only from
  `PerformMeleeAttack`; consumes the Safety Wall `group.Val2` pool.
- `Map.Server/Skills/SkillDefinition.cs:SkillUnitFlag` — no `NoLandProtector` member.
- `Map.Server/Skills/SkillUnitService.cs:Place` — no Land Protector cell gate (Safety
  Wall's own behavior checks LP overlap, but the general gate is missing).

## rAthena reference (source of truth)

- `skill.cpp` `skill_unitsetting` — refuses placement on a Land Protector cell unless the
  skill has `UF_NOLP`; `battle_calc_damage` MG_SAFETYWALL/AL_PNEUMA for skills.

## Scope — every sub-system that must be touched

- [ ] Add `NoLandProtector` (UF_NOLP) to `SkillUnitFlag` + load it into
      `SkillDefinition.UnitFlags` from skill_db.
- [ ] `SkillUnitService.Place`: when a `SA_LANDPROTECTOR` unit covers the center cell and
      the placed skill lacks `UF_NOLP`, refuse (return null) — refund handled by the caller.
- [ ] Thread the skill's BF_SHORT/BF_LONG lane into the skill damage funnel so
      `TryGroundUnitBlock` runs for melee/ranged SKILLS too (Safety Wall blocks a melee
      skill; Pneuma blocks a ranged skill).

## Done criteria

- ➡️ from COMBAT-25: a hostile ground-unit skill cannot place/tick on a Land Protector cell.
- A melee skill on a Safety Wall cell is blocked + consumes the pool; a ranged skill on a
  Pneuma cell is blocked.

## Test plan

- `Combat47LandProtectorTests`: place LP, attempt a hostile ground unit on its cell → refused;
  a UF_NOLP skill places fine; a melee skill vs Safety Wall → blocked.

## Notes / gotchas

- Keep Safety Wall's block-pool model (group.Val2) consistent between the auto-attack and
  skill paths — both decrement the same pool.
