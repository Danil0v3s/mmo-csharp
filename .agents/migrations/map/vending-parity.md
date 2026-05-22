# vending.cpp parity · 2026-05-22 (T9.D — per-fn rollup)

`src/map/vending.cpp` (768 lines, ~15 public functions + lifecycle).
Player-vendor stall registry, auto-trade reopen on login, item search.

Canonical entry points: [IVendingService](/Map.Server/Shop/Vending/IVendingService.cs).

## Per-function coverage

| rAthena fn | Status | C# location / note |
|---|---|---|
| `vending_getdb` | ✅ | `IVendingService` (registry) |
| `vending_getuid` | ✅ | Unique shop ID gen |
| `vending_closevending` | ✅ | `CloseVending` |
| `vending_vendinglistreq` | ✅ | `VendingListReq` |
| `vending_calc_tax` | ✅ | `IVendingService` (implicit in PurchaseReq) |
| `vending_purchasereq` | ✅ | `PurchaseReq` (buy + inventory transfer) |
| `vending_search` | ✅ | `Search` (single item) |
| `vending_searchall` | ✅ | `SearchAll` (multi-item + filters) |
| `vending_reopen` | ✅ | `Reopen` (autotrader reconnect) |
| `do_init_vending_autotrade` | ✅ | `InitAutotrade` (boot load) |
| `vending_autotrader_remove` | ✅ | Implicit (cleanup) |
| `vending_autotrader_free` | ✅ | Implicit (cleanup) |
| `vending_update` | ⚠️ | Update shop coords; low-impact (shops static) |
| `do_init_vending` / `do_final_vending` | ✅ | Implicit via DI |

## Coverage summary

| Bucket | ✅ | ⚠️ | ❌ | Total |
|---|---|---|---|---|
| Shop lifecycle / search / autotrade | 13 | 1 | 0 | 14 |
| **Totals** | **13** | **1** | **0** | **14** |

## History

### 2026-05-22 — T9.D per-fn rollup

Per-function audit. Baseline: **13 ✅ / 1 ⚠️ / 0 ❌** — vending is
feature-complete. The lone ⚠️ is `vending_update` (vendor-coord
persist while vending — rare case; shops are usually static).

### 2026-05-20 — initial audit + service
- 10 functions covered (canonical entry points; data-pending
  on parent dependency).
