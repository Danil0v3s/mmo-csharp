# log.cpp parity · 2026-05-20

`src/map/log.cpp` (718 lines, 13 functions) — game-event auditing
(atcommand / chat / pick / zeny / mvp drops / cash / branch /
feeding / NPC).

| rAthena fn | Status | C# location |
|---|---|---|
| `log_atcommand` | ✅ | [GameLogService.Atcommand](/Map.Server/Logging/GameLogService.cs) + AtCommandLogger (SQL) |
| `log_branch` | ⚠️ | Info-log only; SQL table data-pending |
| `log_cash` | ⚠️ | same |
| `log_chat` | ⚠️ | same |
| `log_feeding` | ⚠️ | same |
| `log_mvpdrop` | ⚠️ | same |
| `log_npc` | ⚠️ | same |
| `log_pick` | ⚠️ | same |
| `log_pick_pc` | ⚠️ | same |
| `log_pick_mob` | ⚠️ | same |
| `log_zeny` | ⚠️ | same |
| `log_set_defaults` | ✅ | no-op |
| `log_config_read` | ✅ | returns true |

## History

### 2026-05-20 — initial audit + service
- 13 functions covered. Atcommand SQL log already shipped;
  remaining 10 tables data-pending on their EF Core entities.
