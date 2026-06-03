# FEATURE-13 — Cash shop

> **Epic:** Gameplay-Shop · **Status:** ✅ Done (2026-06-03) · **Size:** M · **Player-visible:** yes
> **Depends on:** none · **Blocks:** none
> **Related:** PACKET-08 (cash-shop UI packets)

## Problem

The cash shop sale-timer machinery works (GM-scheduled sales activate/expire on
timers), but **the buy path is a hard `return false`**: `BuyList` refuses every
purchase ("catalog load pending"). There is no `cashshop_db` catalog, so the
canonical price + tab membership a purchase needs don't exist, and the
cash-point currencies are never debited nor items granted. A player cannot buy
anything from the cash shop.

## Current state (C#)

- `Map.Server/Shop/Cash/CashShopService.cs`:
  - `BuyList(pc, items)` (`:20`) — *"Real fulfillment requires cashshop_db.yml load... For now refuse with an audit log"* → logs + `return false;` (`:28`). **Unconditional refusal.**
  - Sale machinery (real): `SaleAddItem` (`:47`, schedules start/end timers), `SaleFindItem` (`:70`), `SaleRemoveItem` (`:73`), `SaleNotifyLogin` (`:80`), `Reload`/`ReloadDb` (`:31`/`:40`), `Dispose` (`:89`). `_sales` keyed by item id.
- **No `cashshop_db` catalog** loaded (no repository, no entity, no `_catalog`).
- Cash-point currencies (`CASHPOINT` / `KAFRAPOINT`) — confirm `PlayerEntity` exposes them; the debit path doesn't exist.

## rAthena reference (source of truth)

- `rathena/src/map/cashshop.cpp`:
  - `cashshop_buylist(sd, kafra_pay, n, item_list)` — the real purchase:
    - per item: look up the `cash_shop_db` entry (tab + price); apply the active **sale** discount (`sale_find_item`) if any; sum the cost.
    - validate inventory space + stack limits for all items first.
    - **Debit**: split the cost across `#CASHPOINTS` and `#KAFRAPOINTS` (kafra points used first up to `kafra_pay`, remainder from cash points); reject if insufficient (`ERROR_TYPE_MONEY`).
    - **Grant**: `pc_additem` each item (with the cash-shop "from cash shop" flag).
    - `clif_cashshop_ack` (success/fail code) + `clif_cashshop_result`.
  - `cash_shop_db` (`db/cashshop_db.yml`) — tabs (NewItem/Popular/Limited/Rental/etc.) → item id + cash price.
  - `sale_*` (the sale window machinery) already mirrored in C#.

## Scope — every sub-system that must be touched

- [ ] **Catalog**: add the `cashshop_db` EF entity + repository (item id, tab, cash price) seeded from `db/cashshop_db.yml`, and load it into a `_catalog` in `CashShopService` (mirror the `Reload()` pattern used by quest/pet/instance services). `ReloadDb` reloads it.
- [ ] Confirm/add cash-point currencies on `PlayerEntity` (`CashPoints`, `KafraPoints`) and the debit/credit accessors.
- [ ] `BuyList` — **implement the real purchase**: resolve each item's catalog price (apply active sale discount via `SaleFindItem`), validate inventory space for all items, debit kafra-then-cash points (with the `kafraPay` split), grant each item, emit the cash-shop ack/result. Reject (insufficient points / inventory full / unknown item / wrong tab) with the matching fail code. Remove the unconditional `return false`.
- [ ] **Sale discount integration**: `BuyList` must apply the active sale price from `_sales` when an item is on sale (the sale machinery already tracks active windows).
- [x] **Client packets**: ZC_ACK_SCHEDULER_CASHITEM (catalog/tab list), ZC_PC_CASH_POINT_ITEMLIST, ZC_ACK_CASH_BARGAIN_SALE_ITEM_INFO, CASHSHOP buy ack/result (ZC_SE_PC_BUY_CASHITEM_RESULT). Define or use PACKET-* seam; **point debit + item grant happen here**. ➡️ The CZ buy handler + ZC ack/result/list packet defs are the **PACKET-08** seam (no cash-shop packets exist in `Core.Server/Packets` yet); the **debit + grant are done here in `BuyList`** (the service is parity-complete, PACKET-08 just wires the wire). `BuyList` returns a per-condition `CashShopResult` the ack consumes.
- [x] `SaleNotifyLogin` — surfaces the **real** active-sale snapshot (`ActiveSales()` — item id + Sale-tab price + remaining stock); the ZC sale-list packet emit is ➡️ **PACKET-08** (no `ZC_NOTIFY_BARGAIN_SALE` def exists yet).

## Done criteria

