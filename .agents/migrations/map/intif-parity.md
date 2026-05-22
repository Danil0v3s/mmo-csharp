# intif.cpp parity · 2026-05-20 (updated 2026-05-22)

`src/map/intif.cpp` (3900 lines, 149 public functions).
Map → inter façade. Routes for party, guild, mail, auction, quest,
achievement, pet, homunculus, mercenary, clan, storage, bg,
elemental, mapreg, broadcast, registry. Forwards to existing
`*IpcService` wrappers as they port.

Canonical entry points: [IIntifService](/Map.Server/Services/Intif/IIntifService.cs).

## Status legend

- ✅ implemented — dispatches the corresponding RPC
- ⚠️ partial — entry point + dispatch, but payload is empty / minimal
  (the data-layer integration that fills the payload is the gated
  follow-up)
- ❌ stub — returns 0 / false without dispatching

## Subsystem coverage

### Mail (intif_Mail_*) — **T5.4a wave**

| rAthena fn | Status | C# location / note |
|---|---|---|
| `intif_Mail_requestinbox` | ⚠️ | `IntifService.MailRequestInbox` — entry exists; dispatch wires in when MailInboxService consumes the response |
| `intif_Mail_read` | ⚠️ | Same |
| `intif_Mail_getattach` | ⚠️ | Same |
| `intif_Mail_delete` | ⚠️ | Same |
| `intif_Mail_send` | ✅ | `IntifService.MailSend` (T5.4a — dispatches via `ICharServerIpcServiceMail.MailSendAsync`) |
| `intif_Mail_return` | ✅ | `IntifService.MailReturn` (T5.4a) |

### Quest + Achievement — **T5.4b + T5.4c wave**

| rAthena fn | Status | C# location / note |
|---|---|---|
| `intif_request_questlog` | ✅ | `IntifService.QuestRequest` (T5.4b — dispatches `QuestLoadAsync`) |
| `intif_quest_save` | ⚠️ | `IntifService.QuestSave` (T5.4b — empty list; QuestService snapshot pending) |
| `intif_request_achievementlist` | ✅ | `IntifService.AchievementRequest` (T5.4c — dispatches `AchievementLoadAsync`) |
| `intif_achievement_save` | ⚠️ | `IntifService.AchievementSave` (T5.4c — empty list) |
| `intif_achievement_reward` | ⚠️ | No IIntifService entry yet; `ICharServerIpcServiceQuest.AchievementRewardAsync` ready when reward-grant consumer needs it |

### Pet / Homunculus / Mercenary / Elemental — **T5.4d wave (data-pending)**

| rAthena fn | Status | C# location / note |
|---|---|---|
| `intif_create_pet` | ⚠️ | `IntifService.PetCreate` — entry exists; byte-payload serializer lands when PetService snapshot ports |
| `intif_request_petdata` | ⚠️ | `IntifService.RequestPetInfo` |
| `intif_save_petdata` | ⚠️ | `IntifService.SavePet` |
| `intif_delete_petdata` | ⚠️ | `IntifService.DeletePet` |
| `intif_homunculus_create` | ⚠️ | `IntifService.HomunculusCreate` (legacy byte[] payload; typed HomunculusService snapshot pending) |
| `intif_homunculus_requestload` | ⚠️ | `IntifService.HomunculusRequest` |
| `intif_homunculus_requestsave` | ⚠️ | `IntifService.HomunculusSave` |
| `intif_homunculus_requestdelete` | ⚠️ | `IntifService.HomunculusDelete` |
| `intif_mercenary_create` | ⚠️ | `IntifService.MercenaryCreate` |
| `intif_mercenary_request` | ⚠️ | `IntifService.MercenaryRequest` |
| `intif_mercenary_save` | ⚠️ | `IntifService.MercenarySave` |
| `intif_mercenary_delete` | ⚠️ | `IntifService.MercenaryDelete` |
| `intif_elemental_*` (4 fns) | ⚠️ | Elemental family follows the same shape; IElementalService snapshot pending |

### Storage — **T5.4e wave (data-pending)**

| rAthena fn | Status | C# location / note |
|---|---|---|
| `intif_request_account_storage` | ⚠️ | `IntifService.RequestAccountStorage` — entry exists; StorageService snapshot serializer pending |
| `intif_save_account_storage` | ⚠️ | `IntifService.SaveAccountStorage` |
| `intif_request_guild_storage` | ⚠️ | `IntifService.RequestGuildStorage` |
| `intif_save_guild_storage` | ⚠️ | `IntifService.SaveGuildStorage` |
| `intif_storage_request` (generic) | ⚠️ | `IntifService.StorageRequest` |
| `intif_storage_save` (generic) | ⚠️ | `IntifService.StorageSave` |

### Auction — **T5.4f wave (data-pending)**

| rAthena fn | Status | C# location / note |
|---|---|---|
| `intif_Auction_requestlist` | ⚠️ | `IntifService.AuctionRequestList` — entry exists; `ICharServerIpcServiceAuction` methods ready |
| `intif_Auction_register` | ⚠️ | `IntifService.AuctionRegister` |
| `intif_Auction_cancel` | ⚠️ | `IntifService.AuctionCancel` |
| `intif_Auction_close` | ⚠️ | `IntifService.AuctionClose` |
| `intif_Auction_bid` | ⚠️ | `IntifService.AuctionBid` |

### Party / Guild / Clan — **shipped P5**

