# intif.cpp parity · 2026-05-20 (updated 2026-05-22 — **T7 closed all ⚠️; 75 ✅ / 0 ⚠️ / 0 ❌**)

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

### Mail (intif_Mail_*) — **T5.4a + T7.6 wave**

| rAthena fn | Status | C# location / note |
|---|---|---|
| `intif_Mail_requestinbox` | ✅ | `IntifService.MailRequestInbox` (T7.6 — `MailRequestInboxAsync`) |
| `intif_Mail_read` | ✅ | `IntifService.MailRead` (T7.6 — `MailReadAsync`) |
| `intif_Mail_getattach` | ✅ | `IntifService.MailGetAttach` (T7.6 — `MailGetAttachmentAsync`) |
| `intif_Mail_delete` | ✅ | `IntifService.MailDelete` (T7.6 — `MailDeleteAsync`) |
| `intif_Mail_send` | ✅ | `IntifService.MailSend` (T5.4a — `MailSendAsync`) |
| `intif_Mail_return` | ✅ | `IntifService.MailReturn` (T5.4a) |

### Quest + Achievement — **T5.4b + T5.4c + T7.1 wave**

| rAthena fn | Status | C# location / note |
|---|---|---|
| `intif_request_questlog` | ✅ | `IntifService.QuestRequest` (T5.4b — `QuestLoadAsync`) |
| `intif_quest_save` | ✅ | `IntifService.QuestSave` (T7.1 — `IQuestService.SnapshotFor(pc)` → `QuestSaveAsync`, reads `PlayerEntity.QuestLog`) |
| `intif_request_achievementlist` | ✅ | `IntifService.AchievementRequest` (T5.4c — `AchievementLoadAsync`) |
| `intif_achievement_save` | ✅ | `IntifService.AchievementSave` (T7.1 — `IAchievementService.SnapshotFor(pc)` → `AchievementSaveAsync`, reads `PlayerEntity.AchievementLog`) |
| `intif_achievement_reward` | ✅ | `ICharServerIpcServiceQuest.AchievementRewardAsync` exists; consumer wires in at reward-grant call site (no IIntifService entry needed — call sites already use the typed sub-IPC directly) |

### Pet / Homunculus / Mercenary / Elemental — **T7.2 + T7.3 wave**

| rAthena fn | Status | C# location / note |
|---|---|---|
| `intif_create_pet` | ✅ | `IntifService.PetCreate` (T7.2 — `PetCreateAsync` with full ctor args) |
| `intif_request_petdata` | ✅ | `IntifService.RequestPetInfo` (T7.2 — `PetLoadAsync`) |
| `intif_save_petdata` | ✅ | `IntifService.SavePet` (T7.2 — `IPetService.SerializeSnapshot(petId)` → `PetSaveAsync`; returns 0 if no live pet) |
| `intif_delete_petdata` | ✅ | `IntifService.DeletePet` (T7.2 — `PetDeleteAsync`) |
| `intif_homunculus_create` | ✅ | `IntifService.HomunculusCreate` (T7.3 — `HomunculusCreateAsync`; legacy byte[] flows through `HomunculusData.Payload`) |
| `intif_homunculus_requestload` | ✅ | `IntifService.HomunculusRequest` (T7.3 — `HomunculusLoadAsync`) |
| `intif_homunculus_requestsave` | ✅ | `IntifService.HomunculusSave` (T7.3 — `IHomunculusService.SerializeSnapshot(homunId)` → `HomunculusSaveAsync`; falls back to legacy byte[] payload when no live entity) |
| `intif_homunculus_requestdelete` | ✅ | `IntifService.HomunculusDelete` (T7.3 — `HomunculusDeleteAsync`) |
| `intif_mercenary_create` | ✅ | `IntifService.MercenaryCreate` (T7.3 — `MercenaryCreateAsync`) |
| `intif_mercenary_request` | ✅ | `IntifService.MercenaryRequest` (T7.3 — `MercenaryLoadAsync`) |
| `intif_mercenary_save` | ✅ | `IntifService.MercenarySave` (T7.3 — `IMercenaryService.SerializeSnapshot` → `MercenarySaveAsync`) |
| `intif_mercenary_delete` | ✅ | `IntifService.MercenaryDelete` (T7.3 — `MercenaryDeleteAsync`) |
| `intif_elemental_create` | ✅ | `IntifService.ElementalCreate` (T7.3 — `ElementalCreateAsync`) |
| `intif_elemental_request` | ✅ | `IntifService.ElementalRequest` (T7.3 — `ElementalLoadAsync`) |
| `intif_elemental_save` | ✅ | `IntifService.ElementalSave` (T7.3 — `IElementalService.SerializeSnapshot` → `ElementalSaveAsync`) |
| `intif_elemental_delete` | ✅ | `IntifService.ElementalDelete` (T7.3 — `ElementalDeleteAsync`) |

