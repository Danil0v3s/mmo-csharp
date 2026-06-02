# COMBAT-80 — can-hit GvG/BG gate (guardian/Emperium/immune) + Emperium GvG branch

> **Epic:** Combat parity · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** COMBAT-62 · FEATURE-15 (WoE — guardian/Emperium entities must be spawnable)
> **Blocks:** none
> **Filed by:** COMBAT-62 — the `battle_can_hit_gvg/bg_target` → 0 gate and the Emperium
> `battle_calc_attack_plant` GvG branch. Both are entirely WoE-entity-gated and cannot fire
> until guardians/Emperium exist, so they are split out of COMBAT-62 (which shipped the
> INF2 ignore-reduction bypass + PK rate).

## Problem

rAthena's `battle_can_hit_gvg_target` / `battle_can_hit_bg_target` (battle.cpp:2082 / 2051)
return false — the target takes **0** — for: a unit with `ud->immune_attack`; a guardian /
`AI_GUILD` mob that is `MD_SKILLIMMUNE` (or an Emperium without `INF2_TARGETEMPERIUM`) on a
BF_SKILL hit; an Emperium when the attacker's guild lacks `GD_APPROVAL`, has hit the castle
cap, or owns the castle. The Emperium also has its own `battle_calc_attack_plant` GvG branch
(battle.cpp:7104-7118). None of this is modeled.

## Current state (C#)

- `Map.Server/Combat/ZoneDamageService.cs:Scale` — applies the GvG/BG rate + INF2 bypass + PK
  rate (COMBAT-62), but has **no can-hit gate** (always lets the hit through) and no Emperium
  branch.
- `Map.Server/Entities/MobEntity.cs` — has no `guardian_data` / `AI_GUILD` / `immune_attack`
  flag, and there is no Emperium mob. The gate's trigger conditions don't exist in the entity
  model yet.
- `Map.Server/Skills/SkillDefinition.cs:SkillInf2` — has no `TargetEmperium` flag.

## rAthena reference

- `battle.cpp:2051` `battle_can_hit_bg_target`, `battle.cpp:2082` `battle_can_hit_gvg_target`
  (the guardian/`AI_GUILD`/`MD_SKILLIMMUNE`/Emperium/guild-castle/`immune_attack` branches).
- `battle.cpp:7104-7118` Emperium `battle_calc_attack_plant` GvG branch
  (`battle_can_hit_gvg_target` + `battle_calc_gvg_damage`).
- `INF2_TARGETEMPERIUM` (skill.hpp) — skills allowed to hit the Emperium.

## Scope

- [ ] Add the `guardian`/`AI_GUILD` distinction + `immune_attack` flag to the mob entity
      model (lands with FEATURE-15's guardian/Emperium spawn).
- [ ] Add `TargetEmperium` to `SkillInf2` + seed it for the rAthena `INF2_TARGETEMPERIUM`
      skills (curated set, same mechanism as COMBAT-62's ignore-reduction overlay).
- [ ] Implement `CanHitZoneTarget(src, target, skillId, isSkill)` in `ZoneDamageService` and
      gate `Scale` → return 0 when it fails (both GvG and BG).
- [ ] Emperium GvG branch in the plant path (`battle_calc_attack_plant`) — coordinate with
      FEATURE-15.

## Done criteria

- ➡️ from COMBAT-62: a can't-hit GvG target (guardian `MD_SKILLIMMUNE` on a skill / Emperium
  without `INF2_TARGETEMPERIUM` / `immune_attack` unit) takes 0; an Emperium uses the GvG
  branch.

## Test plan

- can't-hit guardian/Emperium → 0 (once the entities exist); `INF2_TARGETEMPERIUM` skill
  hits the Emperium; `immune_attack` unit takes 0.

## Notes / gotchas

- This is gated on FEATURE-15 (WoE): guardians and the Emperium are not spawnable yet, so the
  gate has no live trigger today. Do not ship a dead always-true gate before the entities
  exist — implement it together with (or after) FEATURE-15.
