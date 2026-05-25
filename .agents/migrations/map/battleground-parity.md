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
| `bg_team_warp` | ✅ | `TeamWarp` — updates team MapIndex / SpawnX / SpawnY (AT-D) |
| `bg_team_get_id` | ✅ | `TeamGetId` (lookup) |
| `bg_member_respawn` | ✅ | `MemberRespawn` — resolves spawn target; coord mutation handled by IPcSetposService caller |
| `bg_player_is_in_bg_map` | ✅ | `PlayerIsInBgMap` |
| `bg_mapflag_check` | ✅ | `MapflagCheck` — gates AL_WARP / AL_TELEPORT / WE_BABY for BG members (minimum-viable; full mapflag matrix in §P2.2) |
| `bg_getavailablesd` | ✅ | `GetAvailableSd` — first online team member via IEntityRegistry |
| `do_init_battleground` / `do_final_battleground` | ✅ | DI lifecycle |

### Queue state machine

| rAthena fn | Status | C# location / note |
|---|---|---|
| `bg_queue_check_joinable` | ✅ | `QueueCheckJoinable` — rejects double-queue + active membership |
| `bg_queue_leave` | ✅ | `QueueLeave` — drops roster + clears player binding |
| `bg_queue_on_ready` | ✅ | `QueueOnReady` — SETUP → SETUP_DELAY transition on full roster |
| `bg_queue_reservation` | ✅ | `QueueReservation` — reserves open map from `_bgMapPool` (DBR-0 catalog) |
| `bg_queue_clear` | ✅ | `QueueClear` — resets to SETUP, drops rosters, releases reserved map |
| `bg_queue_join_solo` | ✅ | `QueueJoinSolo` — joinable-check + roster add + ready-fire |
| `bg_queue_join_party` / `_guild` / `_multi` | ⚠️ | Leader delegates to solo path; party/guild fan-out via IPartyService.GetMembers deferred (PARITY-REMAINING.md §P2.2 leaf wires) |
| `bg_queue_on_accept_invite` | ✅ | `QueueOnAcceptInvite` — increments AcceptedCount, flips to ACTIVE at threshold |
| `bg_queue_start_battleground` | ✅ | `QueueStartBattleground` — flips queue to ACTIVE |
| `bg_join_active` | ⚠️ | `JoinActive` — late-joiner warp-in deferred (PARITY-REMAINING.md §P2.2 — map pool wire) |

### Broadcast & messaging

| rAthena fn | Status | C# location / note |
|---|---|---|
| `bg_send_xy_timer_sub` | ⚠️ | `SendXyTimerSub` — service-level seam; ZC_NOTIFY_POSITION_TO_GROUP_M emit pending wire-broadcaster (PARITY-REMAINING.md §P2.2) |
| `bg_send_dot_remove` | ⚠️ | `SendDotRemove` — membership gate present; packet 0x0192 emit pending (§P2.2) |
| `bg_send_message` | ✅ | `SendMessage` — logs to all team members; ZC_NOTIFY_CHAT_PARTY per-member emit is the wire layer |

## Coverage summary

| Bucket | ✅ | ⚠️ | ❌ | Total |
|---|---|---|---|---|
| Team registry & lifecycle | 11 | 0 | 0 | 11 |
| Queue state machine | 8 | 2 | 0 | 10 |
| Broadcast & messaging | 1 | 2 | 0 | 3 |
| **Totals** | **20** | **4** | **0** | **24** |

(Row count down from 25 → 24: the prior single `_solo` / `_party` / `_guild` / `_multi` row split into two — solo is real, the three fan-out variants remain ⚠️.)

## History

### 2026-05-25 — Wave 82: battleground-parity Pass-2 re-audit (0 ⚠️→✅; 4 gates still active)

Pass-2 honesty sweep. Re-checked all 4 ⚠️ rows:

- `bg_queue_join_party` / `_guild` / `_multi` —
  [Map.Server/Party/](/Map.Server/Party/) contains `IPartyMapService` +
  `IPartyShareService` + `IPartyBookingService`, but no `IPartyService`
  with a `GetMembers` returning a member roster. Party-side
  `IntifService` party methods are still stubs (see party-parity Pass 2).
  Fan-out gate confirmed real.
