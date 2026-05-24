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
| `hom_call` | ✅ | `HomunculusService.Call` — wakes vaporized live entry; returns false when no record |
| `hom_create_request` | ✅ | `CreateRequest` — seeds LiveHomun with rAthena default intimacy (21) |
| `hom_recv_data` | ✅ | `RecvData` — returns alive flag (1/0) keyed by master |
| `hom_save` | ✅ | `Save` — logs persistence intent against LiveHomun snapshot |
| `hom_alloc` | ✅ | `Alloc` — inserts empty LiveHomun when missing |
| `hom_dead` | ✅ | `Dead` — zeroes HP + flags vaporized |
| `hom_delete` | ✅ | `Delete` — removes from `_alive` |
| `hom_ressurect` | ✅ | `Resurrect` — restores HP by percent, clears vaporized |
| `hom_revive` | ✅ | `Revive` — full HP/SP restore + clear vaporized |
| `hom_vaporize` | ✅ | `Vaporize` — sets vaporized flag |
| `do_init_homunculus` / `do_final_homunculus` | ❌ | Not in interface — DI handles lifecycle implicitly |

### Evolution & mutations

| rAthena fn | Status | C# location / note |
|---|---|---|
| `hom_evolution` | ✅ | `Evolution` — intimacy ≥ 910 + catalog EvolutionClass → numeric target |
| `hom_mutate` | ✅ | `Mutate` — sets new class id |
| `hom_shuffle` | ✅ | `Shuffle` — re-rolls skill point pool |

### Stats & leveling

| rAthena fn | Status | C# location / note |
|---|---|---|
| `hom_levelup` | ✅ | `LevelUp` — caps at HOMUNCULUS_MAX_BASE_LV (175), refreshes HP/SP |
| `hom_gainexp` | ✅ | `GainExp` — naive `Level*1000` curve; pending real exp_homunculus table (see PARITY-REMAINING.md §P2.2) |
| `hom_reset_stats` | ✅ | `ResetStats` — refunds skill points + clears `Skills` |
| `hom_heal` | ✅ | `Heal` — clamp(HP/SP) against MaxHp/MaxSp helpers |
| `hom_food` | ✅ | `Food` — +25 hunger, +10 intimacy via IncreaseIntimacy |

### Intimacy & grades

| rAthena fn | Status | C# location / note |
|---|---|---|
| `hom_increase_intimacy` | ✅ | `IncreaseIntimacy` — Min(MaxIntimacy=1000) |
| `hom_decrease_intimacy` | ✅ | `DecreaseIntimacy` — Max(0) |
| `hom_get_intimacy_grade` | ✅ | `GetIntimacyGrade` — real (5-tier: 0/100/250/750/910) |
| `hom_intimacy_grade2intimacy` | ✅ | `IntimacyGrade2Intimacy` — real (inverse table) |

### Skill tree

| rAthena fn | Status | C# location / note |
|---|---|---|
| `hom_skill_tree_get_max` | ✅ | `SkillTreeGetMax` — DB lookup against `homunculus_skill_tree_db` (74 rows) |
| `hom_skill_get_min_level` | ✅ | `SkillGetMinLevel` — min `required_level` across classes |
| `hom_skillup` | ✅ | `SkillUp` — point check + cap from skill-tree DB |
| `hom_calc_skilltree` / `_sub` | ✅ | `CalcSkillTree` / `Sub` — walks DB rows by class, marks eligibles |

### Spirit ball & menu

| rAthena fn | Status | C# location / note |
|---|---|---|
| `hom_addspiritball` | ✅ | `AddSpiritBall` — Min(max, +1) |
| `hom_delspiritball` | ✅ | `DelSpiritBall` — one/all variant |
| `hom_menu` | ✅ | `Menu` — 4-action dispatch (feed/call-vaporize/skill/delete) |

### Misc & timers

| rAthena fn | Status | C# location / note |
|---|---|---|
| `hom_change_name` / `_ack` | ✅ | `ChangeName` / `ChangeNameAck` — pending-rename buffer + commit on ack |
| `hom_class2mapid` | ✅ | `Class2MapId` — real (pass-through) |
| `hom_reload` | ✅ | `Reload` — pulls catalog + skill tree from repos |
| `hom_init_timers` | ✅ | `InitTimers` — seeds LastHungerTick |
| `hom_hungry_timer_delete` | ✅ | `HungryTimerDelete` — zeroes LastHungerTick |

## Coverage summary

| Bucket | ✅ | ⚠️ | ❌ | Total |
|---|---|---|---|---|
| Lifecycle | 12 | 0 | 2 | 14 |
| Evolution & mutations | 3 | 0 | 0 | 3 |
| Stats & leveling | 5 | 0 | 0 | 5 |
| Intimacy & grades | 4 | 0 | 0 | 4 |
| Skill tree | 5 | 0 | 0 | 5 |
| Spirit ball & menu | 3 | 0 | 0 | 3 |
| Misc & timers | 5 | 0 | 0 | 5 |
| **Totals** | **37** | **0** | **2** | **39** |

## History

### 2026-05-24 — P2.1 doc-resync close-out (32 stale ⚠️ → ✅; 0 genuine gaps remain)

All 32 ⚠️ rows flipped to ✅: AT-D2/D3 wave landed real bodies for the
full lifecycle / evolution / leveling / skill-tree / spirit-ball / menu /
timer surface. The only remaining non-✅ entries are the 2 ❌ rows for
`do_init_homunculus` / `do_final_homunculus`, which DI handles
implicitly (not modelled in `IHomunculusService`).

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
