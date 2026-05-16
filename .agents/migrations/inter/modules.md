# Inter-module IPC (`int_*.cpp`)

Per-feature inter-server modules: party, guild, storage, mail, auction, quest, achievement, pet, homunculus, mercenary, elemental, clan. The Char server holds authoritative DB state; map servers call these RPCs to read/write.

**rAthena source:** [rathena/src/char/int_*.cpp](/Volumes/1TB/Projetos/rathena/src/char/)
**C# implementation:** [Char.Server/CharGrpcService.cs](../../../Char.Server/CharGrpcService.cs) lines ~1193-3800
**Proto:** [Core.Server/Protos/char_service.proto](../../../Core.Server/Protos/char_service.proto)
**Entities:** [Core.Database/Entities/](../../../Core.Database/Entities/)

> **Status pattern:** Char-side server implementations are mostly DB-backed and complete (✅). The big systemic gap is map-side callers — see [../map/ipc-integration.md](../map/ipc-integration.md). Bugs called out below are real divergences in the char-side impl itself.

## Party (`int_party.cpp`) ✅ char side

10 RPCs, all DB-backed against `party` table and `char.party_id` updates. [CharGrpcService.cs:1193-1482](../../../Char.Server/CharGrpcService.cs).

| RPC | rAthena packet | Notes |
|---|---|---|
| `CreateParty` | 0x3020 | DB insert + leader char update |
| `PartyInfo` | 0x3021 | DB read |
| `PartyAddMember` | 0x3022 | DB update + char.party_id |
| `PartyChangeOption` | 0x3023 | DB update |
| `PartyLeave` | 0x3024 | DB update |
| `PartyChangeMap` | 0x3025 | DB update |
| `BreakParty` | 0x3026 | DB delete + clear char.party_id |
| `PartyMessage` | 0x3027 | Forward (route to map members) |
| `PartyLeaderChange` | 0x3029 | DB update |
| `PartyShareLevel` | 0x302A | Persists to `CharServerState.PartyShareLevel` (process-global, matches rAthena `inter.cpp:party_share_level`) |

## Guild (`int_guild.cpp`) ✅ char side

19 RPCs across `guild`, `guild_member`, `guild_position`, `guild_skill`, `guild_alliance`, `guild_castle`, plus `char.guild_id`. [CharGrpcService.cs:1482-2090](../../../Char.Server/CharGrpcService.cs).

Lifecycle (`CreateGuild`, `GuildInfo`, `GuildAddMember`, `GuildLeave`, `BreakGuild`, `GuildMasterChange`), state (`GuildChangeMemberInfoShort`, `GuildBasicInfoChange`, `GuildMemberInfoChange`, `GuildPosition`, `GuildSkillUp`, `GuildAlliance`, `GuildNotice`, `GuildEmblem`, `GuildEmblemVersion`), castle (`GuildCastleDataLoad`/`Save` map rAthena `CD_*` indexes to `guild_castle` columns), message (`GuildMessage`).

## Storage (`int_storage.cpp`) ✅ char side

5 RPCs at [CharGrpcService.cs:2095-2182](../../../Char.Server/CharGrpcService.cs).

| RPC | rAthena packet | Notes |
|---|---|---|
| `StorageLoad` | 0x308a | Account storage; opaque-payload table `account_storage_payload` |
| `StorageSave` | 0x308b | Same |
| `LoadGuildStorage` | 0x3018 | Opaque-payload table `guild_storage_payload` |
| `SaveGuildStorage` | 0x3019 | Same |
| `ItemboundRetrieve` | 0x3056 | Bound-item retrieval |

The opaque-payload model is a deliberate change from rAthena's row-per-item schema; the doc note in old plan called this "guild/account storage payload RPCs persist opaque bytes."

## Mail (`int_mail.cpp`) ✅ char side

7 RPCs at [CharGrpcService.cs:2187-2352](../../../Char.Server/CharGrpcService.cs) (also referenced as 2409-2490).

| RPC | rAthena packet | Notes |
|---|---|---|
| `MailRequestInbox` | 0x3048 | DB read + attachments per mail |
| `MailRead` | 0x3049 | DB update (mark read) + attachments |
| `MailGetAttach` | 0x304a | DB read + delete attachment rows + clear zeny |
| `MailDelete` | 0x304b | DB delete (cascades to attachments) |
| `MailReturn` | 0x304c | DB update + attachments cloned to returned mail |
| `MailSend` | 0x304d | DB insert + persist attachments to `mail_attachments` |
| `MailReceiverCheck` | 0x304e | DB lookup |

Attachments use the typed `repeated MailAttachmentItem items` proto field; the legacy `bytes attachment` field is preserved for back-compat but unused.

## Auction (`int_auction.cpp`) ✅ char side

5 RPCs at [CharGrpcService.cs:2357-2480](../../../Char.Server/CharGrpcService.cs) (also referenced as 2618-2672).

| RPC | rAthena packet | Notes |
|---|---|---|
| `AuctionRequestList` | 0x3050 | DB read |
| `AuctionRegister` | 0x3051 | DB insert |
| `AuctionCancel` | 0x3052 | DB delete |
| `AuctionClose` | 0x3053 | DB update + winner mail |
| `AuctionBid` | 0x3055 | DB update + auto-refund mail to outbid prior bidder |

## Quest / Achievement (`int_quest.cpp`, `int_achievement.cpp`) ✅ char side

5 RPCs at [CharGrpcService.cs:2485-2581](../../../Char.Server/CharGrpcService.cs).

| RPC | rAthena packet | Notes |
|---|---|---|
| `QuestLoad` | 0x3060 | DB read |
| `QuestSave` | 0x3061 | Delete-all + reinsert (rAthena parity) |
| `AchievementLoad` | 0x3062 | DB read |
| `AchievementSave` | 0x3063 | DB upsert |
| `AchievementReward` | 0x3064 | DB write |

