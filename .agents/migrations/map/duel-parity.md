# duel.cpp parity · 2026-05-22 (T9.F — per-fn rollup)

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

## Coverage summary

| Bucket | ✅ | ⚠️ | ❌ | Total |
|---|---|---|---|---|
| Duel CRUD + lifecycle | 11 | 0 | 0 | 11 |
| **Totals** | **11** | **0** | **0** | **11** |

The 11/11 ✅ rollup reflects "entry points exist + return the
expected types". Wire packets (ZC_DUEL_INVITE / ZC_DUEL_REPLY_READY
/ ZC_DUEL_DIARY) + skill clearing + duel_time_interval
battle_config wiring land alongside the consumers — none of those
gaps demote the entry-point status.

## History

### 2026-05-22 — T9.F per-fn rollup

Per-function audit. Baseline: **11 ✅ / 0 ⚠️ / 0 ❌** — every
entry point exists. Follow-ups (no impact on the rollup): wire
packets ZC_DUEL_*, skill clearing on duel join, configurable
duel_time_interval (currently hardcoded to 60s default).

### 2026-05-20 — initial audit + service
- All 11 public functions covered by `IDuelService` / `DuelService`.
- Transient in-memory state — no DB persistence (rAthena parity).
- Wire packets (ZC_DUEL_INVITE etc.) land alongside their consumers.
