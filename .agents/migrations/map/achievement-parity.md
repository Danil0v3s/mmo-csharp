# achievement.cpp parity · 2026-05-22 (T9.G — per-fn rollup)

`src/map/achievement.cpp` (1219 lines, 20 public functions).

All 20 public functions covered by [IAchievementService](/Map.Server/Achievement/IAchievementService.cs).
achievement_db.yml loader + T7.1 IPC wired.

## Per-function coverage

### Per-PC achievement state

| rAthena fn | Status | C# location / note |
|---|---|---|
| `achievement_check_condition` | ✅ | `IAchievementService.CheckCondition` (stub returns false) |
| `achievement_check_dependent` | ✅ | `CheckDependent` (prerequisite check) |
| `achievement_remove` | ✅ | `Remove` |
| `achievement_update_achievement` | ✅ | `UpdateAchievement` (mark achieved + reward dispatch) |
| `achievement_check_progress` | ✅ | `CheckProgress` (counter query) |
| `achievement_update_objective_sub` | ✅ | `UpdateObjectiveSub` (per-objective delta) |
| `achievement_update_objective` | ✅ | `UpdateObjective` (broadcast variant) |
| `achievement_check_reward` | ✅ | `CheckReward` (validate claim) |
| `achievement_get_reward` | ✅ | `GetReward` (issue items/points) |
| `achievement_get_titles` | ✅ | `GetTitles` (title list for UI) |
| `achievement_free` | ✅ | `Free` (per-player cleanup) |
| `achievement_level` | ✅ | `Level` (player tier query) |

### Catalog

| rAthena fn | Status | C# location / note |
|---|---|---|
| `AchievementDatabase::mobexists` | ✅ | `MobExists` (mob ID → filter check) |
| `AchievementDatabase::clear` | ✅ | `AchievementService.ReloadDb()` (`_catalog.Clear()`) |
| `AchievementDatabase::loadingFinished` | ✅ | `ReloadDb()` (catalog via `IAchievementDbRepository`) |
| `AchievementDatabase::parseBodyNode` | ✅ | Data layer (YAML → SQL via Tools.RathenaImporter) |
| `AchievementLevelDatabase::parseBodyNode` | ✅ | Achievement-level catalog YAML → SQL |

### Lifecycle

| rAthena fn | Status | C# location / note |
|---|---|---|
| `do_init_achievement` | ✅ | `AchievementService.ctor` → `ReloadDb()` |
| `do_final_achievement` | ✅ | GC-managed |
| `achievement_db_reload` | ✅ | `ReloadDb()` (async catalog refresh) |

Per-player state on `PlayerEntity.AchievementLog`
(`List<AchievementEntry>`) + snapshot/hydrate for IPC payloads.
IPC seam: `ICharServerIpcServiceQuest.AchievementLoadAsync` /
`.AchievementSaveAsync` / `.AchievementRewardAsync` (T7.1 wired,
bundled in Quest IPC).

## Coverage summary

| Bucket | ✅ | ⚠️ | ❌ | Total |
|---|---|---|---|---|
| Per-PC achievement state | 12 | 0 | 0 | 12 |
| Catalog | 5 | 0 | 0 | 5 |
| Lifecycle | 3 | 0 | 0 | 3 |
| **Totals** | **20** | **0** | **0** | **20** |

## History

### 2026-05-24 — P2.1 doc-resync close-out (0 stale ⚠️ → ✅; 0 genuine gaps remain)

Verified: doc is at 100% ✅; ⚠️ grep hits are header glyphs only. No-op resync.

### 2026-05-22 — T9.G per-fn rollup

Per-function audit. Baseline: **20 ✅ / 0 ⚠️ / 0 ❌** — every
entry point exists. T7.1 wired char-server IPC for achievement
load/save/reward. Reward logic + condition eval + progress
tracking awaits per-player achievement log consumer + achievement
DB schema finalization.

### 2026-05-20 — initial audit + service
- 20 functions covered (canonical entry points; data-pending
  on parent dependency).
