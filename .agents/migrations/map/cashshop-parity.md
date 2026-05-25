# cashshop.cpp parity · 2026-05-22 (T9.D — per-fn rollup)

`src/map/cashshop.cpp` (672 lines, ~16 public functions) — real-money
shop + sale schedule.

Canonical entry points: [ICashShopService](/Map.Server/Shop/Cash/ICashShopService.cs) /
[CashShopService](/Map.Server/Shop/Cash/CashShopService.cs).

## Per-function coverage

### Item database

| rAthena fn | Status | C# location / note |
|---|---|---|
| `CashShopDatabase::getDefaultLocation` | ✅ | Implicit (YAML path lookup) |
| `CashShopDatabase::parseBodyNode` | ✅ | `ICashShopService.Reload` (cashshop_db.yml) |
| `CashShopDatabase::findItemInTab` | ✅ | Implicit (find by tab + ID) |
| `cashshop_read_db` | ✅ | `Init` / `Reload` |
| `cashshop_reloaddb` | ✅ | `ReloadDb` |
| `do_init_cashshop` / `do_final_cashshop` | ✅ | Implicit via DI |

### Purchase flow

| rAthena fn | Status | C# location / note |
|---|---|---|
| `cashshop_buylist` | ✅ | `BuyList` (validate avail + weight + charge cashpoints + grant items) |

### Sale management

| rAthena fn | Status | C# location / note |
|---|---|---|
| `sale_parse_dbrow` | ✅ | Implicit (parse sale row) |
| `sale_read_db_sql` | ✅ | Implicit (load sales from SQL) |
| `sale_remove_item` | ✅ | `SaleRemoveItem` |
| `sale_notify_login` | ✅ | `SaleNotifyLogin` |
| `sale_add_item` | ✅ | `CashShopService.SaleAddItem` ([CashShopService.cs:47-67](/Map.Server/Shop/Cash/CashShopService.cs)) — stores in `_sales` registry with start/end timers; activates immediately when `startAt` is in the past |
| `sale_find_item` | ✅ | `CashShopService.SaleFindItem` ([CashShopService.cs:70-71](/Map.Server/Shop/Cash/CashShopService.cs)) — returns true only when an active sale entry exists for the item id |
| `sale_end_timer` | ✅ | Inline closure inside `SaleAddItem` ([CashShopService.cs:60-66](/Map.Server/Shop/Cash/CashShopService.cs)) — `Timer` fires at `endDelay`, flips `sale.Active=false` and drops the entry |
| `sale_start_timer` | ✅ | Inline closure inside `SaleAddItem` ([CashShopService.cs:55-59](/Map.Server/Shop/Cash/CashShopService.cs)) — `Timer` fires at `startDelay`, flips `sale.Active=true` |

## Coverage summary

| Bucket | ✅ | ⚠️ | ❌ | Total |
|---|---|---|---|---|
| Item database | 6 | 0 | 0 | 6 |
| Purchase flow | 1 | 0 | 0 | 1 |
| Sale management | 8 | 0 | 0 | 8 |
| **Totals** | **15** | **0** | **0** | **15** |

## History

### 2026-05-25 — Wave 76: cashshop-parity close-out (4 ❌ → ✅)

Re-audited the four sale-timer entries against
[CashShopService.cs](/Map.Server/Shop/Cash/CashShopService.cs). All four
already have working bodies that landed during AT-D2 but the doc never
reflected them:

- `sale_add_item` → `SaleAddItem` (lines 47-67) — stores in the
  `_sales` registry, schedules a start `Timer` at `startDelay` and an
  end `Timer` at `endDelay`, activates immediately when `startAt` is
  in the past.
- `sale_find_item` → `SaleFindItem` (lines 70-71) — gated on
  `sale.Active`, returns false when the entry is scheduled-but-not-yet-
  active.
- `sale_start_timer` → inline closure in `SaleAddItem` lines 55-59 —
  one-shot `Timer` flips `sale.Active=true`.
- `sale_end_timer` → inline closure in `SaleAddItem` lines 60-66 —
  one-shot `Timer` flips `sale.Active=false` and removes the entry from
  `_sales`. All timers tracked in `_activeTimers` for clean disposal.

**Coverage:** 11 ✅ / 0 ⚠️ / 4 ❌ → **15 ✅ / 0 ⚠️ / 0 ❌**. Doc-resync
only; no C# code touched.

### 2026-05-24 — P2.1 doc-resync close-out (0 stale ⚠️ → ✅; 0 genuine gaps remain)

Verified: no functional ⚠️ rows; ⚠️ grep hits are header glyphs only. 4 ❌
sale-timer entries remain as documented (sales display without scheduled
auto-activation). No-op resync.

### 2026-05-22 — T9.D per-fn rollup

Per-function audit. Baseline: **11 ✅ / 0 ⚠️ / 4 ❌** across 15
entries. Core item DB + purchase flow + sale display all ✅.
4 ❌ are sale-schedule timers (`sale_start_timer`, `sale_end_timer`)
+ `sale_add_item` (GM command) + `sale_find_item` (internal
lookup) — sales display correctly but don't auto-activate on
schedule.

### 2026-05-20 — initial audit + service
- 7 functions covered. Catalog + purchase log data-pending.
