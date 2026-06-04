# GP-BUYSTORE — Buying store works end-to-end

> **Epic:** gameplay · **Status:** 🚧 In progress · **Size:** M · **Player-visible:** yes
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

- [~] **CZ handlers**: open/create (`CZ_REQ_OPEN_BUYING_STORE` 0x0811 → `OpenBuyingStoreHandler`,
      escrow) + close (`CZ_REQ_CLOSE_BUYING_STORE` 0x0815 → `CloseBuyingStoreHandler`) — turn 1.
      Remaining: click-to-store (view) + trade (sell items in) + search.
- [~] **Service**: `Update` (escrow) + `Close` (refund) verified + emits wired (turn 1). Remaining:
      verify `Trade` + the buyer free-slot gate at HEAD (turn 2).
- [~] **ZC emits**: store sign (`ZC_BUYING_STORE_ENTRY` 0x0814, AOI) + my-item-list owner
      (`ZC_MYITEMLIST_BUYING_STORE` 0x0813) + open-fail (`ZC_FAILED_OPEN_BUYING_STORE` 0x0812) +
      disappear (`ZC_DISAPPEAR_BUYING_STORE_ENTRY` 0x0816) + escrow/refund zeny par-change — turn 1.
      Remaining: visitor item list, trade result, item-amount updates.
- [ ] **Persistence**: autotrade — persist offers + the held-escrow amount; respawn the offline buyer
      on boot. ➡️ Moved to **GP-AUTOTRADE-RUNTIME** (the shared offline-shop headless runtime, filed by
      GP-VEND — the same subsystem; the persistence tables already exist).

## Done criteria

- Buyer opens a store paying 1000z each for potions, escrowing 50,000z → others see it →
  a seller sells 3 in → seller +3000 from escrow, buyer +3 potions, escrow −3000; closing
  refunds the 47,000 remainder.
- Insufficient escrow / buyer-full / stale store-id are rejected with no partial transfer.
- Autotrade buyer stays open after logout; relog rehydrates the escrow + offers.

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
- **Remaining (next turns → done):** click-to-store (visitor item list, `ZC_ITEMLIST_BUYING_STORE`) +
  trade (`CZ_REQ_TRADE_BUYING_STORE` → `Trade` + update/result emits) + search; autotrade persistence
  ➡️ GP-AUTOTRADE-RUNTIME (shared with GP-VEND). The loop resumes this card.

## Notes / gotchas

- Escrow is held on the stall (`ZenyHeld`), decremented per trade, refunded on close (archive FEATURE-12).
- Per-open `StoreId` anti-desync token the seller echoes back.
- Buying-store offers are by `nameId` (not a cart slot), unlike vending — no client/server index convert.
