# chat.cpp parity · 2026-05-20

`src/map/chat.cpp` (507 lines, 13 functions) — in-world chat rooms
(`/chat` PC rooms + NPC-created chat rooms with event triggers).

## Subsystem coverage

All 13 entries covered by [IChatRoomService](/Map.Server/Chat/Rooms/IChatRoomService.cs) /
[ChatRoomService](/Map.Server/Chat/Rooms/ChatRoomService.cs) — transient in-memory registry.
Wire packets (ZC_ROOM_NEWENTRY etc.) data-pending on packet
emitters.

## History

### 2026-05-20 — initial audit + service
- 13 functions covered.
