# FEATURE-36 — Buying-store autotrade persistence + overweight gate

> **Epic:** Gameplay-Shop · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** FEATURE-12 (buying-store transfer) · **Blocks:** none

## Problem

FEATURE-12 implemented the real buying-store flow (escrow on open, seller→buyer transfer paid from
escrow, refund on close, anti-desync, auto-close). Two pieces remain:

1. **Autotrade persistence** — `InitAutotrade`/`Reopen` are wire seams (log only); there's no EF
   entity/repository for an offline autotrade buying store (store name, zeny limit, **held escrow**,
   offers), so it doesn't survive a restart and the offline buyer NPC is never respawned.
2. **Buyer overweight gate** — `Trade` rejects when the buyer has no free inventory **slot**, but not
   when the bought items would exceed the buyer's **max weight**.

## Current state (C#)

- `Map.Server/Shop/Buying/BuyingStoreService.cs` — `Open`/`Update` escrow; `Trade` transfers + free-slot
  gate; `Close` refunds; `Reopen`/`InitAutotrade` are log seams.
- No buying-store autotrade EF entity / repository.

## rAthena reference

- `rathena/src/map/buyingstore.cpp` — the autotrade tables (store + offers + escrow) + boot hydrate;
  the buyer-side `pc_checkadditem` weight gate in `buyingstore_trade`.

## Scope

- [ ] Add the buyer overweight gate to `Trade` (inject item-weight; reject when the bought items exceed
      the buyer's max weight).
- [ ] Add the autotrade EF entity + repository (record the **held escrow**, not just the limit, so a
      relog re-escrows the right amount), persist on open, hydrate in `InitAutotrade` + `Reopen`.
- [ ] Offline-buyer NPC respawn for hydrated stores.

## Done criteria

- A trade that would overweight the buyer is rejected with no transfer.
- An autotrade buying store persists (with its held escrow) and re-opens on boot / relog.

## Test plan

- `BuyingStoreServiceTests` — overweight reject; autotrade persist + reopen with the right re-escrow.
