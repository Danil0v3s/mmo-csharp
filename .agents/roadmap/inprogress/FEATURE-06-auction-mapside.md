# FEATURE-06 — Auction (map-side)

> **Epic:** Gameplay-Auction · **Status:** 🚧 In progress · **Size:** M · **Player-visible:** yes
> **Depends on:** FEATURE-05 (auction refunds/payouts go via mail) · **Blocks:** none
> **Related:** PACKET-* (ZC auction UI packets)

## Problem

Auction has real char-side persistence RPCs and `IntifService` wrappers, but
**no map-side gameplay code calls them**. There is no `AuctionService` on the
map driving the auction window: a player cannot register an item for auction,
bid, buy-now, cancel, or browse the list. The whole feature is orphaned plumbing.

## Current state (C#)

- `Map.Server/Services/Intif/IntifService.cs`: real wrappers, all orphaned —
  - `:376 AuctionRequestList(charId, type, price, search, page)`
  - `:393 AuctionRegister(charId, type, sellerCharId, sellerName, now, hours, priceStart, priceBuyNow, itemId, refine, attribute, identify, amount)` — packs `Core.Server.IPC.AuctionData` and dispatches.
  - `:419 AuctionCancel(charId, auctionId)`
  - `:431 AuctionClose(charId, auctionId)` (buy-now)
  - `:443 AuctionBid(charId, auctionId, bid, bidder)`
- Char side: `Char.Server/CharGrpcService.cs` auction RPC overrides + `ICharServerIpcServiceAuction`. Inter logic mirrors `rathena/src/char/int_auction.cpp`.
- **No** `Map.Server/Auction/` directory, no `AuctionService`, no auction packet handlers. Grep confirms no map-side callsite for the auction wrappers.

## rAthena reference (source of truth)

- `rathena/src/map/clif.cpp` auction packet handlers (`clif_parse_Auction_*`) call into the inter layer:
  - `clif_parse_Auction_register` — validate the item is in inventory + not equipped + zeny for the listing fee; `intif_Auction_register` (char allocates `auction_id`, writes the row, removes the item from inventory by escrowing it into the auction row).
  - `clif_parse_Auction_cancelreg` → `intif_Auction_cancel` — seller cancels; char returns the item via mail and refunds any current bidder via mail.
  - `clif_parse_Auction_bid` → `intif_Auction_bid` — validate bid > current high bid and >= start price; char debits the bidder's zeny (escrow), refunds the previously-outbid bidder via mail.
  - `clif_parse_Auction_buysell` / buy-now → `intif_Auction_close` — char mails the item to the buyer and the zeny to the seller.
  - `clif_parse_Auction_search` → `intif_Auction_requestlist` — filtered/paged browse.
- `rathena/src/char/int_auction.cpp` — `auction_end_timer` expires auctions (winner gets item via mail, seller gets zeny; no bids → item returns to seller). The map server doesn't own expiry (char-side timer) but must reflect list state.

## Scope — every sub-system that must be touched

