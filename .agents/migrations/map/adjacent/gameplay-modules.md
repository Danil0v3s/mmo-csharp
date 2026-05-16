# MS3 · Gameplay modules — party / guild / mail / quest / clan / pet / homunculus / mercenary / elemental

**Phase:** MS3 (adjacent)
**Depends on:** [session.md](../session.md), [combat.md](combat.md), [items.md](items.md), [skills.md](skills.md), [chat.md](chat.md)
**Blocks:** —

All of these have their IPC wrappers ready (P6). What's missing is the gameplay layer that triggers them and renders state to the client.

## Source of truth

| Module | rAthena ref | Char IPC wrapper (ready) |
|---|---|---|
| Party | [party.cpp](/Volumes/1TB/Projetos/rathena/src/map/party.cpp) (1575 lines) | `ICharServerIpcServiceParty` |
| Guild | [guild.cpp](/Volumes/1TB/Projetos/rathena/src/map/guild.cpp) (2755 lines) | `ICharServerIpcServiceGuild` |
| Mail | [mail.cpp](/Volumes/1TB/Projetos/rathena/src/map/mail.cpp) | `ICharServerIpcServiceMail` |
| Quest | [quest.cpp](/Volumes/1TB/Projetos/rathena/src/map/quest.cpp) | `ICharServerIpcServiceQuest` |
| Achievement | [achievement.cpp](/Volumes/1TB/Projetos/rathena/src/map/achievement.cpp) | (in Quest service) |
| Clan | [clan.cpp](/Volumes/1TB/Projetos/rathena/src/map/clan.cpp) | `ICharServerIpcServiceClan` |
| Pet | [pet.cpp](/Volumes/1TB/Projetos/rathena/src/map/pet.cpp) | `ICharServerIpcServicePet` |
| Homunculus | [homunculus.cpp](/Volumes/1TB/Projetos/rathena/src/map/homunculus.cpp) | `ICharServerIpcServiceHomunculus` |
| Mercenary | [mercenary.cpp](/Volumes/1TB/Projetos/rathena/src/map/mercenary.cpp) | `ICharServerIpcServiceMercenary` |
| Elemental | [elemental.cpp](/Volumes/1TB/Projetos/rathena/src/map/elemental.cpp) | `ICharServerIpcServiceElemental` |

## Scope

These can all parallelize. Each module's pattern is the same: receive a client packet (or react to a gameplay event), call the corresponding IPC, broadcast the result to view / party / guild.

**Per-module checklist:**
- Inbound client packets (party invite, guild invite, mail send UI, quest tracker, etc.).
- Outbound client packets (party member list, guild emblem update, mail inbox).
- Gameplay events that trigger IPC: `mob_dead` → `QuestUpdate` for kill-counters; `player_levelup` → `AchievementUpdate`; etc.
- View-range broadcast of relevant changes.

## Done

All IPC wrappers ready. Char-side persistence done (P1, P8 closed the cascade bugs).

## Pending — per module

Each module gets its own focused PR/doc when we land it. Key client packets per module:

### Party
- `CZ_PARTY_INVITE2`, `CZ_PARTY_JOIN_REQ_ACK`, `CZ_REQ_LEAVE_GROUP`, `CZ_REG_EXPULSION` (kick).
- Trigger: `PartyShareLevel` config read on tick (P2 wired the persistence — gameplay uses it for exp eligibility check on `mob_dead`).
- Broadcast member position to other party members (mini-map dots).

### Guild
- `CZ_REQ_GUILD_*` family.
- Emblem upload/download (`CZ_REQ_GUILD_EMBLEM_IMG` / `ZC_GUILD_EMBLEM_IMG`).
- Castle siege (WoE) — defer, big system.

### Mail
- Modern RoDEX mail UI (`CZ_*_RODEX`).
- Open inbox / read / take attachment / send / return → all via IPC.

### Quest
- Quest journal UI; tracker; quest log update on `mob_dead` if matching mob_id.

### Achievement
- Achievement progress check on every relevant gameplay event (`mob_dead`, `pc_levelup`, `item_obtain`).

### Clan
- Mostly just chat (clan chat channel) and the join/leave hooks already in `ClanRequest` IPC.

### Pet / Homun / Mercenary / Elemental
- Each is a follower entity that walks with the player and acts. Pet has affection/hunger; Homun has skill tree; Merc has timer + skills; Elemental has aura + element.
- Spawn/despawn lifecycle hooks to char IPC (already wired).
- Combat hooks (each can attack / be attacked) — depends on combat doc.

### Acceptance
Per-module functional test against rAthena reference behavior, with the IPC round-trip verified end-to-end.

## History
- **2026-05-16** — Plan stub.
