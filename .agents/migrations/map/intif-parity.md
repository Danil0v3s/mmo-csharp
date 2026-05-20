# intif.cpp parity · 2026-05-20

`src/map/intif.cpp` (3900 lines, 149 public functions).
Map → inter façade. Routes for party, guild, mail, auction, quest, achievement, pet, homunculus, mercenary, clan, storage, bg, elemental, mapreg, broadcast, registry. Forwards to existing *IpcService wrappers as they port.

Canonical entry points: [IIntifService](/Map.Server/Services/Intif/IIntifService.cs).

## History

### 2026-05-20 — initial audit + service
- 149 public functions covered (canonical entry points). Detailed
  per-function table to follow in a richer per-file pass; the bar
  for this sweep is "every public function has a named C# entry
  point."
