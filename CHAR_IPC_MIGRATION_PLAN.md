# Char IPC Migration Plan (rAthena parity)

Source of truth: `rathena/src/char/*` (especially `char_logif.cpp`, `char_mapif.cpp`, `inter.cpp`, `int_*.cpp`).
Target: `mmo-csharp` gRPC-based IPC, following the same pattern already used in `Login.Server`.

## Migration Pattern (match Login.Server)
For each IPC method, implement in this order:
1. Add proto RPC/message contract under `Core.Server/Protos/*.proto`.
2. Regenerate/use generated IPC types.
3. Add client wrapper method in calling server service (`*IpcService`).
4. Implement server RPC handler in callee `*GrpcService`.
5. Wire through runtime (`CharServerImpl`/`MapServerImpl`/`LoginServerImpl`) and repository/use-case layer.
6. Add focused test (unit/integration) for request/response and side effects.
7. Land one method per commit.

## Current Parity Snapshot
- Char <-> Login (`char_logif` + loginchrif-equivalent): mostly implemented.
- Char <-> Map (`char_mapif`): mostly missing.
- Inter map->char (`inter.cpp` + `int_*` modules): mostly missing.

## A. Char <-> Login IPC (rAthena `char_logif.cpp`)
Status against packet switch in `char_logif.cpp`:

- [x] `0x2711` `chlogif_parse_ackconnect` (registration/ack flow)
- [x] `0x2713` `chlogif_parse_ackaccreq` (auth result)
- [x] `0x2717` `chlogif_parse_reqaccdata` (full account data)
- [x] `0x2718` keepalive (handled by gRPC channel health)
- [x] `0x2721` `chlogif_parse_AccInfoAck` (account info)
- [x] `0x2723` `chlogif_parse_ackchangesex` (sex change push)
- [x] `0x2726` `chlogif_parse_ack_global_accreg` (global accreg fetch)
- [x] `0x2731` `chlogif_parse_accbannotification` (state/ban broadcast)
- [x] `0x2734` `chlogif_parse_askkick` (force disconnect)
- [x] `0x2735` `chlogif_parse_updip` (address sync request)
- [x] `0x2743` `chlogif_parse_vipack` (VIP push)

Notes:
- Keep as baseline reference for how to structure new Char<->Map and Inter migrations.

## B. Char <-> Map Core IPC (rAthena `char_mapif.cpp`)
### B1. Handshake / topology / health
- [x] `0x2afa` `chmapif_parse_getmapname` (map-server registration, map list exchange)
- [x] `0x2afe` `chmapif_parse_getusercount`
- [x] `0x2aff` `chmapif_parse_regmapuser`
- [x] `0x2b23` `chmapif_parse_keepalive`
- [x] `0x2b13` `chmapif_parse_updmapip`

### B2. Character auth / handoff lifecycle
- [x] `0x2b26` `chmapif_parse_reqauth`
- [x] `0x2b02` `chmapif_parse_authok`
- [x] `0x2b05` `chmapif_parse_reqchangemapserv` (map change server routing) via `CharacterService.RequestMapServerChange`
- [x] `0x2b01` `chmapif_parse_reqsavechar`

### B3. Character/account updates from map
- [x] `0x2afc` `chmapif_parse_askscdata`
- [x] `0x2b15` `chmapif_parse_req_saveskillcooldown`
- [x] `0x2b0a` `chmapif_parse_req_skillcooldown`
- [x] `0x2b1c` `chmapif_parse_save_scdata`
- [x] `0x2b17` `chmapif_parse_setcharoffline`
- [x] `0x2b18` `chmapif_parse_setalloffline`
- [x] `0x2b19` `chmapif_parse_setcharonline`

### B4. Social/account ops forwarded via char
- [x] `0x2b07` `chmapif_parse_askrmfriend`
- [x] `0x2b08` `chmapif_parse_reqcharname`
- [x] `0x2b0c` `chmapif_parse_reqnewemail`
- [x] `0x2b0e` `chmapif_parse_fwlog_changestatus`
- [x] `0x2b11` `chmapif_parse_reqdivorce`
- [x] `0x2b28` `chmapif_parse_reqcharban`
- [x] `0x2b2a` `chmapif_parse_reqcharunban`

### B5. Fame, bonus scripts, misc
- [x] `0x2b10` `chmapif_parse_updfamelist`
- [x] `0x2b1a` `chmapif_parse_reqfamelist`
- [x] `0x2b2d` `chmapif_bonus_script_get`
- [x] `0x2b2e` `chmapif_bonus_script_save`

## C. Inter Base IPC (rAthena `inter.cpp`)
- [x] `0x3000` `mapif_parse_broadcast`
- [x] `0x3001` `mapif_parse_WisRequest`
- [x] `0x3002` `mapif_parse_WisReply`
- [x] `0x3003` `mapif_parse_WisToGM`
- [x] `0x3004` `mapif_parse_Registry`
- [x] `0x3005` `mapif_parse_RegistryRequest`
- [x] `0x3006` `mapif_parse_NameChangeRequest`
- [x] `0x3007` `mapif_parse_accinfo`
- [x] `0x3009` `mapif_parse_broadcast_item`

## D. Inter Module IPC (rAthena `int_*.cpp`)
### D1. Party (`int_party.cpp`)
- [x] `0x3020` CreateParty
- [x] `0x3021` PartyInfo
- [x] `0x3022` PartyAddMember
- [x] `0x3023` PartyChangeOption
- [x] `0x3024` PartyLeave
- [x] `0x3025` PartyChangeMap
- [x] `0x3026` BreakParty
- [x] `0x3027` PartyMessage
- [x] `0x3029` PartyLeaderChange
- [x] `0x302A` PartyShareLevel