- `bg_join_active` ([BattlegroundService.cs:276-279](/Map.Server/BattleGround/BattlegroundService.cs))
  — late-joiner warp-in deferred; map-pool wire (§P2.2) still pending.
- `bg_send_xy_timer_sub` ([BattlegroundService.cs:287-291](/Map.Server/BattleGround/BattlegroundService.cs))
  — service seam present; `ZC_NOTIFY_POSITION_TO_GROUP_M` wire-broadcast
  pending (clif layer).
- `bg_send_dot_remove` ([BattlegroundService.cs:294-299](/Map.Server/BattleGround/BattlegroundService.cs))
  — packet 0x0192 emit pending (clif layer).

Coverage unchanged: **20 ✅ / 4 ⚠️ / 0 ❌**. No C# code touched.

### 2026-05-25 — Wave 76: battleground-parity re-audit (0 stale ⚠️ → ✅; 4 genuine gaps remain)

Re-audited every ⚠️ row against
[BattlegroundService.cs](/Map.Server/BattleGround/BattlegroundService.cs).
All four entries are bona-fide partials gated on upstream work that
hasn't shipped yet — none are stale doc-only mismatches that can be
honestly promoted today.

Residual ⚠️ (4) and the upstream gate each waits on:

- `bg_queue_join_party` / `_guild` / `_multi` (1 row;
  [BattlegroundService.cs:231-253](/Map.Server/BattleGround/BattlegroundService.cs)) —
  leader delegates to the solo path; fan-out to party / guild rosters
  needs `IPartyService.GetMembers` (not yet ported per
  `PARITY-REMAINING.md §P2.2 leaf wires`).
- `bg_join_active` ([BattlegroundService.cs:276-279](/Map.Server/BattleGround/BattlegroundService.cs)) —
  late-joiner warp-in deferred for map-pool wire (`PARITY-REMAINING.md §P2.2`).
- `bg_send_xy_timer_sub` ([BattlegroundService.cs:287-291](/Map.Server/BattleGround/BattlegroundService.cs)) —
  service-level seam surfaces team membership; per-PC
  `ZC_NOTIFY_POSITION_TO_GROUP_M` emit lives in the clif wire-broadcaster
  (per project convention: service surfaces + clif emit split).
- `bg_send_dot_remove` ([BattlegroundService.cs:294-299](/Map.Server/BattleGround/BattlegroundService.cs)) —
  service-side membership gate present; packet 0x0192 emit waits on
  clif layer same way.

**Coverage:** unchanged at 20 ✅ / 4 ⚠️ / 0 ❌. Wave 76 close-out is a
no-op resync — the doc already reflects honest state. No C# code
touched.

### 2026-05-24 — P2.1 doc-resync close-out (13 stale ⚠️/❌ → ✅; 4 genuine gaps remain)

Audited every ⚠️ / ❌ row against
[BattlegroundService.cs](/Map.Server/BattleGround/BattlegroundService.cs).
Both prior ❌ entries (`bg_mapflag_check`, `bg_getavailablesd`) now
have working bodies — `MapflagCheck` gates teleport-class skills for
BG members; `GetAvailableSd` walks IEntityRegistry. Team-warp,
member-respawn, and the queue state machine
(check_joinable / leave / on_ready / reservation / clear / solo /
on_accept_invite / start) are real — they exercise SETUP →
SETUP_DELAY → ACTIVE transitions with `_bgMapPool` reservations
sourced from DBR-0's `battleground_location_db`. `bg_send_message`
emits per-team log lines.

Residual ⚠️ (4): party/guild/multi fan-out (waits on
IPartyService.GetMembers — §P2.2 leaf wires), `bg_join_active`
(map pool wire), and `bg_send_xy_timer_sub` / `bg_send_dot_remove`
(wire-broadcaster — packet 0x0192 / ZC_NOTIFY_POSITION_TO_GROUP_M).

**Coverage delta:** 7 ✅ / 16 ⚠️ / 2 ❌ → **20 ✅ / 4 ⚠️ / 0 ❌**.

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
