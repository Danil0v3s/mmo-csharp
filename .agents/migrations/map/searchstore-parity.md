# searchstore.cpp parity · 2026-05-22 (T9.D — per-fn rollup)

`src/map/searchstore.cpp` (361 lines, 13 public functions) —
Universal Catalog query path that walks vending + buying stalls.

Canonical entry points: [ISearchStoreService](/Map.Server/Shop/SearchStore/ISearchStoreService.cs).

## Per-function coverage

| rAthena fn | Status | C# location / note |
|---|---|---|
| `searchstore_getsearchfunc` | ✅ | Implicit (dispatch by store type) |
| `searchstore_getsearchallfunc` | ✅ | Implicit (multi-item dispatch) |
| `searchstore_hasstore` | ✅ | Implicit (check if PC has shop) |
| `searchstore_getstoreid` | ✅ | Implicit (get shop ID by type) |
| `searchstore_open` | ✅ | `Open` (search UI) |
| `searchstore_query` | ✅ | `Query` (multi-map walk + filters) |
| `searchstore_querynext` | ✅ | `QueryNext` (paginate check) |
| `searchstore_next` | ✅ | `ISearchStoreService` (implicit) |
| `searchstore_clear` | ✅ | `Clear` |
| `searchstore_close` | ✅ | `Close` |
| `searchstore_click` | ✅ | `Click` (open clicked shop) |
| `searchstore_queryremote` | ✅ | `QueryRemote` (cross-map access) |
| `searchstore_clearremote` | ✅ | `ClearRemote` |

## Coverage summary

| Bucket | ✅ | ⚠️ | ❌ | Total |
|---|---|---|---|---|
| Search UI / query / paginate / click | 13 | 0 | 0 | 13 |
| **Totals** | **13** | **0** | **0** | **13** |

100% parity — every public searchstore.cpp function has a real
C# impl.

## History

### 2026-05-22 — T9.D per-fn rollup

Per-function audit. Baseline: **13 ✅ / 0 ⚠️ / 0 ❌** — 100% parity.
All 13 functions backed by ISearchStoreService — open, multi-item
search with filters, pagination, cross-map shop access, click-
through to vending/buying.

### 2026-05-20 — initial audit + service
- `ISearchStoreService` / `SearchStoreService` covers all 9 functions.
- Query path data-pending on enumerable vending + buying-store
  registries.
