# FEATURE-11 — Vending (player shops)

> **Epic:** Gameplay-Shop · **Status:** 🚧 In progress · **Size:** M · **Player-visible:** yes
> **Depends on:** none · **Blocks:** none
> **Related:** PACKET-* (vending UI packets)

## Problem

Player vending stalls exist in-memory and track titles/offers, but the **actual
trade transfers nothing**: `PurchaseReq` decrements the listed quantity but
*explicitly does not transfer zeny or items* between buyer and vendor. So a
buyer "buys" an item, the stall qty drops, and nobody's inventory or zeny
changes. There is also no autotrade persistence (`InitAutotrade` is a no-op),
so offline vending across restarts doesn't work.

## Current state (C#)

- `Map.Server/Shop/Vending/VendingService.cs`:
  - `PurchaseReq(buyer, vendorAccountId, items)` (`:62`) — *"Inventory + zeny mutation handled by the calling packet handler; here we just bump remaining qty."* Decrements `stall.Items[i]` qty (`:76`) and **does not transfer zeny/items**.
  - `Update` (`:24`, open/refresh stall), `CloseVending` (`:39`), `Reopen` (`:46`, log only), `VendingListReq` (`:54`, lookup only), `Search`/`SearchAll` (`:81`/`:92`).
  - `InitAutotrade` (`:102`) — *log only* ("loader pending"), no hydrate.
  - Stalls in `_stalls` keyed by `EntityId`, `_accountIndex` by account id.
- No persistence entity / repository for autotrade vendors.

## rAthena reference (source of truth)

- `rathena/src/map/vending.cpp`:
  - `vending_purchasereq(sd, aid, uid, data, count)` — the real trade:
    - resolve the vendor (`map_id2sd`), validate the stall id (`vender_id`) matches (anti-desync), per-item: validate the listed index/amount/price, check the buyer has zeny `price*amount` and the vendor's inventory still holds the item.
    - **Transfer**: `pc_payzeny(buyer, total, ...)`, `pc_getzeny(vendor, total - tax, ...)` (Town tax / VAT applies), `pc_additem(buyer, item, amount)`, `pc_delitem(vendor, index, amount)`.
    - decrement the stall's listed amount; if all sold, `vending_closevending(vendor)`.
    - `clif_buyvending` (buyer ack) + `clif_vendingreport` (vendor sale notice) + refresh both stalls.
    - Overweight / inventory-full / out-of-zeny → reject with the matching fail clif.
  - `vending_openvending` / `vending_reopen` — open the stall from the player's cart items; autotrade re-opens persisted stalls on login (`autotrade_db` / `autotrade_data`).
  - Town map tax: `battle_config.vending_tax` reduces the vendor's zeny gain.

## Scope — every sub-system that must be touched

- [ ] Inject the inventory + zeny services into `VendingService`.
- [ ] `PurchaseReq` — **implement the real transfer**: per item, validate price/amount/qty + buyer zeny + vendor stock + buyer weight/inventory-space; then `payzeny(buyer)`, `getzeny(vendor, total - tax)`, `additem(buyer)`, `delitem(vendor cart)`; decrement stall qty; auto-close when sold out. Emit buyer ack + vendor sale notice. Reject paths emit the matching fail clif. Remove the "mutation handled by the calling packet handler" comment — do it here (or in a clearly-owned handler that this method drives, not a no-op).
- [ ] Anti-desync: validate the passed `vender_id`/stall id against the live stall (rAthena's `vender_id` guard) before transferring.
- [ ] Vending tax: apply `battle_config.vending_tax` (config service) to the vendor's zeny gain.
- [ ] `Reopen` — accept the char-side persisted stall (title + offers) and call `Update` to re-open (the seam exists; wire the response).
- [ ] **Autotrade persistence**: add the EF entity + repository for autotrade vendors (`autotrade_data`/`autotrade_merchant`), persist a stall on open (autotrade flag), and hydrate in `InitAutotrade` on boot — re-spawn the offline vendor NPC + stall. (If full offline-vendor NPC spawn is out of scope for this pass, persist + reopen on the owner's relog and note the offline-NPC piece as a follow-up — but no log-only `InitAutotrade`.)
- [ ] **Client packets**: ZC_PC_PURCHASE_RESULT_FROMMC (buyer), ZC_DELETEITEM_FROM_MCSTORE / vending report (vendor), ZC_PC_PURCHASE_ITEMLIST_FROMMC (list). Define or use PACKET-* seam; **the zeny/item transfer must occur here**.

## Done criteria

- A purchase transfers the correct zeny from buyer to vendor (minus tax) and the item from vendor cart to buyer inventory; both stalls/inventories reflect it.
- Insufficient buyer zeny, overweight, inventory-full, or stale stall id reject the purchase with no partial transfer.
- Selling out the last listed item auto-closes the stall.
- Autotrade vendors persist on open and re-open on the owner's relog (and on boot if offline-NPC spawn is in scope).
- No "we just bump remaining qty" no-op transfer, no log-only `InitAutotrade`.

## Test plan

- `Map.Server.Tests` (add `VendingServiceTests`):
  - purchase transfers exact zeny (with tax) + item and decrements stall qty;
  - insufficient zeny / overweight / stale stall id reject with no mutation;
  - sold-out auto-close;
  - autotrade persist + reopen round-trip (mock repo).
- Manual/live: open a vending stall, buy from a second character, confirm both zeny + inventories + the sale notice.

## Transfer math (per item, rAthena `vending_purchasereq`)

```
total      = price * amount                 // price is the vendor's listed price
tax        = total * vending_tax / 10000    // battle_config.vending_tax (basis points)
buyer:  pc_payzeny(buyer, total)            // buyer pays full price
vendor: pc_getzeny(vendor, total - tax)     // vendor receives price minus tax
items:  pc_additem(buyer, item, amount); pc_delitem(vendor_cart, index, amount)
stall:  Items[i].qty -= amount; if all qty == 0 → CloseVending(vendor)
```

Gate order before any mutation: stall id matches → buyer has `total` zeny → vendor still holds the item+amount → buyer has weight/slot for it. Any fail → reject clif, no partial transfer.

## Anti-desync

rAthena passes a `vender_id` (per-open stall id) with the purchase packet; validate it against the live stall before transferring so a stale client packet (vendor re-opened with new offers) can't buy at old prices. Add a per-open `VenderId` to the `Stall` record (`VendingService.cs:111`) and check it in `PurchaseReq`.

## Notes / gotchas

- The transfer must be all-or-nothing per purchase request (validate every item first, then mutate) — rAthena validates the full request before transferring.
- Don't reintroduce a `ConcurrentDictionary` for persisted autotrade state — the autotrade row goes through `GameDbContext`/a repository per CLAUDE.md.
- Vending tax goes to "nowhere" (sink), not to a town fund — just reduce the vendor's gain.
- The stall already lives on `EntityId` (`_stalls`) + account index (`_accountIndex`, `:18`/`:19`); keep that mapping when adding persistence.
- Vending sells from the vendor's **cart**, not the main inventory — confirm the cart service exists and `delitem` targets the cart.
- `Reopen` (`:46`) is the autotrade rehydrate seam — wire the char-side persisted stall response into `Update` there.
