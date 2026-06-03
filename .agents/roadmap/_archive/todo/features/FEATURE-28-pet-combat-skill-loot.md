# FEATURE-28 — Pet combat: auto-skill dispatch + loot bag

> **Epic:** Feature unlock · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** FEATURE-07 (live pet) · **Blocks:** none

## Problem

FEATURE-07 made the egg→hatch loop work, but the live pet does not yet **act in combat**:

1. **Auto-skill** — `PetOpsService.AttackSkill` always returns 0 ("no skill cast this tick"). rAthena
   `pet_skill_attack` / `pet_skill_support` roll the pet's `pet_db` skill at its rate and cast it
   through the skill engine when the master is fighting.
2. **Loot bag** — a `pet_db Loot:true` pet (e.g. Bapho Jr.) should pick up nearby floor items into a
   loot bag and drop them when vaporised/renamed; FEATURE-07 left this unmodelled.

## Current state (C#)

- `Map.Server/Pet/PetOps/PetOpsService.cs` `AttackSkill` (`:197`) → `return 0`; `TargetCheck` (`:206`)
  gates on intimacy ≥ 900 (loyal) but no cast follows.
- No pet loot bag on `PetEntity`; no pet-side loot pickup in the AI/loop.
- `PetService.Tick` runs hunger/intimacy decay; no combat/skill tick for the pet.

## rAthena reference (source of truth)

- `rathena/src/map/pet.cpp` `pet_skill_attack` / `pet_skill_support` / `pet_ai_sub_hard` — the pet AI
  rolls `pet_db AttackRate` / `SupportSkill` and casts via `unit_skilluse_*`; `pet_lootitem` picks up
  floor items for MD_LOOTER-style pets.
- `db/re/pet_db.yml` `AttackRate`, `RetaliateRate`, `ChangeTargetRate`, `Loot`, `SupportSkill`.

## Scope

- [ ] Confirm/extend `PetDbEntity` with the skill + loot columns (`AttackRate`, `SupportSkill`,
      `Loot`); importer mapping if missing.
- [ ] `AttackSkill` — when the master is engaged and the pet is loyal, roll the pet's attack-skill rate
      and dispatch it through the skill engine (mirror the mob skill-cast path).
- [ ] Pet loot bag — a `Loot` pet picks up floor items in range into a bag (cap), dropped on
      vaporise/rename; wire into the pet tick.

## Done criteria

- A pet with an attack skill casts it at its rate while the master fights; a loot pet collects floor
  items and drops them on vaporise — matching rAthena's rates/behaviour.

## Test plan

- `PetLifecycleTests` — forced-rate roll casts the pet skill; a loot pet picks up a floor item.

## Notes / gotchas

- Reuse the mob skill-cast / looter services where possible (`IMobSkillCastService` / looter) rather
  than a parallel pet path.
