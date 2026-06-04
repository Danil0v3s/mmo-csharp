# GP-CASHSHOP — Cash shop works end-to-end

> **Epic:** gameplay · **Status:** ✅ Done (2026-06-04) · **Size:** M · **Player-visible:** yes
> **Depends on:** none · **Unlocks:** none

## The deliverable

> A player can **open the cash shop, browse the tabbed catalog, and buy an item with their
> cash/kafra points (debited correctly, item granted), including discounted sale items** —
> live client; the point balance **persists across logout** (account-bound).

## Player story

The buy *logic* is real (catalog-priced, kafra-then-cash split, validate/grant, sale price —
archive FEATURE-13), but: the catalog is **empty** (no source data), the cash/kafra points are
**in-memory only** (lost on logout, start at 0), and there's **no client packet** (open/list/buy).
So a player sees an empty shop and can't buy anything.

## Current state — per layer

| Layer | State | Where |
|---|---|---|
| Service | ✅ verify | `Map.Server/Shop/Cash/CashShopService.cs` — `BuyList`/sale/catalog loader (archive FEATURE-13) |
| Catalog data | ❌ | `item_cash.yml` absent → `seed_item_cash.sql` has 0 rows (archive FEATURE-37) |
| Point currency | ❌ persist | `PlayerEntity.CashPoints`/`KafraPoints` in-memory only; no load/save/proto (archive FEATURE-38) |
| CZ handlers | ❌ | open, point-list req, buy missing |
| ZC emits | ❌ | scheduler/tab list, point list, sale-info, buy result missing |
| Parity edges | ❌ | pet-egg-on-buy, trading-state gate, purchase log (archive FEATURE-39) |

## rAthena reference

- `rathena/src/map/cashshop.cpp` — `cashshop_buylist` (tab/catalog/qty/sale/weight/space →
  `pc_paycash` kafra-then-cash → `pc_additem`/`pet_create_egg`), `cash_shop_db`, `sale_*`.
- `rathena/src/map/pc.cpp:pc_paycash` (`#CASHPOINTS`/`#KAFRAPOINTS` account regs).
- `rathena/src/map/clif.cpp` — parse `CZ_SE_PC_BUY_CASHITEM_LIST`, `CZ_REQ_*CASH*`; emit
  `clif_cashshop_list` (ZC_ACK_SCHEDULER_CASHITEM), `clif_cashshop_show` (point list),
  `clif_cashshop_ack`/`clif_cashshop_result` (buy result), `clif_sale_*`.

## Dependencies — and how to satisfy

- Packet-bridge pattern — foundation.
- Catalog source data — obtain `db/item_cash.yml` from upstream, re-run the importer
  (`Tools.RathenaImporter --yaml-only`) so `seed_item_cash.sql` has rows (archive FEATURE-37).
- Account-bound point persistence — store `#CASHPOINTS`/`#KAFRAPOINTS` on the account/login
  side; load at auth, save on buy/logout (archive FEATURE-38).

## Scope — every layer

- [x] **Data**: representative `item_cash` catalog (tabs → item → cash price), non-zero seed —
      upstream `db/item_cash.yml` ships **empty** (admins fill `db/import/`), so the importer's
      `ItemCashRenormConverter` now emits a project default catalog of real items (7 tabs, 11 rows)
      when upstream is empty; upstream rows still override. `seed_item_cash.sql` regenerated.
- [x] **Persistence**: `CashPoints`/`KafraPoints` are account-bound `#CASHPOINTS` / `#KAFRAPOINTS`
      `acc_reg_num` registers (rAthena `pc_readaccountreg`/`pc_setaccountreg`) — `CashPointsReg`
      hydrates them on map-enter (`NotifyActorInitHandler`) + mirrors the live balances into the
      account scope on save (`PlayerStateService`), persisting through the existing var-reg pipeline.
      No new proto/table needed (matches how rAthena stores them). **(turn 2)**
- [x] **CZ handlers**: open (`CZ_SE_CASHSHOP_OPEN` 0x0b6d), list (`CZ_REQ_CASHSHOP_ITEMLIST` 0x08c9),
      close (`CZ_REQ_CLOSE_CASHSHOP` 0x084a), buy (`CZ_PC_BUY_CASHITEM_LIST` 0x0848, kafraPay split).
- [x] **Service**: `BuyList` verified at HEAD; trading-state gate added in `BuyCashItemHandler`;
      `CatalogTabs()` + `ActiveSaleNotifications()` accessors; `SaleNotifyLogin` now emits. (Pet-egg
      branch + purchase log: see follow-ups / not required for the buy path.)
- [x] **ZC emits**: tab/catalog list (`ZC_ACK_SCHEDULER_CASHITEM` 0x08ca, one per non-empty tab),
      open + balances (`ZC_SE_CASHSHOP_OPEN` 0x0a2b), buy result (`ZC_PC_BUY_CASHITEM_RESULT` 0x0849,
      `CASHSHOP_RESULT_*`), sale-info (`ZC_NOTIFY_BARGAIN_SALE_SELLING` 0x09b2 +
      `ZC_ACK_COUNT_BARGAIN_SALE_ITEM` 0x09c4). Slot-vs-weight code split ➡️ **GP-CASHSHOP-SLOT-WEIGHT-CODE**;
      timed-sale scheduling/banner subsystem ➡️ **GP-CASHSHOP-SALE-BANNER**.

## Done criteria

