# FEATURE-12 — Buying store

> **Epic:** Gameplay-Shop · **Status:** ✅ Done (2026-06-03) · **Size:** M · **Player-visible:** yes
> **Depends on:** none · **Blocks:** none
> **Related:** PACKET-* (buying-store UI packets)

## Problem

The buying store (a player who pays others for items they want) is a 135-line
shell vs. rAthena's 832-line `buyingstore.cpp`. Stalls open/update/close
in-memory and `Trade` decrements remaining buy-quantities and trips the zeny
limit, but **the actual zeny + item transfer is not performed** ("The packet
handler does inventory + zeny mutation; here we just decrement"), there is no
real setup gate (zeny-cap escrow), and no autotrade persistence. A seller can't
actually sell into a buying store.

## Current state (C#)

- `Map.Server/Shop/Buying/BuyingStoreService.cs`:
  - `Open(buyer, effectId)` (`:27`) — gate (effect ≤3, not already open), seeds stall.
  - `Update(buyer, title, zenyLimit, offers)` (`:51`) — sets title/limit/offers, coord refresh; validates offer count ≤ `MaxOffers` (5) + zeny limit ≤ 99,999,999.
  - `Trade(seller, buyerAccountId, storeId, items)` (`:76`) — *"The packet handler does inventory + zeny mutation; here we just decrement remaining buy-quantities and trip the zeny limit guard."* Decrements `stall.Offers` amount + reduces `stall.ZenyLimit` by cost (`:91`–`:94`); auto-close on full buyout (`:97`). **No zeny/item transfer.**
  - `Close` (`:65`), `Reopen` (`:71`, log only), `Search`/`SearchAll` (`:101`/`:109`), `InitAutotrade` (`:118`, log only).
- No buyer zeny escrow on open; no persistence entity.

## rAthena reference (source of truth)

- `rathena/src/map/buyingstore.cpp` (~832 lines):
  - `buyingstore_setup(sd, slots)` — gate (skill level, not already vending/buying, slots ≤ max).
  - `buyingstore_create(sd, zenylimit, result, storename, itemlist, count, at)` — validate every offer (item is buyable/tradable, price within bounds, total `price*amount` ≤ zenylimit), **escrow the buyer's zeny up to `zenylimit`** (the buying store holds the buyer's zeny), assign a `buyer_id` (store id), open the stall, `clif_buyingstore_myitemlist` + broadcast the store to nearby.
  - `buyingstore_open(sd, account_id, store_id)` — seller opens a buyer's store (`clif_buyingstore_itemlist`).
  - `buyingstore_trade(sd, account_id, buyer_id, itemlist, count)` — the real trade:
    - validate the buyer is still in range + store id matches + the buyer still has enough escrowed zeny.
    - per item: validate the seller has the item + amount, the offer still wants it, price matches.
    - **Transfer**: `pc_delitem(seller, item, amount)`, `pc_additem(buyer, item, amount)`, `pc_payzeny(buyer, total)` **from the escrowed limit**, `pc_getzeny(seller, total)`.
    - decrement the offer's bought count + the buyer's remaining zeny limit; when the limit hits 0 or all offers filled, `buyingstore_close`.
    - `clif_buyingstore_update_item` (both sides) + `clif_buyingstore_delete_item` (seller).
  - `buyingstore_close(sd)` — refund the buyer's unspent escrowed zeny.

## Scope — every sub-system that must be touched

- [x] Inject inventory + zeny + battle-config services into `BuyingStoreService`.
- [x] `Open`/`Update` (create) — **escrow the buyer's zeny up to `zenyLimit`** (debit from buyer, hold in the stall) when the store opens; validate every offer (buyable/tradable/price bounds). Reject + refund on gate failure.
- [x] `Trade` — **implement the real transfer**: validate range + store id + remaining escrow + seller stock; per item `delitem(seller)`, `additem(buyer)`, pay the seller from the escrowed zeny, decrement the offer + remaining limit; close + refund unspent when limit hits 0 or all offers filled. Emit update/delete clif both sides. Remove the "packet handler does mutation" comment.
- [x] `Close` — **refund the buyer's unspent escrowed zeny** (currently just removes the stall — the escrow would be lost).
- [x] ➡️ `Reopen` autotrade re-escrow → **FEATURE-36**. Original: accept the persisted stall + re-escrow.
- [x] ➡️ **Autotrade persistence** (EF + repo + offline NPC) → **FEATURE-36**. Original:: EF entity + repository for buying-store autotrade (store name, zeny limit, offers, escrow), persist on open, hydrate in `InitAutotrade`. (Same offline-NPC scope note as FEATURE-11: at minimum persist + reopen on owner relog; no log-only `InitAutotrade`.)
- [x] ➡️ **Client packets** → **PACKET-08** (transfers here; marked seam). Original:: ZC_BUYING_STORE_ENTRY (broadcast), ZC_MYITEMLIST_BUYING_STORE, ZC_ACK_ITEMLIST_BUYING_STORE, ZC_UPDATE_ITEM_FROM_BUYING_STORE, ZC_ITEM_DELETE_BUYING_STORE, ZC_FAILED_TRADE_BUYING_STORE. Define or use PACKET-* seam; **transfers happen here**.

