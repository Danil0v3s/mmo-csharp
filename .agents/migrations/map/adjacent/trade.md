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

Nothing on the map side. Char IPC for the underlying inventory moves is already wired.

## Pending

1. `TradeService` — per-pair state machine. Validate both parties in view + alive; track items+zeny on each side; both-confirm before commit.
2. `VendingService` — `VendNpc`-like virtual entity in front of the seated vending player; click → browse → buy.
3. `BuyingStoreService` — similar but inverse.
4. Standard rAthena anti-cheat: can't trade bound items, can't trade items the player is wearing, can't initiate trade while moving/sitting/dead.

### Acceptance
- A and B trade items + zeny end-to-end; both inventories update; cancel from either side restores.
- A sits and starts vending with 3 items; B clicks A, sees the list, buys one; A's vend list updates; both inventories update; both notify char server.

## History
- **2026-05-16** — Plan stub.
