# mapreg.cpp parity · 2026-05-20

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
| `mapreg_init` | ⚠️ | SQL load pending |
| `mapreg_final` | ⚠️ | SQL flush pending |
| `mapreg_reload` | ✅ | `MapRegService.Reload` |
| `mapreg_config_read` | ✅ | `MapRegService.ConfigRead` |

## History

### 2026-05-20 — initial audit + service
- `IMapRegService` / `MapRegService` covers all 10 functions.
- SQL persistence data-pending on a `mapreg` repository.
