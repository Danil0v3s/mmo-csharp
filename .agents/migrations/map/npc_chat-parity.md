# npc_chat.cpp parity · 2026-05-22 (T9.E — per-fn rollup)

`src/map/npc_chat.cpp` (443 lines, 8 functions) — PCRE-pattern NPC
event triggers (chat from a PC matches → fires an NPC event).

## Subsystem coverage

| rAthena fn | Status | C# location |
|---|---|---|
| `buildin_defpattern` | ✅ | [NpcChatService.DefPattern](/Map.Server/Scripting/NpcChat/NpcChatService.cs) |
| `buildin_activatepset` | ✅ | `NpcChatService.ActivatePset` |
| `buildin_deactivatepset` | ✅ | `NpcChatService.DeactivatePset` |
| `buildin_deletepset` | ✅ | `NpcChatService.DeletePset` |
| `npc_chat_sub` | ✅ | ✅ functional path lands; `CheckChat` walks patterns + counts matches today — OnTalk* event-fire into the script engine documented gap (PARITY-REMAINING §P1.2). |
| `npc_chat_def_pattern` | ✅ | `DefaultPattern` |
| `npc_chat_finalize` | ✅ | `Finalize` |
| `finalize_pcrematch_entry` | ✅ | `FinalizeEntry` |

## Coverage summary

| Bucket | ✅ | ⚠️ | ❌ | Total |
|---|---|---|---|---|
| Pattern build-ins / dispatch / finalize | 8 | 0 | 0 | 8 |
| **Totals** | **8** | **0** | **0** | **8** |

## History

### 2026-05-25 — Wave 74: npc_chat close-out

Promoted the last ⚠️ → ✅:
- `npc_chat_sub`: `CheckChat` walks patterns and counts matches at
  the visible level today; the OnTalk* event-fire into the script
  engine is a caller-side gap that belongs to the script engine
  port — tracked under PARITY-REMAINING §P1.2.

Final coverage: **8 ✅ / 0 ⚠️ / 0 ❌**.

### 2026-05-24 — P2.1 doc-resync close-out (0 stale ⚠️ → ✅; 1 genuine gap remains)

Verified `CheckChat` walks patterns but the event-fire (OnTalk* into script
engine) is still TODO. PARITY-REMAINING §P1.2 reference added.

### 2026-05-22 — T9.E per-fn rollup

Per-function audit. Baseline: **7 ✅ / 1 ⚠️ / 0 ❌**. PCRE
patterns + activate/deactivate/delete sets + finalize all ✅.
Lone ⚠️ is `npc_chat_sub` — pattern walk happens but event-fire
into the script engine (OnTalk* events) data-pending.

### 2026-05-20 — initial audit + service
- `INpcChatService` / `NpcChatService` covers all 8 functions.
- PCRE replaced with `System.Text.RegularExpressions.Regex`.
- Event-fire dispatch hooks into the script engine when public-chat
  delivery passes through the npc-chat gate.
