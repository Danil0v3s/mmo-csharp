# mob.cpp parity · 2026-05-20

`src/map/mob.cpp` (6967 lines, 76 public functions).
Mob lifecycle (spawn, warpslave, dead, damage, heal, setclass, summon_slave, clone, drop_adjust). AI + skill use live in Map.Server.Mob; IMobOpsService is the rAthena-name shim.

Canonical entry points: [IMobOpsService](/Map.Server/Spawn/MobOps/IMobOpsService.cs).

## History

### 2026-05-20 — initial audit + service
- 76 public functions covered (canonical entry points). Detailed
  per-function table to follow in a richer per-file pass; the bar
  for this sweep is "every public function has a named C# entry
  point."
