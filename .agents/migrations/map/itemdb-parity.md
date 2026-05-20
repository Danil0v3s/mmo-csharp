# itemdb.cpp parity · 2026-05-20

`src/map/itemdb.cpp` (4948 lines, 48 public functions).
Per-item rule gates (canauction / cancartstore / canguildstore / canmail / canpartnertrade / cansell / canstore / cantrade / isdropable / isrestricted / isNoEquip / isequip2 / isstackable2 / ishatched_egg / isidentified) + aux loaders (combos / enchant / item-groups / package / random-options / reform / laphine). Bare item catalog already lives in Map.Server.Items.ItemCatalog.

Canonical entry points: [IItemDbService](/Map.Server/Items/Db/IItemDbService.cs).

## History

### 2026-05-20 — initial audit + service
- 48 public functions covered (canonical entry points). Detailed
  per-function table to follow in a richer per-file pass; the bar
  for this sweep is "every public function has a named C# entry
  point."
