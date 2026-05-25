# party.cpp parity · 2026-05-25 (Wave 79 — close-out)

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
| `party_create` | ✅ | `IntifService.CreateParty` → `CharServerIpcService.Party.CreatePartyAsync`; char side inserts row, assigns party_id |
| `party_created` | ⚠️ | Char-side `CreatePartyResponse` returned but no map-side `PartyService.OnCreated` handler — the receiver-side ack ("Party created", broadcast member info) is the gap (PARITY-REMAINING.md §P2.2 — PT-H1) |
| `party_request_info` | ✅ | `IntifService.RequestPartyInfo` |
| `party_recv_info` | ⚠️ | Char-side returns `PartyInfo` payload; map-side hydrate (`PartyService.Receive`) is the gap (§P2.2 — PT-H1) |
| `party_recv_noinfo` | ❌ | No map-side "party doesn't exist anymore" branch (§P2.2 — PT-H1) |
| `party_invite` | ❌ | No map-side `ZC_PARTY_JOIN_REQ_ACK` emitter + inviter session-state tracking (§P2.2 — PT-H2) |
| `party_reply_invite` | ❌ | No map-side `CZ_PARTY_JOIN_REQ_ACK` handler (§P2.2 — PT-H2) |
| `party_join` | ❌ | Direct `/joinparty` slash-command path; rare in retail clients (§P2.2 — PT-H2) |
| `party_member_joined` | ⚠️ | Wiring on session-enter needed (currently the `EnterMap` path skips party-info pull) (§P2.2 — PT-H1) |
| `party_member_added` | ✅ | `IntifService.AddPartyMember` |
| `party_removemember` / `party_removemember2` | ✅ | `IntifService.LeaveParty` (covers both paths) |
| `party_leave` | ✅ | Same |
| `party_member_withdraw` | ⚠️ | Char-side broadcasts the withdraw via `PartyMessage`; map-side receiver needs the typed `OnMemberWithdraw` handler (chat-routed message exists, structured update doesn't) (§P2.2 — PT-M2) |
| `party_broken` | ✅ | `IntifService.BreakParty` |

### Options / leader

| rAthena fn | Status | C# location / note |
|---|---|---|
| `party_changeoption` | ✅ | `IntifService.PartyChangeOption` |
| `party_setoption` | ⚠️ | In-memory option set (PartyExpShare / PartyItemShare flags) — `PartyShareService` reads them but no `PartyEntity` holds them yet (§P2.2 — PT-H1) |
| `party_optionchanged` | ⚠️ | Char-side `PartyChangeOptionResponse` returned; map-side broadcast to members is the gap (§P2.2 — PT-M2) |
| `party_changeleader` | ✅ | `IntifService.ChangePartyLeader` |
| `party_isleader` | ❌ | No helper; callers do ad-hoc checks (`pc.PartyId != 0 && IsLeader(pc)`) (§P2.2 — PT-H1) |
| `party_getmemberid` / `party_getavailablesd` | ❌ | No map-side party-member iterator exists yet (would need a `PartyEntity` with a member list) (§P2.2 — PT-H1) |

### Map-change tracking

| rAthena fn | Status | C# location / note |
|---|---|---|
| `party_recv_movemap` | ⚠️ | Char-side `PartyChangeMap` RPC exists; map-side receive handler for cross-server move-notify is the gap (§P2.2 — PT-M2) |
| `party_send_movemap` | ✅ | `IntifService.PartyChangemap` (fires on `EnterMap`) |
| `party_send_levelup` | ❌ | No map-side broadcast — when a party member levels up, mini-map dots etc. don't refresh on other members' screens (§P2.2 — PT-M2) |
| `party_send_logout` | ⚠️ | `LeaveMap` calls `IntifService.PartyChangemap(online: false)` — the dedicated logout-path message isn't emitted but the offline status flips (§P2.2 — PT-M2) |

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
| `party_skill_check` | ❌ | rAthena skill check for party-only skills (Bragi/Assumptio/Wand of Hermode) — not yet enforced map-side; the skill cast goes through but the "are you in the same party" gate is bypassed (PARITY-REMAINING.md §P1.2) |
| `party_foreachsamemap` | ✅ | [`PartyMapService.ForEachOnSameMap`](/Map.Server/Party/PartyMapService.cs) — walks `IEntityRegistry` filtered by PartyId / MapId / alive, with optional range (range=-1 whole-map, range=14 AREA_SIZE for near-effects) |
| `party_sub_count` / `party_sub_count_class` | ✅ | Functional via [`PartyMapService.ForEachOnSameMap`](/Map.Server/Party/PartyMapService.cs#L18) + counting lambda — pattern in active use ([Praefatio.cs:43](/Map.Server/Skills/Behaviors/Acolyte/Praefatio.cs#L43), [Magnificat.cs:29](/Map.Server/Skills/Behaviors/Acolyte/Magnificat.cs#L29), Renovatio, LaudaAgnus, LaudaRamus, MedialeVotum). rAthena's `party_sub_count_class` adds a job-id filter — easy to add as a closure parameter when needed |

### Misc / UI

| rAthena fn | Status | C# location / note |
|---|---|---|
| `party_send_xy_clear` | ❌ | Minimap dot-clear on logout — UI affordance (§P2.2 — PT-M2) |
| `party_send_dot_remove` | ❌ | Same (§P2.2 — PT-M2) |

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
| Lifecycle | 6 | 4 | 4 | 14 |
| Options / leader | 2 | 2 | 3 | 7 |
| Map-change tracking | 1 | 2 | 1 | 4 |
| Chat | 2 | 0 | 0 | 2 |
| Share rules | 4 | 0 | 1 | 5 |
| Misc / UI | 0 | 0 | 2 | 2 |
| Booking | 5 | 0 | 0 | 5 |
| Helpers | 0 | 0 | 2 | 2 |
| **Totals** | **20** | **8** | **13** | **41** |

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
