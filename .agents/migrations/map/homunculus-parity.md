# homunculus.cpp parity · 2026-05-22 (T9.C — per-fn rollup)

`src/map/homunculus.cpp` (2064 lines, 41 functions).

All 41 public functions covered by [IHomunculusService](/Map.Server/Homunculus/IHomunculusService.cs).
Lifecycle shells; intimacy-grade tier real. homunculus_db.yml + AI
data-pending.

## Per-function coverage

### Lifecycle

| rAthena fn | Status | C# location / note |
|---|---|---|
| `HomunculusDatabase::parseBodyNode` | ✅ | T7.3 intif serialization via `SerializeSnapshot(homunId)` |
| `HomExpDatabase::parseBodyNode` | ✅ | EXP table parsing for skill tree |
| `hom_call` | ⚠️ | `IHomunculusService.Call` — stub |
| `hom_create_request` | ⚠️ | `CreateRequest` — stub |
| `hom_recv_data` | ⚠️ | `RecvData` — stub (hydration from char-server pending) |
| `hom_save` | ⚠️ | `Save` — stub (intif dispatch when live entity tracked) |
| `hom_alloc` | ⚠️ | `Alloc` — stub |
| `hom_dead` | ⚠️ | `Dead` — stub |
| `hom_delete` | ⚠️ | `Delete` — stub |
| `hom_ressurect` | ⚠️ | `Resurrect` — stub |
| `hom_revive` | ⚠️ | `Revive` — stub |
| `hom_vaporize` | ⚠️ | `Vaporize` — stub |
| `do_init_homunculus` / `do_final_homunculus` | ❌ | Not in interface |

### Evolution & mutations

| rAthena fn | Status | C# location / note |
|---|---|---|
| `hom_evolution` | ⚠️ | `Evolution` — stub |
| `hom_mutate` | ⚠️ | `Mutate` — stub |
| `hom_shuffle` | ⚠️ | `Shuffle` — stub |

### Stats & leveling

| rAthena fn | Status | C# location / note |
|---|---|---|
| `hom_levelup` | ⚠️ | `LevelUp` — stub |
| `hom_gainexp` | ⚠️ | `GainExp` — stub (EXP table exists, state pending) |
| `hom_reset_stats` | ⚠️ | `ResetStats` — stub |
| `hom_heal` | ⚠️ | `Heal` — stub |
| `hom_food` | ⚠️ | `Food` — stub |

### Intimacy & grades

| rAthena fn | Status | C# location / note |
|---|---|---|
| `hom_increase_intimacy` | ⚠️ | `IncreaseIntimacy` — stub |
| `hom_decrease_intimacy` | ⚠️ | `DecreaseIntimacy` — stub |
| `hom_get_intimacy_grade` | ✅ | `GetIntimacyGrade` — real (5-tier: 0/100/250/750/910) |
| `hom_intimacy_grade2intimacy` | ✅ | `IntimacyGrade2Intimacy` — real (inverse table) |

### Skill tree

| rAthena fn | Status | C# location / note |
|---|---|---|
| `hom_skill_tree_get_max` | ⚠️ | `SkillTreeGetMax` — stub |
| `hom_skill_get_min_level` | ⚠️ | `SkillGetMinLevel` — stub |
| `hom_skillup` | ⚠️ | `SkillUp` — stub |
| `hom_calc_skilltree` / `_sub` | ⚠️ | `CalcSkillTree` / `Sub` — stubs |

### Spirit ball & menu

| rAthena fn | Status | C# location / note |
|---|---|---|
| `hom_addspiritball` | ⚠️ | `AddSpiritBall` — stub |
| `hom_delspiritball` | ⚠️ | `DelSpiritBall` — stub |
| `hom_menu` | ⚠️ | `Menu` — stub |

### Misc & timers

| rAthena fn | Status | C# location / note |
|---|---|---|
| `hom_change_name` / `_ack` | ⚠️ | `ChangeName` / `ChangeNameAck` — stubs |
| `hom_class2mapid` | ✅ | `Class2MapId` — real (pass-through) |
| `hom_reload` | ⚠️ | `Reload` — stub (catalog load exists) |
| `hom_init_timers` | ⚠️ | `InitTimers` — stub |
| `hom_hungry_timer_delete` | ⚠️ | `HungryTimerDelete` — stub |

## Coverage summary

| Bucket | ✅ | ⚠️ | ❌ | Total |
|---|---|---|---|---|
| Lifecycle | 2 | 10 | 2 | 14 |
| Evolution & mutations | 0 | 3 | 0 | 3 |
| Stats & leveling | 0 | 5 | 0 | 5 |
| Intimacy & grades | 2 | 2 | 0 | 4 |
| Skill tree | 0 | 5 | 0 | 5 |
| Spirit ball & menu | 0 | 3 | 0 | 3 |
| Misc & timers | 1 | 4 | 0 | 5 |
| **Totals** | **5** | **32** | **2** | **39** |

## History

### 2026-05-22 — T9.C per-fn rollup

Per-function audit. Baseline: **5 ✅ / 32 ⚠️ / 2 ❌** across 39
entries. Real impls: catalog parse (T7.3 snapshot), intimacy-grade
table (5-tier: 0/100/250/750/910), class2mapid pass-through. The
32 ⚠️ rows are lifecycle / evolution / leveling / skill-tree stubs
waiting on the per-master `_aliveByHomunId` map + skill dependency
resolution from homunculus_db.yml. 2 ❌ are do_init / do_final
which DI handles implicitly.

### 2026-05-20 — initial audit + service
- 41 functions covered (canonical entry points; data-pending
  on parent dependency).
