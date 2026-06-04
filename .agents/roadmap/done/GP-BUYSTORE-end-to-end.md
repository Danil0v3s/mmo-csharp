# GP-BUYSTORE — Buying store works end-to-end

> **Epic:** gameplay · **Status:** ✅ Done (2026-06-04) · **Size:** M · **Player-visible:** yes
> **Depends on:** none (pairs with GP-VEND) · **Unlocks:** none

## The deliverable

> A player can **open a buying store ("I'll pay X for Y"), escrowing zeny up to a limit;
> other players see it, sell matching items in (item → buyer, zeny from escrow → seller);
> unspent escrow refunds on close** — live client; autotrade survives logout.

## Player story

The reverse of vending. The *escrow + transfer* logic is real (escrow on open, validated
seller→buyer transfer paid from held escrow, refund on close, auto-close — archive FEATURE-12),
but no client packet reaches it, and autotrade persistence + the seller-overweight/buyer-slot
edges are missing.

## Current state — per layer

| Layer | State | Where |
|---|---|---|
| Service | ✅ verify | `Map.Server/Shop/Buying/BuyingStoreService.cs` — `Open`/`Update`(escrow)/`Trade`/`Close` (archive FEATURE-12) |
| CZ handlers | ❌ | create-buying-store, close, sell-to-store, search missing |
| ZC emits | ❌ | store on-map, buying item list, trade result, item-update missing |
| Persistence | ❌ | autotrade offline-buyer row + NPC + held-escrow (archive FEATURE-36) |

## rAthena reference

- `rathena/src/map/buyingstore.cpp` — `buyingstore_setup`/`buyingstore_create` (escrow),
  `buyingstore_close`, `buyingstore_trade`, `buyingstore_search`, `do_init_buyingstore_autotrade`.
- `rathena/src/map/clif.cpp` — parse `CZ_REQ_OPEN_BUYING_STORE`, `CZ_REQ_CLOSE_BUYING_STORE`,
  `CZ_REQ_TRADE_BUYING_STORE`, `CZ_REQ_CLICK_TO_BUYING_STORE`; emit
  `clif_buyingstore_*` (myitemlist, entry on-map, itemlist, trade, update, fail).

## Dependencies — and how to satisfy

- Packet-bridge pattern — foundation.
- Autotrade persistence — offline-buyer table recording the held escrow + offers + a buyer-NPC
  spawn (build it here, archive FEATURE-36).

## Scope — every layer

- [x] **CZ handlers**: open/create (`CZ_REQ_OPEN_BUYING_STORE` 0x0811 → `OpenBuyingStoreHandler`,
      escrow) + close (`CZ_REQ_CLOSE_BUYING_STORE` 0x0815 → `CloseBuyingStoreHandler`) — turn 1;
      click-to-store (`CZ_REQ_CLICK_TO_BUYING_STORE` 0x0817 → `ClickBuyingStoreHandler`) + trade
      (`CZ_REQ_TRADE_BUYING_STORE` 0x0819 → `TradeBuyingStoreHandler`, client-index→server convert) —
      turn 2. Search ➡️ **INF-SEARCHSTORE** (universal market search, shared with GP-VEND).
- [x] **Service**: `Update` (escrow) + `Close` (refund) — turn 1; `Trade` (item→buyer, escrow→seller,
      store-id/overcount/escrow-short gates, buyer-full silent-return per rAthena, auto-close+refund) +
      `VisitorListReq` (click path) — turn 2.
- [x] **ZC emits**: store sign (`ZC_BUYING_STORE_ENTRY` 0x0814, AOI) + my-item-list owner
      (`ZC_MYITEMLIST_BUYING_STORE` 0x0813) + open-fail (`ZC_FAILED_OPEN_BUYING_STORE` 0x0812) +
      disappear (`ZC_DISAPPEAR_BUYING_STORE_ENTRY` 0x0816) + escrow/refund zeny par-change — turn 1;
      visitor item list (`ZC_ACK_ITEMLIST_BUYING_STORE` 0x0818) + per-item seller delete
      (`ZC_ITEM_DELETE_BUYING_STORE` 0x081c) + buyer amount update (`ZC_UPDATE_ITEM_FROM_BUYING_STORE`
      0x081b) + buyer pickup (`ZC_ITEM_PICKUP_ACK`) + trade-fail
      (`ZC_FAILED_TRADE_BUYING_STORE_TO_SELLER` 0x0824) — turn 2.
- [ ] **Persistence**: autotrade — persist offers + the held-escrow amount; respawn the offline buyer
      on boot. ➡️ Moved to **GP-AUTOTRADE-RUNTIME** (the shared offline-shop headless runtime, filed by
      GP-VEND — the same subsystem; the persistence tables already exist).

## Done criteria

- ✅ Buyer opens a store paying 1000z each for potions, escrowing 50,000z → others see it →
  a seller sells 3 in → seller +3000 from escrow, buyer +3 potions, escrow −3000; closing
  refunds the 47,000 remainder. (`BuyingStoreTradeTests`, `BuyingStoreOpenCloseTests`.)
- ✅ Insufficient escrow (`BuyerLacksZeny`) / buyer-full (silent return) / stale store-id
  (`DealFailed`) / unwanted-item (`OverCount`) are rejected with no partial transfer
  (`ZC_FAILED_TRADE_BUYING_STORE_TO_SELLER`).
