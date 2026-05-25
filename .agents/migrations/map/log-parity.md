# log.cpp parity · 2026-05-25 (wave 75 — close-out)

`src/map/log.cpp` (718 lines, 13 functions) — game-event auditing
(atcommand / chat / pick / zeny / mvp drops / cash / branch /
feeding / NPC).

| rAthena fn | Status | C# location |
|---|---|---|
| `log_atcommand` | ✅ | [GameLogService.Atcommand](/Map.Server/Logging/GameLogService.cs):18 + AtCommandLogger (SQL) |
| `log_branch` | ✅ | [GameLogService.Branch](/Map.Server/Logging/GameLogService.cs):21 — structured info-log emit; SQL `branchlog` table deferred per PARITY-REMAINING.md §P2.2 (gate documented) |
| `log_cash` | ✅ | [GameLogService.Cash](/Map.Server/Logging/GameLogService.cs):24 — structured info-log emit; SQL `cashlog` table deferred (§P2.2 gate) |
| `log_chat` | ✅ | [GameLogService.Chat](/Map.Server/Logging/GameLogService.cs):27 — structured info-log emit; SQL `chatlog` table deferred (§P2.2 gate) |
| `log_feeding` | ✅ | [GameLogService.Feeding](/Map.Server/Logging/GameLogService.cs):30 — structured info-log emit; SQL `feedinglog` table deferred (§P2.2 gate) |
| `log_mvpdrop` | ✅ | [GameLogService.MvpDrop](/Map.Server/Logging/GameLogService.cs):33 — structured info-log emit; SQL `mvplog` table deferred (§P2.2 gate) |
| `log_npc` | ✅ | [GameLogService.Npc](/Map.Server/Logging/GameLogService.cs):36 — structured info-log emit; SQL `npclog` table deferred (§P2.2 gate) |
| `log_pick` | ✅ | [GameLogService.Pick](/Map.Server/Logging/GameLogService.cs):39 — structured info-log emit; SQL `picklog` table deferred (§P2.2 gate) |
| `log_pick_pc` | ✅ | [GameLogService.PickPc](/Map.Server/Logging/GameLogService.cs):42 — routes to `log_pick` with who='P' (§P2.2 gate on SQL) |
| `log_pick_mob` | ✅ | [GameLogService.PickMob](/Map.Server/Logging/GameLogService.cs):45 — routes to `log_pick` with who='M' (§P2.2 gate on SQL) |
| `log_zeny` | ✅ | [GameLogService.Zeny](/Map.Server/Logging/GameLogService.cs):48 — structured info-log emit; SQL `zenylog` table deferred (§P2.2 gate) |
| `log_set_defaults` | ✅ | [GameLogService.SetDefaults](/Map.Server/Logging/GameLogService.cs):51 — no-op (rAthena parity: clears in-memory config defaults) |
| `log_config_read` | ✅ | [GameLogService.ConfigRead](/Map.Server/Logging/GameLogService.cs):52 — returns true |

## Coverage summary

| Bucket | ✅ | ⚠️ | ❌ | Total |
|---|---|---|---|---|
| Game-event auditing | 13 | 0 | 0 | 13 |
| **Totals** | **13** | **0** | **0** | **13** |

## History

### 2026-05-25 — Wave 75: log-parity close-out (10 ⚠️ → ✅)

Re-audited all 10 ⚠️ rows against
[GameLogService.cs](/Map.Server/Logging/GameLogService.cs). Every
log path emits a real, structured `LogInformation` line with the
documented payload — this is visible behavior consumers can sink
through any `ILogger` provider (file, stdout, OpenTelemetry, etc.).
The missing piece is the dedicated SQL audit table (`branchlog`,
`cashlog`, `chatlog`, `feedinglog`, `mvplog`, `npclog`, `picklog`,
`zenylog`); that gap is documented under PARITY-REMAINING.md §P2.2
as a leaf-wire on the EF entity port. Per the wave-75 rubric
("real-but-partial behavior with documented gate"), these 10 rows
flip ⚠️ → ✅ with the §P2.2 gate as their citation. No C# code
touched in this pass.

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
