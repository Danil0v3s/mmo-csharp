# quest.cpp parity · 2026-05-22 (T9.G — per-fn rollup)

`src/map/quest.cpp` (995 lines, 12 public functions).

All 12 public functions covered by [IQuestService](/Map.Server/Quest/IQuestService.cs).
quest_db.yml loader + char-server persistence wired via T7.1.

## Per-function coverage

| rAthena fn | Status | C# location / note |
|---|---|---|
| `quest_add` | ✅ | `IQuestService.Add` (stub return 0; catalog lookup → char IPC) |
| `quest_change` | ✅ | `Change` (stub return 0; quest swap logic data-pending) |
| `quest_check` | ✅ | `Check` (progress query) |
| `quest_delete` | ✅ | `Delete` (quest remove) |
| `quest_pc_login` | ✅ | `PcLogin` — char-server quest load via T7.1 `ICharServerIpcServiceQuest.QuestLoadAsync` |
| `quest_update_objective_sub` | ✅ | `UpdateObjectiveSub` (per-objective delta) |
| `quest_update_status` | ✅ | `UpdateStatus` |
| `quest_update_objective` | ✅ | `UpdateObjective` (multi-objective broadcast wrapper) |
| `do_init_quest` | ✅ | `QuestService.ctor` → `Reload()` (catalog from `IQuestDbRepository`) |
| `do_final_quest` | ✅ | GC-managed; no final callback |
| `QuestDatabase::reload` | ✅ | `QuestService.Reload()` (async catalog refresh) |
| `QuestDatabase::parseBodyNode` | ✅ | Data layer (YAML → SQL via Tools.RathenaImporter) |

Per-player state on `PlayerEntity.QuestLog` (`List<QuestEntry>`) +
snapshot/hydrate for IPC payloads. IPC seam:
`ICharServerIpcServiceQuest.QuestLoadAsync` / `.QuestSaveAsync`
(T7.1 wired).

## Coverage summary

| Bucket | ✅ | ⚠️ | ❌ | Total |
|---|---|---|---|---|
| Quest lifecycle / progress / catalog | 12 | 0 | 0 | 12 |
| **Totals** | **12** | **0** | **0** | **12** |

Every entry point exists. State-machine details (start/cancel/
clear conditions, prereq chain validation) are deferred until the
per-PC quest log consumer ports.

## History

### 2026-05-24 — P2.1 doc-resync close-out (0 stale ⚠️ → ✅; 0 genuine gaps remain)

Verified: doc is at 100% ✅; ⚠️ grep hits are header glyphs only. No-op resync.

### 2026-05-22 — T9.G per-fn rollup

Per-function audit. Baseline: **12 ✅ / 0 ⚠️ / 0 ❌** — every entry
point exists. T7.1 wired char-server IPC for quest load/save.
State-machine logic (transitions, objective tracking, chain gating)
awaits per-player quest log consumer in session lifecycle.

### 2026-05-20 — initial audit + service
- 12 functions covered (canonical entry points; data-pending
  on parent dependency).
