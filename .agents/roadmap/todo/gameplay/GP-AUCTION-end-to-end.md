# GP-AUCTION — Auction house works end-to-end

> **Epic:** gameplay · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
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

- [ ] **CZ handlers**: add-item (stage), register (fee + duration + escrow), cancel, bid,
      buy-now, search (category/price/name), request-my-info.
- [ ] **Service**: verify register/bid/buynow/cancel at HEAD; full item fidelity on escrow +
      delivery; search-type bucketing (armor/weapon/card/misc + price/name filters).
- [ ] **Persistence**: full-item auction rows; expiry timer → deliver to winner (or return to
      seller if no bids) via mail; outbid → refund the previous bidder.
- [ ] **ZC emits**: auction results list, my-auctions, search results, message/notify codes.

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

## Notes / gotchas

- Fee const `auction_feeperhour` (12000) already in the service.
- Delivery via mail means GP-MAIL's attachment path should land first or co-develop.