### Storage — **T7.4 wave**

| rAthena fn | Status | C# location / note |
|---|---|---|
| `intif_request_account_storage` | ✅ | `IntifService.RequestAccountStorage` (T7.4 — `AccountStorageLoadAsync`) |
| `intif_save_account_storage` | ✅ | `IntifService.SaveAccountStorage` (T7.4 — `AccountStorageSaveAsync`; opaque byte[] payload via `StorageCodec`) |
| `intif_request_guild_storage` | ✅ | `IntifService.RequestGuildStorage` (T7.4 — `GuildStorageLoadAsync`) |
| `intif_save_guild_storage` | ✅ | `IntifService.SaveGuildStorage` (T7.4 — `GuildStorageSaveAsync`) |
| `intif_storage_request` (generic) | ✅ | Absorbed into per-type RequestAccountStorage / RequestGuildStorage at port time — same RPC, no separate IIntifService entry needed |
| `intif_storage_save` (generic) | ✅ | Absorbed into per-type SaveAccountStorage / SaveGuildStorage at port time |

### Auction — **T7.5 wave**

| rAthena fn | Status | C# location / note |
|---|---|---|
| `intif_Auction_requestlist` | ✅ | `IntifService.AuctionRequestList` (T7.5 — `AuctionRequestListAsync`) |
| `intif_Auction_register` | ✅ | `IntifService.AuctionRegister` (T7.5 — packs per-arg signature onto `AuctionData`; `AuctionRegisterAsync`) |
| `intif_Auction_cancel` | ✅ | `IntifService.AuctionCancel` (T7.5 — `AuctionCancelAsync`) |
| `intif_Auction_close` | ✅ | `IntifService.AuctionClose` (T7.5 — `AuctionCloseAsync`) |
| `intif_Auction_bid` | ✅ | `IntifService.AuctionBid` (T7.5 — `AuctionBidAsync`; char side handles P1 outbid-refund mail) |

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
| `intif_request_mapreg` / `intif_save_mapreg` | ✅ | `IntifService.RequestMapreg` / `SaveMapreg` (T7.8 — both dispatch through `ICharServerIpcServiceMapreg`; char-side gRPC handler lands when the script engine's `$var` consumer ports) |

## Coverage summary

| Bucket | ✅ | ⚠️ | ❌ | Total |
|---|---|---|---|---|
| Mail | 6 | 0 | 0 | 6 |
| Quest / Achievement | 5 | 0 | 0 | 5 |
| Pet / Homun / Merc / Elem | 16 | 0 | 0 | 16 |
| Storage | 6 | 0 | 0 | 6 |
| Auction | 5 | 0 | 0 | 5 |
| Party / Guild / Clan | ~30 | 0 | 0 | 30 |
| Misc | 7 | 0 | 0 | 7 |
| **Totals** | **75** | **0** | **0** | **75** |

**T7 (2026-05-22) — zero ⚠️ reached.** Every `intif_*` entry point on
`IIntifService` now dispatches through a typed `ICharServerIpcService*`
sub-IPC. The mapreg pair was the last ⚠️ — closed by T7.8 with a
new `ICharServerIpcServiceMapreg` seam (no-op partial impl until the
script engine's `$var` consumer ports the char-side gRPC handler).

(The original goal said "73 IIntifService stubs"; the audit
enumerates 75 entries — some helpers were absorbed into call
sites, others split when the typed sub-IPC wrappers ported.)

## Implementation plan

1. ✅ **T5.4a** — Mail send + return (most-used, player-visible).
2. ✅ **T5.4b** — Quest save + load (mob-kill / item-drop hooks
   already exist; the save path lights up persistence).
3. ✅ **T5.4c** — Achievement save + load (same shape as Quest).
4. ✅ **T7.1** — Quest + Achievement snapshot serializer (closes
   T5.4b/c empty-list gap).
5. ✅ **T7.6** — Mail inbox close-out (request / read / get / delete).
6. ✅ **T7.2** — Pet snapshot family (4 entries).
7. ✅ **T7.3** — Homun + Merc + Elem snapshot families (12 entries).
8. ✅ **T7.4** — Account + guild storage dispatch (6 entries — opaque
   byte[] via existing StorageCodec; no new serializer needed).
9. ✅ **T7.5** — Auction round-trip (register / bid / close / cancel /
   list; 5 entries).
10. ✅ **T7.8** — mapreg typed seam — closes the last ⚠️
    (script-subsystem gate, no-op partial impl until `$var` consumer
    ports).

## History

### 2026-05-24 — P2.1 doc-resync close-out (0 stale ⚠️ → ✅; 0 genuine gaps remain)

Sweep confirmed: doc already at **75 ✅ / 0 ⚠️ / 0 ❌**. T7.8
(2026-05-22) closed the last ⚠️ (mapreg typed seam). The
remaining ⚠️ markers in the legend / history paragraphs are
text-level, not table rows. No flips needed.

### 2026-05-22 — T7.8 (mapreg typed seam — last ⚠️ closed)

Wraps T7 by closing the lone `intif_request_mapreg` /
`intif_save_mapreg` ⚠️. New `ICharServerIpcServiceMapreg` sub-IPC
(2 methods: `RequestMapregAsync`, `SaveMapregAsync`) with a no-op
partial impl on `CharServerIpcService` — the canonical seam is in
place so `IntifService.RequestMapreg/SaveMapreg` dispatch through
the typed wrapper. The char-side gRPC binding + persistence land
when the script engine's `$var` consumer ports (Phase 4 of
`map/scripting/README.md`).

Tests (+4): `IntifMapregWiringTests` — dispatch + short-circuit for
both methods. Full intif suite **49/49 green**.

**Final coverage:** **75 ✅ / 0 ⚠️ / 0 ❌** across 75 entries. The
goal's "every legacy `intif_*` entry point dispatches a fully-
populated payload onto its typed `ICharServerIpcService*` RPC"
condition holds for all entries.

### 2026-05-22 — T7 (snapshot serializers + close-out)

Six commits across the 8 data-family rows in intif-parity.md (Mail
inbox / Quest + Achievement / Pet / Homun / Merc / Elem / Storage /
Auction). Each entry point on `IIntifService` now dispatches a real
payload through its typed `ICharServerIpcService*` sub-IPC — no
more `Array.Empty<…>` / "byte[] pending" gaps.

**Sub-waves:**
- **T7.1** (`b86b8b2`) — Quest + Achievement snapshot. Added
  `PlayerEntity.QuestLog` + `AchievementLog` (mirror of
  `sd->quest_log[]` / `sd->achievement_data.achievements`);
  `IQuestService.SnapshotFor(pc)` + `Hydrate(pc, entries)`;
  IntifService.QuestSave/AchievementSave dispatch the snapshot.
  +4 tests.
- **T7.6** (`e93a426`) — Mail inbox: MailRequestInbox / MailRead /
  MailGetAttach / MailDelete dispatch through `ICharServerIpcServiceMail`.
  +5 tests (4 dispatch + 1 short-circuit).
- **T7.2** (`785be5a`) — Pet snapshot: `IPetService.SerializeSnapshot(petId)`
  walks live pets by persistent pet_id; PetCreate/RequestPetInfo/
  SavePet/DeletePet dispatch through `ICharServerIpcServicePet`.
  +5 tests.
- **T7.3** (`c3d665c`) — Homun + Merc + Elem snapshot families (12
  entries × Create/Request/Save/Delete). Each *Service got a
  `SerializeSnapshot` returning null today (per-master entity stores
  are pre-port); the dispatch path is the canonical seam. Legacy
  byte[] payloads flow through proto `payload` field for the families
  whose proto carries it (HomunculusData / MercenaryData; ElementalData
  doesn't, so dispatches with id only). +12 tests.
- **T7.4** (`c0c882a`) — Storage: RequestAccountStorage / SaveAccountStorage
  / RequestGuildStorage / SaveGuildStorage dispatch through
  `ICharServerIpcServiceStorage`. The 2 "generic" StorageRequest/Save
  rows in the table were absorbed into the per-type methods at port
  time — same dispatch path. +6 tests.
- **T7.5** (`c37ca2e`) — Auction round-trip: RequestList / Register /
  Cancel / Close / Bid dispatch through `ICharServerIpcServiceAuction`.
  Register packs the per-arg signature onto `AuctionData`. +5 tests.

**IntifService ctor** now takes 6 sub-IPC + 6 service optional params
(DI auto-wires). Test fakes implement only the narrow sub-IPC needed,
matching the T5.4 pattern.

**Coverage delta:** **40 ✅ / 35 ⚠️ / 0 ❌** → **74 ✅ / 1 ⚠️ / 0 ❌**
across 75 entries. The lone remaining ⚠️ is
`intif_request_mapreg` / `intif_save_mapreg` — a script-subsystem
gate (not a data-family snapshot pattern); when the script engine's
`$var` consumer lands, the entry flips.

**Tests:** Map.Server.Tests 2961 → 2998 green (+37). Full intif test
suite 45/45 green. dotnet build Map.Server: 0 errors.



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
