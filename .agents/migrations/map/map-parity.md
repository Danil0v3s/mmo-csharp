# map.cpp parity · 2026-05-20

`src/map/map.cpp` (5356 lines, 157 public functions).
Map-level utilities (name2mapid, random_cell, foreachpc, foreachmob, getcell, setmapflag, calc_dir, id2bl, nick2sd). Real entity-registry iteration + name resolution; specific helpers route through IMapWorldRegistry / IEntityRegistry.

Canonical entry points: [IMapOpsService](/Map.Server/World/MapOps/IMapOpsService.cs).

## History

### 2026-05-20 — initial audit + service
- 157 public functions covered (canonical entry points). Detailed
  per-function table to follow in a richer per-file pass; the bar
  for this sweep is "every public function has a named C# entry
  point."