- [ ] **New service** `Map.Server/Auction/AuctionService.cs` + `IAuctionService` — the rAthena-named map-side seam (`Register`, `Bid`, `Close` (buy-now), `Cancel`, `RequestList`). Register in `Program.cs` DI.
- [ ] `Register(seller, inventoryIndex, amount, startPrice, buyNowPrice, hours)` — validate item present/unequipped/tradable, zeny for the listing fee (rAthena `AUCTION_FEEPERCENT`), escrow the item out of inventory (`pc_delitem`), pack `AuctionData`, call `IntifService.AuctionRegister(...)`. Reject (and don't remove the item) on gate failure.
- [ ] `Bid(bidder, auctionId, bid)` — validate against the cached current high bid + start price + buy-now, debit the bidder's zeny (escrow), call `IntifService.AuctionBid`. Char side refunds the prior bidder via mail.
- [ ] `Close(buyer, auctionId)` (buy-now) — debit buy-now zeny, call `IntifService.AuctionClose`.
- [ ] `Cancel(seller, auctionId)` — call `IntifService.AuctionCancel` (char returns item + refunds bidder via mail). Reject if there is already a bidder (rAthena gate).
- [ ] `RequestList(searcher, type, price, search, page)` — call `IntifService.AuctionRequestList`; cache the response for display + bid-validation.
- [ ] **Map-side list cache**: hold the last-fetched auction list per searcher (for the bid/buy-now validation gate and the window refresh) — read-through, char is the source of truth.
- [ ] **Client packets**: CZ_AUCTION_* handlers (`Map.Server/Handlers/`) → call the service; ZC_AUCTION_RESULT / ZC_AUCTION_ITEM_REQ_SEARCH / ZC_AUCTION_RESULTS emit on response. Define packets in `Core.Server/Packets` or use PACKET-* seam; **inventory/zeny escrow must happen here**.
- [ ] **Response handlers**: the char-side push (auction registered ack, bid ack, list result) routes back to the map handler that emits the client packet — wire these (they don't exist yet).

## Done criteria

- Registering an item escrows it out of the seller's inventory, deducts the listing fee, and creates the auction row char-side (allocated id returned).
- Bidding debits the bidder's zeny and refunds the prior high bidder via mail (FEATURE-05 path); a bid below the current high is rejected without debit.
- Buy-now closes the auction, mails the item to the buyer and zeny to the seller.
- Cancel (no bidders) returns the item to the seller via mail; cancel with a bidder is rejected per rAthena.
- Browse returns the filtered/paged list and refreshes the window.
- No orphaned auction IPC wrapper remains uncalled.

## Test plan

- `Map.Server.Tests` (add) `AuctionServiceTests`:
  - `Register` escrows the item + fee and calls `IntifService.AuctionRegister`;
  - `Bid` below current high is rejected with no zeny debit; valid bid debits + dispatches;
  - `Cancel` with an active bidder is rejected;
  - `RequestList` caches the stubbed char response.
- Integration with char-side `int_auction` semantics (winner/seller payouts via mail).
- Manual/live: register → bid from a second char → buy-now → confirm item + zeny movement and mail refunds.

## Where the work splits (map vs. char)

- **Map (this ticket):** the `AuctionService` + handlers; inventory escrow on register; zeny escrow on bid/buy-now; gate validation against the cached list; dispatch the 5 IPC calls; emit the client packets on response.
- **Char (already real):** `Char.Server/CharGrpcService.cs` auction RPC overrides allocate the `auction_id`, write/read the `auction` table, run the `auction_end_timer`, and issue the winner/seller/refund **mail**. Do not duplicate any of this map-side.

## AuctionData fields (IPC, already defined)

`Core.Server.IPC.AuctionData` (packed in `IntifService.AuctionRegister` :398): `SellerCharacterId, SellerName, ItemId, ItemType, Refine, Attribute, Price (start), BuyNow, Hours`. Char computes `EndTimeUnix = now + Hours*3600`. The full escrowed item (cards, options) needs to ride along — confirm `AuctionData` carries the item's card/option payload; **extend the proto if it only has `ItemId`/`Refine`/`Attribute`** (a real item escrow needs the full item struct).

## Notes / gotchas

- All payouts/refunds go through **mail** (FEATURE-05) — auction has no direct hand-to-hand transfer. Land FEATURE-05's `GetAttachment` credit path first or auction winners can't claim.
- Auction expiry is a **char-side** timer (`auction_end_timer`); the map must not run its own expiry — just refresh the cached list.
- `IntifService.AuctionRegister` already computes `EndTimeUnix = now + hours*3600` char-side; pass `hours` only.
- Listing fee + escrow must be reverted if the synchronous register gate fails.
- Bid validation needs the *current* high bid, which lives char-side; the map's cached list may be stale — either re-fetch before accepting a bid or let the char side be the final authority (refund the loser on a late-loss). rAthena lets the char side arbitrate; mirror that.
- The listing fee is `AUCTION_FEEPERCENT` of the start price (or a flat fee per config) — deduct on register.
