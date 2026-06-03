# MOBAI-05 — True cell-grid range query for the mob scans (perf)

> **Epic:** Mob AI parity · **Status:** ❌ Not started · **Size:** S · **Player-visible:** no (perf)
> **Depends on:** MOBAI-04 (it moved the aggro scan onto `ForEachInRange`) · **Blocks:** none
> **Filed by:** MOBAI-04 — its "Perf follow-up note (do NOT implement, just record)": the aggro
> scan now uses `IEntityRegistry.ForEachInRange`, but that query itself may iterate a coarse bucket
> rather than a true cell-grid (rAthena `map_foreachinrange` over the per-cell block list).

## Problem

`IEntityRegistry.ForEachInRange(mapId, cx, cy, range, mask)` is the parity-correct range query the
mob scans (`MobAiService` aggro + `HasAnyPcInView` + `SpotPcsInView`) now use, but its internal
implementation may walk a coarse bucket / partial registry rather than a true per-cell block list.
rAthena `map_foreachinrange` walks only the `BL_*` chains of the cells inside the radius — O(cells
in range), not O(entities on map). At scale (many mobs × think-ticks) the current form is heavier
than rAthena.

## Current state (C#)

- `Map.Server/Mob/MobAiService.cs` — aggro scan uses `ForEachInRange` (MOBAI-04); no longer `All()`.
- `Map.Server/Entities/EntityRegistry.cs` / the `ForEachInRange` impl — confirm whether it is a true
  cell-grid block-list walk or a coarser scan; optimize if coarse.

## rAthena reference (source of truth)

- `rathena/src/map/map.cpp` `map_foreachinrange` / `map_foreachinallrange` — block-list iteration
  over the cells within the radius (`map[m].block[]` / `block_mob[]`).

## Scope

- [ ] Audit `ForEachInRange`'s implementation; if it is not already a per-cell block-list walk,
      add a cell→entity-list index (updated on move/spawn/despawn) and iterate only the in-radius
      cells. Keep the signature + results identical (parity-neutral).

## Done criteria

- `ForEachInRange` is O(cells-in-range + entities-in-range), not O(entities-on-map); the mob scans'
  results are unchanged. A microbenchmark (or a registry-spy assert) shows the scan no longer
  touches out-of-range entities.

## Test plan

- A registry test: with N entities scattered on a large map, `ForEachInRange` over a small radius
  visits only the in-radius entities (assert via a counting wrapper), not the full set.