### D2. Guild (`int_guild.cpp`)
- [x] `0x3030` CreateGuild
- [x] `0x3031` GuildInfo
- [x] `0x3032` GuildAddMember
- [x] `0x3033` GuildMasterChange
- [x] `0x3034` GuildLeave
- [x] `0x3035` GuildChangeMemberInfoShort
- [x] `0x3036` BreakGuild
- [x] `0x3037` GuildMessage
- [x] `0x3039` GuildBasicInfoChange
- [x] `0x303A` GuildMemberInfoChange
- [x] `0x303B` GuildPosition
- [x] `0x303C` GuildSkillUp
- [x] `0x303D` GuildAlliance
- [x] `0x303E` GuildNotice
- [x] `0x303F` GuildEmblem
- [x] `0x3040` GuildCastleDataLoad
- [x] `0x3041` GuildCastleDataSave
- [x] `0x3042` GuildEmblemVersion

### D3. Storage (`int_storage.cpp`)
- [x] `0x3018` LoadGuildStorage
- [x] `0x3019` SaveGuildStorage
- [x] `0x3056` ItemboundRetrieve
- [x] `0x308a` StorageLoad
- [x] `0x308b` StorageSave

### D4. Mail (`int_mail.cpp`)
- [x] `0x3048` MailRequestInbox
- [x] `0x3049` MailRead
- [x] `0x304a` MailGetAttach
- [x] `0x304b` MailDelete
- [x] `0x304c` MailReturn
- [x] `0x304d` MailSend
- [x] `0x304e` MailReceiverCheck

### D5. Auction (`int_auction.cpp`)
- [x] `0x3050` AuctionRequestList
- [x] `0x3051` AuctionRegister
- [x] `0x3052` AuctionCancel
- [x] `0x3053` AuctionClose
- [x] `0x3055` AuctionBid

### D6. Quest/Achievement
- [x] `0x3060` QuestLoad
- [x] `0x3061` QuestSave
- [x] `0x3062` AchievementLoad
- [x] `0x3063` AchievementSave
- [x] `0x3064` AchievementReward

### D7. Pet/Homunculus/Mercenary/Elemental/Clan
- [x] `0x3080` CreatePet
- [x] `0x3081` LoadPet
- [x] `0x3082` SavePet
- [x] `0x3083` DeletePet
- [x] `0x3090` HomunculusCreate
- [x] `0x3091` HomunculusLoad
- [x] `0x3092` HomunculusSave
- [x] `0x3093` HomunculusDelete
- [x] `0x3094` HomunculusRename
- [x] `0x3070` MercenaryCreate
- [x] `0x3071` MercenaryLoad
- [x] `0x3072` MercenaryDelete
- [x] `0x3073` MercenarySave
- [x] `0x307c` ElementalCreate
- [x] `0x307d` ElementalLoad
- [x] `0x307e` ElementalDelete
- [x] `0x307f` ElementalSave
- [x] `0x30A0` ClanRequest
- [x] `0x30A1` ClanMessage
- [x] `0x30A2` ClanMemberLeft
- [x] `0x30A3` ClanMemberJoined

## Ordered Execution Plan (one-by-one)

### Phase 1: Complete Char <-> Map core auth/handoff first
1. `0x2b05` ReqChangeMapServ (server handoff) via Char RPC + Map callback. [DONE]
2. `0x2b26` ReqAuth parity hardening (align request/response semantics with rAthena). [DONE]
3. `0x2b01` ReqSaveChar. [DONE]
4. `0x2b02` AuthOk callback behavior. [DONE]
5. `0x2b23` Keepalive + disconnect handling parity. [DONE]

### Phase 2: Character/account state correctness
6. `0x2b15` SaveSkillCooldown. [DONE]
7. `0x2b0a` LoadSkillCooldown. [DONE]
8. `0x2b1c` Save SC data. [DONE]
9. `0x2b17` SetCharOffline. [DONE]
10. `0x2b19` SetCharOnline. [DONE]
11. `0x2b18` SetAllOffline. [DONE]

### Phase 3: Inter base transport
12. `0x3001` Wisp request/reply chain (`0x3001/0x3002/0x3003`). [DONE]
13. `0x3000` and `0x3009` broadcasts. [DONE]
14. `0x3004/0x3005` registry read/write. [DONE]
15. `0x3006` name change request. [DONE]
16. `0x3007` account info request. [DONE]

### Phase 4: High-value gameplay modules
17. Party `0x3020..0x302A`. [DONE]
18. Guild `0x3030..0x3042`. [DONE]
19. Storage `0x3018/0x3019/0x3056/0x308a/0x308b`. [DONE]
20. Mail `0x3048..0x304e`. [DONE]
21. Auction `0x3050/0x3051/0x3052/0x3053/0x3055`. [DONE]

### Phase 5: Remaining modules
22. Quest/Achievement `0x3060..0x3064`. [DONE]
23. Pet/Homunculus/Mercenary/Elemental `0x3070..0x3094`. [DONE]
24. Clan `0x30A0..0x30A3`. [DONE]
25. Fame/divorce/ban/unban/bonus-script endpoints from `char_mapif.cpp`. [DONE]

## Immediate Next Migration Unit
Recommended next unit: wire map-side gameplay consumers to the new IPC methods and run integration scenarios for auth/social/guild/storage/mail/auction/quest/pet/clan flows.
Reason: migration scope is contract-complete; remaining work is runtime integration and behavior validation.
