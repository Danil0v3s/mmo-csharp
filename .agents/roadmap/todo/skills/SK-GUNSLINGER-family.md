# SK-GUNSLINGER — Gunslinger skill family (coin/ammo/chain)

> **Epic:** skills · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** SK-ENGINE · **Unlocks:** none

## The deliverable

> The Gunslinger skills get their real behaviour: coin (Gatling/Desperado coin spend), ammo/
> bullet consumption, Chain Action, the per-weapon (revolver/rifle/gatling/shotgun/grenade) gates.

## What this absorbs (archive)

- `_archive/todo/skills/SKILL-10` — Family: Gunslinger (coin/ammo/chain).

## rAthena reference

- `rathena/src/map/skill.cpp` — the `GS_*` `case` arms (coin cost, bullet consume, per-weapon
  requirement); ammo consume already landed (archive COMBAT-36/58).

## Scope

- [ ] Port each Gunslinger skill: coin spend + bullet consume + per-weapon gate + ratio/effect.

## Done criteria

- Each Gunslinger skill spends the right coins/bullets, gates on the right weapon, and computes
  the rAthena ratio; per-skill tests pass.

## Test plan

- Per-skill tests (coin/ammo consume + ratio).

## Notes

- Deferred. Reuses the landed `AmmoService` (archive COMBAT-36/58) + SK-ENGINE.
