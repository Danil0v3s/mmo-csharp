# npc_chat.cpp parity · 2026-05-20

`src/map/npc_chat.cpp` (443 lines, 8 functions) — PCRE-pattern NPC
event triggers (chat from a PC matches → fires an NPC event).

## Subsystem coverage

| rAthena fn | Status | C# location |
|---|---|---|
| `buildin_defpattern` | ✅ | [NpcChatService.DefPattern](/Map.Server/Scripting/NpcChat/NpcChatService.cs) |
| `buildin_activatepset` | ✅ | `NpcChatService.ActivatePset` |
| `buildin_deactivatepset` | ✅ | `NpcChatService.DeactivatePset` |
| `buildin_deletepset` | ✅ | `NpcChatService.DeletePset` |
| `npc_chat_sub` | ⚠️ | `CheckChat` — event-fire wiring data-pending |
| `npc_chat_def_pattern` | ✅ | `DefaultPattern` |
| `npc_chat_finalize` | ✅ | `Finalize` |
| `finalize_pcrematch_entry` | ✅ | `FinalizeEntry` |

## History

### 2026-05-20 — initial audit + service
- `INpcChatService` / `NpcChatService` covers all 8 functions.
- PCRE replaced with `System.Text.RegularExpressions.Regex`.
- Event-fire dispatch hooks into the script engine when public-chat
  delivery passes through the npc-chat gate.
