# MS3 · Trade, vending, buyingstore

**Phase:** MS3 (adjacent)
**Depends on:** [items.md](items.md)
**Blocks:** —

Direct trade between players + the vendor/buying-store systems that let players act as merchants.

## Source of truth

- [rathena/src/map/trade.cpp](/Volumes/1TB/Projetos/rathena/src/map/trade.cpp) — 1:1 trade state machine
- [rathena/src/map/vending.cpp](/Volumes/1TB/Projetos/rathena/src/map/vending.cpp) — vending shop
- [rathena/src/map/buyingstore.cpp](/Volumes/1TB/Projetos/rathena/src/map/buyingstore.cpp) — buying store

## Scope (MS3 first pass)

**In scope:**
- 1:1 trade: request, accept, add items + zeny, both-confirm, finalize. State machine with timeouts.
- Vending: player sits and lists items for sale; other players click to browse + purchase.
- Buying store: inverse of vending — player advertises items they want to buy at a price.
- Persistence via existing IPC (inventory saves go through char on confirm).

**Out of scope:**
- Auction (different system, already has IPC, fold into a later phase).
- Cash shop (real-money store) — needs payment integration.

## Done

- **1:1 trade** ([Map.Server/Trade/](../../../../Map.Server/Trade/)):
  - `TradeState` mirrors rAthena `sd->trade_partner + sd->state.trading + sd->state.deal_locked + sd->deal.item[] + sd->deal.zeny`. Lives on `MapSessionData.Trade` while a deal is in progress.
  - `ITradeService` + `TradeService`:
    - `Request` — `TRADE_DISTANCE = 5` cell gate, kills any prior trade on initiator, refuses if target already trading.
    - `Acknowledge` — accept opens the window on both sides; decline cancels both.
    - `AddItem` / `AddZeny` — gated on Accepted + LockedStage=0, slot validity, sufficient zeny; collapses duplicate-slot offers.
    - `Ok` / `Commit` — rAthena two-stage lock (1 = pressed OK, 2 = pressed Trade). Commit waits for both sides at stage 2, runs `ValidateBothSides` (≈ `impossible_trade_check` + `trade_check`), then swaps atomically. Any failure aborts; no partial commits.
    - `Cancel` — clears both sides.
  - Item swap descends slot indices in reverse so RemoveAt doesn't shift unprocessed entries. Stacking on receive matches the storage merge rules (nameid + refine + cards + identified).
- **NPC shop buy/sell** ([Map.Server/Shop/](../../../../Map.Server/Shop/)):
  - `ShopService.Buy` validates each item against the script-loaded `ShopRegistration.Items`, sums total cost from `ShopItem.Price`, gates on buyer zeny, atomic deduct + deposit on success.
  - `ShopService.Sell` validates inventory slots, computes proceeds at the rAthena default 50% sell ratio of `item_db.PriceBuy`, descending slot order on remove + RemovedInventoryIds tracking for the persistence layer.
- **Account storage (kafra)** ([Map.Server/Storage/](../../../../Map.Server/Storage/)):
  - `StorageState` + `StorageItem` + `StorageCodec` (binary serializer for the `AccountStorageLoad/Save` IPC bytes blob — already wired in P5).
  - `IStorageService` — `OpenAsync` hydrates via IPC, `AddFromInventory` / `TakeToInventory` mediate transfers with stack merging on nameid+refine+cards+identified, `CloseAsync` flushes when dirty.
- **15 service tests** across `TradeServiceTests`, `ShopServiceTests`, `StorageServiceTests`.

## Pending

1. **Wire packets**: `CZ_REQ_EXCHANGE_ITEM`, `CZ_ACK_EXCHANGE_ITEM`, `CZ_ADD_EXCHANGE_ITEM`, `CZ_CONCLUDE_EXCHANGE_ITEM`, `CZ_CANCEL_EXCHANGE_ITEM`, `CZ_EXEC_EXCHANGE_ITEM` for trade; `CZ_NPC_BUY_LIST_REQ` / `CZ_PC_PURCHASE_ITEMLIST_FROMNPC` / `CZ_NPC_SELL_LIST_REQ` / `CZ_PC_SELL_ITEMLIST` for shop. All the gameplay invariants live in the services already.
2. **Vending / Buying store** — different surface (sit-and-list mechanic). Defer.
3. **Anti-cheat tail** — can't trade bound items, can't trade equipped items, can't trade while sitting/dead/moving. Hooks live in the services where validation already runs.

### Acceptance
- ✅ A and B exchange items + zeny atomically through `TradeService.Commit`.
- ✅ Cancel from either side clears both sides' state.
- ✅ Buyer cannot purchase without enough zeny; seller receives 50% of item_db buy price.
- ✅ Storage transfers in/out merge stacks correctly.

## History
- **2026-05-16** — Plan stub.
- **2026-05-19** — Trade + Shop + Storage services all shipped with strategy-pattern shape and atomic commits. 15 service tests green. Wire packets queued; service contract is stable.
