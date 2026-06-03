# FEATURE-26 — Auction full-item fidelity (cards/options) + search-type bucketing

> **Epic:** Feature unlock · **Status:** ❌ Not started · **Size:** S · **Player-visible:** yes
> **Depends on:** FEATURE-06 (auction escrow) · **Blocks:** none

## Problem

FEATURE-06 escrows the listed item out of the seller's inventory and dispatches it to the char-side
auction RPC, but the cross-process auction record only carries **id + refine + attribute** — the
item's **cards and random options are dropped**. So auctioning a carded/optioned item delivers a
plain item to the winner. Two gaps:

1. **Item fidelity** — `AuctionData` has an `item_payload` bytes field, but the char-side
   `AuctionRegister` (and the `AuctionEntity` row) only persist `Refine`/`Attribute`. The cards,
   random options, bound, enchant-grade, unique-id are lost, and the win/refund **mail** sends only
   the simplified item.
2. **Search-type bucketing** — `AuctionService.AuctionType` maps the item to a coarse bucket
   (Weapon→1 / Armor→0 / Card→2 / else→3); rAthena's auction search-type enum is finer.

## Current state (C#)

- `Map.Server/Auction/AuctionService.cs` — `RegisterAsync` builds `AuctionData` with id/refine/
  attribute only; `AuctionType(nameId)` is the coarse bucket.
- `Char.Server/CharGrpcService.cs` `AuctionRegister` — persists `AuctionEntity { Refine, Attribute }`
  (no card columns); the win/refund mail uses that simplified item.
- `Core.Database/Entities/AuctionEntity.cs` — no card/option columns.
- `Core.Server/Protos/char_service.proto` `AuctionData` — has `item_payload` (unused) but no
  structured card/option fields.

## rAthena reference (source of truth)

- `rathena/src/char/int_auction.cpp` — the auction row stores the full `struct item` (cards, options,
  bound, etc.); `auction_end_timer` mails that full item to the winner / back to the seller.

## Scope

- [ ] Carry the full escrowed item across IPC — either serialize the `InventoryItem` into
      `AuctionData.item_payload` (codec) and parse it char-side, or add structured card/option columns
      to `AuctionEntity` + `AuctionData`. EF migration if columns are added.
- [ ] Char-side: persist the full item and mail it with full fidelity on win/refund/return (reuse the
      FEATURE-05 mail-attachment shape — `MailAttachmentItem` already carries cards/options).
- [ ] `AuctionService.AuctionType` — match rAthena's auction search-type enum precisely.

## Done criteria

- Auctioning a carded + optioned item delivers the same item (cards, options, refine, grade) to the
  winner; a returned/cancelled listing restores it intact to the seller.
- Search-type filtering matches rAthena's buckets.

## Test plan

- `AuctionServiceTests` — register a carded item, assert the dispatched `AuctionData` carries the
  cards/options; char-side round-trip if a harness exists.

## Notes / gotchas

- `MailAttachmentItem` (FEATURE-05) already models the full item for the payout mail — reuse it as the
  auction item shape to keep one codec.