## Pet (`int_pet.cpp`) ✅ char side

4 RPCs at [CharGrpcService.cs:2797-2832](../../../Char.Server/CharGrpcService.cs).

`CreatePet 0x3080`, `LoadPet 0x3081`, `SavePet 0x3082`, `DeletePet 0x3083`. All DB-backed against `pet` table.

## Homunculus (`int_homun.cpp`) ✅ char side

5 RPCs at [CharGrpcService.cs:2958-2976](../../../Char.Server/CharGrpcService.cs) (and `ToHomunculusData` mapper).

`HomunculusCreate 0x3090`, `HomunculusLoad 0x3091`, `HomunculusSave 0x3092`, `HomunculusDelete 0x3093`, `HomunculusRename 0x3094`.

Skills round-trip via `skill_homunculus` using rAthena's DELETE-all + INSERT-non-zero pattern (`SaveHomunculusSkillsAsync`). The proto exposes `repeated HomunculusSkillEntry skills` on `HomunculusData`.

## Mercenary (`int_mercenary.cpp`) ✅ char side

4 RPCs at [CharGrpcService.cs:3061-3079](../../../Char.Server/CharGrpcService.cs).

`MercenaryCreate 0x3070`, `MercenaryLoad 0x3071`, `MercenaryDelete 0x3072`, `MercenarySave 0x3073`. DB-backed against `mercenary` table.

## Elemental (`int_elemental.cpp`) ✅ char side

4 RPCs. `ElementalCreate 0x307c`, `ElementalLoad 0x307d`, `ElementalDelete 0x307e`, `ElementalSave 0x307f`. DB-backed against `elemental` table.

## Clan (`int_clan.cpp`) ✅ char side

4 RPCs at [CharGrpcService.cs:3241-3283](../../../Char.Server/CharGrpcService.cs).

| RPC | rAthena packet | Notes |
|---|---|---|
| `ClanRequest` | 0x30A0 | DB read; `clan.connect_member` is now persisted (deliberate divergence from rAthena's dynamic count, see note below) |
| `ClanMessage` | 0x30A1 | Forward |
| `ClanMemberLeft` | 0x30A2 | DB update + `connect_member` decrement |
| `ClanMemberJoined` | 0x30A3 | DB update + `connect_member` increment |

**Deliberate divergence:** rAthena counts clan online members dynamically by walking `char` online flags. C# persists `clan.connect_member` directly and updates it on join/leave events. This is faster but means the count can drift if a char goes offline ungracefully without a `ClanMemberLeft` event. Acceptable per migration plan.

## Summary of remaining char-side bugs

None. All HIGH/MEDIUM bugs closed in P1; LOW `PartyShareLevel` closed in P2.

## Files / structure

- Server impl: [CharGrpcService.cs](../../../Char.Server/CharGrpcService.cs)
- Proto: [char_service.proto](../../../Core.Server/Protos/char_service.proto)
- Entities: [Core.Database/Entities/](../../../Core.Database/Entities/)

## History

- **2026-05-16** — **P8 cascade & persistence fixes** (pre-gameplay audit closed 4 gaps):
  - **PartyLeave**: when the leader leaves, party now fully disbands (clears all members' `party_id` + removes the party row) matching rAthena's `mapif_parse_PartyLeave` → `mapif_parse_BreakParty` chain in `int_party.cpp:633`. Non-leader leaves still just remove that member.
  - **GuildBreak**: now cascades cleanup to `guild_member`, `guild_position`, `guild_skill`, `guild_alliance` (both sides), `guild_expulsion`, and `guild_storage_payload`. `guild_castle` rows are kept but `guild_id` is cleared (rAthena parity — castles re-capturable, not deleted).
  - **MercenarySave**: now persists skill cooldowns via new `MercenarySkillCooldown` proto message + repeated `cooldowns` field on `MercenaryData`. Uses rAthena `mapif_mercenary_save` DELETE-all + INSERT-non-zero pattern.
  - **MercenaryDelete**: now cascades to `skill_cooldown_mercenary` AND `mercenary_owner` AND `mercenary` (matches rAthena `mercenary_owner_delete` cascade).
  - 8 new cascade regression tests in [CharGrpcModuleCascadeTests.cs](../../../Char.Server.Tests/Services/CharGrpcModuleCascadeTests.cs). Suite 148/148.
- **2026-05-16** — **P2 closed for Party.** `PartyShareLevel` now persists via `CharServerState.PartyShareLevel` (rAthena `inter.cpp` global parity).
- **2026-05-16** — **P1 closed for Mail / Auction / Homunculus.**
  - Mail attachments persisted via typed `MailAttachmentItem` proto rows on send, returned + cleared on `MailGetAttachment`, included in inbox/read responses.
  - Auction bid refunds the outbid prior buyer via auto-generated mail (rAthena `mail_sendmail` parity).
  - Homunculus skills round-trip via `skill_homunculus` table on load/save/create/delete (rAthena DELETE-all + INSERT-non-zero pattern).
  - 9 regression tests added in `CharGrpcDataIntegrityTests.cs` using EF Core InMemory provider.
- **2026-05-15** — Audit confirmed char-side DB-backing for all modules. Found 4 specific divergences (mail attach, auction refund, homun skills, party share-level stub). Map-side callers absent across the board — split into [../map/ipc-integration.md](../map/ipc-integration.md).
- **2026-02-26** — Phase C migration: all module RPCs moved from in-memory `ConcurrentDictionary` stubs to DB-backed flows. `clan.connect_member` persistence pattern decided.
