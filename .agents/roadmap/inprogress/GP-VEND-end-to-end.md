# GP-VEND — Vending works end-to-end

> **Epic:** gameplay · **Status:** 🚧 In progress · **Size:** M · **Player-visible:** yes
> **Depends on:** none · **Unlocks:** GP-BUYSTORE (shared shop packet patterns)

## The deliverable

> A player can **open a vending shop from their cart, other players see the stall, click it,
> browse the price list, and buy items (zeny → vendor minus tax, item → buyer)** — live
> client; an autotrading vendor stays open across logout.

## Player story

Player-run shops drive the economy. The *transfer* logic is real (sells from cart, buyer pays
full, vendor gets total-minus-tax, fidelity, anti-desync, sold-out auto-close — archive
FEATURE-11), but no client packet reaches it (open/list/purchase) and autotrade persistence +
the overweight gate are missing.

## Current state — per layer

| Layer | State | Where |
|---|---|---|
| Service | ✅ verify | `Map.Server/Shop/Vending/VendingService.cs` — `Update`/`PurchaseReq` real transfer + `VendingTaxBp` (archive FEATURE-11) |
| CZ handlers | ❌ | open-vending, vending-list-req, purchase-req missing |
| ZC emits | ❌ | stall on-map, vending item list, purchase result, item-update missing |
| Persistence | ❌ | autotrade offline-vendor row + NPC (archive FEATURE-35) |
| Overweight gate | ❌ | buyer-overweight refusal (archive FEATURE-35) |

## rAthena reference

- `rathena/src/map/vending.cpp` — `vending_openvending`, `vending_vendinglistreq`,
  `vending_purchasereq` (tax = `battle_config.vending_tax`), `vending_reopen` (autotrade),
  `do_init_vending_autotrade`.
- `rathena/src/map/clif.cpp` — parse `CZ_REQ_OPENSTORE2`/`CZ_REQ_CLOSESTORE`,
  `CZ_REQ_BUY_FROMMC`, `CZ_PC_PURCHASE_ITEMLIST_FROMMC`; emit `clif_openvending`,
  `clif_vendinglist`, `clif_buyvending`, `clif_vending_*`.

## Dependencies — and how to satisfy

- Packet-bridge pattern — foundation.
- Autotrade persistence — new offline-vendor table + a vendor-NPC spawn at the saved cell; an
  `@at`/autotrade flow keeps the stall after logout (build it here, archive FEATURE-35).

## Scope — every layer

- [~] **CZ handlers**: open-store (`CZ_REQ_OPENSTORE2` 0x01b2 → `OpenStoreHandler`, offers validated
      against the cart) + close-store (`CZ_REQ_CLOSESTORE` 0x012e → `CloseStoreHandler`) — turn 1.
      Remaining: vending-list-req (click a stall) + purchase-from-MC.
- [ ] **Service**: verify `PurchaseReq` at HEAD; add the **buyer-overweight gate**.
- [~] **ZC emits**: stall sign on-map (`ZC_STORE_ENTRY` 0x0131, AOI) + open ack (`ZC_ACK_OPENSTORE2`
      0x0a28) + stall-disappear (`ZC_DISAPPEAR_ENTRY` 0x0132) via new `IVendingClientService` — turn 1.
      Remaining: vending item list, purchase result, buyer/vendor item-amount updates.
- [ ] **Persistence**: autotrade — persist the open stall + offers; respawn an autotrade
      vendor NPC at the saved map/cell on boot (`vending_reopen`).
- [x] **Wiring**: AOI broadcast of the stall sign (turn 1, `IVisibilityService.SendToArea` AreaWos).

## Done criteria

- Vendor opens a shop from cart → nearby players see the stall + name → a buyer clicks, sees
  the list, buys 3 potions → buyer −price, vendor +price−tax, cart −3; sold-out auto-closes.
- Buyer over weight limit is refused with no transfer.
- Autotrade vendor stays open + sellable after the owner logs out; relog rehydrates.

## Test plan

- Handler tests: open/list/purchase → service.
- Service: tax, overweight refusal (extend archived VendingServiceTests).
- Persistence: autotrade rehydrate round-trip.
- Live: open → buy → sold-out close → autotrade relog.

## Progress log (multi-turn vertical)

- **2026-06-04 (turn 1)** — Open + close + stall sign. New packets `CZ_REQ_OPENSTORE2` (0x01b2, variable
  `<name>.80 <flag>.B {index.W amount.W price.L}*`), `CZ_REQ_CLOSESTORE` (0x012e), `ZC_STORE_ENTRY`
  (0x0131, 86B stall sign), `ZC_DISAPPEAR_ENTRY` (0x0132), `ZC_ACK_OPENSTORE2` (0x0a28). New
  `IVendingClientService`/`VendingClientService` (stall sign → area-WOS via `IVisibilityService`, open
  ack → vendor session) wired into `VendingService.Update`/`CloseVending`. `OpenStoreHandler` parses the
  offers, converts the cart client index → server index, validates each against the live cart (held +
  in-stock + price ≥ 0), and opens via `Update`; an empty name / no valid offers doesn't open.
  `CloseStoreHandler` tears the stall down. `VendingOpenCloseTests` (6: offer validation/index-convert,
  empty-name + no-valid-offer rejects, close routing, stall-sign+ack emit, disappear emit); full suite
  4486 pass (1 = standing replay-fixture).
- **Remaining (next turns → done):** vending-list-req (buyer clicks a stall → the item list,
  `ZC_PC_PURCHASE_ITEMLIST_FROMMC`) + purchase (`CZ_PC_PURCHASE_ITEMLIST_FROMMC` → `PurchaseReq` +
  result/item-update emits) + the vendor's own-list on open (`ZC_PC_PURCHASE_MYITEMLIST`) + the
  buyer-overweight gate + autotrade persistence (FEATURE-35). The loop resumes this card.

## Notes / gotchas

- `VendingTaxBp` basis-points const already in the service; per-open `VenderId` anti-desync.
- Cart is `session.Cart`; build `InventoryItem` with full fidelity on transfer (archive FEATURE-11).
- The wire offer index is the cart **client** index (server cart index + 2) — `OpenStoreHandler`
  converts it before storing so `PurchaseReq`'s `FindBySlot(cart, serverIndex)` matches.
