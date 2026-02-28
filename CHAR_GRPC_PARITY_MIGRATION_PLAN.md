# Char gRPC Parity Migration Plan (rAthena 1:1)

Source of truth:
- `rathena/src/char/char_mapif.cpp` (map<->char inter flows)
- `rathena/src/char/char_logif.cpp` (char<->login inter flows)
- `rathena/src/char/char_clif.cpp` (client-facing character auth/select flows)
- `rathena/src/char/int_*.cpp` (party/guild/storage/mail/auction/quest/achievement/pet/homun/mercenary/elemental/clan persistence)

Target:
- `mmo-csharp/Char.Server/CharGrpcService.cs` and related services/repositories
- Ensure each gRPC endpoint behaves as the rAthena equivalent would (same validation gates, state transitions, persistence semantics, and failure behavior), not merely returning success.

## Scope

This plan covers parity for char-server RPC surfaces (map/login/internal service calls) that are currently TODO, mocked, or in-memory stubs.

## Progress updates

- 2026-02-26:
  - `RequestFameList` now applies rAthena fame-type class-family filters (Blacksmith/Alchemist/Taekwon) using the same job groups from `char_read_fame_list`.
  - Party RPCs moved from in-memory dictionaries to DB-backed `party`/`char.party_id` flows for create/info/add/change/leave/map/break/leader updates.
  - Guild core lifecycle and state RPCs moved to DB-backed flows (`guild`, `guild_member`, `guild_position`, `guild_skill`, `guild_alliance`, `char.guild_id`) for create/info/member/master/leave/break/message/notice/emblem/basic/member/position/skill/alliance paths.
  - Guild castle data RPCs (`GuildCastleDataLoad/Save`) now map rAthena `CD_*` indexes directly to persisted `guild_castle` columns with DB-backed load/save semantics.
  - Quest and Achievement RPCs now use DB-backed persistence (`quest`, `achievement`) for load/save/reward flows instead of in-memory dictionaries.
  - Clan RPCs now use DB-backed `clan`/`clan_alliance` reads with online-member count derived from `char` online state (and transient connect-member cache updates for join/left notifications).
  - Pet/Homunculus/Mercenary/Elemental RPCs now use DB-backed persistence (`pet`, `homunculus`, `mercenary`, `elemental`) for create/load/save/delete flows; in-memory state dictionaries were removed.
  - Mail and Auction RPCs now use DB-backed persistence (`mail`, `mail_attachments`, `auction`) for inbox/read/get/delete/return/send/check and list/register/cancel/close/bid flows.
  - Removed migration-era in-memory caches for fame/bonus-script/scdata/skill-cooldown paths; these flows are now DB-sourced only.
  - Clan connect-member parity is now persisted via `clan.connect_member` with join/left acknowledgements writing DB-backed counts.
  - Guild/account storage payload RPCs now persist opaque bytes in DB (`guild_storage_payload`, `account_storage_payload`) instead of in-memory dictionaries.
- 2026-02-28:
  - Added `CharGrpcService` parity regression tests for map-auth ticket consume/replay rejection and online-state transitions (`SetCharacterOnline`, `SetAllCharactersOffline`) in `Char.Server.Tests`.
  - Added `CharGrpcService` parity regression tests for `SaveCharacterState` offline-save ack semantics and `DeleteCharacter` restriction gates (party/guild/base-level) plus soft-delete success path.
  - `SetCharacterOffline` parity behavior tightened to treat `character_id <= 0` as account-wide offline update and return success for valid requests.
  - Map-server registry/address/usercount state moved to singleton `IMapServerRegistryService` so routing metadata is shared across gRPC calls (not per-request instance state).
  - Added offline parity tests for account-wide (`character_id = 0`) and specific-character offline requests.
  - Added packet-level handler flow tests for `CH_SELECT_CHAR` map handoff path: success sends `HC_SEND_MAP_DATA` and issues map-auth ticket; reject paths send `SC_NOTIFY_BAN` / `HC_REFUSE_ENTER` as expected.
  - Expanded RPC branch-matrix tests for core char gRPC endpoints (`GetCharacterList`, `GetCharacterData`, `RequestCharacterName`, `CreateCharacter`, `RequestCharacterMapAuth`, `RequestMapServerChange`, `SetCharacterOnline`, map-server usercount registry flows) with both success and reject branches.
  - Completed core RPC branch-matrix coverage for migrated char flows by adding invalid/not-found/restriction branches across `RequestCharacterMapAuth`, `SaveCharacterState`, `DeleteCharacter`, online/offline state RPCs, and map-server registry validation RPCs.

## 1. Current gap audit

## 1.1 Explicit TODO endpoints (high priority)

- `GetCharacterList`:
  - Current: returns hardcoded sample characters.
  - Needed parity: load account-owned chars from DB, sorted by slot, honoring delete/ban flags.