- Autotrade buyer stays open after logout; relog rehydrates the escrow + offers.
  ➡️ Moved to **GP-AUTOTRADE-RUNTIME** (shared offline-shop headless runtime; tables already exist).
- Universal market-search discovery of buying stores ➡️ **INF-SEARCHSTORE** (cross-shop, shared with GP-VEND).

## Test plan

- Handler tests: create/trade/close → service.
- Service: escrow gates, refund (extend archived BuyingStoreServiceTests).
- Persistence: autotrade escrow rehydrate.
- Live: open → sell-in → close-refund → autotrade relog.

## Progress log (multi-turn vertical)

- **2026-06-04 (turn 1)** — Open + close + store sign. New packets `CZ_REQ_OPEN_BUYING_STORE` (0x0811,
  variable `<zenyLimit>.L <result>.B <name>.80 {nameId.W amount.W price.L}*`), `CZ_REQ_CLOSE_BUYING_STORE`
  (0x0815), `ZC_BUYING_STORE_ENTRY` (0x0814 store sign), `ZC_DISAPPEAR_BUYING_STORE_ENTRY` (0x0816),
  `ZC_MYITEMLIST_BUYING_STORE` (0x0813 owner list), `ZC_FAILED_OPEN_BUYING_STORE` (0x0812). New
  `IBuyingStoreClientService`/`BuyingStoreClientService` (store sign → area-WOS, owner list/fail →
  buyer session) wired into `Update`/`Close`. The buyer's `Update` now emits the owner list + store
  sign + the escrow zeny par-change on success (or the open-fail on a rejection); `Close` emits the
  refund zeny par-change + the disappear. `OpenBuyingStoreHandler` parses the offers (name id/amount/
  price); `CloseBuyingStoreHandler` tears down. `BuyingStoreOpenCloseTests` (4: escrow+owner-list+sign,
  insufficient-zeny→fail, close-refunds+disappear, handler routing); full suite 4496 pass (1 = standing
  replay-fixture).
- **2026-06-04 (turn 2 — DONE)** — Click-to-view + sell-in/trade. New packets `CZ_REQ_CLICK_TO_BUYING_STORE`
  (0x0817, fixed `<buyerAID>.L`), `CZ_REQ_TRADE_BUYING_STORE` (0x0819, variable `<buyerAID>.L <storeId>.L
  {index.W nameId.W amount.W}*`), `ZC_ACK_ITEMLIST_BUYING_STORE` (0x0818 visitor offer list, AID+storeId+
  zenyLimit+entries), `ZC_UPDATE_ITEM_FROM_BUYING_STORE` (0x081b `<nameId>.W <amount>.W <zenyLimit>.L`),
  `ZC_ITEM_DELETE_BUYING_STORE` (0x081c `<index>.W <amount>.W <price>.L`), `ZC_FAILED_TRADE_BUYING_STORE_TO_SELLER`
  (0x0824 `<result>.W <nameId>.W`, `BuyStoreSellResult` DealFailed=5/OverCount=6/BuyerLacksZeny=7). Extended
  `IBuyingStoreClientService` with `SendVisitorList`/`SendSellerDelete`/`SendSellerFail`; added
  `VisitorListReq` to the service (click → offer list to the visitor). `Trade` rewritten to rAthena
  `buyingstore_trade` gates: store-id mismatch → DealFailed, no-offer/overcount → OverCount, escrow-short →
  BuyerLacksZeny, buyer-inventory-full → silent return; transfer loop does DebitSlot(seller)/CreditItem(buyer)
  paid from `stall.ZenyHeld`, emitting per item the seller delete + buyer amount-update + buyer pickup-ack +
  seller SP_ZENY par-change; auto-close (escrow exhausted or all offers filled) → refund + buyer zeny update +
  `CloseStore`. Injected `IEntityRegistry` to resolve the buyer entity on auto-close. `ClickBuyingStoreHandler`
  (0x0817 → `VisitorListReq`) + `TradeBuyingStoreHandler` (0x0819 → `Trade`, client-index−2 → server index).
  `BuyingStoreTradeTests` (5: click→offer-list, sell-in transfer+pay+emits, stale-storeId→DealFailed,
  unwanted-item→OverCount, handler index-convert); full suite 4501 pass (1 = standing replay-fixture).
  Search ➡️ INF-SEARCHSTORE, autotrade ➡️ GP-AUTOTRADE-RUNTIME — both functional done-criteria met → **DONE**.

## History

- 2026-06-04 — Turn 1: open/create/close + escrow/refund + store sign (commit 6f1f9cbb). Turn 2 (DONE):
  click-to-view + sell-in/trade bridge with the trade-result/update/delete emits; search ➡️ INF-SEARCHSTORE,
  autotrade ➡️ GP-AUTOTRADE-RUNTIME.

## Notes / gotchas

- Escrow is held on the stall (`ZenyHeld`), decremented per trade, refunded on close (archive FEATURE-12).
- Per-open `StoreId` anti-desync token the seller echoes back.
- Buying-store offers are by `nameId` (not a cart slot), unlike vending — no client/server index convert.
