# COMBAT-46 — SA_ABRACADABRA abra_db random-skill selection

> **Epic:** Combat parity · **Status:** 🚧 In progress · **Size:** M · **Player-visible:** yes
> **Depends on:** COMBAT-24 (the 0-cast/0-delay timing is already in place)
> **Blocks:** none
> **Filed by:** COMBAT-24 — the random-skill behavior it deferred (timing was the cast-service half).

## Problem

COMBAT-24 made SA_ABRACADABRA cast and after-cast-delay 0 (the cast-timing half).
The actual gameplay behavior — on cast, pick a RANDOM skill from `abra_db` (weighted by
the caster's Abracadabra level) and cast that skill at the target — is not implemented.
There is no `abra_db` table loaded and no SA_ABRACADABRA `SkillImpl` that performs the
roll + sub-cast.

## Current state (C#)

- `Map.Server/Skills/SkillCastTimingService.cs` — SA_ABRACADABRA returns 0 cast + 0 delay
  (COMBAT-24).
- No `abra_db` catalog; no `SA_ABRACADABRA` plugin → casting it currently does nothing.

## rAthena reference (source of truth)

- `db/abra_db.yml` — the random-skill table (skill id, level, per-Abracadabra-level rate).
- `skill.cpp` SA_ABRACADABRA cast-end: rolls `abra_db`, then `skill_castend_*` the picked
  skill (`ud->skill_id != SA_ABRACADABRA` re-entrancy guard, skill.cpp:14208 / 20018).

## Scope — every sub-system that must be touched

- [ ] Import `abra_db.yml` → a catalog (entity + loader + seed, via Tools.RathenaImporter)
      or an in-code table if small.
- [ ] `SA_ABRACADABRA` plugin (`CastendNoDamageId` / `CastendPos2`): roll the table by the
      caster's skill level, resolve the picked skill + level, and dispatch it (reuse the
      skill-cast service; guard against re-picking Abracadabra).
- [ ] Wire the picked skill's own cast/delay (rAthena uses the picked skill's delay).

## Done criteria

- ➡️ from COMBAT-24: casting SA_ABRACADABRA selects a skill from abra_db at the rAthena
  weights and casts it.

## Test plan

- `Combat46AbraTests`: a seeded RNG picks the expected skill from a stub abra table; the
  picked skill is dispatched; Abracadabra is never picked.

## Notes / gotchas

- Re-entrancy: the dispatched skill must not itself be treated as another Abracadabra cast
  (rAthena's `ud->skill_id != SA_ABRACADABRA` guard).
