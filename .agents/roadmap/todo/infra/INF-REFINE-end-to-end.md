# INF-REFINE — Refine works end-to-end

> **Epic:** infra · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** none · **Unlocks:** none

## The deliverable

> A player at a refiner can **refine equipment: pay the zeny + ore, roll the success chance, and
> on success the item's refine level goes up (with the safe/break rules), on failure it breaks or
> downgrades** — live client, persisting the new refine.

## What this absorbs (archive)

- `_archive/todo/infra/INFRA-01` — WeaponRefine consume `RefineService` (chance/material/break).

## rAthena reference

- `rathena/src/map/refine.cpp` / `skill.cpp` — `WS_WEAPONREFINE` + the NPC refine path: success
  rate by refine level + item type (`refine_db`), safe-refine cap, break-on-fail / downgrade rules,
  ore + zeny consume.

## Scope

- [ ] **Service**: `RefineService` — chance from `refine_db`, ore+zeny consume, success→+1 refine,
      fail→break/downgrade per the rules.
- [ ] **CZ handler**: refine request (NPC refine + WS_WEAPONREFINE).
- [ ] **ZC emit**: refine result (success/fail/break) + the inventory update.
- [ ] **Persistence**: the new refine level persists.

## Done criteria

- Refining succeeds/fails per the `refine_db` rates, consumes the ore+zeny, applies the new refine
  (or breaks), and the result shows client-side + persists across logout.

## Test plan

- Service: rate + consume + break tests; handler test; persistence round-trip.

## Notes

- `refine_db` is seeded; verify the loader. Parallel — can be pulled any time.
