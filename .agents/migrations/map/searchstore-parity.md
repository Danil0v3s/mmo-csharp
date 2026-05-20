# searchstore.cpp parity · 2026-05-20

`src/map/searchstore.cpp` (361 lines, 9 functions) — Universal
Catalog query path that walks vending + buying stalls.

## Subsystem coverage

| rAthena fn | Status | C# location |
|---|---|---|
| `searchstore_open` | ✅ | [SearchStoreService.Open](/Map.Server/Shop/SearchStore/SearchStoreService.cs) |
| `searchstore_query` | ⚠️ | `Query` — shop enumeration data-pending |
| `searchstore_next` | ⚠️ | `Next` — pagination data-pending |
| `searchstore_clear` | ✅ | `Clear` |
| `searchstore_close` | ✅ | `Close` |
| `searchstore_click` | ⚠️ | `Click` — vendor lookup pending |
| `searchstore_querynext` | ⚠️ | `QueryNext` |
| `searchstore_queryremote` | ⚠️ | `QueryRemote` |
| `searchstore_clearremote` | ✅ | `ClearRemote` |

## History

### 2026-05-20 — initial audit + service
- `ISearchStoreService` / `SearchStoreService` covers all 9 functions.
- Query path data-pending on enumerable vending + buying-store
  registries.
