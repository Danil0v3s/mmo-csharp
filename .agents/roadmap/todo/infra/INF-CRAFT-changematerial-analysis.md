# INF-CRAFT — Change-material + elemental-analysis crafting

> **Epic:** infra · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** none · **Unlocks:** none

## The deliverable

> A Genetic/Sorcerer can **use Change Material + Elemental Analysis to convert items** (consume
> inputs, roll the recipe, produce outputs) — live client, persisting the inventory change.

## What this absorbs (archive)

- `_archive/todo/infra/INFRA-02` — ChangeMaterial DB (`skill_changematerial_db`) + wire.
- `_archive/todo/infra/INFRA-03` — ElementalAnalysis (Sorcerer) port.

## rAthena reference

- `rathena/src/map/skill.cpp` — `GN_CHANGEMATERIAL` (`skill_changematerial_db`) + `SO_EL_ANALYSIS`
  (elemental analysis convert), input consume + output produce.

## Scope

- [ ] **Data**: `skill_changematerial_db` loader + the elemental-analysis recipe table.
- [ ] **Service**: consume inputs, roll the recipe, produce outputs for both skills.
- [ ] **CZ/ZC**: the produce-list request + result packets.
- [ ] **Persistence**: inventory change persists.

## Done criteria

- Change Material / Elemental Analysis convert the right inputs→outputs per the recipe DB; the
  result shows + persists.

## Test plan

- Service recipe tests + handler tests + persistence round-trip.

## Notes

- Parallel. Reuses the InventoryService transfer pattern.
