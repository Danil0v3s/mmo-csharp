# duel.cpp parity · 2026-05-20

`src/map/duel.cpp` (311 lines, 11 functions) — transient 1v1 duel
state. invite / accept / reject / leave + duel-time cooldown +
participant list.

## Subsystem coverage

| rAthena fn | Status | C# location |
|---|---|---|
| `duel_create` (impl detail) | ✅ | [DuelService.Create](/Map.Server/Duel/DuelService.cs) |
| `duel_invite` | ✅ | `DuelService.Invite` |
| `duel_accept` | ✅ | `DuelService.Accept` |
| `duel_reject` | ✅ | `DuelService.Reject` |
| `duel_leave` | ✅ | `DuelService.Leave` |
| `duel_exist` | ✅ | `DuelService.Exists` |
| `duel_check_player_limit` | ✅ | `DuelService.CheckPlayerLimit` |
| `duel_checktime` | ✅ | `DuelService.CheckTime` (60s default) |
| `duel_savetime` | ✅ | `DuelService.SaveTime` |
| `duel_showinfo` | ✅ | `DuelService.ShowInfo` |
| `do_init_duel` / `do_final_duel` | ✅ | DI lifecycle |

## History

### 2026-05-20 — initial audit + service
- All 11 public functions covered by `IDuelService` / `DuelService`.
- Transient in-memory state — no DB persistence (rAthena parity).
- Wire packets (ZC_DUEL_INVITE etc.) land alongside their consumers.
