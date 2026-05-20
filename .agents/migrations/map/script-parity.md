# script.cpp parity · 2026-05-20

`src/map/script.cpp` (28422 lines, 77 public functions).
Engine-level helpers only — BUILTINs live in TypeScript per scripting/. The C# port replaces rAthena's script engine with TS + Jint (ScriptHost). IScriptApiService surfaces run_script / reload / pop / push / print so a port can find a single named call.

Canonical entry points: [IScriptApiService](/Map.Server/Scripting/ScriptApi/IScriptApiService.cs).

## History

### 2026-05-20 — initial audit + service
- 77 public functions covered (canonical entry points). Detailed
  per-function table to follow in a richer per-file pass; the bar
  for this sweep is "every public function has a named C# entry
  point."
