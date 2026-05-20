# navi.cpp parity · 2026-05-20

`src/map/navi.cpp` (655 lines, 17 functions) — in-game GPS file
generator (produces /Navi binary distance matrices the client uses
for `navigate_to`).

All 17 entries covered by [INaviService](/Map.Server/Navi/INaviService.cs) /
[NaviService](/Map.Server/Navi/NaviService.cs). Generator body data-pending —
the entry points are reserved so the `@navi_generate` GM command +
script `navigate_to` BUILTIN have somewhere to land.

## History

### 2026-05-20 — initial audit + service
- 17 functions covered (entry points only). Generator port deferred.
