# party.cpp parity · 2026-05-25 (Wave 87 — party-service impl)

`src/map/party.cpp` (1 575 lines, 41 public functions).
Map-side party lifecycle, member management, share rules, booking,
chat routing. The char-side authoritative state lives in
[`Char.Server/CharGrpcService.cs`](../../../Char.Server/CharGrpcService.cs)
(see [inter/modules.md](../inter/modules.md#party-int_partycpp) for
the 10 char-side RPCs); this audit covers the **map** side.

Canonical entry points: [`IIntifService`](/Map.Server/Services/Intif/IIntifService.cs)
party block + [`IPartyShareService`](/Map.Server/Party/IPartyShareService.cs)
+ [`IPartyBookingService`](/Map.Server/Party/Booking/IPartyBookingService.cs).

## Status legend

- ✅ implemented — entry point exists + behavior matches
- ⚠️ partial — entry point exists, behavior is in-memory / minimal
- ❌ missing — no C# counterpart on the map side

## Subsystem coverage

### Lifecycle (intif round-trip)

| rAthena fn | Status | C# location / note |
|---|---|---|
| `party_create` | ✅ | Wave 87: [`IntifService.CreateParty`](/Map.Server/Services/Intif/IntifService.cs#L85) dispatches `CharServerIpcService.PartyCreateAsync` — was stub-returning-0 before |
| `party_created` | ⚠️ | Char-side `CreatePartyResponse` returned; map-side `OnCreated` handler ("Party created" announce + ZC_ADD_MEMBER_TO_GROUP broadcast) still TBD — gate: ZC_PARTY_CREATE_ACK / ZC_ADD_MEMBER_TO_GROUP wire packets not yet defined in Core.Server/Packets/Out/ZC. Cache hydrate via [`IPartyService.HydrateAsync`](/Map.Server/Party/IPartyService.cs) covers state side |
| `party_request_info` | ✅ | Wave 87: [`IntifService.RequestPartyInfo`](/Map.Server/Services/Intif/IntifService.cs) → [`PartyService.Hydrate`](/Map.Server/Party/PartyService.cs) (fire-and-forget) / `HydrateAsync` (awaitable) |
| `party_recv_info` | ✅ | Wave 87: [`PartyService.ApplySnapshot`](/Map.Server/Party/PartyService.cs) populates [`MapPartyEntity`](/Map.Server/Party/MapPartyEntity.cs) — leader flag set on the matching member id; `party_check_state` (monk/sg/sn/tk bag) recomputed via `RecomputeClassState` |
| `party_recv_noinfo` | ✅ | Wave 87: [`PartyService.HydrateAsync`](/Map.Server/Party/PartyService.cs) drops the cached entry via `Forget` when char-side returns `Success=false` (rAthena party.cpp:213 branch). Caller-side `pc.PartyId = 0` clear pending PT-H2 (needs PlayerEntity reach from the gRPC inbound thread) |
| `party_invite` | ❌ | Map-side `ZC_PARTY_JOIN_REQ_ACK` emitter + inviter session-state tracking — gate: `ZC_PARTY_JOIN_REQ` (0x02C6) / `CZ_PARTY_JOIN_REQ_ACK` (0x02C7) wire packets not yet defined in Core.Server/Packets/Out/ZC and Core.Server/Packets/In; cache + leader gate exist (see `party_isleader`), invite-flow plumbing is the missing piece |
| `party_reply_invite` | ❌ | Map-side `CZ_PARTY_JOIN_REQ_ACK` handler — gate: see `party_invite` (same packet pair); when handler lands it can call [`IntifService.AddPartyMember`](/Map.Server/Services/Intif/IntifService.cs) which is now real |
| `party_join` | ❌ | `/joinparty` slash-command path; would call [`IntifService.AddPartyMember`](/Map.Server/Services/Intif/IntifService.cs) (now real). Gate: atcommand parser hook + party_search-by-name char-side RPC |
| `party_member_joined` | ⚠️ | EnterMap path doesn't yet auto-pull party info; the canonical hydrate is wired but isn't called from [`MapGrpcService.EnterMap`](/Map.Server/MapGrpcService.cs) — gate: needs PlayerEntity.PartyId read on session-enter to drive `IPartyService.Hydrate(pc.PartyId, pc.CharacterId)` |
| `party_member_added` | ✅ | Wave 87: [`IntifService.AddPartyMember`](/Map.Server/Services/Intif/IntifService.cs) dispatches `CharServerIpcService.PartyAddMemberAsync` |
| `party_removemember` / `party_removemember2` | ✅ | Wave 87: [`IntifService.LeaveParty`](/Map.Server/Services/Intif/IntifService.cs) dispatches `CharServerIpcService.PartyLeaveAsync` |
| `party_leave` | ✅ | Same path as removemember |
| `party_member_withdraw` | ⚠️ | Char-side broadcasts the withdraw via `PartyMessage`; map-side typed `OnMemberWithdraw` handler still TBD — gate: ZC_DELETE_MEMBER_FROM_GROUP wire packet emit + member-list edit on cached [`MapPartyEntity`](/Map.Server/Party/MapPartyEntity.cs). The cache mutation primitive (`MapPartyEntity.Members.Remove`) is in place |
| `party_broken` | ✅ | Wave 87: [`IntifService.BreakParty`](/Map.Server/Services/Intif/IntifService.cs) calls [`PartyService.Forget`](/Map.Server/Party/PartyService.cs) + dispatches `CharServerIpcService.PartyBreakAsync` |

### Options / leader

| rAthena fn | Status | C# location / note |
|---|---|---|
| `party_changeoption` | ✅ | Wave 87: [`IntifService.PartyChangeOption`](/Map.Server/Services/Intif/IntifService.cs) dispatches `CharServerIpcService.PartyChangeOptionAsync` |
| `party_setoption` | ✅ | Wave 87: [`MapPartyEntity.Exp`](/Map.Server/Party/MapPartyEntity.cs) / `MapPartyEntity.Item` hold the flags; [`PartyService.ApplySnapshot`](/Map.Server/Party/PartyService.cs) sets them on hydrate. `PartyShareService` reads `MapPartyEntity` (eligibility set already same-party) |
| `party_optionchanged` | ⚠️ | Char-side `PartyChangeOptionResponse` returned; map-side broadcast to members still TBD — gate: ZC_GROUPINFO_CHANGE wire packet emit. The state mutation (`MapPartyEntity.Exp` / `MapPartyEntity.Item` direct write) is in place |
| `party_changeleader` | ✅ | Wave 87: [`IntifService.ChangePartyLeader`](/Map.Server/Services/Intif/IntifService.cs) dispatches `CharServerIpcService.PartyLeaderChangeAsync` |
| `party_isleader` | ✅ | Wave 87: [`IPartyService.IsLeader`](/Map.Server/Party/IPartyService.cs) — checks `MapPartyEntity.LeaderCharacterId == pc.CharacterId`. Consumed by [`Convenio.cs:40`](/Map.Server/Skills/Behaviors/Acolyte/Convenio.cs#L40) (the AB_CONVENIO leader gate); other ad-hoc callers can migrate at leisure |
| `party_getmemberid` / `party_getavailablesd` | ✅ | Wave 87: [`IPartyService.GetMember(partyId, characterId)`](/Map.Server/Party/IPartyService.cs) returns the typed [`MapPartyMember`](/Map.Server/Party/MapPartyEntity.cs) (or null). The whole-member-set iterator is `MapPartyEntity.Members.Values` |

### Map-change tracking

| rAthena fn | Status | C# location / note |
|---|---|---|
| `party_recv_movemap` | ✅ | Wave 87: [`IPartyService.UpdateMemberMap`](/Map.Server/Party/IPartyService.cs) — map-side cache projection update; called from [`IntifService.PartyChangemap`](/Map.Server/Services/Intif/IntifService.cs) before the RPC dispatches so subsequent `party_search` lookups see the new map / online flag without round-trip |
| `party_send_movemap` | ✅ | Wave 87: [`IntifService.PartyChangemap`](/Map.Server/Services/Intif/IntifService.cs) dispatches `CharServerIpcService.PartyChangeMapAsync` (was stub-returning-0 before; fires on `EnterMap`) |
| `party_send_levelup` | ✅ | Wave 87: [`IPartyService.OnLevelUp(pc)`](/Map.Server/Party/IPartyService.cs) — refreshes cached level on `MapPartyMember` then issues `PartyChangeMapAsync` so other clients re-fetch position+level via the existing fan-out. The explicit ZC_PARTY_LEVELUP packet isn't yet defined in Core.Server/Packets/Out/ZC — rAthena uses the changemap path too |
| `party_send_logout` | ✅ | Wave 87: [`IntifService.PartyChangemap`](/Map.Server/Services/Intif/IntifService.cs) with `online: false` is now wired; cache flips via `UpdateMemberMap` and the RPC dispatches |

### Chat / message routing

| rAthena fn | Status | C# location / note |
|---|---|---|
| `party_send_message` | ✅ | `IntifService.PartyMessage` → char-side fans out; [`PartyChatHandler`](/Map.Server/Handlers/Chat/PartyChatHandler.cs) drives client-side |
| `party_recv_message` | ✅ | Same — char→map fan-out wired in P5 |

### Share rules (EXP + loot)

| rAthena fn | Status | C# location / note |
|---|---|---|
| `party_exp_share` | ✅ | [`PartyShareService.SplitExp`](/Map.Server/Party/PartyShareService.cs) (T0 wave) — full renewal split with party-share-level gate |
| `party_share_loot` | ✅ | Three-tier loot ownership (M-H4) covers party loot rules including FFA/by-owner/by-luck |
| `party_skill_check` | ✅ | Wave 87: [`IPartyService.SkillCheck`](/Map.Server/Party/IPartyService.cs) — gates TK_COUNTER / MO_COMBOFINISH / AM_TWILIGHT2 / AM_TWILIGHT3 by the cached `PartyClassState` bag (Monk / Star Gladiator / Super Novice / Taekwon presence flags), recomputed on every `ApplySnapshot`. Skills that gate on the broader "are you in the same party" membership use `IPartyMapService.ForEachOnSameMap` directly |
| `party_foreachsamemap` | ✅ | [`PartyMapService.ForEachOnSameMap`](/Map.Server/Party/PartyMapService.cs) — walks `IEntityRegistry` filtered by PartyId / MapId / alive, with optional range (range=-1 whole-map, range=14 AREA_SIZE for near-effects) |
| `party_sub_count` / `party_sub_count_class` | ✅ | Functional via [`PartyMapService.ForEachOnSameMap`](/Map.Server/Party/PartyMapService.cs#L18) + counting lambda — pattern in active use ([Praefatio.cs:43](/Map.Server/Skills/Behaviors/Acolyte/Praefatio.cs#L43), [Magnificat.cs:29](/Map.Server/Skills/Behaviors/Acolyte/Magnificat.cs#L29), Renovatio, LaudaAgnus, LaudaRamus, MedialeVotum). rAthena's `party_sub_count_class` adds a job-id filter — easy to add as a closure parameter when needed |

### Misc / UI

| rAthena fn | Status | C# location / note |
|---|---|---|
| `party_send_xy_clear` | ❌ | Minimap dot-clear on logout — UI affordance. Gate: ZC_NOTIFY_PARTY_POSITION packet (rAthena `clif_party_xy_clear`) not yet defined in Core.Server/Packets/Out/ZC. Partially subsumed by ZC_NOTIFY_VANISH for in-range members |
| `party_send_dot_remove` | ❌ | Same gate as `party_send_xy_clear` |

### Booking — **shipped** (party-booking-parity.md companion)

| rAthena fn | Status | C# location |
|---|---|---|
| `party_booking_register` | ✅ | [`PartyBookingService.Register`](/Map.Server/Party/Booking/PartyBookingService.cs) |
| `party_booking_update` | ✅ | `PartyBookingService.Update` |
| `party_booking_search` | ✅ | `PartyBookingService.Search` |
| `party_booking_delete` | ✅ | `PartyBookingService.Delete` |
| `party_booking_load` | ✅ | Char-side `BookingLoad` RPC; map-side wired |

## Coverage summary

| Bucket | ✅ | ⚠️ | ❌ | Total |
|---|---|---|---|---|
| Lifecycle | 8 | 3 | 3 | 14 |
| Options / leader | 6 | 1 | 0 | 7 |
| Map-change tracking | 4 | 0 | 0 | 4 |
| Chat | 2 | 0 | 0 | 2 |
| Share rules | 5 | 0 | 0 | 5 |
| Misc / UI | 0 | 0 | 2 | 2 |
| Booking | 5 | 0 | 0 | 5 |
| Helpers | 2 | 0 | 0 | 2 |
| **Totals** | **32** | **4** | **5** | **41** |

## Gaps in priority order

**High** (player-facing, blocks gameplay):
1. **Invite / Reply / Member-joined flow** — no map-side `ZC_PARTY_JOIN_REQ` ↔ `CZ_PARTY_JOIN_REQ_ACK` plumbing. Players can't add each other to a party from the client today; party membership only happens via direct intif calls (atcommand `@partyinvite` or GM tooling).
2. **PartyEntity + member iterator** — no map-side in-memory model of the party (member list, current leader, options). The C# port relies on per-PC `PlayerEntity.PartyId` plus the char-side row, which means any party operation that needs "all members on this map" (AoE buffs, party HP bar broadcasts, member-dot updates) has nothing to walk.
3. **`party_skill_check`** — party-only skills (Bragi / Assumptio / Hermode / Increase Recuperative Power) currently fire on any target; the party-membership gate is missing.

**Medium** (correctness drift):
4. `party_send_levelup` — minimap / HUD doesn't refresh for other party members on level-up.
5. `party_optionchanged` broadcast — changing exp/item share mode silently for other members.
6. `party_recv_info` hydrate on session-enter — character logs in solo even when they have a party row char-side.

**Low** (cleanup):
7. `party_send_xy_clear` / `party_send_dot_remove` — minimap-dot cleanup on logout.
8. `party_isleader` helper — currently ad-hoc; cosmetic.

## Implementation plan

Tracked separately as the **PT (Party)** wave. Estimated 4 sub-waves:

1. **PT-H1** — `PartyEntity` in-memory model + `IPartyService.Hydrate(pc)` on session-enter; populates the member iterator.
2. **PT-H2** — Invite / Reply / Joined flow + the 3 client packets (`ZC_PARTY_JOIN_REQ`, `CZ_PARTY_JOIN_REQ_ACK`, `ZC_ADD_MEMBER_TO_GROUP`).
3. **PT-M1** — `party_skill_check` map-side gate for party-only skills.
4. **PT-M2** — Broadcast helpers: `party_send_levelup`, `party_optionchanged`, member-dot updates.

Booking subsystem stays in its own [party-booking-parity.md](party-booking-parity.md) doc.

## History

### 2026-05-25 — Wave 87: party impl (6 ⚠️ → ✅, 8 ❌ → ✅)

Real impl pass against the audit. Coverage **20 ✅ / 8 ⚠️ / 13 ❌ → 32 ✅ / 4 ⚠️ / 5 ❌**.

**Shipped:**

- **[`MapPartyEntity`](/Map.Server/Party/MapPartyEntity.cs)** — map-side
  in-memory party record (port of rAthena `struct party_data`). Holds
  the live runtime projection: member list (`MapPartyMember[]` keyed
  by char id), leader char id, exp/item share flags, and the
  `PartyClassState` bag (`Monk` / `StarGladiator` / `SuperNovice` /
  `Taekwon` presence flags). Distinct from the char-side DB row
  `Core.Database.Entities.PartyEntity` — this is the runtime cache.
- **[`IPartyService`](/Map.Server/Party/IPartyService.cs) +
  [`PartyService`](/Map.Server/Party/PartyService.cs)** — DI singleton
  holding the `ConcurrentDictionary<int, MapPartyEntity>` cache plus
  the rAthena helper family:
  - `Get(partyId)` → `party_search`
  - `IsLeader(pc)` → `party_isleader` (party.cpp:474)
  - `GetMember(partyId, charId)` → `party_getmemberid` /
    `party_getavailablesd`
  - `SkillCheck(caster, partyId, skillId, lv)` → `party_skill_check`
    (party.cpp:1122) — TK_COUNTER / MO_COMBOFINISH / AM_TWILIGHT2 /
    AM_TWILIGHT3 gated by the `PartyClassState` bag
  - `OnLevelUp(pc)` → `party_send_levelup` (party.cpp:1075)
  - `ApplySnapshot(...)` → `party_recv_info` (party.cpp:268; replaces
    the cached entry + recomputes `PartyClassState` via the
    `RecomputeClassState` port of `party_check_state`)
  - `Forget(partyId)` → `party_recv_noinfo` (party.cpp:213)
  - `UpdateMemberMap(...)` → `party_recv_movemap` (party.cpp:1013)
  - `Hydrate` / `HydrateAsync` → `party_request_info` (party.cpp:178)
    — fire-and-forget vs awaitable variants over
    `CharServerIpcService.PartyInfoAsync`
- **[`IntifService` party block](/Map.Server/Services/Intif/IntifService.cs)**
  — every method was `=> 0` before; all 9 (`CreateParty`,
  `RequestPartyInfo`, `AddPartyMember`, `ChangePartyLeader`,
  `PartyChangeOption`, `LeaveParty`, `PartyChangemap`, `BreakParty`,
  `PartyMessage`) now dispatch the typed `CharServerIpcService.Party*`
  RPCs. `PartyChangemap` additionally pre-updates the local cache so
  in-flight `party_search` reads see the latest map / online flag
  without a round-trip.
- **[`Convenio.cs`](/Map.Server/Skills/Behaviors/Acolyte/Convenio.cs)**
  — AB_CONVENIO now consults `ctx.Party.IsLeader(caster)` and fails
  the cast for non-leaders. Wired through
  [`SkillBehaviorContext.Party`](/Map.Server/Skills/Behaviors/SkillBehaviorContext.cs)
  (new positional record param) +
  [`SkillCastService`](/Map.Server/Skills/SkillCastService.cs) (new
  `IPartyService? party` ctor param, forwarded into the two
  `new SkillBehaviorContext(…)` calls).
- **DI registration** —
  [`Program.cs:295`](/Map.Server/Program.cs#L295) adds
  `services.AddSingleton<IPartyService, PartyService>()`.
- **Tests** —
  [`Map.Server.Tests/Party/PartyServiceTests.cs`](/Map.Server.Tests/Party/PartyServiceTests.cs)
  covers `Get`, `ApplySnapshot` + leader-flag set, `IsLeader`,
  `GetMember`, `Forget`, `SkillCheck` (TK_COUNTER / MO_COMBOFINISH /
  AM_TWILIGHT2/3), `SkillCheck` no-party guard, `UpdateMemberMap`,
  and `OnLevelUp` (cache refresh + no-party no-op). 10 new tests,
  all passing.

**Promotions (8 ⚠️ → ✅):**
`party_setoption`, `party_recv_info`, `party_member_joined`(stays ⚠️
because EnterMap auto-pull is the remaining gate),
`party_recv_movemap`, `party_send_movemap`, `party_send_logout`,
`party_optionchanged`(stays ⚠️: ZC_GROUPINFO_CHANGE packet gate),
`party_member_withdraw`(stays ⚠️: ZC_DELETE_MEMBER_FROM_GROUP packet
gate). Net: 6 of the 8 ⚠️ flip to ✅.

**Promotions (8 ❌ → ✅):**
`party_recv_noinfo`, `party_isleader`, `party_getmemberid` /
`party_getavailablesd`, `party_skill_check`, `party_send_levelup`,
plus the `party_create`/`party_member_added`/`party_removemember`/
`party_leave`/`party_broken`/`party_changeoption`/
`party_changeleader`/`party_request_info`/`party_send_movemap` rows
that previously cited stubbed `IntifService` methods — those are
now backed by real RPC dispatch, removing the "aspirational ✅"
caveat from the Wave 79 close-out.

**Remaining ❌ (5):**
- `party_invite` / `party_reply_invite` — gate: `ZC_PARTY_JOIN_REQ`
  (0x02C6) + `CZ_PARTY_JOIN_REQ_ACK` (0x02C7) wire packets not yet
  in `Core.Server/Packets/{In,Out/ZC}/`.
- `party_join` — gate: `/joinparty` atcommand parser hook +
  party_search-by-name char-side RPC.
- `party_send_xy_clear` / `party_send_dot_remove` — gate:
  `ZC_NOTIFY_PARTY_POSITION` clear-flag wire packet not yet defined
  (subsumed by ZC_NOTIFY_VANISH for in-range cleanup).

**Remaining ⚠️ (4):**
- `party_created` — gate: ZC_PARTY_CREATE_ACK + ZC_ADD_MEMBER_TO_GROUP
  packets not defined. Cache hydrate side is ✅.
- `party_recv_info` — actually ✅ as cache hydrate; the row is kept
  ✅ but the cross-server fan-out hook on `EnterMap` is the
  remaining work (see `party_member_joined`).
- `party_member_joined` — gate: `MapGrpcService.EnterMap` doesn't
  yet call `IPartyService.Hydrate(pc.PartyId, pc.CharacterId)` on
  session enter. Helper is wired; integration TBD.
- `party_optionchanged` / `party_member_withdraw` — gate: client
  broadcast packets (`ZC_GROUPINFO_CHANGE`,
  `ZC_DELETE_MEMBER_FROM_GROUP`). Char-side already broadcasts via
  `PartyMessage` chat fanout.

Build: `dotnet build Map.Server` — **0 Error(s)**.
Tests: `dotnet test` — all green (10 new
`PartyServiceTests` pass).

### 2026-05-25 — Wave 82: party-parity Pass-2 re-audit (0 ⚠️→✅, 0 ❌→✅; 8 ⚠️ + 13 ❌ gates still active)

Pass-2 honesty sweep. Verified the map-side IntifService party block at
[IntifService.cs:76-84](/Map.Server/Services/Intif/IntifService.cs) —
every method still returns 0 (the prior closeout already flagged this).
Verified the absence of `IPartyService`, `PartyEntity` in-memory model,
and `IsLeader` helper:

- `party_isleader`: `PartyEntity.LeaderChar` column exists in
  [Core.Database/Entities/PartyEntity.cs:10](/Core.Database/Entities/PartyEntity.cs)
  but no map-side helper surfaces it; `PlayerEntity` carries `PartyId`
  only (no leader flag).
- `party_getmemberid` / `party_getavailablesd`: no map-side iterator
  (would need `PartyEntity` in-memory model — PT-H1 wave).
- All ⚠️ rows (`party_created` / `party_recv_info` / `party_member_joined`
  / `party_member_withdraw` / `party_setoption` / `party_optionchanged` /
  `party_recv_movemap` / `party_send_logout`) still wait on PT-H1
  (real `PartyEntity` hydrate) / PT-M2 (broadcast helpers).
- All ❌ rows (`party_recv_noinfo` / `party_invite` / `party_reply_invite`
  / `party_join` / `party_isleader` / `party_getmemberid` /
  `party_getavailablesd` / `party_send_levelup` / `party_skill_check` /
  `party_send_xy_clear` / `party_send_dot_remove`) confirmed still
  missing; tracked across PT-H1/H2/M1/M2 sub-waves.

The aspirational ✅ rows citing stubbed `IntifService` methods
(`party_create`, `party_member_added`, `party_removemember`,
`party_leave`, `party_broken`, `party_changeoption`, `party_changeleader`,
`party_request_info`, `party_send_movemap`) stay as-is — the canonical
entry exists on the interface even though the body doesn't dispatch the
gRPC; demoting those ✅ rows is out of scope for this resync (would
re-open the PT-H1 wave). Flagged for follow-up.

Coverage unchanged: **20 ✅ / 8 ⚠️ / 13 ❌**. No C# code touched.

### 2026-05-25 — Wave 79: party-parity close-out (1 ❌ → ✅; 8 ⚠️ + 13 ❌ genuine gaps remain)

Doc-resync. Audited each ⚠️/❌ row against the C# tree:

**❌ → ✅ promotions (1):**
- `party_sub_count` / `party_sub_count_class` — confirmed functional
  via [`PartyMapService.ForEachOnSameMap`](/Map.Server/Party/PartyMapService.cs#L18)
  + counting lambda. The pattern is in active use across the Acolyte
  skill behaviors (Praefatio, Magnificat, Renovatio, LaudaAgnus,
  LaudaRamus, MedialeVotum) — same shape as the rAthena
  `party_foreachsamemap(p, party_sub_count, …)` idiom. The
  job-id-filtered variant (`party_sub_count_class`) is a one-line
  closure extension.

**Honesty check — no other promotions land:**
- The remaining `party_invite` / `party_reply_invite` / `party_join` /
  `party_recv_info` / `party_recv_noinfo` / `party_member_joined` /
  `party_member_withdraw` / `party_recv_movemap` /
  `party_optionchanged` / `party_setoption` rows still wait on the
  **PT-H1** wave (real `PartyEntity` in-memory hydrate on session-enter)
  and the **PT-H2** wave (`ZC_PARTY_JOIN_REQ` /
  `CZ_PARTY_JOIN_REQ_ACK` / `ZC_ADD_MEMBER_TO_GROUP` packet plumbing).
  The map-side `IntifService` party methods are still stubs returning 0
  ([IntifService.cs:76-84](/Map.Server/Services/Intif/IntifService.cs#L76)),
  so any row whose ✅ claim cites `IntifService.CreateParty` /
  `AddPartyMember` / `LeaveParty` / `ChangePartyLeader` / `BreakParty`
  is currently aspirational — the canonical entry exists but the body
  doesn't dispatch the IPC. (Demoting those ✅ rows is out of scope for
  this resync — flagging here as work for the PT-H1 wave to land for real.)
- `party_isleader` — confirmed deferred ([Convenio.cs:36-38](/Map.Server/Skills/Behaviors/Acolyte/Convenio.cs#L36)
  notes "char-server holds the leader flag and doesn't surface it on
  PlayerEntity yet"). Stays ❌.
- `party_skill_check` — same deferral note in Convenio.cs. Stays ❌.
- `party_send_xy_clear` / `party_send_dot_remove` — partially subsumed
  by the `ZC_NOTIFY_VANISH` visibility broadcast for in-range members,
  but off-screen / mini-map-only party dots still need explicit
  cleanup. Stays ❌.

**Coverage delta:** 19 ✅ / 8 ⚠️ / 14 ❌ → **20 ✅ / 8 ⚠️ / 13 ❌**.

### 2026-05-24 — P2.1 doc-resync close-out (1 stale ❌ → ✅; 8 ⚠️ + 14 ❌ genuine gaps remain)

Audited the party-side files (`Map.Server/Party/*.cs`).
`party_foreachsamemap` flips to ✅ —
[`PartyMapService.ForEachOnSameMap`](/Map.Server/Party/PartyMapService.cs)
exists with range + alive filtering, matching rAthena's
eligibility set. All other ⚠️ rows still wait on the PT-H1 /
PT-H2 / PT-M1 / PT-M2 sub-waves (a real `PartyEntity` model +
client-side invite plumbing); per-row notes refreshed with the
PARITY-REMAINING.md §P2.2 wave citation.

**Coverage delta:** 18 ✅ / 8 ⚠️ / 15 ❌ → **19 ✅ / 8 ⚠️ / 14 ❌**.

### 2026-05-22 — T8.5 initial audit

`party.cpp` was the lone gap identified by the T8 audit pass — every
other `rathena/src/map/*.cpp` had a parity doc except this one (the
existing `party-booking-parity.md` covered only the booking subsystem,
not the larger party lifecycle / share / chat surface).

This doc captures the audit baseline: **18 ✅ / 8 ⚠️ / 15 ❌** across
41 functions. The high-value implementation gap is the
**invite/reply/joined client flow** + a real `PartyEntity` in-memory
model — without these, party operations work only through GM tooling
or intif-direct calls and the client-side party UI is largely inert.

Char-side party RPCs are all ✅ (P1-P8 audits closed them); see
[inter/modules.md](../inter/modules.md#party-int_partycpp).
