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

- [ ] **CZ handlers**: open-store (with cart item/price list), close-store, vending-list-req
      (click a stall), purchase-from-MC.
- [ ] **Service**: verify `PurchaseReq` at HEAD; add the **buyer-overweight gate**.
- [ ] **ZC emits**: stall sign on-map (vendor name), vending item list, purchase result,
      buyer/vendor item-amount updates.
- [ ] **Persistence**: autotrade — persist the open stall + offers; respawn an autotrade
      vendor NPC at the saved map/cell on boot (`vending_reopen`).
- [ ] **Wiring**: vendor sit/stall state + AOI broadcast of the stall.

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

## Notes / gotchas

- `VendingTaxBp` basis-points const already in the service; per-open `VenderId` anti-desync.
- Cart is `session.Cart`; build `InventoryItem` with full fidelity on transfer (archive FEATURE-11).
