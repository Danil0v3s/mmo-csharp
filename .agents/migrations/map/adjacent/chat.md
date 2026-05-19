# MS3 · Chat

**Phase:** MS3 (adjacent)
**Depends on:** [session.md](../session.md), Inter-base routing (P5 done)
**Blocks:** —

Most of chat is already wired at the IPC layer (P5 inter-base routing). The map server side is mostly receiving the push notifications from char and emitting them to clients in view.

## Source of truth

- [rathena/src/map/clif.cpp](/Volumes/1TB/Projetos/rathena/src/map/clif.cpp) — `clif_parse_GlobalMessage` (public chat), `clif_parse_WisMessage` (whisper), `clif_parse_PartyMessage`, `clif_parse_GuildMessage`, `clif_parse_Broadcast`
- [rathena/src/map/atcommand.cpp](/Volumes/1TB/Projetos/rathena/src/map/atcommand.cpp) — GM commands (`/atcommand` style)
- [rathena/src/map/channel.cpp](/Volumes/1TB/Projetos/rathena/src/map/channel.cpp) — channel chat (`#main`)

## Scope (MS3 first pass)

**In scope:**
- Public chat (`CZ_REQUEST_CHAT (0x00f3)` — PACKETVER 20220401 shuffle, was `0x008c`) → emit `ZC_NOTIFY_PLAYERCHAT` to view-range.
- Whisper (`CZ_WHISPER (0x0096)`) → look up target name via existing IPC; if local map, deliver direct; if remote, route via `InterWhisper`.
- Party chat / Guild chat: scoped broadcast via party/guild member lookups (Char IPC for member lists).
- Server broadcast (`/b`, `/nb`): GM command → `InterBroadcast` → fan-out.
- GM `@commands` (start with `@`): handled locally on map server (no IPC, except for char-state changes which use existing IPC).
- Channel chat (`#main`, `#trade`, etc.): map server hosts a per-channel subscriber list; messages broadcast to all in channel.

**Out of scope (later):**
- Channel-database persistence (which players are in which channels across logout).
- Whisper ignore list.
- Cross-server channel propagation (currently each map server hosts its own channels).

## Done

- **P5 inter-base routing** for broadcast and whisper exists (char→map). Map-side receive handlers log+ack today.
- **Public chat** ([ChatMessageHandler.cs](../../../../Map.Server/Handlers/ChatMessageHandler.cs)) — plain text (no `@` prefix) now broadcasts [`ZC_NOTIFY_CHAT`](../../../../Core.Server/Packets/Out/ZC/ZC_NOTIFY_CHAT.cs) (0x008d) to the speaker's AOI via `IVisibilityService.SendToArea`. Wire format: `008d <packet_len>.W <src_id>.L <message>.?B`.
- **GM `@command` parser + dispatch** — `@where`, `@killmob`, `@warp`, `@damage` shipped in [Map.Server/Gm/](../../../../Map.Server/Gm/). Permission gate via `MapSessionData.GroupId ≥ command.MinGroupId`.

## Pending

1. **Whisper** (`CZ_WHISPER 0x0096`) — wire packet not yet built; service contract is that names resolve locally first, then fall through to char-server `InterWhisper` for cross-server delivery.
2. **Party chat / Guild chat** scoped broadcast — needs party/guild member-list lookup (existing P5 IPC). Receive-side `MapGrpcService.ReceiveWhisper` already has queue+ack stub.
3. **`/b` server-wide broadcast** — GM command → `InterBroadcast` → fan-out. Char-side IPC ready.
4. **Channel system** (`#main`, `#trade`) — per-server `ChannelRegistry` with subscribe/unsubscribe handlers.

### Acceptance
- ✅ Two players in view: A types `hello` → B sees it (public chat).
- ✅ GM uses `@warp prontera 155 191` → calling GM warps (via `IPcSetposService`).
- ⚠️ Whisper / party / guild / broadcast / channel — service routing exists, wire emission pending.

## History
- **2026-05-16** — Plan stub.
- **2026-05-19** — Public chat broadcast shipped. ZC_NOTIFY_CHAT defined and emitted from ChatMessageHandler when no `@` prefix is present. Whisper / party / guild / channel still pending wire packets; routing infrastructure (P5 IPC) untouched.
