# SKILL-19 — Spirit-ball skill-requirement consumption (Asura delspiritball + SpiritBallCost)

> **Epic:** Skill bodies · **Status:** ❌ Not started · **Size:** S · **Player-visible:** yes
> **Depends on:** none · **Blocks:** none
> **Filed by:** COMBAT-13 on 2026-06-02 (the spirit-sphere consume the ×2 ratio sits next to).

## Problem

`SkillRequirementService.ConsumeRequirement` only spends HP/SP/AP — it never
consumes spirit balls, even though `SkillDefinition.SpiritBallCost` /
`ISkillDb.GetSpiritBall` exist (and have **zero callers**). As a result skills
that require spheres don't pay them, and MO_EXTREMITYFIST (Asura Strike) does not
clear the caster's spheres after firing (rAthena `pc_delspiritball(sd, sd->spiritball, 0)`).

This is benign for COMBAT-13's ×2 ratio (the live `SpiritBall` is trivially the
pre-cast value because nothing consumes it), but it's a real parity gap: a Monk
keeps all spheres after Asura, and sphere-cost skills cast for free.

## Current state (C#)

- `Map.Server/Skills/SkillRequirementService.cs:80` `ConsumeRequirement` — HP/SP/AP
  only; items/zeny/ammo are noted as deferred; spirit balls not handled at all.
- `Map.Server/Skills/SkillDefinition.cs:173` `SpiritBallCost` + `SkillDb.GetSpiritBall`
  — defined, **no callers**.
- `Map.Server/Skills/Behaviors/Acolyte/AsuraStrike.cs` — does not clear `SpiritBall`.
- `Map.Server/Status/PlayerOrbService.cs` — `SetOrb(OrbKind.Spirit, …)` is the
  canonical sphere mutator; use it so the ZC_SPIRITS broadcast stays consistent.

## rAthena reference

- `skill.cpp` skill_consume_requirement → `pc_delspiritball` for `req.spiritball`.
- MO_EXTREMITYFIST cast: captures `sd->spiritball_old`, then on cast-end
  `pc_delspiritball(sd, sd->spiritball, 0)` (drops all spheres).

## Scope

- [ ] Wire `SpiritBallCost` into `ConsumeRequirement` (type & 1): remove
      `GetSpiritBall(skillId,lvl)` spheres via `IPlayerOrbService` so the wire
      broadcast fires; refund in `RefundRequirement`.
- [ ] Asura: clear all spheres on cast-end (`pc_delspiritball(sd, sd->spiritball, 0)`).
      Keep COMBAT-13's pre-cast `>5` read correct (capture before the consume, or
      read in CastendDamageId before clearing — Asura's clear happens post-damage).
- [ ] Confirm no double-consume with any plugin that already mutates spheres.

## Done criteria

- A skill with a SpiritBall requirement removes that many spheres on cast (and the
  client orb count updates); insufficient spheres fails the cast.
- After an Asura cast the Monk has 0 spirit spheres; the ×2 ratio (COMBAT-13) still
  keys off the pre-cast count.

## Test plan

- ConsumeRequirement removes N spheres for a sphere-cost skill; RefundRequirement
  restores them.
- Asura cast with 7 spheres → ×2 damage AND 0 spheres afterward.

## Notes

- COMBAT-13 reads the live `SpiritBall` as the pre-cast count precisely because no
  consume exists yet; once this lands, make sure the >5 capture happens before the
  Asura sphere-clear so the ratio doesn't regress.
