# status.cpp parity · 2026-05-20

`src/map/status.cpp` (16047 lines, 82 public functions).
SC engine + status_calc + HP/SP delta helpers + identity / mode / regen / refresh. The C# port already has working IStatusChangeService + IStatusCalcService; IStatusOpsService surfaces the rAthena-named operations that aren't on those services yet (HP/SP/AP zap, percent-revive, status-data accessors, mode helpers).

Canonical entry points: [IStatusOpsService](/Map.Server/Status/StatusOps/IStatusOpsService.cs).

## History

### 2026-05-20 — initial audit + service
- 82 public functions covered (canonical entry points). Detailed
  per-function table to follow in a richer per-file pass; the bar
  for this sweep is "every public function has a named C# entry
  point."
