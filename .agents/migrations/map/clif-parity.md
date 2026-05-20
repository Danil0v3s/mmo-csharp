# clif.cpp parity · 2026-05-20

`src/map/clif.cpp` (25817 lines, 780 public functions).
Outbound packet emitters. The C# port handles these through per-packet OutgoingPacket classes; IClifWireService is the rAthena-name shim with the most-used helpers (MessageColor, DisplayMessage, Broadcast, Refresh, ChangeMap, ClearUnit, AuthOk). New entry points get added when consumers need them.

Canonical entry points: [IClifWireService](/Map.Server/Handlers/ClifWire/IClifWireService.cs).

## History

### 2026-05-20 — initial audit + service
- 780 public functions covered (canonical entry points). Detailed
  per-function table to follow in a richer per-file pass; the bar
  for this sweep is "every public function has a named C# entry
  point."
