# unit.cpp parity · 2026-05-20

`src/map/unit.cpp` (4010 lines, 51 public functions).
Entity-action helpers (warp, walktoxy, stop_walking, stop_attack, can_move, attack, blown_by, set_dir, skilluse_id, skilluse_pos, remove_map, free). Forwards to MovementService / AttackService when wired.

Canonical entry points: [IUnitOpsService](/Map.Server/Movement/UnitOps/IUnitOpsService.cs).

## History

### 2026-05-20 — initial audit + service
- 51 public functions covered (canonical entry points). Detailed
  per-function table to follow in a richer per-file pass; the bar
  for this sweep is "every public function has a named C# entry
  point."
