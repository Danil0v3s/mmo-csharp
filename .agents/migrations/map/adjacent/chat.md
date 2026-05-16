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
- Public chat (`CZ_REQUEST_CHAT (0x008c)`) → emit `ZC_NOTIFY_PLAYERCHAT` to view-range.
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

P5 inter-base routing for broadcast and whisper exists. The char→map `ReceiveBroadcast` / `ReceiveWhisper` handlers in [MapGrpcService.cs](../../../Map.Server/MapGrpcService.cs) currently log+ack. Gameplay must emit to clients.

## Pending

1. `PublicChatHandler` (`CZ_REQUEST_CHAT`) → broadcast `ZC_NOTIFY_PLAYERCHAT` to view-range.
2. `WhisperHandler` (`CZ_WHISPER`) → resolve recipient locally; if not found here, route via Char IPC `InterWhisper`.
3. Receive-side from char IPC: `MapGrpcService.ReceiveWhisper` → look up recipient session locally → emit `ZC_WHISPER` to client. Already has the queue+ack stub.
4. `@command` parser + handler dispatch. Start with: `@kill`, `@warp`, `@item`, `@heal`, `@monster`, `@kick`. Implementations vary; keep them small + GM-permission-gated.
5. Channel system: per-server `ChannelRegistry`; subscribe / unsubscribe handlers; broadcast within channel.

### Acceptance
- Two players in view: A types `hello` → B sees it.
- A whispers to B (on same map server): B receives it.
- A whispers to C (on a different map server): the message routes via Char IPC; C receives it.
- `@warp prontera 155 191` warps the calling GM.
- `/b Hello world` broadcasts to every player on every map server.

## History
- **2026-05-16** — Plan stub.
