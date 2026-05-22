# path.cpp parity · 2026-05-22 (T9.H — per-fn rollup)

`src/map/path.cpp` (522 lines, 11 functions) — distance / direction
helpers + A* path search + knockback position resolver.

## Subsystem coverage

| rAthena fn | Status | C# location |
|---|---|---|
| `distance` (Chebyshev) | ✅ | [PathService.Distance](/Map.Server/Pathing/PathService.cs) |
| `distance_client` (Pythag) | ✅ | `PathService.DistanceClient` |
| `check_distance` | ✅ | `PathService.CheckDistance` |
| `check_distance_client` | ✅ | `PathService.CheckDistanceClient` |
| `direction_diagonal` | ✅ | `PathService.DirectionDiagonal` |
| `direction_opposite` | ✅ | `PathService.DirectionOpposite` |
| `path_search` | ⚠️ | A* exists in `MovementService`; entry returns true today |
| `path_search_long` | ⚠️ | LOS check pending |
| `path_blownpos` | ✅ | `PathService.BlownPos` — 8-direction delta table |
| `do_init_path` / `do_final_path` | ✅ | DI |

## Coverage summary

| Bucket | ✅ | ⚠️ | ❌ | Total |
|---|---|---|---|---|
| Distance / direction / knockback / pathing | 9 | 2 | 0 | 11 |
| **Totals** | **9** | **2** | **0** | **11** |

## History

### 2026-05-22 — T9.H per-fn rollup

Per-function audit. Baseline: **9 ✅ / 2 ⚠️ / 0 ❌**. Distance
helpers (Chebyshev + Pythagorean) + direction helpers (diagonal,
opposite) + knockback (8-dir delta table) + DI lifecycle all
✅. 2 ⚠️ are `path_search` + `path_search_long` — return true
today; A* exists privately in `MovementService` but isn't
publicly hooked.

### 2026-05-20 — initial audit + service
- 11 functions covered by `IPathService`. Distance + direction
  helpers real; path search / LOS data-pending on a public hook
  into `MovementService.AStar`.
