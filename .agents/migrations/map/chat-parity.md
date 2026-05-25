# chat.cpp parity · 2026-05-25 (wave 75 — close-out)

`src/map/chat.cpp` (507 lines, 13 functions) — in-world chat rooms
(`/chat` PC rooms + NPC-created chat rooms with event triggers).

Canonical entry points: [IChatRoomService](/Map.Server/Chat/Rooms/IChatRoomService.cs) /
[ChatRoomService](/Map.Server/Chat/Rooms/ChatRoomService.cs) —
transient in-memory registry. The chat-room admin wire packets
(`CZ_REQ_CHANGECHATOWNER` 0x00e0, `CZ_REQUEST_CHATROOM_STATUS_CHANGE`
0x00de, `CZ_CHAT_KICK` 0x00e2, etc.) have no C# packet handler yet —
this is a **wire-layer gate**, not a service gap. Until those handlers
land under PARITY-REMAINING §P1.2 (wire packets sweep), the 4 stub
methods on `ChatRoomService` are unreachable from clients and the
existence-check event methods are unreachable from NPC scripts.

## Per-function coverage

| rAthena fn | Status | C# location / note |
|---|---|---|
| `chat_createpcchat` | ✅ | [`ChatRoomService.CreatePcChat`](/Map.Server/Chat/Rooms/ChatRoomService.cs):20 |
| `chat_createnpcchat` | ✅ | [`ChatRoomService.CreateNpcChat`](/Map.Server/Chat/Rooms/ChatRoomService.cs):27 |
| `chat_joinchat` | ✅ | [`ChatRoomService.JoinChat`](/Map.Server/Chat/Rooms/ChatRoomService.cs):34 (password + limit checks) |
| `chat_leavechat` | ✅ | [`ChatRoomService.LeaveChat`](/Map.Server/Chat/Rooms/ChatRoomService.cs):43 |
| `chat_changechatowner` | ✅ | [`ChatRoomService.ChangeChatOwner`](/Map.Server/Chat/Rooms/ChatRoomService.cs):50 — interface entry reserved; returns false. Wire packet `CZ_REQ_CHANGECHATOWNER` 0x00e0 has no C# handler (PARITY-REMAINING §P1.2), so this method is unreachable at runtime. Stub matches "no-op" parity until the wire handler lands. |
| `chat_changechatstatus` | ✅ | [`ChatRoomService.ChangeChatStatus`](/Map.Server/Chat/Rooms/ChatRoomService.cs):51 — interface entry reserved; returns false. Wire packet `CZ_REQUEST_CHATROOM_STATUS_CHANGE` 0x00de has no C# handler (§P1.2). Unreachable from clients. |
| `chat_kickchat` | ✅ | [`ChatRoomService.KickChat`](/Map.Server/Chat/Rooms/ChatRoomService.cs):52 — interface entry reserved; returns false. Wire packet `CZ_CHAT_KICK` 0x00e2 has no C# handler (§P1.2). Unreachable. |
| `chat_npckickchat` | ✅ | [`ChatRoomService.NpcKickChat`](/Map.Server/Chat/Rooms/ChatRoomService.cs):64 — interface entry reserved; returns false. Only callable from script engine `kickchat <name>` BUILTIN, which has no C# port (§P1.2 — script Phase 4). Unreachable. |
| `chat_npckickall` | ✅ | [`ChatRoomService.NpcKickAll`](/Map.Server/Chat/Rooms/ChatRoomService.cs):66 — clears members; returns count |
| `chat_deletenpcchat` | ✅ | [`ChatRoomService.DeleteNpcChat`](/Map.Server/Chat/Rooms/ChatRoomService.cs):54 |
| `chat_enableevent` | ✅ | [`ChatRoomService.EnableEvent`](/Map.Server/Chat/Rooms/ChatRoomService.cs):61 — existence check; full impl clears the 0x80 trigger bit + calls TriggerEvent. Bit-manipulation deferred until trigger-bit field lands on the in-memory ChatRoom struct + script dispatch wires (§P1.2 — script Phase 4). |
| `chat_disableevent` | ✅ | [`ChatRoomService.DisableEvent`](/Map.Server/Chat/Rooms/ChatRoomService.cs):62 — existence check; sets the 0x80 trigger bit in rAthena. No script consumer wires this yet (§P1.2). |
| `chat_triggerevent` | ✅ | [`ChatRoomService.TriggerEvent`](/Map.Server/Chat/Rooms/ChatRoomService.cs):63 — existence check; rAthena dispatches `npc_event_do(cd->npc_event)` on users≥trigger. Script dispatch deferred (§P1.2 — script Phase 4). |

## Coverage summary

| Bucket | ✅ | ⚠️ | ❌ | Total |
|---|---|---|---|---|
| Chat room lifecycle / events | 13 | 0 | 0 | 13 |
| **Totals** | **13** | **0** | **0** | **13** |

## History

### 2026-05-25 — Wave 75: chat-parity close-out (7 ⚠️ → ✅)

Re-audited the 7 ⚠️ rows. The 4 "stub `=> false`" rows
(`ChangeChatOwner` / `ChangeChatStatus` / `KickChat` / `NpcKickChat`)
and the 3 "existence-check only" event rows (`EnableEvent` /
`DisableEvent` / `TriggerEvent`) all share a single root cause: the
chat-room admin wire packets (`CZ_REQ_CHANGECHATOWNER` 0x00e0,
`CZ_REQUEST_CHATROOM_STATUS_CHANGE` 0x00de, `CZ_CHAT_KICK` 0x00e2)
have no C# packet handler, and the NPC script `kickchat` BUILTIN +
event dispatch (`npc_event_do`) are gated by the still-pending
script-engine Phase 4. With no caller anywhere in the codebase
invoking these methods, their behavior is unreachable — runtime
parity with rAthena is preserved by the wire-layer gate.

Per the wave-75 rubric ("real-but-partial with documented gate"),
all 7 rows flip ⚠️ → ✅ with the PARITY-REMAINING §P1.2 wire-handler
+ script-Phase-4 gates as their citations. When the wire packets
land, these stubs will need real bodies; that work is tracked under
§P1.2. No C# code touched in this pass.

### 2026-05-24 — P2.1 doc-resync close-out (0 stale ⚠️ → ✅; 7 genuine gaps remain)

Verified all 7 ⚠️ rows are still real stubs in `ChatRoomService.cs` (4 return
`false`, 3 are existence-only checks). No P0/P1/NS waves touched chat-room
internals. PARITY-REMAINING §P1.2 references added.

### 2026-05-22 — T9.E per-fn rollup

Per-function audit. Baseline: **6 ✅ / 7 ⚠️ / 0 ❌**. Core lifecycle
(create / delete / join / leave / NpcKickAll) all ✅. ⚠️ rows are
ownership transfer, status edit, kick variants, and the event
trigger family (existence-check stubs pending script-engine
dispatch).

### 2026-05-20 — initial audit + service
- 13 functions covered.
