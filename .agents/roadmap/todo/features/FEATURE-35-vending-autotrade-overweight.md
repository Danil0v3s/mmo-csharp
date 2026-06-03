# FEATURE-35 — Vending autotrade persistence + overweight gate

> **Epic:** Gameplay-Shop · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** FEATURE-11 (vending transfer) · **Blocks:** none

## Problem

FEATURE-11 implemented the real vending purchase transfer (zeny + item, tax, anti-desync, sold-out
auto-close), but two pieces remain:

1. **Autotrade persistence** — `InitAutotrade` and `Reopen` are wire seams (log only); there is no
   EF entity / repository for offline autotrade vendors, so a `@autotrade` stall does not survive a
   restart and the offline-vendor NPC is never respawned.
2. **Overweight gate** — `PurchaseReq` rejects when the buyer has no free inventory **slot**, but not
   when the items would exceed the buyer's **max weight** (rAthena `pc_checkadditem` /
   `pc_inventoryblank` + weight checks both apply).

## Current state (C#)

- `Map.Server/Shop/Vending/VendingService.cs` — `PurchaseReq` does the transfer + free-slot gate;
  `Reopen`/`InitAutotrade` are log seams pointing here.
- No `autotrade` EF entity / repository.

## rAthena reference

- `rathena/src/map/vending.cpp` — `vending_purchasereq` weight gate; the `autotrade_data` /
  `autotrade_merchant` tables + `do_init_vending_autotrade` boot hydrate; offline-vendor NPC.

## Scope

- [ ] Add the overweight gate to `PurchaseReq` (inject the item catalog for weights + read the buyer's
      current/max weight; reject before any mutation when the purchase would exceed max weight).
- [ ] Add the autotrade EF entity + repository (vendor + offers), persist a stall on `@autotrade`
      open, hydrate in `InitAutotrade` on boot + re-open on the owner's relog (`Reopen`).
- [ ] Offline-vendor NPC respawn for hydrated autotrade stalls.

## Done criteria

- A purchase that would overweight the buyer is rejected with no transfer.
- An autotrade vendor persists on open and re-opens (NPC + stall) on boot / relog.

## Test plan

- `VendingServiceTests` — overweight purchase rejected; autotrade persist + reopen round-trip (mock repo).