- `CreateCharacter`:
  - Current: random id + synthetic object, no DB write.
  - Needed parity: full creation path with naming/rule validation and DB insert.
- `DeleteCharacter`:
  - Current: unconditional success, no DB change.
  - Needed parity: rAthena delete restrictions and DB update/delete semantics.
- `GetCharacterData`:
  - Current: `BuildCharacterDataResponse` stub data.
  - Needed parity: actual persisted char/map position/status payload.

## 1.2 Additional non-TODO parity gaps (also high priority)

- `RequestCharacterMapAuth`:
  - Uses ticket checks, but character payload is still stubbed.
- `SaveCharacterState`:
  - Returns success without persisting data.
- `RequestCharacterName`:
  - Resolves through stubbed `BuildCharacterDataResponse`.
- `SetCharacterOnline` / `SetAllCharactersOffline`:
  - Placeholder responses; does not fully mirror char online state updates.

## 1.3 Broad subsystem parity risk (medium priority, staged)

Many RPCs currently backed by `ConcurrentDictionary` in-memory state only (party/guild/storage/mail/auction/quest/achievement/pet/homunculus/mercenary/elemental/clan/fame/bonus-script/scdata/cooldowns). rAthena persists these via `int_*` and inter-server SQL flows.

## 2. rAthena mapping (key RPC groups)

- Character list/select/auth/save/map handoff:
  - `char_clif.cpp` + `char_mapif.cpp` (`chmapif_parse_authok`, `chmapif_parse_reqsavechar`, `chmapif_parse_reqauth`, `chmapif_parse_reqchangemapserv`)
- Online/offline/account status propagation:
  - `char_mapif.cpp` (`chmapif_parse_setcharoffline`, `chmapif_parse_setcharonline`, `chmapif_parse_setalloffline`)
  - `char_logif.cpp` (`chlogif_send_setacconline`, `chlogif_send_setaccoffline`, sync paths)
- Social/inter systems:
  - `char_mapif.cpp` + `int_party.cpp`, `int_guild.cpp`, `int_storage.cpp`, `int_mail.cpp`, `int_auction.cpp`, `int_quest.cpp`, `int_achievement.cpp`, `int_pet.cpp`, `int_homun.cpp`, `int_mercenary.cpp`, `int_elemental.cpp`, `int_clan.cpp`

## 3. Ordered migration plan (commit-sized units)

## Phase A: Close core TODOs and map-auth/save parity first

1. [x] Replace `GetCharacterList` hardcoded payload with repository-backed query.
2. [x] Replace `GetCharacterData` stub with repository-backed load + map/save position mapping.
3. [x] Replace `RequestCharacterMapAuth` stub payload with repository-backed character data.
4. [x] Implement `SaveCharacterState` DB persistence (position/map/status + offline transition parity).
5. [x] Implement `CreateCharacter` DB path with validation parity (name/rules/slots).
6. [x] Implement `DeleteCharacter` DB path with restriction parity (party/guild/baselevel/delay).
7. [x] Implement `RequestCharacterName` lookup via repository (no stub fallback).

## Phase B: Online state and map-server registry parity

8. [x] Make `SetCharacterOnline` update persisted online state and login synchronization behavior.
9. [x] Make `SetCharacterOffline` and `SetAllCharactersOffline` fully align with rAthena offline propagation.
10. [x] Align map-server registry/address/usercount flows with actual map-server identity and routing source-of-truth.

## Phase C: Replace in-memory subsystem state with DB-backed inter parity

11. [x] Party RPCs -> DB-backed parity (`int_party` semantics).
12. [x] Guild RPCs + castle + guild storage parity (`int_guild`, `int_storage`).
13. [x] Mail + auction parity (`int_mail`, `int_auction`).
14. [x] Quest + achievement parity (`int_quest`, `int_achievement`).
15. [x] Pet/homunculus/mercenary/elemental parity (`int_pet`, `int_homun`, `int_mercenary`, `int_elemental`).
16. [x] Clan/fame/bonus script/scdata/skill-cooldown persistence parity.

## Phase D: Hardening and parity verification

17. [x] Add RPC-level integration tests for success + reject/error branches per migrated endpoint.
18. [x] Add replay/ordering tests for map-auth ticket consume paths.
19. [x] Add DB state transition tests (online flags, save semantics, delete semantics).
20. [x] Add packet-level parity checks for char select/map handoff paths that depend on these RPCs.

## 4. Immediate next unit (recommended)

Implement together in one focused slice:
- `GetCharacterList`
- `GetCharacterData`
- `RequestCharacterMapAuth` (payload load side)
- `RequestCharacterName`

Reason:
- They share the same repository-read foundation.
- They remove all current stubbed character payload responses.
- They unblock realistic end-to-end map handoff validation with existing ticket flow.
