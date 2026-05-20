# guild.cpp parity · 2026-05-20

`src/map/guild.cpp` (2755 lines, 79 public functions).
Guild create / invite / leave / expulsion / send_message / castledatasave / alliance / break + ack handlers. Persistence lives on char-server; this service surfaces the rAthena-named operations for map-side.

Canonical entry points: [IGuildService](/Map.Server/Guild/IGuildService.cs).

## History

### 2026-05-20 — initial audit + service
- 79 public functions covered (canonical entry points). Detailed
  per-function table to follow in a richer per-file pass; the bar
  for this sweep is "every public function has a named C# entry
  point."
