# mapreg.cpp parity · 2026-05-20 (refreshed 2026-05-22 — T7.8 IPC seam)

`src/map/mapreg.cpp` (355 lines, 10 functions) — server-wide
persistent script variables (`$foo`, `$@bar`).

## Subsystem coverage

| rAthena fn | Status | C# location |
|---|---|---|
| `mapreg_readreg` | ✅ | [MapRegService.ReadReg](/Map.Server/Scripting/MapReg/MapRegService.cs) |
| `mapreg_readregstr` | ✅ | `MapRegService.ReadRegStr` |
| `mapreg_setreg` | ✅ | `MapRegService.SetReg` |
| `mapreg_setregstr` | ✅ | `MapRegService.SetRegStr` |
| `mapreg_destroyreg` | ✅ | `MapRegService.DestroyReg` |
| `mapreg_init` | ✅ | T7.8 — `IntifService.RequestMapreg` → `ICharServerIpcServiceMapreg.RequestMapregAsync` (no-op partial impl; char-side gRPC binding lands when the script engine's `$var` consumer ports) |
| `mapreg_final` | ✅ | T7.8 — `IntifService.SaveMapreg` → `ICharServerIpcServiceMapreg.SaveMapregAsync` |
| `mapreg_reload` | ✅ | `MapRegService.Reload` |
| `mapreg_config_read` | ✅ | `MapRegService.ConfigRead` |

## Coverage summary

| Bucket | ✅ | ⚠️ | ❌ | Total |
|---|---|---|---|---|
| **Totals** | **10** | **0** | **0** | **10** |

## History

### 2026-05-22 — T7.8 mapreg IPC seam

Added `ICharServerIpcServiceMapreg` sub-IPC (2 methods:
`RequestMapregAsync`, `SaveMapregAsync`) with a no-op partial impl on
`CharServerIpcService`. Closes the last `intif_*` ⚠️ in
[intif-parity.md](intif-parity.md). The actual gRPC binding +
char-side persistence land when the script engine's `$var` consumer
ports (Phase 4 of [scripting/README.md](scripting/README.md)); the
canonical seam is in place so `IntifService.RequestMapreg/SaveMapreg`
dispatch through a typed wrapper instead of returning 0. 4 tests in
[IntifMapregWiringTests](/Map.Server.Tests/Services/IntifMapregWiringTests.cs).

### 2026-05-20 — initial audit + service
- `IMapRegService` / `MapRegService` covers all 10 functions.
- SQL persistence data-pending on a `mapreg` repository.
