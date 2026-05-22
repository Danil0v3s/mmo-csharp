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
| `sale_add_item` | ❌ | GM-command add not wired |
| `sale_find_item` | ❌ | Internal lookup helper not exposed |
| `sale_end_timer` | ❌ | Timer: remove sale at end — not wired |
| `sale_start_timer` | ❌ | Timer: activate sale at start — not wired |

## Coverage summary

| Bucket | ✅ | ⚠️ | ❌ | Total |
|---|---|---|---|---|
| Item database | 6 | 0 | 0 | 6 |
| Purchase flow | 1 | 0 | 0 | 1 |
| Sale management | 4 | 0 | 4 | 8 |
| **Totals** | **11** | **0** | **4** | **15** |

## History

### 2026-05-22 — T9.D per-fn rollup

Per-function audit. Baseline: **11 ✅ / 0 ⚠️ / 4 ❌** across 15
entries. Core item DB + purchase flow + sale display all ✅.
4 ❌ are sale-schedule timers (`sale_start_timer`, `sale_end_timer`)
+ `sale_add_item` (GM command) + `sale_find_item` (internal
lookup) — sales display correctly but don't auto-activate on
schedule.

### 2026-05-20 — initial audit + service
- 7 functions covered. Catalog + purchase log data-pending.
