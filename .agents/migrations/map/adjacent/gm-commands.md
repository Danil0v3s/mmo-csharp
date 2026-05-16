# MS3 · GM commands

**Phase:** MS3 (adjacent)
**Depends on:** [session.md](../session.md) (GroupId on session), [entities.md](../entities.md), [visibility.md](../visibility.md)
**Used by:** test harness, dev workflow

rAthena's GM commands (`@`-prefix in chat) cover ~200 commands ranging from
trivial info (`@where`, `@time`) through gameplay manipulation (`@warp`,
`@killmob`, `@item`) to admin operations (`@ban`, `@reloadmobdb`). Per-account
authorization comes from `account_data.group_id` and the rules in
`conf/groups.conf`.

For MS3 first slice we wire the plumbing: the chat-line tokenizer, the
per-command dispatch + auth gate, the self-only feedback packet, and three
canonical commands (`@where`, `@killmob`, `@warp`). Adding more commands is
a one-file-per-command pattern.

## Source of truth

- [rathena/src/map/atcommand.cpp](/Volumes/1TB/Projetos/rathena/src/map/atcommand.cpp) — the giant dispatcher; ~12K lines
- [rathena/conf/groups.conf](/Volumes/1TB/Projetos/rathena/conf/groups.conf) — group permissions
- [rathena/src/map/clif.cpp](/Volumes/1TB/Projetos/rathena/src/map/clif.cpp) — `clif_parse_GlobalMessage` (CZ_REQUEST_CHAT routing) and `clif_displaymessage` (ZC_NOTIFY_PLAYERCHAT)

## Done

- **GroupId in auth payload.** Char server fetches account info from login server on each `RequestCharacterMapAuth` and forwards `group_id` to the map server. `MapSessionData.GroupId` carries it.
- **Packets:**
  - [`CZ_REQUEST_CHAT`](../../../../Core.Server/Packets/In/CZ/CZ_REQUEST_CHAT.cs) (0x008c, variable) — global chat / GM command entry.
  - [`ZC_NOTIFY_PLAYERCHAT`](../../../../Core.Server/Packets/Out/ZC/ZC_NOTIFY_PLAYERCHAT.cs) (0x008e, variable) — self-only system message (rAthena `clif_displaymessage`). Null-terminated body.
- **Command framework** in [Map.Server/Gm/](../../../../Map.Server/Gm/):
  - [`IGmCommand`](../../../../Map.Server/Gm/IGmCommand.cs) — `Name`, `MinGroupId`, `Description`, `ExecuteAsync`.
  - [`IGmCommandRegistry`](../../../../Map.Server/Gm/IGmCommandRegistry.cs) + [`GmCommandRegistry`](../../../../Map.Server/Gm/GmCommandRegistry.cs) — case-insensitive name lookup.
  - [`GmCommandParser`](../../../../Map.Server/Gm/GmCommandParser.cs) — splits chat lines like `"Hero : @warp 156 191"` into `(name, args)`. Tolerates the optional `"<name> : "` prefix and accepts both `@` and `#`.
- **Dispatcher:** [`ChatMessageHandler`](../../../../Map.Server/Handlers/ChatMessageHandler.cs) routes `CZ_REQUEST_CHAT`; non-GM chat is silently dropped (chat broadcast lands with [adjacent/chat.md](chat.md)).
- **Concrete commands** in [Map.Server/Gm/Commands/](../../../../Map.Server/Gm/Commands/):
  - `@where` — echoes map + cell (GroupId ≥ 1).
  - `@killmob` — kills the nearest mob in AOI via `IMobSpawnService.KillMob` (GroupId ≥ 60).
  - `@warp <x> <y>` — same-map teleport with `VanishReason.Teleport` + spawn-broadcast pair (GroupId ≥ 60).
- 12 tests in [Map.Server.Tests/Gm/](../../../../Map.Server.Tests/Gm/): parser edge cases, each command's happy + failure path.

## Pending

- **Cross-map `@warp`** — needs the warp / mapchange flow (rAthena `pc_setpos` cross-map branch). Today `@warp` only teleports within the current map.
- **`conf/groups.conf` parity** — rAthena's full group permissions matrix. Today we use a flat `MinGroupId` check. The `conf/groups.conf` import (custom permission flags, command-name overrides) lands later.
- **More commands** — `@item`, `@heal`, `@speed`, `@gm`, `@ban`, etc. Each is a single file under `Map.Server/Gm/Commands/` plus a DI registration.
- **Public chat broadcast** — the plain-chat code path is wired but the broadcast itself (`ZC_NOTIFY_CHAT`) lands in [chat.md](chat.md).

## History

- **2026-05-16** — Framework + 3 canonical commands shipped. 12 tests; 170 Map.Server tests green.
