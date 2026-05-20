# npc.cpp parity · 2026-05-20

`src/map/npc.cpp` (6341 lines, 76 public functions).
NPC event dispatch + spawn helpers. Dialog logic lives in TS-script engine; this service surfaces the rAthena-named event / timer / click / shop callbacks.

Canonical entry points: [INpcOpsService](/Map.Server/Spawn/NpcOps/INpcOpsService.cs).

## History

### 2026-05-20 — initial audit + service
- 76 public functions covered (canonical entry points). Detailed
  per-function table to follow in a richer per-file pass; the bar
  for this sweep is "every public function has a named C# entry
  point."