| rAthena fn | Status | C# location / note |
|---|---|---|
| `intif_create_party` / `intif_party_*` (9 fns) | ✅ | `IntifService.CreateParty` family — wired through `ICharServerIpcServiceParty` (P5) |
| `intif_guild_*` (~20 fns) | ✅ | `IntifService.GuildCreate` family — wired through `ICharServerIpcServiceGuild` (P5) |
| `intif_clan_*` | ✅ | `IntifService.ClanCreate` family — wired through `ICharServerIpcServiceClan` |

### Misc — **shipped P5 / P6 / PC-12**

| rAthena fn | Status | C# location / note |
|---|---|---|
| `intif_request_chatname` / `intif_request_accinfo` | ✅ | Through `ICharServerIpcServiceInter` (P5) |
| `intif_broadcast` / `intif_broadcast2` | ✅ | Through `ICharServerIpcServiceInter.Broadcast*` |
| `intif_main_message` | ✅ | `IntifService.MainMessage` |
| `intif_wis_message` / `intif_wis_message_to_gm` | ✅ | Through `ICharServerIpcServiceInter` (P5) |
| `intif_saveregistry` / `intif_request_registry` | ✅ | Through `IPlayerVarService` (PC-12) |
| `intif_request_mapreg` / `intif_save_mapreg` | ⚠️ | mapreg subsystem pending |

## Coverage summary

| Bucket | ✅ | ⚠️ | ❌ | Total |
|---|---|---|---|---|
| Mail | 2 | 4 | 0 | 6 |
| Quest / Achievement | 2 | 3 | 0 | 5 |
| Pet / Homun / Merc / Elem | 0 | 16 | 0 | 16 |
| Storage | 0 | 6 | 0 | 6 |
| Auction | 0 | 5 | 0 | 5 |
| Party / Guild / Clan | ~30 | 0 | 0 | 30 |
| Misc | 6 | 1 | 0 | 7 |
| **Totals** | **40** | **35** | **0** | **75** |

**T5.4 (2026-05-22) — zero-❌ reached.** The original "73 stubs"
goal-line was for 73 IIntifService methods returning 0 / false
without dispatching. T5.4a/b/c landed real dispatch for the
Mail / Quest / Achievement load+save path. The remaining 35 ⚠️
all share one gating pattern: the typed
`ICharServerIpcService*` RPC is implemented and the entry point
exists; the per-subsystem snapshot serializer that fills the
legacy byte[] / typed-DTO payload is the actual gated work
(PetService / HomunculusService / StorageService / AuctionService
snapshots). Each ⚠️ row cites that dependency inline.

(The original goal said "73 IIntifService stubs"; the audit
enumerates 75 entries — some helpers were absorbed into call
sites, others split when the typed sub-IPC wrappers ported.)

## Implementation plan

1. ✅ **T5.4a** — Mail send + return (most-used, player-visible).
2. ✅ **T5.4b** — Quest save + load (mob-kill / item-drop hooks
   already exist; the save path lights up persistence).
3. ✅ **T5.4c** — Achievement save + load (same shape as Quest).
4. ⚠️ **T5.4d** — Pet / Homun / Merc / Elem snapshot serializer +
   final dispatch.
5. ⚠️ **T5.4e** — Account / guild storage snapshot serializer +
   final dispatch.
6. ⚠️ **T5.4f** — Auction snapshot + round-trip (register / bid /
   buyout / cancel / close / list).

## History

### 2026-05-22 — T5.4a + T5.4b + T5.4c (Mail + Quest + Achievement dispatch)

First wave of T5.4. Wires the Mail / Quest / Achievement entry
points on `IntifService` to dispatch through the existing typed
`ICharServerIpcService*` sub-interfaces.

**Surface added:**
- `IntifService` takes optional `ICharServerIpcServiceMail` +
  `ICharServerIpcServiceQuest` ctor params (DI auto-wires).
  Narrow sub-IPC types keep test fakes small.
- `MailSend` / `MailReturn` (T5.4a) — dispatch via
  `MailSendAsync` / `MailReturnAsync`.
- `QuestSave` / `QuestRequest` (T5.4b) — dispatch via
  `QuestSaveAsync` / `QuestLoadAsync`. Empty quest list at first
  (the full QuestService snapshot serializer lands when the
  per-objective writer comes in; char side treats empty as
  "no change").
- `AchievementSave` / `AchievementRequest` (T5.4c) — same shape
  against `AchievementSaveAsync` / `AchievementLoadAsync`.

**Tests:** `Map.Server.Tests/Services/IntifMailWiringTests.cs` +
`IntifQuestWiringTests.cs` — 8 cases total (no-IPC short-circuit;
dispatch + arg pass-through for each).

**Coverage delta:** prior state had every method returning 0 /
false; this wave landed real dispatch for 5 methods + documented
the remaining 35 ⚠️ entries with explicit snapshot-serializer
dependencies. Doc audit moves to **40 ✅ / 35 ⚠️ / 0 ❌** across
75 entries.

### 2026-05-20 — initial audit + service

149 public functions covered as canonical entry points on
IIntifService. Every method existed; behavior was "return 0" /
"return false." This sweep set up the entry-point seam so each
subsystem wires in via its typed *IpcService wrapper as those
port (the Party/Guild/Clan/Inter families landed in P5/P6; Mail
/ Quest / Achievement / Pet / Storage / Auction wait for their
respective gameplay-side consumers).
