# GP-CASHSHOP — Cash shop works end-to-end

> **Epic:** gameplay · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
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

- [ ] **Data**: import the real `item_cash` catalog (tabs → item → cash price), non-zero seed.
- [ ] **Persistence**: load/save `CashPoints`/`KafraPoints` (account-bound, via login/account
      IPC + a proto field); debit persists across logout.
- [ ] **CZ handlers**: open cash shop, request point list, buy (with kafraPay split).
- [ ] **Service**: verify `BuyList` at HEAD; pet-egg branch, trading-state gate, purchase log
      (archive FEATURE-39).
- [ ] **ZC emits**: tab/catalog list, cash-point itemlist, sale-info, buy ack/result.

## Done criteria

- Player opens the cash shop → tabs populated → buys an affordable item → correct kafra/cash
  split debited, item granted, success ack; a sale-tab item charges the discounted price.
- Insufficient points / inventory-full / unknown item rejected with the matching fail code.
- Relog → the **remaining** balance is intact (account-bound, persisted).

## Test plan

- Handler tests: open/buy → service.
- Service: point split, sale price, rejects (archived CashShopServiceTests).
- Persistence: grant points → buy → relog → correct balance.
- Live: open → buy → sale buy → relog balance.

## Notes / gotchas

- `pc_paycash`: kafra first up to `kafraPay`, remainder cash; reject if total unaffordable.
- Sale price is the SALE-tab catalog entry (findItemInTab), not a separate multiplier (archive FEATURE-13).
