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
- [x] **Service**: register/bid/buynow/cancel/list verified + wired to the handlers *(turn 1)*; full
      item fidelity on the register RPC (`AuctionData.item` = cards/options/uniqueid/bound/grade,
      populated from the escrowed item) + search-type bucketing (armor/weapon/card/misc → `auction.Type`
      match) *(turn 2)*.
- [~] **Persistence** *(turn 2)*: full-item auction rows (`ApplyAuctionItemFidelity` on register +
      `AuctionItemFidelity` back on browse); delivery by mail — item→winner + winning-bid→seller on
      close (seller-ends) and on instant buy-now-via-bid (buy-now price→seller), item→seller return on
      cancel (`QueueAuctionMail`, full fidelity attachments); outbid → prior-bidder zeny refund (already
      present). Also fixed the previously EF-untranslatable `AuctionRequestList` filter. **Remaining:**
      the **expiry-timer sweep** (deliver expired auctions to winner / return to seller) — **turn 3**.
- [x] **ZC emits** *(turn 1)*: status message (`ZC_AUCTION_RESULT` 0x0250), staged-item ack
      (`ZC_ACK_AUCTION_ADD_ITEM` 0x0256), browse/search + my-auctions results
      (`ZC_AUCTION_ITEM_REQ_SEARCH` 0x0252, 83-byte entries; shared by search + my-info), close ack
      (`ZC_AUCTION_ACK_MY_SELL_STOP` 0x025e), open-window (`ZC_AUCTION_OPENWINDOW` 0x025f). Result rows
      now carry the item's cards/identify from the char-side fidelity payload *(turn 2)*.

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
- **2026-06-04 (turn 2)** — Char-side full-item fidelity + delivery. Added `MailAttachmentItem item = 16`
  to the `AuctionData` proto; the map `RegisterAsync` now populates it from the escrowed item
  (`BuildItemData`) and the result mapper reads cards back into the 83-byte browse rows. Char side:
  `ApplyAuctionItemFidelity` persists cards/options/uniqueid/grade onto the auction row on register,
  `AuctionItemFidelity` carries them back on browse, and `QueueAuctionMail` delivers the auctioned
  item (full-fidelity mail attachment) + zeny: on `AuctionClose` (seller ends → item→winner +
  winning-bid→seller), on the instant buy-now-via-bid path (item→buyer + buy-now-price→seller, overage
  refunded), and on `AuctionCancel` (item→seller return). Search-bucket types 0–3 now filter on
  `auction.Type`. Fixed the previously EF-untranslatable `AuctionRequestList` predicate (now filtered
  in memory). Tests: +4 char delivery/fidelity + 1 category-filter (`CharGrpcDataIntegrityTests`,
  172 char-suite green) + 1 map fidelity (`AuctionServiceTests`, 26 map auction-suite green); full
  Map suite 4526 pass (1 = standing replay-fixture).
- **Remaining (turn 3 → done):** the **expiry-timer sweep** — a char-side background pass that closes
  auctions past their end time, delivering the item to the high bidder (or returning it to the seller
  if there were no bids) by mail. The loop resumes this card.

## Notes / gotchas

- Fee const `auction_feeperhour` (12000) already in the service.
- Delivery via mail means GP-MAIL's attachment path should land first or co-develop.
- rAthena's `clif_Auction_close` writes opcode 0x25d (a known client-compat quirk); we use the
  canonical 0x25e to avoid colliding with the incoming `CZ_AUCTION_REQ_MY_SELL_STOP` (0x025d) in the
  global opcode registry — byte-exact opcode is part of the standing live-client validation pass.