- `BuyList` succeeds for a catalog item the player can afford: debits the correct cash/kafra points (kafra first up to `kafraPay`), grants the items, emits success ack.
- An item on an active sale is charged the discounted price.
- Insufficient points, inventory full, unknown item, or wrong tab are rejected with the matching fail code and no partial debit/grant.
- `cashshop_db` catalog loads on boot (count logged like the other catalogs).
- No unconditional `return false` in `BuyList`, no log-only `SaleNotifyLogin`.

## Test plan

- `Map.Server.Tests` (add `CashShopServiceTests`):
  - `BuyList` debits the right point split + grants items for an affordable item;
  - active-sale item charged the discounted price;
  - insufficient points / inventory full / unknown item rejected with no mutation;
  - catalog loads from a stubbed repository.
- Manual/live: open the cash shop, buy an item, confirm points debited + item received; put an item on sale (GM) and confirm the discount.

## Fail codes (rAthena `clif_cashshop_ack` result)

`BuyList` must return / emit the matching code, not a bare `false`:
- `ERROR_TYPE_NONE` (0) — success.
- `ERROR_TYPE_MONEY` (1) — insufficient cash/kafra points.
- `ERROR_TYPE_INVENTORY_WEIGHT` (3) — overweight / no inventory space.
- `ERROR_TYPE_QUANTITY` / `ERROR_TYPE_AMOUNT` — bad amount / over stack limit.
- `ERROR_TYPE_PURCHASE_FAIL` — item not in catalog / wrong tab.

The current `BuyList` returns a single `false` (`CashShopService.cs:28`) with no code — replace with the per-condition result.

## Catalog loader pattern (reuse)

Mirror the existing `*_db` catalogs: `QuestService.Reload` (`Quest/QuestService.cs:41`), `PetOpsService.Reload` (`Pet/PetOps/PetOpsService.cs:354`), `InstanceService.LoadCatalog` (`Instance/InstanceService.cs:39`) all use `IServiceScopeFactory` → `GetRequiredService<I*DbRepository>()` → `GetAllAsync()` into a `Dictionary`. Add `ICashShopDbRepository` + `CashShopDbEntity` and load into a `_catalog` keyed by item id (with tab + price), logging the count like the others.

## Notes / gotchas

- The sale-timer machinery already works (`SaleAddItem`/`SaleFindItem`/timers, `:47`–`:71`) — don't rebuild it; just consume `SaleFindItem` for the discount in `BuyList`.
- Kafra-points-first then cash-points is the rAthena debit order; respect the `kafraPay` cap the client sends (the `kafra_pay` argument bounds how many kafra points may be used).
- Validate all items + total cost before debiting anything (all-or-nothing).
- The `cashshop_db` seed pipeline mirrors the other `*_db` importers (Tools.RathenaImporter) — follow that pattern for the catalog rows.
- Confirm the cash-point fields exist on `PlayerEntity` / `CharacterData`; if not, add `CashPoints` + `KafraPoints` and persist them via the core-state save (these are account/char-bound currencies, not item inventory). ➡️ The fields **exist** (`PlayerEntity.CashPoints`/`KafraPoints`) and are debited correctly, but they are **in-memory only** (no DB load/save, no proto field) → persistence moved to **FEATURE-38**.

## History

- 2026-06-03 — Real `cashshop_buylist` landed. `CashShopService` now loads the
  `item_cash_db` catalog (tab→item→cash price) via `IItemCashDbRepository` on boot
  (`LoadCatalog`, tab-name→index map, Aegis→nameId via `IItemCatalog`), and `BuyList`
  resolves each item's price (Sale tab carries its own discounted price), validates the
  whole basket (qty≤99, sale stock, weight, inventory blank slots — all rAthena order),
  pays the kafra-then-cash split (`TryPayCash` ≙ `pc_paycash`), grants items
  (`IInventoryService.GiveItem`, one slot per non-stackable instance), and decrements
  consumed sale stock. Returns a per-condition `CashShopResult` (≙ `e_CASHSHOP_ACK`) —
  no bare `return false`. `SaleNotifyLogin` surfaces the real `ActiveSales()` snapshot.
  Tests: `CashShopServiceTests` (10) green. Matched `cashshop.cpp:441 cashshop_buylist`
  + `pc.cpp:5475 pc_paycash` + `CashShopDatabase::findItemInTab`.
  Follow-ups filed: FEATURE-37 (catalog source data — `item_cash.yml` absent → 0 seed
  rows), FEATURE-38 (cash-point currency persistence — in-memory only), FEATURE-39
  (buylist parity edges: pet-egg grant, trading gate, purchase log); client packet
  wire → PACKET-08.
