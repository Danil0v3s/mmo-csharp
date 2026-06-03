# FEATURE-32 — Mercenary AI + combat

> **Epic:** Gameplay-Companion · **Status:** ❌ Not started · **Size:** L · **Player-visible:** yes
> **Depends on:** FEATURE-09 (live merc entity) · **Blocks:** none

## Problem

FEATURE-09 spawned the `MercenaryEntity` into the world, but it doesn't yet **act**: no follow,
no assist on the master's target, no take/deal damage, no skill cast. The merc skill grant is
DB-sourced (`CheckSkill`) but the live unit never casts.

## Current state (C#)

- `Map.Server/Entities/MercenaryEntity.cs` — has `TargetId`; no AI.
- `Map.Server/Mercenary/MercenaryService.cs` — spawns/vanishes the entity; no AI registration.
- `Map.Server/MapServerImpl.cs:307 _summonAi.Tick` is the hook.

## rAthena reference

- `rathena/src/map/mercenary.cpp` + the unit AI — the merc follows the master, assists the
  master's target, and casts its class skills.

## Scope

- [ ] Register the merc in the summon-AI loop (follow when idle; assist the master's target).
- [ ] Wire the merc into the attack loop (deal/take damage; reuse the mob/attack services).
- [ ] Cast the merc's class skills through the skill engine.
- [ ] HP-bar updates on damage/heal (the packet is FEATURE-33).

## Done criteria

- The merc follows the master, attacks the master's target, takes and deals damage.

## Test plan

- AI tests — merc retargets the master's target; deals damage in the loop.
