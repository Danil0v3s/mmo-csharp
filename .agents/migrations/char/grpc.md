# Char gRPC server implementation

`CharGrpcService` parity vs rAthena's `char_mapif.cpp` lifecycle RPCs and char-side `int_*.cpp` flows.

**rAthena source:** [rathena/src/char/char_mapif.cpp](/Volumes/1TB/Projetos/rathena/src/char/char_mapif.cpp), [rathena/src/char/int_*.cpp](/Volumes/1TB/Projetos/rathena/src/char/)
**C# implementation:** [Char.Server/CharGrpcService.cs](../../../Char.Server/CharGrpcService.cs) (~3000 lines)
**Proto:** [Core.Server/Protos/char_service.proto](../../../Core.Server/Protos/char_service.proto)

> **Scope note:** This doc covers **char-side gRPC server impls** for character lifecycle (data, list, create, delete, save, ticket auth, online/offline). Per-module shared inter-server flows (party, guild, mail, etc.) are tracked in [../inter/modules.md](../inter/modules.md). Map-side callers are tracked in [../map/ipc-integration.md](../map/ipc-integration.md).

## Done ✅

### Character lifecycle endpoints (Phase A of old plan)

All DB-backed against [Core.Database](../../../Core.Database) entities. No in-memory state survivors as of 2026-05-15 audit.

| RPC | C# location | Notes |
|---|---|---|
| `GetCharacterList` | CharGrpcService.cs | Repository-backed, sorted by slot, honors deleted/banned flags |
| `GetCharacterData` | CharGrpcService.cs | Repository-backed character + map/save position |
| `RequestCharacterName` | CharGrpcService.cs | Repository lookup, no stub fallback |
| `CreateCharacter` | CharGrpcService.cs | Full creation path: name normalization/structural validation, DB insert |
| `DeleteCharacter` | CharGrpcService.cs:425-581 | Restriction parity: party, guild, base-level, delete delay, soft-delete |
| `RequestCharacterMapAuth` | CharGrpcService.cs:326-576 | Ticket consume + replay rejection; full character payload |
| `SaveCharacterState` | CharGrpcService.cs:417-491 | Persists position/map/status; offline-transition ack semantics |

### Online state + map registry (Phase B)

| RPC | C# location | Notes |
|---|---|---|
| `SetCharacterOnline` | CharGrpcService.cs:722-753 | Updates `char.online`; propagates to login via `NotifyAccountStatusAsync` |
| `SetCharacterOffline` | CharGrpcService.cs:686-720 | Supports `character_id <= 0` (account-wide); login sync |
| `SetAllCharactersOffline` | CharGrpcService.cs:755-774 | Bulk update |
| `IMapServerRegistryService` | [Char.Server/Services/MapServerRegistryService.cs](../../../Char.Server/Services/MapServerRegistryService.cs) | Singleton, shared across calls; map list / usercount / addresses |

### Fame / bonus-script / scdata / skill-cooldown

All DB-sourced; in-memory caches removed.

| RPC | C# location |
|---|---|
| `RequestFameList` | CharGrpcService.cs:949-978 (class-family filter parity with `char_read_fame_list`) |
| `GetBonusScript` / `SaveBonusScript` | CharGrpcService.cs:991-1043 |
| `RequestStatusChangeData` / `SaveStatusChangeData` | CharGrpcService.cs:576-628 |
| `LoadSkillCooldown` / `SaveSkillCooldown` | CharGrpcService.cs:631-684 |

### P1 data-integrity fixes (2026-05-16)

| RPC | Fix | Tests |
|---|---|---|
| `HomunculusLoad` / `Save` / `Create` / `Delete` | Now round-trips skills via `skill_homunculus` (rAthena DELETE-then-INSERT pattern). New `HomunculusSkillEntry` proto message and `repeated skills` on `HomunculusData`. | [CharGrpcDataIntegrityTests.cs](../../../Char.Server.Tests/Services/CharGrpcDataIntegrityTests.cs) — 3 tests |
| `AuctionBid` | Refunds outbid prior bidder via auto-generated mail (sender="Auction Manager", zeny = prior price). Mirrors rAthena `mail_sendmail` in `mapif_parse_Auction_bid`. | 3 tests |
| `MailSend` / `MailGetAttachment` / `MailRequestInbox` / `MailRead` | Persists attachments to `mail_attachments` table per item; returned + cleared on `MailGetAttachment`. New `MailAttachmentItem` proto message and `repeated items` on `MailMessageData`/`MailSendRequest`/`MailGetAttachmentResponse`. | 3 tests |

