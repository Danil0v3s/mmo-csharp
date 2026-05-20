# pet.cpp parity · 2026-05-20

`src/map/pet.cpp` (2504 lines, 33 public functions).
Pet lifecycle (create_egg, get_egg, food, attack_skill, evolution, change_name, equip_item, autobonus, catch_process). Real pet AI + lifecycle in Map.Server.Pet; IPetOpsService is the rAthena-name shim.

Canonical entry points: [IPetOpsService](/Map.Server/Pet/PetOps/IPetOpsService.cs).

## History

### 2026-05-20 — initial audit + service
- 33 public functions covered (canonical entry points). Detailed
  per-function table to follow in a richer per-file pass; the bar
  for this sweep is "every public function has a named C# entry
  point."
