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

All IPC wrappers ready (P6). Char-side persistence done (P1, P8 closed the cascade bugs).

**2026-05-19 wave:**

### Party
- `PlayerEntity.PartyId` field hydrated at session enter.
- **`PartyShareService`** ([Party/PartyShareService.cs](../../../../Map.Server/Party/PartyShareService.cs)) — ports `party_exp_share` (party.cpp:1238). On mob kill, splits EXP across eligible same-map alive party members, applies the rAthena even-share bonus (+10% per extra member). `DamageService.HandleDeath` routes through party share first, falls back to solo `pc_gainexp` if not in a party. 5 tests.

### Pet
- **`PetEntity`** specialized `MobEntity` with intimacy / hunger / equip / pet name.
- **`PetService`** ([Pet/PetService.cs](../../../../Map.Server/Pet/PetService.cs)) — `Summon` / `Recall` lifecycle, 60s hunger decay tick with runaway-on-hunger-zero + intimacy decay when starving.

### Pet / Homun / Mercenary / Elemental — shared AI
- **`SummonAiService`** ([Mob/SummonAiService.cs](../../../../Map.Server/Mob/SummonAiService.cs)) collapses the per-type AI loops (pet_ai / hom_ai / merc_ai / elem_ai / mob_ai_sub_hard_slavemob) into one ticker keyed by `Entity.MasterId`. Follow when far + assist master's target + despawn when master leaves map.

### Trade / Shop / Storage
See [trade.md](trade.md) — `TradeService` (atomic 1:1 exchange), `ShopService` (NPC buy/sell with 50% sell ratio), `StorageService` (kafra over P5 `AccountStorageLoad/Save` IPC).

## Pending — per module

### Party (remaining)
- `CZ_PARTY_INVITE2`, `CZ_PARTY_JOIN_REQ_ACK`, `CZ_REQ_LEAVE_GROUP`, `CZ_REG_EXPULSION` packet handlers — char-side IPC (`ICharServerIpcServiceParty`) already wired.
- Mini-map dot broadcast.

### Guild
- `CZ_REQ_GUILD_*` family + emblem upload.
- Castle siege (WoE) — separate phase.

### Mail
- Modern RoDEX mail UI (`CZ_*_RODEX`). IPC ready.

### Quest
- Quest journal UI; tracker hook on `mob_dead` if matching mob_id.

### Achievement
- Progress check on `mob_dead` / `pc_levelup` / `item_obtain`.

### Clan
- Clan chat channel + join/leave hooks (already in `ClanRequest` IPC).

### Pet (remaining)
- Pet egg item interaction (`CZ_USE_ITEM2` for pet egg → `IPetService.Summon`).
- Pet capture from mob (mob_egg drop table).
- Pet feed (`CZ_COMMAND_PET`) with intimacy gain table.
- Pet skill (`pet_attackskill`) per-pet skill use.
- Char-server load/save on enter/leave via existing IPC.

### Homunculus / Mercenary / Elemental
- Spawn / call / dismiss lifecycle wire packets.
- Per-type AI quirks (homun skill tree learning, merc contract timer, elem mode switching).

## History
- **2026-05-16** — Plan stub.
- **2026-05-19** — Party EXP share shipped (last-hit + share + even-share bonus). Pet entity + service for hunger/intimacy. Generic SummonAiService covers pet/homun/merc/elem/slave-mob follow-and-assist. Trade / shop / storage services wired with strategy-pattern shape. Wire packets for the long tail (party invite, guild, mail, quest, etc.) defer to focused per-module slices; the IPC surface they consume is unchanged.
