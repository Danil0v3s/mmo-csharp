# INF-SEARCHSTORE — Search-store enumeration works end-to-end

> **Epic:** infra · **Status:** ❌ Not started · **Size:** S · **Player-visible:** yes
> **Depends on:** GP-VEND, GP-BUYSTORE (the shops to search) · **Unlocks:** none

## The deliverable

> A player can **use the universal "search stores" UI to find vending/buying-store offers by item
> + price across the map, and click a result to view/warp to that shop** — live client.

## What this absorbs (archive)

- `_archive/todo/infra/INFRA-05` — SearchStore `GetAllShops` enumeration.

## rAthena reference

- `rathena/src/map/searchstore.cpp` — `searchstore_query`/`searchstore_open`/`searchstore_click`,
  the all-shops enumeration over vending + buying stores.

## Scope

- [ ] **Service**: enumerate all open vending + buying stores; filter by item id/card + price range.
- [ ] **CZ/ZC**: search query + results + click-to-view/warp packets.

## Done criteria

- A search for "Red Potion under 100z" lists every matching open shop; clicking a result opens/
  navigates to it.

## Test plan

- Service enumeration/filter tests + handler tests.

## Notes

- Parallel, but needs vending + buying stores reachable (GP-VEND/GP-BUYSTORE) to have anything to find.
