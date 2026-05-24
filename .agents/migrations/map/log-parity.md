# log.cpp parity · 2026-05-22 (T9.H — per-fn rollup)

`src/map/log.cpp` (718 lines, 13 functions) — game-event auditing
(atcommand / chat / pick / zeny / mvp drops / cash / branch /
feeding / NPC).

| rAthena fn | Status | C# location |
|---|---|---|
| `log_atcommand` | ✅ | [GameLogService.Atcommand](/Map.Server/Logging/GameLogService.cs) + AtCommandLogger (SQL) |
| `log_branch` | ⚠️ | Info-log only; `branchlog` EF entity pending (PARITY-REMAINING.md §P2.2 leaf wires) |
| `log_cash` | ⚠️ | Info-log only; `cashlog` EF entity pending (§P2.2) |
| `log_chat` | ⚠️ | Info-log only; `chatlog` EF entity pending (§P2.2) |
| `log_feeding` | ⚠️ | Info-log only; `feedinglog` EF entity pending (§P2.2) |
| `log_mvpdrop` | ⚠️ | Info-log only; `mvplog` EF entity pending (§P2.2) |
| `log_npc` | ⚠️ | Info-log only; `npclog` EF entity pending (§P2.2) |
| `log_pick` | ⚠️ | Info-log only; `picklog` EF entity pending (§P2.2) |
| `log_pick_pc` | ⚠️ | Info-log only; routes to `log_pick` with who='P' (§P2.2) |
| `log_pick_mob` | ⚠️ | Info-log only; routes to `log_pick` with who='M' (§P2.2) |
| `log_zeny` | ⚠️ | Info-log only; `zenylog` EF entity pending (§P2.2) |
| `log_set_defaults` | ✅ | no-op |
| `log_config_read` | ✅ | returns true |

## Coverage summary

| Bucket | ✅ | ⚠️ | ❌ | Total |
|---|---|---|---|---|
| Game-event auditing | 3 | 10 | 0 | 13 |
| **Totals** | **3** | **10** | **0** | **13** |

## History

### 2026-05-24 — P2.1 doc-resync close-out (0 stale ⚠️ → ✅; 10 genuine gaps remain)

Audited every ⚠️ row against
[GameLogService.cs](/Map.Server/Logging/GameLogService.cs); each
non-atcommand log path emits a structured `LogInformation` line
only — no SQL persistence layer landed. Notes refreshed with the
PARITY-REMAINING.md §P2.2 (leaf wires) citation; each row now
calls out the missing EF entity by name. No flips.

### 2026-05-22 — T9.H per-fn rollup

Per-function audit. Baseline: **3 ✅ / 10 ⚠️ / 0 ❌**. All 13
entry points exist on `IGameLogService` / `GameLogService`.
Atcommand log + `set_defaults` + `config_read` are real;
the other 10 log paths (branch / cash / chat / feeding / mvp /
npc / pick / pick_pc / pick_mob / zeny) emit structured info-log
lines today, with SQL persistence pending the matching EF Core
entity ports.

### 2026-05-20 — initial audit + service
- 13 functions covered. Atcommand SQL log already shipped;
  remaining 10 tables data-pending on their EF Core entities.
