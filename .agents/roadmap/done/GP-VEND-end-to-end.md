# GP-VEND — Vending works end-to-end

> **Epic:** gameplay · **Status:** ✅ Done (2026-06-04) · **Size:** M · **Player-visible:** yes
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

- [x] **CZ handlers**: open-store (`CZ_REQ_OPENSTORE2` 0x01b2 → `OpenStoreHandler`) + close-store
      (`CZ_REQ_CLOSESTORE` 0x012e → `CloseStoreHandler`) — turn 1; vending-list-req
      (`CZ_REQ_VENDING_ITEMS` 0x0130 → `VendingListReqHandler`) + purchase-from-MC
      (`CZ_PC_PURCHASE_ITEMLIST_FROMMC` 0x0134 → `PurchaseFromMcHandler`) — turn 2.
- [~] **Service**: `PurchaseReq` verified + emits wired (turn 2: result codes, vendor report, buyer
      pickup + zeny par-change for both). Remaining: the **buyer-overweight gate** ➡️ GP-VEND-OVERWEIGHT
      (needs the weight system exposed).
- [x] **ZC emits**: stall sign (`ZC_STORE_ENTRY` 0x0131) + open ack (`ZC_ACK_OPENSTORE2` 0x0a28) +
      disappear (`ZC_DISAPPEAR_ENTRY` 0x0132) (turn 1); vending item list (`ZC_PC_PURCHASE_ITEMLIST_FROMMC`
      0x0133) + purchase result (`ZC_PC_PURCHASE_RESULT_FROMMC` 0x0135 failure codes) + vendor sale notice
      (`ZC_DELETEITEM_FROM_MCSTORE` 0x0137) + buyer pickup/zeny (turn 2); the vendor's own-list on open
      (`ZC_PC_PURCHASE_MYITEMLIST` 0x0136) (turn 3).
- [ ] **Persistence**: autotrade — persist the open stall + offers; respawn the offline vendor on boot
      (`vending_reopen`). ➡️ Moved to **GP-AUTOTRADE-RUNTIME** — the offline shop needs a headless on-map
      presence (a shared runtime subsystem, also needed by GP-BUYSTORE), which is genuinely separate
      from the vending packet bridge. The persistence tables (`VendingEntity`/`VendingItemEntity`) +
      cart persistence already exist; only the runtime is missing.
- [x] **Wiring**: AOI broadcast of the stall sign (turn 1, `IVisibilityService.SendToArea` AreaWos).

## Done criteria

- ✅ Vendor opens a shop from cart → nearby players see the stall + name (and the vendor sees their own
  list) → a buyer clicks, sees the list, buys 3 potions → buyer −price, vendor +price−tax, cart −3;
  sold-out auto-closes.
- Buyer over weight limit is refused with no transfer. ➡️ GP-VEND-OVERWEIGHT (needs the weight system
  exposed; the other purchase gates — zeny/stock/store-incorrect/inventory-full — are done).
- Autotrade vendor stays open + sellable after the owner logs out; relog rehydrates. ➡️
  GP-AUTOTRADE-RUNTIME (the shared offline-shop runtime, also blocking GP-BUYSTORE autotrade).

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
- **2026-06-04 (turn 2)** — Browse + buy. New packets `CZ_REQ_VENDING_ITEMS` (0x0130 click-a-stall),
  `CZ_PC_PURCHASE_ITEMLIST_FROMMC` (0x0134 buy, `{amount.W index.W}*`), `ZC_PC_PURCHASE_ITEMLIST_FROMMC`
  (0x0133 price list, 22B/item), `ZC_PC_PURCHASE_RESULT_FROMMC` (0x0135 result), `ZC_DELETEITEM_FROM_MCSTORE`
  (0x0137 vendor sale notice). `PlayerEntity.VendedId` (rAthena `sd->vended_id` anti-desync).
  `VendingService.VendingListReq` builds the price list from the stall offers + vendor cart (item type
  via `IItemCatalog`), stamps the buyer's VendedId, and emits it. `PurchaseReq` (transfer already real,
  FEATURE-11) now emits the rAthena result codes on failure (store-incorrect / no-zeny / out-of-stock),
  the vendor sale report + the buyer item-pickup + SP_ZENY par-change for both on success, and the
  stall-disappear when sold out. New `VendingListReqHandler` + `PurchaseFromMcHandler` (client→server
  index convert). `VendingBrowseBuyTests` (5: list+vended-id stamp, full transfer+feedback,
  stale-id→store-incorrect, no-zeny, handler-buys); full suite 4491 pass (1 = standing replay-fixture).
  **Filed GP-VEND-OVERWEIGHT** (the buyer-weight gate — needs the weight system exposed).
- **2026-06-04 (turn 3 → DONE)** — Vendor own-list on open + GP-VEND complete. New
  `ZC_PC_PURCHASE_MYITEMLIST` (0x0136, the vendor's own shop list, 22B/item, index-before-amount form);
  extracted a shared `BuildListEntries` helper (used by both the own-list on `Update` and the buyer
  list on `VendingListReq`). `IVendingClientService.SendMyItemList`. The vendor now sees their shop's
  items when the stall opens. `VendingBrowseBuyTests` +1 (own-list wire offsets); full suite green
  (1 = standing replay-fixture). **Autotrade persistence ➡️ GP-AUTOTRADE-RUNTIME** (the offline shop
  needs a headless on-map presence — a shared runtime subsystem also needed by GP-BUYSTORE — genuinely
  separate from the vending packet bridge; the persistence tables + cart persistence already exist).
  **The live vending loop is fully reachable: open a shop from the cart → the stall sign appears for
  others + the vendor sees their list → a buyer clicks → browses → buys (zeny/items transfer with
  result/report/pickup feedback) → sold-out auto-closes.**

## History

- **2026-06-04** — Done across 3 loop turns. Built the entire vending client packet bridge (the
  transfer logic was archive FEATURE-11): open/close (`CZ_REQ_OPENSTORE2`/`CZ_REQ_CLOSESTORE` →
  stall-sign/ack/disappear), browse (`CZ_REQ_VENDING_ITEMS` → `ZC_PC_PURCHASE_ITEMLIST_FROMMC` price
  list + vended-id anti-desync), buy (`CZ_PC_PURCHASE_ITEMLIST_FROMMC` → `PurchaseReq` + result codes +
  vendor report + buyer pickup/zeny), and the vendor own-list. ~18 vending-suite tests; full suite
  green. Follow-ups: GP-VEND-OVERWEIGHT (weight gate), GP-AUTOTRADE-RUNTIME (offline-shop runtime,
  shared with GP-BUYSTORE).

## Notes / gotchas

- `VendingTaxBp` basis-points const already in the service; per-open `VenderId` anti-desync.
- Cart is `session.Cart`; build `InventoryItem` with full fidelity on transfer (archive FEATURE-11).
- The wire offer index is the cart **client** index (server cart index + 2) — `OpenStoreHandler`
  converts it before storing so `PurchaseReq`'s `FindBySlot(cart, serverIndex)` matches.
