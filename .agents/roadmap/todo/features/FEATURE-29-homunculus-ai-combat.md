# FEATURE-29 — Homunculus AI + combat

> **Epic:** Gameplay-Companion · **Status:** ❌ Not started · **Size:** L · **Player-visible:** yes
> **Depends on:** FEATURE-08 (live homun entity) · **Blocks:** none

## Problem

FEATURE-08 spawned the `HomunculusEntity` into the world (visible, HP bar slot), but it does not yet
**act**: it doesn't follow the master, doesn't assist the master's target, and doesn't take/deal
damage. The homun skill tree is DB-sourced but the live unit never casts.

## Current state (C#)

- `Map.Server/Entities/HomunculusEntity.cs` — has `TargetId` but no AI driving it.
- `Map.Server/Homunculus/HomunculusService.cs` — spawns/vanishes the entity; no AI registration.
- `Map.Server/MapServerImpl.cs:307 _summonAi.Tick` — the summon-AI loop ("pets/homunc/mercs/slaves
  follow their master") is the hook to register the homun into.

## rAthena reference

- `rathena/src/map/homunculus.cpp` `hom_ai_sub_hard` / the unit attack loop — the homun follows the
  master, assists the master's target, and casts its `homunculus_db` skill at its rate.

## Scope

- [ ] Register the homun in the summon-AI loop (follow master when idle; assist the master's target).
- [ ] Wire the homun into the attack loop so it deals/takes damage (reuse the mob/attack services).
- [ ] Cast the homun's skill-tree skills at their rate through the skill engine.
- [ ] HP-bar updates on damage/heal (the packet emit is FEATURE-31).

## Done criteria

- The homun follows the master, attacks the master's target, takes and deals damage.

## Test plan

- `HomunculusSpawnTests`/AI tests — homun retargets the master's target; deals damage in the loop.
