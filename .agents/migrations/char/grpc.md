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

## Pending ⚠️

### MEDIUM — Server-side stubs

These return hardcoded success without performing the rAthena-side side effect. Marked done in old plans but functionally inert.

| RPC | File:line | Missing behavior |
|---|---|---|
| `KeepAlive` (Char↔Map) | CharGrpcService.cs:512-516 | No-op success. rAthena tracks last-seen + disconnects on stale |
| `RequestAddressSync` (0x2735) | CharGrpcService.cs:4207 | Doesn't broadcast updated IP to map servers |
| `PartyShareLevel` (0x302A) | CharGrpcService.cs:1474-1482 | Returns success without persisting |
| `UpdateFame` (0x2b10) | _missing entirely_ | No server impl found; proto present |

(Inter-base RPC stubs — broadcast/whisper/name change — are tracked in [../inter/base.md](../inter/base.md).)

### LOW — Test coverage gaps

- No test covers **replayed `LoginId1/LoginId2` on a new connection** — claimed by Phase D13 of old plan.

## Files / structure

- Service impl: [CharGrpcService.cs](../../../Char.Server/CharGrpcService.cs)
- Registry singleton: [MapServerRegistryService.cs](../../../Char.Server/Services/MapServerRegistryService.cs)
- Proto: [char_service.proto](../../../Core.Server/Protos/char_service.proto)
- Tests: [CharGrpcServiceParityTests.cs](../../../Char.Server.Tests/Services/CharGrpcServiceParityTests.cs)

## History

- **2026-05-16** — **P1 complete.** Three data-loss bugs fixed:
  - Homunculus skills now load/save/delete with the homun entity. Added `HomunculusSkillEntry` proto message. `SaveHomunculusSkillsAsync` helper applies rAthena DELETE-all + INSERT-non-zero pattern.
  - `AuctionBid` now inserts a refund mail to the prior bidder before overwriting (rAthena `mail_sendmail` parity). Sender 0/"Auction Manager", body differs for same-bidder vs different-bidder refund.
  - Mail attachments persist to `mail_attachments` on send, return via `MailGetAttachment`, are included in inbox + read responses. Added `MailAttachmentItem` proto message; `bytes attachment` field kept but unused.
  - Added [CharGrpcDataIntegrityTests.cs](../../../Char.Server.Tests/Services/CharGrpcDataIntegrityTests.cs) with 9 tests using EF Core InMemory provider; full Char.Server.Tests suite green at 119 tests.
- **2026-05-15** — Audit found 3 data-loss bugs (mail attach, auction refund, homun skills) and 4 server-side stubs still marked "done" in old plan. Reclassified as Pending. Confirmed core character-lifecycle and online-state RPCs are genuinely DB-backed.
- **2026-02-28** — Added parity regression tests covering map-auth replay, online-state transitions, `SaveCharacterState` ack semantics, `DeleteCharacter` restrictions. `SetCharacterOffline` tightened to treat `character_id <= 0` as account-wide. Map registry promoted to singleton `IMapServerRegistryService`.
- **2026-02-26** — Initial Phase C migration: party/guild/quest/achievement/mail/auction/pet/homunculus/mercenary/elemental/clan/fame/bonus-script/scdata/skill-cooldown all moved from `ConcurrentDictionary` stubs to DB-backed flows. Guild storage and account storage payloads persisted as opaque bytes.