### Tests

[Char.Server.Tests/Services/CharGrpcServiceParityTests.cs](../../../Char.Server.Tests/Services/CharGrpcServiceParityTests.cs) (~768 lines):
- Map-auth ticket consume + replay rejection
- Online state transitions (`SetCharacterOnline`, account-wide / per-char offline)
- `SaveCharacterState` offline-save ACK
- `DeleteCharacter` restrictions (party/guild/base-level + soft-delete)
- Branch-matrix tests for map registry validation

## Pending

None.

### Won't-fix / by-design divergences

- **`KeepAlive` (Char↔Map) is intentionally a no-op acknowledgement** ([CharGrpcService.cs:512-516](../../../Char.Server/CharGrpcService.cs)). rAthena tracks last-seen at the connection layer; in C# this is owned by `ServerSession.IsHealthCheckTimedOut()` plus the gRPC channel's connection state. The handler exists so the map-side periodic KeepAlive call has a well-defined target; no additional bookkeeping is needed.
- **`autosave_interval` is structurally absent.** rAthena keeps `mmo_charstatus` in memory and flushes dirty rows on a timer; the C# port persists every gameplay op through EF (`GameDbContext.SaveChangesAsync`) so there's no concept of "in-memory dirty char". The knob exists in `CharServerConfiguration` for config-file compatibility but has no effect.
- **`char_check_db` is covered by EF migrations.** rAthena boots a `SHOW TABLES` probe; in C# the schema is enforced by `dotnet ef database update` and any missing table fails at first query.
- **`save_log` is covered by Serilog.** rAthena writes save events to `log/save.log` when this is on; the C# port logs through `ILogger<T>` and the same information lands in whichever sink is configured.
- **`clear_parties` is mostly inert** — rAthena boots cleared empty parties as a safety; the C# port's FK constraints (party_member → party with cascade delete) already prevent orphan rows. Defensive cleanup would still be useful long-term but is not user-visible behavior.

## Files / structure

- Service impl: [CharGrpcService.cs](../../../Char.Server/CharGrpcService.cs)
- Registry singleton: [MapServerRegistryService.cs](../../../Char.Server/Services/MapServerRegistryService.cs)
- Proto: [char_service.proto](../../../Core.Server/Protos/char_service.proto)
- Tests: [CharGrpcServiceParityTests.cs](../../../Char.Server.Tests/Services/CharGrpcServiceParityTests.cs)

## History

- **2026-05-19** — **Final char-side parity gaps closed (100%).**
  - **Periodic housekeeping**: new [CharMaintenanceService](../../../Char.Server/Services/CharMaintenanceService.cs) ports rAthena's three background timers (`mail_return_timer` + `mail_delete_timer` from `int_mail.cpp:317/321`, `char_clan_member_cleanup` from `char.cpp:2216`). `MailReturnDays` / `MailDeleteDays` / `ClanRemoveInactiveDays` / `MailReturnEmpty` config knobs now drive real behavior. Driven from `CharServerImpl.UpdateGameLogicAsync` per game tick; each pass is deadline-gated internally so the loop call is cheap. 7 regression tests in `CharMaintenanceServiceTests.cs` cover return + delete + clan flows including the `MailReturnEmpty=0` skip and the disabled-knob no-op.
  - **`MailRetrieve` gate**: [CharGrpcService.MailGetAttachment](../../../Char.Server/CharGrpcService.cs) now refuses attachment retrieval when `mail_retrieve == 0` and the mail isn't yet `MAIL_READ`, matching `int_mail.cpp:385`.
  - **`allowed_job_flag` gate**: [CharacterCreateHandler.IsJobAllowed](../../../Char.Server/Handlers/CharacterCreateHandler.cs) ports `char.cpp:1481` — JOB_NOVICE / JOB_SUMMONER bitmask check on the start-job. Sentinel `-1` keeps the C# legacy "no gate" default. 11 unit tests pin the truth table.
  - **`char_rename_party` / `char_rename_guild` gates**: [CharacterRenameApplyHandler](../../../Char.Server/Handlers/CharacterRenameApplyHandler.cs) now returns reject codes 6 / 5 when the corresponding flag is off and the char belongs to a party/guild (rAthena `char.cpp:1277`).
  - **`guild_exp_rate` modifier**: [CharGrpcService.GuildMemberInfoChange](../../../Char.Server/CharGrpcService.cs) handles the previously-unimplemented GMI_EXP type (3): member exp delta is applied to guild total via `delta * guild_exp_rate / 100` (int_guild.cpp:1564) with safe overflow cap.
  - **Won't-fix divergences documented** above: `autosave_interval`, `char_check_db`, `save_log`, `clear_parties`.
  - Char.Server.Tests fixed (was failing to build due to missing `IReturningClientAuthService` ctor arg). Suite is now 166 tests green, up from 119.

