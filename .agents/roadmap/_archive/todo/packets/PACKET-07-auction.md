# PACKET-07-auction — Auction client→map packet bridge

> **Epic:** Packet bridge · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** FEATURE-auction (CharServerIpcService.Auction + Intif auction RPCs exist) · **Blocks:** none

## Problem

The auction char-side IPC wrappers exist
(`Map.Server/Services/ICharServerIpcService.Auction.cs`: `AuctionRequestListAsync`,
`AuctionRegisterAsync`, `AuctionCancelAsync`, `AuctionCloseAsync`, `AuctionBidAsync`) and
`IIntifService` mirrors them (`AuctionRequestList`, `AuctionRegister`, `AuctionCancel`,
`AuctionClose`, `AuctionBid`). But **no client→map auction packet is wired**. A player cannot
open the auction window, register an item, search/list, bid, or cancel from the client.

## Current state (C#)

- No handler exists for any auction packet.
- `Map.Server/Services/ICharServerIpcService.Auction.cs` — async gRPC wrappers returning typed
  responses (`AuctionRegisterResponse`, `AuctionBidResponse`, etc.).
- `Map.Server/Services/Intif/IIntifService.cs:72-76` — `AuctionRequestList(charId, type, price,
  search, page)`, `AuctionRegister(charId, type, sellerCharId, sellerName, now, hours, priceStart,
  priceBuyNow, itemId, refine, attribute, identify, amount)`, `AuctionCancel(charId, auctionId)`,
  `AuctionClose(charId, auctionId)`, `AuctionBid(charId, auctionId, bid, bidder)`.

## rAthena reference (source of truth)

`rathena/src/map/clif.cpp` parse functions to port:

- `clif_parse_Auction_buysell` (`clif.cpp:17193`) → open auction window (buy/sell tab). This is the
  `CZ_AUCTION_*` open / `CZ_AUCTION_CREATE` entry; it also implicitly cancels any pending register.
- `clif_parse_Auction_setitem` (`clif.cpp:16949`) → stage the item to auction (`CZ_AUCTION_ADD_ITEM`).
- `clif_parse_Auction_cancelreg` (`clif.cpp:16938`) → cancel the staged register (un-stage item).
- `clif_parse_Auction_register` (`clif.cpp:17031`) → finalize registration: now-price, buy-now-price,
  duration hours. Validates fee, price ceilings, item state → `intif_Auction_register`.
- `clif_parse_Auction_cancel` (`clif.cpp:17120`) → cancel an active own auction (`intif_Auction_cancel`).
- `clif_parse_Auction_close` (`clif.cpp:17128`) → end-now / close (`intif_Auction_close`).
- `clif_parse_Auction_bid` (`clif.cpp:17136`) → place a bid (`intif_Auction_bid`); zeny escrow.
- `clif_parse_Auction_search` (`clif.cpp:17170`) → search/list (`intif_Auction_requestlist`) with
  filter type + price + search string + page.

ZC responses: `ZC_AUCTION_RESULT` (register/bid/cancel/close result code), `ZC_AUCTION_ITEM_REQ_SEARCH`
(search result list page), `ZC_ACK_AUCTION_ADD_ITEM` (set-item ack), `ZC_AUCTION_WINDOW_OPEN`.
**Read `clif_packetdb.hpp` for the `CZ_AUCTION_*` / `ZC_AUCTION_*` numeric ids.**

## Scope — every sub-system that must be touched

- [ ] **In packets** (`Core.Server/Packets/In/CZ/`):
  - [ ] `CZ_AUCTION_CREATE` / open (`clif_parse_Auction_buysell`) — `<type>.B`.
  - [ ] `CZ_AUCTION_ADD_ITEM` (`clif_parse_Auction_setitem`) — `<index>.W <amount>.L`.
  - [ ] `CZ_AUCTION_ADD_CANCEL` (`clif_parse_Auction_cancelreg`) — header-only.
  - [ ] `CZ_AUCTION_ADD` (`clif_parse_Auction_register`) — `<now_price>.L <max_price>.L <hours>.W`.
  - [ ] `CZ_AUCTION_REQ_MY_SELL_STOP` / cancel (`clif_parse_Auction_cancel`) — `<auction_id>.L`.
  - [ ] `CZ_AUCTION_REQ_MY_INFO` / close (`clif_parse_Auction_close`) — `<auction_id>.L`.
  - [ ] `CZ_AUCTION_BUY` (`clif_parse_Auction_bid`) — `<auction_id>.L <bid>.L`.
  - [ ] `CZ_AUCTION_ITEM_SEARCH` (`clif_parse_Auction_search`) — `<type>.W <auction_id>.L
        <search>.24B <page>.W`.
- [ ] **Out packets** (`Core.Server/Packets/Out/ZC/`): `ZC_AUCTION_RESULT`,
      `ZC_AUCTION_ITEM_REQ_SEARCH` (var-len list), `ZC_ACK_AUCTION_ADD_ITEM`, open-window ack.
- [ ] **PacketHeader.cs** + **appsettings.packets.json** (search + list var-len).
- [ ] **Handlers** (`Map.Server/Handlers/Auction/`):
  - [ ] `AuctionOpenHandler` → stage register state; cancel any prior staged item.
  - [ ] `AuctionSetItemHandler` → stage item (escrow), emit set-item ack.
  - [ ] `AuctionCancelRegHandler` → un-stage (refund).
  - [ ] `AuctionRegisterHandler` → validate fee/price ceilings, then
        `ICharServerIpcService.AuctionRegisterAsync` (or `IIntifService.AuctionRegister`); emit
        `ZC_AUCTION_RESULT` from the response code.
  - [ ] `AuctionCancelHandler` → `AuctionCancelAsync` → result.
  - [ ] `AuctionCloseHandler` → `AuctionCloseAsync` → result.
  - [ ] `AuctionBidHandler` → escrow bid zeny, `AuctionBidAsync` → result (refund on fail).
  - [ ] `AuctionSearchHandler` → `AuctionRequestListAsync` → emit `ZC_AUCTION_ITEM_REQ_SEARCH` page.
- [ ] No new char-side RPC — auction RPCs exist (async wrappers).

## Done criteria

- Staging an item escrows it; cancel-register refunds it; register charges the listing fee and
  enforces the price ceiling (rAthena `battle_config` auction caps) before calling the RPC.
- Bidding escrows the bidder's zeny and refunds it if the bid is rejected (outbid handled on the
  char side); search returns a filtered page list matching the request type/price/string.
- Cancel/close own auction returns the item or proceeds to settlement per char-side result codes.
- Every action emits the correct `ZC_AUCTION_RESULT` code; no stub, no `// TODO`.

## Test plan

- Handler tests pinning: register below fee → reject; register above price ceiling → reject; bid
  refund path on failed bid; search args forwarded verbatim (type/price/string/page).
- Manual: register an item, search and find it, bid from a second account, close/cancel.

## Notes / gotchas

- Auction wrappers are **async** (`Task<...Response>`) — handlers must `await` them off the parse
  call without blocking the game loop, and emit the ZC when the task resolves (mirror how other
  async-IPC handlers structure this in `Map.Server/Handlers`).
- Item staging is escrow: the item must physically leave inventory when set and be refunded on
  cancel — do not just mark a flag.
- `clif_parse_Auction_search` reuses one packet for the search filter + paging; decode all fields.