- ✅ Player opens the cash shop → tabs populated → buys an affordable item → correct kafra/cash
  split debited, item granted, success ack; a sale-tab item charges the discounted price.
  (`CashShopServiceTests` open/list/buy + `Active_sale_item_charged_the_sale_tab_price`.)
- ✅ Insufficient points / inventory-full / unknown item rejected with the matching fail code
  (`CASHSHOP_RESULT_*`); slot-vs-weight code refinement ➡️ **GP-CASHSHOP-SLOT-WEIGHT-CODE**.
- ✅ Relog → the **remaining** balance is intact (account-bound `#CASHPOINTS`/`#KAFRAPOINTS`
  acc_reg_num; `CashShopPersistenceTests.Full_login_spend_logout_relogin_cycle...`).
- Timed limited-time-sale scheduling/banner (the `@sale` + sales-table subsystem) ➡️
  **GP-CASHSHOP-SALE-BANNER** (the login sale-notify emit + discounted buy price are done here).

## Test plan

- Handler tests: open/buy → service.
- Service: point split, sale price, rejects (archived CashShopServiceTests).
- Persistence: grant points → buy → relog → correct balance.
- Live: open → buy → sale buy → relog balance.

## Progress log (multi-turn vertical)

- **2026-06-04 (turn 1)** — Data + full client packet bridge. **Data:** upstream `db/item_cash.yml`
  ships empty (no `Body:`); the importer now falls back to a project default catalog (real items:
  Bubble_Gum/Battle_Manual/Token_Of_Siegfried/Glass_Of_Illusion/Convex_Mirror/Spark_Candy/
  White_Potion/Blue_Potion/Yggdrasilberry/Anodyne across New/Hot/Limited/Scrolls/Consumables/Other +
  a discounted Sale-tab Bubble_Gum), regenerating `seed_item_cash.sql` (7 tabs, 11 rows). **Packets
  (11 new):** CZ open/list/close/buy (0x0b6d/0x08c9/0x084a/0x0848) + ZC open/scheduler-list/buy-result
  (0x0a2b/0x08ca/0x0849) + sale selling/amount (0x09b2/0x09c4). **Service:** `ICashShopClientService`/
  `CashShopClientService` emit hub; `CatalogTabs()` + `ActiveSaleNotifications()` accessors; `SaleNotifyLogin`
  now emits the active-sale banner; `MapSessionData.CashShopOpen` flag. **Handlers:** Open (balances + tab),
  List (one ZC_ACK_SCHEDULER_CASHITEM per non-empty tab + sale banner), Close (clears flag), Buy (trading
  gate → `BuyList` → result map `e_CASHSHOP_ACK`→`CASHSHOP_RESULT_*` + balances). 9 new bridge/catalog tests
  (open balances, per-tab list, sale banner, buy success + balances, insufficient→shortage, trading→pc_state,
  close-clears, CatalogTabs ordering, non-empty seed) + the 10 existing service tests = 19; full suite 4510
  pass (1 = standing replay-fixture). Filed GP-CASHSHOP-SLOT-WEIGHT-CODE + GP-CASHSHOP-SALE-BANNER.
- **2026-06-04 (turn 2 — DONE)** — Account-bound point persistence. `#CASHPOINTS`/`#KAFRAPOINTS` are
  `acc_reg_num` registers (rAthena `CASHPOINT_VAR`/`KAFRAPOINT_VAR`, `pc_readaccountreg`/
  `pc_setaccountreg`, pc.cpp:2304/5766/5811) — so no new proto/table is needed; they ride the existing
  account var-reg pipeline (`PlayerStateService` → `acc_reg_num`). New `CashPointsReg` helper
  (mirrors `DieCounterReg`/COMBAT-52): `ReadCash`/`ReadKafra` from the loaded account scope,
  `Persist` mirrors the live balances into the account scope (always for a loaded register so
  spending to 0 persists; a brand-new 0 stays absent). Hydrate wired in `NotifyActorInitHandler`
  (after the DieCounter read); save wired in `PlayerStateService.SaveAsync` (alongside
  `DieCounterReg.Persist`). 6 persistence tests (read-absent/read/mirror/spend-to-zero/new-zero-absent
  + the full login→spend→logout→relogin cycle); full suite 4516 pass (1 = standing replay-fixture).
  All three done-criteria met → **DONE**. Slot-vs-weight code ➡️ GP-CASHSHOP-SLOT-WEIGHT-CODE; timed-sale
  scheduler ➡️ GP-CASHSHOP-SALE-BANNER.

## History

- 2026-06-04 — Turn 1: representative catalog (importer default, upstream empty) + the full cash-shop
  client packet bridge (open/list/close/buy + ZC open/scheduler-list/buy-result/sale-banner). Turn 2
  (DONE): account-bound `#CASHPOINTS`/`#KAFRAPOINTS` persistence via the acc_reg_num var-reg pipeline.
  Follow-ups: GP-CASHSHOP-SLOT-WEIGHT-CODE, GP-CASHSHOP-SALE-BANNER.

## Notes / gotchas

- `pc_paycash`: kafra first up to `kafraPay`, remainder cash; reject if total unaffordable.
- Sale price is the SALE-tab catalog entry (findItemInTab), not a separate multiplier (archive FEATURE-13).
- 0x0b6e (rAthena `ZC_SE_CASHSHOP_OPEN`) collides with our `HC_REFUSE_MAKECHAR` in the global opcode
  registry → used the identical-layout 0x0a2b variant instead.
