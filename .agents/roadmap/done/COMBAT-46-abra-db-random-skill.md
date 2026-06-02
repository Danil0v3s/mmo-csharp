# COMBAT-46 — SA_ABRACADABRA abra_db random-skill selection

> **Epic:** Combat parity · **Status:** ✅ Done (2026-06-02) · **Size:** M · **Player-visible:** yes
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

- [x] **Already present** (built since the audit): `AbraDbEntity` + `IAbraDbRepository` +
      `seed_abra_db.sql` + the `AbraDatabase` loader populate the pool from the DB. The
      renewal `db/abra_db.yml` is a FLAT uniform list (no per-level rates — those are
      pre-renewal), so `AbraDatabase`'s uniform `PickRandom` IS the rAthena weighting.
- [x] `SA_ABRACADABRA` plugin = `HocusPocus` (Mage): rolls `ctx.Abra.PickRandom` and
      dispatches via `ctx.Cast.ResolveSkill`/`ResolveSkillAt`. Added the **re-entrancy
      guard** (`picked != SA_ABRACADABRA`, rAthena skill.cpp:14208) — the only real gap.
- [x] The picked skill's own cast/delay come from dispatching it through the cast
      service (`ResolveSkill` runs the picked skill's plugin).

## Done criteria

- ➡️ from COMBAT-24: casting SA_ABRACADABRA selects a skill from abra_db at the rAthena
  (renewal = uniform) weights and casts it. ✅

## Note

- The rAthena PC path additionally sets `sd->skillitem` + emits `clif_item_skill` (the
  spell-pick UI popup); the C# dispatches the rolled spell directly (matching the mob
  path) — a pre-existing, documented behavior choice in `HocusPocus`, not a regression.

## Test plan

- `Combat46AbraTests`: a seeded RNG picks the expected skill from a stub abra table; the
  picked skill is dispatched; Abracadabra is never picked.

## Notes / gotchas

- Re-entrancy: the dispatched skill must not itself be treated as another Abracadabra cast
  (rAthena's `ud->skill_id != SA_ABRACADABRA` guard).

## History

- 2026-06-02 · Found the abra_db infra already built since the audit (AbraDbEntity +
  IAbraDbRepository + seed_abra_db.sql + AbraDatabase loader + the HocusPocus plugin
  rolling + dispatching). Renewal abra_db is a flat uniform list so the existing
  uniform PickRandom is rAthena-correct. Added the missing re-entrancy guard to
  HocusPocus (never dispatch SA_ABRACADABRA itself) + Combat46AbraTests (3: rolled
  skill dispatched, empty pool no-ops, Abracadabra never recursed). Full Map.Server.
  Tests green except the pre-existing INFRA-11 replay gate.
