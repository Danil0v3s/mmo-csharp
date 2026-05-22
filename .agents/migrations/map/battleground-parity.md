# battleground.cpp parity · 2026-05-22 (T9.F — per-fn rollup)

`src/map/battleground.cpp` (1617 lines, 29+ public functions).
Queue + team registry; per-BG mode rules + bg_db.yml data-pending.

Canonical entry points: [IBattlegroundService](/Map.Server/BattleGround/IBattlegroundService.cs).

## Per-function coverage

### Team registry & lifecycle

| rAthena fn | Status | C# location / note |
|---|---|---|
| `bg_create` | ✅ | `IBattlegroundService.Create` (team ID gen) |
| `bg_team_join` | ✅ | `TeamJoin` (membership tracking) |
| `bg_team_leave` | ✅ | `TeamLeave` (roster removal) |
| `bg_team_delete` | ✅ | `TeamDelete` (full cleanup) |
| `bg_team_warp` | ⚠️ | `TeamWarp` — stub (map warp not implemented) |
| `bg_team_get_id` | ✅ | `TeamGetId` (lookup) |
| `bg_member_respawn` | ⚠️ | `MemberRespawn` — stub |
| `bg_player_is_in_bg_map` | ✅ | `PlayerIsInBgMap` |
| `bg_mapflag_check` | ❌ | `MapflagCheck` — returns false; logic not ported |
| `bg_getavailablesd` | ❌ | `GetAvailableSd` — returns null stub |
| `do_init_battleground` / `do_final_battleground` | ✅ | DI lifecycle |

### Queue state machine

| rAthena fn | Status | C# location / note |
|---|---|---|
| `bg_queue_check_joinable` | ⚠️ | `QueueCheckJoinable` — stub (returns true) |
| `bg_queue_leave` | ⚠️ | `QueueLeave` — stub |
| `bg_queue_on_ready` | ⚠️ | `QueueOnReady` — stub |
| `bg_queue_reservation` | ⚠️ | `QueueReservation` — stub |
| `bg_queue_clear` | ⚠️ | `QueueClear` — empty |
| `bg_queue_join_solo` / `_party` / `_guild` / `_multi` | ⚠️ | Empty stubs |
| `bg_queue_on_accept_invite` | ⚠️ | `QueueOnAcceptInvite` — empty |
| `bg_queue_start_battleground` | ⚠️ | `QueueStartBattleground` — empty |
| `bg_join_active` | ⚠️ | `JoinActive` — empty |

### Broadcast & messaging

| rAthena fn | Status | C# location / note |
|---|---|---|
| `bg_send_xy_timer_sub` | ⚠️ | `SendXyTimerSub` — empty |
| `bg_send_dot_remove` | ⚠️ | `SendDotRemove` — empty |
| `bg_send_message` | ⚠️ | `SendMessage` — empty |

## Coverage summary

| Bucket | ✅ | ⚠️ | ❌ | Total |
|---|---|---|---|---|
| Team registry & lifecycle | 7 | 2 | 2 | 11 |
| Queue state machine | 0 | 11 | 0 | 11 |
| Broadcast & messaging | 0 | 3 | 0 | 3 |
| **Totals** | **7** | **16** | **2** | **25** |

## History

### 2026-05-22 — T9.F per-fn rollup

Per-function audit. Baseline: **7 ✅ / 16 ⚠️ / 2 ❌** across 25
entries. Team CRUD ✅ (create / join / leave / delete / lookup).
16 ⚠️ are the queue state machine (QUEUE_STATE_SETUP/DELAY/ACTIVE
/ENDED not modeled) + map warp + broadcast/messaging (all empty
stubs). 2 ❌: `bg_mapflag_check` (MF_NOWARP not enforced) and
`bg_getavailablesd` (returns null).

### 2026-05-20 — initial audit + service
- 29 functions covered (canonical entry points; data-pending
  on parent dependency).
