# chrif.cpp parity · 2026-05-20

`src/map/chrif.cpp` (1974 lines, 67 public functions).
Map → char IPC façade. The C# port already has IServerConnectionService + CharServerIpcService wrappers; IChrifService surfaces the rAthena-named operations (save, authreq, charselectreq, changemapserver, divorce, scdata, skillcooldown, bsdata, fame, keepalive).

Canonical entry points: [IChrifService](/Map.Server/Services/Chrif/IChrifService.cs).

## History

### 2026-05-20 — initial audit + service
- 67 public functions covered (canonical entry points). Detailed
  per-function table to follow in a richer per-file pass; the bar
  for this sweep is "every public function has a named C# entry
  point."