## Done criteria

- Opening a buying store escrows the buyer's zeny up to the limit; closing refunds the unspent remainder.
- A seller selling into the store transfers the item to the buyer and the zeny (from escrow) to the seller; both inventories/zeny reflect it.
- Filling the last offer or exhausting the escrow auto-closes the store and refunds the remainder.
- Validation rejects (range, stale store id, insufficient escrow, seller lacks item) with no partial transfer.
- No "we just decrement" no-op transfer, no lost escrow on close, no log-only `InitAutotrade`.

## Test plan

- `Map.Server.Tests` (add `BuyingStoreServiceTests`):
  - open escrows the buyer's zeny; close refunds the unspent remainder exactly;
  - trade transfers item→buyer and zeny→seller from escrow, decrements offer + limit;
  - over-escrow / stale store id / seller-lacks-item reject with no mutation;
  - exhausting the escrow auto-closes + refunds.
- Manual/live: open a buying store, sell into it from a second character, confirm both sides + the refund on close.

## Escrow + transfer math (rAthena `buyingstore_create` / `buyingstore_trade`)

```
// create (buyer opens):
escrow = min(zenyLimit, buyer.zeny)         // hold the buyer's zeny in the stall
pc_payzeny(buyer, escrow)                   // debit now; stall.ZenyHeld = escrow

// trade (seller sells in), per item:
total = price * amount
assert seller has item+amount; assert offer wants amount; assert stall.ZenyHeld >= total
pc_delitem(seller, item, amount); pc_additem(buyer, item, amount)
pc_getzeny(seller, total)                   // paid FROM the held escrow
stall.ZenyHeld -= total; offer.amount -= amount
if stall.ZenyHeld == 0 || all offers filled → Close(buyer)

// close:
pc_getzeny(buyer, stall.ZenyHeld)           // refund unspent escrow
```

The current `Trade` (`:91`–`:94`) only decrements `ZenyLimit` and offer amount — it pays no one and the `Close` (`:65`) loses the escrow. Both are the core fixes.

## Notes / gotchas

- The defining difference from vending: the **buyer escrows zeny up front**; getting the escrow + refund-on-close right is the core of this ticket. The current shell has neither (it never debits on open and never refunds on close).
- `MaxOffers` = 5 (`:13`), zeny cap 99,999,999 (`:14`) are already correct — keep.
- Persisted escrow must survive a relog (autotrade) — go through `GameDbContext`/repository, no in-memory shortcut. The persisted row must record the **held escrow**, not just the limit, so a relog re-escrows the right amount.
- All-or-nothing per trade request (validate all items first).
- `Update` auto-opens via `Open(buyer, 0)` if no stall exists (`:57`) — the escrow debit must happen on the *create* (first `Open`/`Update`), not on every `Update` refresh.
- Items bought into a buying store go to the **buyer's inventory** (or cart) — confirm the target and that overweight/full rejects per item.

## History

- 2026-06-03 · Implemented the real buying-store flow. `Update` (create) **escrows the buyer's zeny**
  (gate: buyer must back the full limit; debit + hold in the stall). `Trade` (now `bool`, takes the
  `storeId` for anti-desync) validates the whole request before any mutation (store-id, seller stock,
  a live offer wanting each item, held-escrow coverage, buyer inventory slots) then transfers the item
  seller→buyer (full fidelity) and pays the seller **from the held escrow**, decrementing the offer +
  escrow; auto-closes + refunds the remainder when the escrow is spent or every offer is filled.
  `Close` refunds the unspent escrow. Injected `ISessionManagerAccessor`; per-open `StoreId`.
  `BuyingStoreServiceTests` (7) green; full suite 4370 pass (1 fail = pre-existing INFRA-11).
  Follow-ups: FEATURE-36 (autotrade persistence with held-escrow + buyer overweight gate); client CZ/ZC
  buying-store packets → PACKET-08.
