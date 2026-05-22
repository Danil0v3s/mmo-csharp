# chat.cpp parity · 2026-05-22 (T9.E — per-fn rollup)

`src/map/chat.cpp` (507 lines, 13 functions) — in-world chat rooms
(`/chat` PC rooms + NPC-created chat rooms with event triggers).

Canonical entry points: [IChatRoomService](/Map.Server/Chat/Rooms/IChatRoomService.cs) /
[ChatRoomService](/Map.Server/Chat/Rooms/ChatRoomService.cs) —
transient in-memory registry. Wire packets (ZC_ROOM_NEWENTRY etc.)
data-pending on packet emitters.

## Per-function coverage

| rAthena fn | Status | C# location / note |
|---|---|---|
| `chat_createpcchat` | ✅ | `CreatePcChat` |
| `chat_createnpcchat` | ✅ | `CreateNpcChat` |
| `chat_joinchat` | ✅ | `JoinChat` (password + limit checks) |
| `chat_leavechat` | ✅ | `LeaveChat` |
| `chat_changechatowner` | ⚠️ | `ChangeChatOwner` — stub |
| `chat_changechatstatus` | ⚠️ | `ChangeChatStatus` — stub |
| `chat_kickchat` | ⚠️ | `KickChat` — stub |
| `chat_npckickchat` | ⚠️ | `NpcKickChat` — stub |
| `chat_npckickall` | ✅ | `NpcKickAll` (clears members) |
| `chat_deletenpcchat` | ✅ | `DeleteNpcChat` |
| `chat_enableevent` | ⚠️ | `EnableEvent` — existence check only |
| `chat_disableevent` | ⚠️ | `DisableEvent` — existence check only |
| `chat_triggerevent` | ⚠️ | `TriggerEvent` — existence check only |

## Coverage summary

| Bucket | ✅ | ⚠️ | ❌ | Total |
|---|---|---|---|---|
| Chat room lifecycle / events | 6 | 7 | 0 | 13 |
| **Totals** | **6** | **7** | **0** | **13** |

## History

### 2026-05-22 — T9.E per-fn rollup

Per-function audit. Baseline: **6 ✅ / 7 ⚠️ / 0 ❌**. Core lifecycle
(create / delete / join / leave / NpcKickAll) all ✅. ⚠️ rows are
ownership transfer, status edit, kick variants, and the event
trigger family (existence-check stubs pending script-engine
dispatch).

### 2026-05-20 — initial audit + service
- 13 functions covered.
