# buyingstore.cpp parity · 2026-05-22 (T9.D — per-fn rollup)

`src/map/buyingstore.cpp` (832 lines, ~16 public functions).
Buyer's stall registry — opposite of vending: buyers post bids,
sellers fulfill from inventory.

Canonical entry points: [IBuyingStoreService](/Map.Server/Shop/Buying/IBuyingStoreService.cs).

## Per-function coverage

| rAthena fn | Status | C# location / note |
|---|---|---|
| `buyingstore_getdb` | ✅ | `IBuyingStoreService` (registry) |
| `buyingstore_getuid` | ✅ | Unique store ID gen |
| `buyingstore_setup` | ✅ | AT-D2 — `BuyingStoreService.Open` gates effect-id ≤ 3 + already-open + seeds stall slot |
| `buyingstore_create` | ✅ | `Update` (create store + persist items) |
| `buyingstore_close` | ✅ | `Close` |
| `buyingstore_open` | ✅ | `Open` (buyer's store view) |
| `buyingstore_trade` | ✅ | `Trade` (seller sells to buyer) |
| `buyingstore_search` | ✅ | `Search` |
| `buyingstore_searchall` | ✅ | `SearchAll` |
| `buyingstore_reopen` | ✅ | `Reopen` (autotrader reconnect) |
| `do_init_buyingstore_autotrade` | ✅ | `InitAutotrade` (boot load) |
| `buyingstore_autotrader_remove` / `_free` | ✅ | Implicit (cleanup) |
| `buyingstore_update` | ✅ | AT-D2 — `BuyingStoreService.Update` refreshes X/Y/MapId on each update call |
| `do_init_buyingstore` / `do_final_buyingstore` | ✅ | Implicit via DI |

## Coverage summary

| Bucket | ✅ | ⚠️ | ❌ | Total |
|---|---|---|---|---|
| Store lifecycle / trade / search / autotrade | 15 | 0 | 0 | 15 |
| **Totals** | **15** | **0** | **0** | **15** |

## History

### 2026-05-24 — P2.1 doc-resync close-out (2 stale ⚠️ → ✅; 0 genuine gaps remain)

`buyingstore_setup` + `buyingstore_update` both flipped to ✅ — AT-D2 wave
landed real validation gates on `Open` (effect ≤ 3 + not-already-open + slot
seed) and coord refresh on `Update`. Rollup: 13/2/0 → 15/0/0.

### 2026-05-22 — T9.D per-fn rollup

Per-function audit. Baseline: **13 ✅ / 2 ⚠️ / 0 ❌** — buying
stores are feature-complete. ⚠️ rows: `setup` (UI enum scope may
differ) + `update` (coord persist, low-priority).

### 2026-05-20 — initial audit + service
- 10 functions covered (canonical entry points; data-pending
  on parent dependency).