- **2026-05-16** — **P2 complete.** Char-server completeness closed:
  - `PartyShareLevel` now persists to a process-global `CharServerState.PartyShareLevel` (rAthena's `inter.cpp:party_share_level` parity). Default 10.
  - `UpdateFame` impl was already DB-backed at [CharGrpcService.cs:920-946](../../../Char.Server/CharGrpcService.cs); audit doc was wrong about it being missing.
  - `PartyShareLevel`, `UpdateFame` removed from server-side-stubs Pending list.
  - Reject codes: confirmed rAthena's `chclif_reject` always uses `HC_REFUSE_ENTER` error code 0; richer reasons go through `RejectAuthResult`/`SC_NOTIFY_BAN` which C# already does correctly. Plan item was based on a misread; no change needed.
  - Rename burst: confirmed C# `ResendCharacterWindowAsync` already sends the same 4-packet burst (`HC_ACCEPT_ENTER2` + `HC_ACCEPT_ENTER` + `HC_CHARLIST_NOTIFY` + `HC_BLOCK_CHARACTER`) as rAthena's `chclif_mmo_char_send`. No change needed.
  - `CH_KEEP_ALIVE`: kept C# stricter behavior (validates account_id, disconnects on mismatch) as a deliberate divergence — catches forged packets that rAthena lets through.
- **2026-05-16** — **P1 complete.** Three data-loss bugs fixed:
  - Homunculus skills now load/save/delete with the homun entity. Added `HomunculusSkillEntry` proto message. `SaveHomunculusSkillsAsync` helper applies rAthena DELETE-all + INSERT-non-zero pattern.
  - `AuctionBid` now inserts a refund mail to the prior bidder before overwriting (rAthena `mail_sendmail` parity). Sender 0/"Auction Manager", body differs for same-bidder vs different-bidder refund.
  - Mail attachments persist to `mail_attachments` on send, return via `MailGetAttachment`, are included in inbox + read responses. Added `MailAttachmentItem` proto message; `bytes attachment` field kept but unused.
  - Added [CharGrpcDataIntegrityTests.cs](../../../Char.Server.Tests/Services/CharGrpcDataIntegrityTests.cs) with 9 tests using EF Core InMemory provider; full Char.Server.Tests suite green at 119 tests.
- **2026-05-15** — Audit found 3 data-loss bugs (mail attach, auction refund, homun skills) and 4 server-side stubs still marked "done" in old plan. Reclassified as Pending. Confirmed core character-lifecycle and online-state RPCs are genuinely DB-backed.
- **2026-02-28** — Added parity regression tests covering map-auth replay, online-state transitions, `SaveCharacterState` ack semantics, `DeleteCharacter` restrictions. `SetCharacterOffline` tightened to treat `character_id <= 0` as account-wide. Map registry promoted to singleton `IMapServerRegistryService`.
- **2026-02-26** — Initial Phase C migration: party/guild/quest/achievement/mail/auction/pet/homunculus/mercenary/elemental/clan/fame/bonus-script/scdata/skill-cooldown all moved from `ConcurrentDictionary` stubs to DB-backed flows. Guild storage and account storage payloads persisted as opaque bytes.
