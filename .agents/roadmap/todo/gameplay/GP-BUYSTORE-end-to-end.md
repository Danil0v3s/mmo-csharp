# GP-BUYSTORE — Buying store works end-to-end

> **Epic:** gameplay · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
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

- [ ] **CZ handlers**: open/create (offers + zeny limit), close, click-to-store (view),
      trade (sell items in), search.
- [ ] **Service**: verify `Update`/`Trade`/`Close` at HEAD; seller-overweight is moot (seller
      loses items) but enforce the **buyer free-slot gate** (already partly there — verify).
- [ ] **ZC emits**: store sign on-map, my-item-list (owner), item list (visitor), trade
      result, item-amount updates, fail codes.
- [ ] **Persistence**: autotrade — persist offers + the held-escrow amount; respawn the
      buyer NPC + re-escrow on boot.

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

## Notes / gotchas

- Escrow is held on the stall (`ZenyHeld`), decremented per trade, refunded on close (archive FEATURE-12).
- Per-open `StoreId` anti-desync token the seller echoes back.
