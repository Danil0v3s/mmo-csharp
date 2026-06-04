# GP-AUCTION — Auction house works end-to-end

> **Epic:** gameplay · **Status:** 🚧 In progress · **Size:** M · **Player-visible:** yes
> **Depends on:** none · **Unlocks:** none

## The deliverable

> A player can **register an item for auction (paying the fee, item escrowed), others browse/
> search, bid or buy-now, the winner gets the item + the seller gets the zeny, expiry/outbid
> refunds** — live client, surviving logout.

## Player story

The map-side auction wiring is real (register with item+fee escrow, bid, buy-now, cancel,
list — archive FEATURE-06), but no client packet reaches it, and full item fidelity
(cards/options) + search-type bucketing are incomplete.

## Current state — per layer

| Layer | State | Where |
|---|---|---|
| Service | ✅ verify | `Map.Server/Auction/AuctionService.cs` — register/bid/buynow/cancel/list (archive FEATURE-06) |
| Char store | partial | stores id/refine/attribute only — full card/option fidelity missing (archive FEATURE-26) |
| CZ handlers | ❌ | register/cancel/bid/buy/search/list missing |
| ZC emits | ❌ | auction results, my-auctions, search results, bid/outbid notify missing |

## rAthena reference

- `rathena/src/map/clif.cpp` + `char/int_auction.cpp` — `clif_parse_Auction_*`
  (`CZ_AUCTION_*`: add-item, add(register w/ fee+duration), close, bid, search, requestmyinfo),
  `auction_*` (fee = `auction_feeperhour`), emit `clif_Auction_results`/`clif_Auction_message`/
  `clif_auction_setitem`. Persistence lives char-side (`auctions` table) — bidding, expiry
  timer, win→mail-delivery of item, outbid→zeny refund.

## Dependencies — and how to satisfy

- Packet-bridge pattern — foundation.
- Char-side full-item persistence — extend the auction row to carry the full
  `InventoryItem` (cards/options/refine/grade), not id/refine/attr only (archive FEATURE-26).
- Win/expiry delivery rides **mail** (GP-MAIL) — auctioned items + zeny are delivered by mail
  in rAthena; reuse the mail attachment path.

## Scope — every layer

- [x] **CZ handlers** *(turn 1)*: open/cancel-reg (`CZ_AUCTION_CREATE` 0x024b), add-item/stage
      (`CZ_AUCTION_ADD_ITEM` 0x024c, auctionable-type/identified/unequipped/unexpired gates),
      register (`CZ_AUCTION_ADD` 0x024d, fee pre-gate + escrow via service), cancel
      (`CZ_AUCTION_CANCEL` 0x024e), bid (`CZ_AUCTION_BUY` 0x024f, zeny pre-gate), buy-now/close
      (`CZ_AUCTION_REQ_MY_SELL_STOP` 0x025d), search (`CZ_AUCTION_ITEM_SEARCH` 0x0251),
      request-my-info (`CZ_AUCTION_REQ_MY_INFO` 0x025c, type+6).
- [~] **Service**: register/bid/buynow/cancel/list verified at HEAD + wired to the handlers *(turn 1)*.
      Remaining: full item fidelity on escrow (cards/options into `item_payload`) + search-type
      bucketing (armor/weapon/card/misc) — **turn 2**.
- [ ] **Persistence**: full-item auction rows; expiry timer → deliver to winner (or return to
      seller if no bids) via mail; outbid → refund the previous bidder. **(turn 2/3 — char-side.)**
      The `AuctionEntity` already has card/option columns + the bid-side outbid-refund mail exists;
      the gaps are (a) populating fidelity on register, (b) item→winner / zeny→seller delivery on
      close/buy-now, (c) return-to-seller on cancel, (d) the expiry sweep.
- [x] **ZC emits** *(turn 1)*: status message (`ZC_AUCTION_RESULT` 0x0250), staged-item ack
      (`ZC_ACK_AUCTION_ADD_ITEM` 0x0256), browse/search + my-auctions results
      (`ZC_AUCTION_ITEM_REQ_SEARCH` 0x0252, 83-byte entries; shared by search + my-info), close ack
      (`ZC_AUCTION_ACK_MY_SELL_STOP` 0x025e), open-window (`ZC_AUCTION_OPENWINDOW` 0x025f). Result-row
      card fidelity defaults to 0 until the char-side `item_payload` population lands (turn 2).

## Done criteria

- Seller registers a carded item (fee paid, item escrowed) → another player searches by
  category, bids; a higher bid refunds the first bidder; at expiry the winner receives the
  item (cards intact) + the seller receives the zeny, all by mail.
- Buy-now ends the auction immediately.
- Relog at any point → auction state intact.

## Test plan

- Handler tests: register/bid/buy → service.
- Service: fee, escrow fidelity, outbid refund, search buckets (archived AuctionServiceTests).
- Persistence + mail delivery round-trip.
- Live: register → search → bid → outbid → expiry delivery.

## Progress log (multi-turn vertical)

- **2026-06-04 (turn 1)** — The auction client packet bridge. 13 new packets — CZ:
  `CZ_AUCTION_CREATE` (0x024b), `CZ_AUCTION_ADD_ITEM` (0x024c), `CZ_AUCTION_ADD` (0x024d),
  `CZ_AUCTION_CANCEL` (0x024e), `CZ_AUCTION_BUY` (0x024f), `CZ_AUCTION_ITEM_SEARCH` (0x0251),
  `CZ_AUCTION_REQ_MY_INFO` (0x025c), `CZ_AUCTION_REQ_MY_SELL_STOP` (0x025d); ZC: `ZC_AUCTION_RESULT`
  (0x0250 message), `ZC_AUCTION_ITEM_REQ_SEARCH` (0x0252, 83-byte rows), `ZC_ACK_AUCTION_ADD_ITEM`
  (0x0256), `ZC_AUCTION_ACK_MY_SELL_STOP` (0x025e), `ZC_AUCTION_OPENWINDOW` (0x025f). New
  `IAuctionClientService`/`AuctionClientService` emit hub + 8 handlers wired to the existing
  `AuctionService` (escrow + char IPC). Session staging state (`AuctionStageIndex`/`Amount`) mirrors
  `sd->auction`. Register fee + bid affordability are pre-gated in the handlers for the specific
  not-enough-zeny messages (flags 5/8); register-success emits flag 1 + clears the stage; search +
  my-info share the results emit (my-info offsets type by 6 → my-selling/my-buying). 9 bridge tests
  (stage success/reject, register confirm + fee-gate, bid gate + confirm, close ack, search render,
  my-info type offset); full suite 4525 pass (1 = standing replay-fixture).
- **Remaining (turn 2/3 → done):** char-side full-item fidelity (`item_payload` → cards/options on
  register + result rows) + delivery (item→winner / zeny→seller on close/buy-now, return→seller on
  cancel, all by mail) + the expiry-timer sweep + search-bucket types 0–3. The loop resumes this card.

## Notes / gotchas

- Fee const `auction_feeperhour` (12000) already in the service.
- Delivery via mail means GP-MAIL's attachment path should land first or co-develop.
- rAthena's `clif_Auction_close` writes opcode 0x25d (a known client-compat quirk); we use the
  canonical 0x25e to avoid colliding with the incoming `CZ_AUCTION_REQ_MY_SELL_STOP` (0x025d) in the
  global opcode registry — byte-exact opcode is part of the standing live-client validation pass.
