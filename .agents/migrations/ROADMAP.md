# Pre-map parity roadmap

**Goal:** reach 1:1 rAthena parity on Login, Char, and the interop surface the Map server will call, **before** any map-server gameplay work begins.

**Out of scope:** map-server gameplay implementation (combat, AI, movement, skill scripts, packets to game clients). That is the next phase after this roadmap completes.

**In scope for map server here:** only **infrastructure-level lifecycle triggers** that the map server must fire regardless of gameplay (TCP connect/disconnect, autosave timer, server startup/shutdown, keepalive). Module triggers like "party leader changed" are gameplay-coupled and deferred.

---

## Phase ordering and dependencies

```
P1 (data integrity) ── independent ───────────────────────┐
P2 (char completeness) ── independent ────────────────────┤
P3 (login completeness) ── independent ───────────────────┤
                                                          │
P4 (cross-server dup-online) ── needs P2 + P3 ────────────┤
P5 (inter-base routing) ── needs map-side stubs ──────────┤
P6 (map→char IPC wiring) ── needs P1 + P2 + P3 + P5 ──────┤
P7 (verification) ── needs all of P1-P6 ──────────────────┘
```

Phases P1, P2, P3 can run in parallel. P4 is a small bridge. P5 needs minimum map-side scaffolding. P6 is the largest. P7 gates the next phase (map gameplay).

---

## P1 — Char data-integrity fixes 🔴

Three silent-failure bugs in `CharGrpcService` that corrupt user state without raising errors. **Do these first** — any time spent on other work risks more bad data on disk.

### Items

1. **`MailSend` drops attachments** — [CharGrpcService.cs:2436-2443](../../Char.Server/CharGrpcService.cs).
   - Decide schema: either add a `payload` blob column to `mail_attachments`, or persist per-item rows mirroring rAthena's `mail_attachments` schema (item_id, amount, refine, card0-3, etc.).
   - Wire the serializer in `MailSend` and the matching reader in `MailGetAttach`.
   - Reference: rAthena `mail_savemessage` in `int_mail.cpp`.

2. **`AuctionBid` doesn't refund the outbid prior bidder** — [CharGrpcService.cs:2618-2672](../../Char.Server/CharGrpcService.cs).
   - When a bid arrives and `auction.BuyerId != 0`, send refund mail to the prior buyer for the prior `Price` before overwriting.
   - Reference: rAthena `mapif_parse_Auction_bid` in `int_auction.cpp` (calls `mail_sendmail` with refund).

3. **`HomunculusLoad` returns no skills** — [CharGrpcService.cs:2958-2976](../../Char.Server/CharGrpcService.cs), mapper at 3556-3574.
   - Query `skill_homunculus` joined on `homun_id`.
   - Extend `ToHomunculusData` to include the skill list.
   - Reference: rAthena `mapif_parse_homunculus_load` in `int_homun.cpp`.

### Acceptance

- Three regression tests in `Char.Server.Tests/Services/CharGrpcServiceParityTests.cs`:
  - `MailSend_WithAttachments_PersistsAndRetrieves`
  - `AuctionBid_WhenOutbiddingExisting_RefundsPriorBidder`
  - `HomunculusLoad_IncludesSkills`
- Each test exercises both the gRPC method and the DB state afterwards.
- Update [inter/modules.md](inter/modules.md) Done section, move items out of Pending, append a History entry.

---

## P2 — Char-server completeness 🟠

Close every remaining gap in the char server that doesn't depend on the map server existing.

### Items

#### Pincode state machine ([char/connect-flow.md](char/connect-flow.md))

1. **Emit `PincodeState.MustChange (3)`** when `(now - PincodeChangeUnixTime) > PincodeChangeInterval`. Wire into [CharacterListFlowService.cs:89-114](../../Char.Server/Services/CharacterListFlowService.cs). Read the interval from config (`Pincode.ChangeTime` in `appsettings.json`).
2. **Honor `pincode_force` config** — when state is `NotSet` AND `Pincode.Force = true`, emit `New (2)` immediately instead of `PassedOrDisabled`.
3. **`NewV2 (4)`** — investigate when rAthena uses this; if unused in target client version, document and mark "won't fix" in History.

#### Connect flow

4. **Richer `HC_REFUSE_ENTER` codes** — map rAthena's full code set in [ClientConnectHandler.cs](../../Char.Server/Handlers/ClientConnectHandler.cs) reject paths. Specifically: 2=already online, 3=incorrect ID/PW, 4=expired. Today everything collapses to 0.
5. **Replayed `LoginId1/LoginId2` test** — add to [ConnectFlowRegressionGuardTests.cs](../../Char.Server.Tests/Services/ConnectFlowRegressionGuardTests.cs): same login pair on a new TCP connection → reject as duplicate auth.
6. **Out-of-order: char-select before charlist** — add a regression test in [CharacterSelectPacketFlowTests.cs](../../Char.Server.Tests/Services/CharacterSelectPacketFlowTests.cs).

#### Server-side stubs ([char/grpc.md](char/grpc.md))

7. **`PartyShareLevel` persistence** — [CharGrpcService.cs:1474-1482](../../Char.Server/CharGrpcService.cs) currently returns success without writing. Persist `party.exp` / `party.item` flags per rAthena `int_party_setoption`.
8. **`UpdateFame` server impl missing** — add an RPC handler that updates `char.fame` and recomputes affected fame list entries. Reference: rAthena `chmapif_parse_updfamelist`.

#### Behavioral divergences ([char/packets.md](char/packets.md))

9. **`CH_KEEP_ALIVE` strictness** — decision: do we relax to ignore account_id (rAthena parity) or keep the stricter check? Document in History either way. If keeping stricter, change classification from "Pending" to "Won't fix — deliberate divergence" with rationale.
10. **Rename burst structure** — verify clients accept the C# `ResendCharacterWindowAsync` burst vs rAthena's `chclif_mmo_char_send`. If not, switch to single-burst path.

### Acceptance

- All Pending items in [char/packets.md](char/packets.md), [char/grpc.md](char/grpc.md), [char/connect-flow.md](char/connect-flow.md) are either Done or explicitly "Won't fix" with documented rationale.
- Test files updated with new regression tests.
- History entries appended to each touched doc.

---

## P3 — Login-server completeness 🟠

### Items

1. **PC-ban check from `login_log`** — [LoginMmoAuth.cs](../../Login.Server/UseCase/LoginMmoAuth.cs). On `CA_LOGIN` success, query `login_log` for active PC ban entries by IP before issuing `AC_ACCEPT_LOGIN`. Reference: rAthena `login_log_check_pcban` (or local equivalent).
2. **Global account-online registry on login server** — needed by P4. Login server already receives `NotifyAccountStatus` from char servers; ensure it's stored in a queryable structure (per-account → char-server-id mapping). Add an RPC `IsAccountOnlineAnywhereAsync(accountId, excludeCharServerId)` to `login_service.proto`.
3. **`RequestAddressSync` broadcast** — [CharGrpcService.cs:4207](../../Char.Server/CharGrpcService.cs) is the receiver; the *sender* lives in login when a char server's address changes. Wire login's char-server-registry to push address updates to all char servers, which then fan out to maps (see P5).

### Acceptance

- Connect attempt from a PC-banned IP is rejected with the rAthena code.
- New RPC `IsAccountOnlineAnywhereAsync` returns correct results across multiple char servers (integration test).
- Address change on char-server reconnect propagates to map servers (integration test, can use mock map server).
- All Pending items in [login/status.md](.agents/migrations/login/status.md) closed; History updated.

---

## P4 — Cross-server duplicate-online 🟡

Bridges P2 + P3. The char server's connect flow ([ClientConnectHandler.cs:213-223](../../Char.Server/Handlers/ClientConnectHandler.cs)) only checks the local server. Now that login (P3) maintains a global online registry, wire the char server to call the new RPC.

### Items

1. **Call `IsAccountOnlineAnywhereAsync`** from `ClientConnectHandler` after local duplicate check, before completing auth continuation.
2. On positive (account online on a different char server), either:
   - Reject this connect with code 8 (already online), OR
   - Kick the existing session via `ForceDisconnectAccountFromCharServersAsync` and continue (decide based on rAthena behavior — it kicks the older session).
3. Integration test with two char-server instances + one login: connect to A, then to B, verify expected behavior.

### Acceptance

- The cross-server case described in [char/connect-flow.md](char/connect-flow.md) Pending is closed.
- Integration test in `Char.Server.Tests/` exercises two-char-server scenario (can use in-process gRPC).

---

## P5 — Inter-base routing 🟡

Make `inter.cpp` primitives actually route. This is char→map fan-out, so it needs a minimum map-side endpoint to receive (which is fine — we're adding the map's gRPC server-side handlers for these pushes, not gameplay).

### Items

#### Char-side fan-out

1. **`InterBroadcast 0x3000`** — fan out to all connected map servers via `ServerConnectionManager.GetSessionsByType(ServerType.Map)`. Each call invokes a new map-side RPC `ReceiveBroadcastAsync(message)`. Reference: rAthena `mapif_broadcast`.
2. **`InterBroadcastItem 0x3009`** — same pattern, separate map RPC `ReceiveItemBroadcastAsync(itemMessage)`.
3. **`InterWhisper 0x3001`** — directed routing. Char server tracks which map server holds the recipient (from `SetCharacterOnline` lookups via `IMapServerRegistryService`). Forward to that map only; if not online, return the appropriate ack via `InterWhisperReply 0x3002`.
4. **`InterWhisperToGm 0x3003`** — fan out to all maps; each map filters by GM group.
5. **`InterNameChange 0x3006`** — remove the TODO. Implement DB update on `char` / `pet` / `homunculus` and broadcast the new name to all maps.

#### Map-side receivers (new — not the gameplay, just the RPC entry points)

6. Define new RPCs in [map_service.proto](../../Core.Server/Protos/map_service.proto): `ReceiveBroadcast`, `ReceiveItemBroadcast`, `ReceiveWhisper`, `ReceiveWhisperToGm`, `NotifyNameChange`.
7. Add `MapGrpcService` handlers for each. For now these can log + queue; map gameplay will pick up the queue and emit to clients later.

### Acceptance

- All Pending items in [inter/base.md](inter/base.md) closed.
- Integration test: char broadcasts → all connected map stubs receive.
- Integration test: char whispers to recipient on map X → only map X receives.

---

## P6 — Map → Char IPC wiring 🟢

The largest phase. Wire the map server's **infrastructure-level lifecycle** events to the corresponding char RPCs. Module-level wrappers exist as no-trigger stubs for the future gameplay phase.

### Items

#### Map-side service layer

1. **Audit and complete [Map.Server/Services/CharServerIpcService.cs](../../Map.Server/Services/)** — ensure typed wrapper methods exist for every RPC in [char_service.proto](../../Core.Server/Protos/char_service.proto). Method signatures take strongly-typed args and return strongly-typed responses; channel selection hidden inside.

#### Infrastructure-level triggers

These fire from map server lifecycle, NOT from gameplay logic:

2. **On map server startup** ([Map.Server/MapServerImpl.cs](../../Map.Server/MapServerImpl.cs) `OnStartingAsync`):
   - Call `RegisterMapServerMaps` with the map list this server hosts (from config).
   - Start a periodic `KeepAlive` timer (e.g. every 10 s).
   - Start a periodic `RegisterMapServerUserCount` timer (every 10 s).

3. **On player TCP connect post-auth-ticket** (map handler for player session ready):
   - `SetCharacterOnline(charId, accountId)`
   - `LoadSkillCooldown(charId)` → stash result on session
   - `RequestStatusChangeData(charId)` → stash result on session
   - `GetBonusScript(charId)` → stash on session

4. **On player TCP disconnect** (map session teardown):
   - `SaveCharacterState(charId, position, status, ...)`
   - `SaveSkillCooldown(charId, cooldowns from session)`
   - `SaveStatusChangeData(charId, scdata from session)`
   - `SaveBonusScript(charId, ...)`
   - `SetCharacterOffline(charId, accountId)`

5. **On map server graceful shutdown** (`OnStoppingAsync`):
   - Save all online characters (batch `SaveCharacterState` for each)
   - Call `SetAllCharactersOffline(serverId)`.

6. **Autosave timer**:
   - Every `AutosaveInterval` seconds, iterate online sessions and call `SaveCharacterState` for each. Reference: rAthena `chrif_save` autosave loop.

7. **Map-change handoff** (when a player warps to a map on a different map server):
   - Call `RequestMapServerChange` to get a fresh auth ticket.
   - Notify the destination map via `NotifyCharacterSelectAuthOk` patterns (or via the existing ticket flow).

#### Map-side handlers for char→map pushes (from P5 + others)

8. Implement `MapGrpcService` handlers added in P5 (`ReceiveBroadcast`, etc.).
9. Add `MapGrpcService.ForceDisconnectAccountAsync` handler so login/char can kick players from the map session (used by `ForceDisconnectAccount` flow today).

#### Module-RPC wrappers (no triggers — for gameplay phase)

10. Ensure typed wrappers exist for: all party, guild, storage, mail, auction, quest, achievement, pet, homunculus, mercenary, elemental, clan RPCs. Wrappers compile, do nothing until gameplay calls them.

### Acceptance

- Integration test: player connects → char server sees `online = 1`, cooldowns loaded, scdata loaded. Player disconnects → char server sees `online = 0`, position persisted, cooldowns persisted.
- Integration test: map server startup → char server's `IMapServerRegistryService` reflects the new map list. Map server shutdown → all that server's chars marked offline.
- Integration test: autosave fires → DB reflects current position for online chars.
- All HIGH items in [map/ipc-integration.md](map/ipc-integration.md) closed; MEDIUM and LOWER items have wrappers but no triggers (documented).

---

## P7 — End-to-end verification 🟢

### Items

1. **Cross-server integration suite**: spin up Login + Char + Char (two char servers) + 2 mock map servers in-process. Exercise:
   - Two players connecting concurrently across both char servers.
   - Account online on Char A → connect attempt on Char B is rejected (P4).
   - Map server crash mid-session → char server detects via health check, marks affected chars offline within 15 s.
   - Char server restart → state recovered from DB; online flags reconcile.
2. **Soak / stress**: 1000 concurrent connections through login → char → mock map for 1 hour. No connection leaks, no DB deadlocks, no orphaned `online = 1` rows.
3. **Doc sweep**: each migration doc's Pending section is empty (or explicitly "Won't fix" with rationale). README status table updated.

### Acceptance

- All seven .agents/migrations/ docs have empty (or annotated) Pending sections for Login/Char/Interop scope.
- A milestone tag `pre-map-parity-complete` on main.

After P7 lands, the next phase is map-server gameplay — combat, skills, AI, NPCs. Those will exercise the Module RPC wrappers added in P6.10.

---

## Tracking conventions

- When you start a phase item, create a branch named after it (e.g. `p1/mail-attachments`).
- When you finish an item, do these in the same commit:
  1. Move the line from Pending → Done in the relevant `.agents/migrations/*/...md`.
  2. Append a History entry with date + 1-line summary.
  3. Add or update the regression test.
- When you discover a new gap during a phase, add it to the appropriate doc's Pending list with a `(found YYYY-MM-DD)` tag, then decide whether it blocks the current phase or rolls into a later one.

## Things explicitly NOT in this roadmap

- Map server gameplay loop (combat, movement, AI, NPCs, skill scripts, item effects, quests, drop rates, etc.).
- Web server feature parity (the Web server is a thin REST API; its rAthena equivalent doesn't exist).
- Client compatibility testing across multiple Ragnarok client versions.
- Performance tuning beyond what's needed for the P7 soak test.

## History

- **2026-05-16** — **P3 complete.** Login server completeness closed. `IsAccountOnlineAnywhere` RPC + char-side cross-server duplicate check wired. PC-ban "missing" claim resolved as misread (rAthena has no such mechanism; existing IP ban check matches). char→map address-sync fan-out folded into P5 since maps need new gRPC receivers anyway. Tests for the multi-server scenarios deferred to P4 which has the harness. Next: P4 (cross-server dup-online integration test).
- **2026-05-16** — **P1 and P2 complete.** Char-server side now ~100% rAthena parity (modulo a few stubs deferred to P6 map wiring). Test suite 129/129. See per-doc Histories for specifics. Next: P3 (Login completeness).
- **2026-05-15** — Roadmap created. Sequenced after audit found Map.Server invokes only 2 of 118 char RPCs; user directive is to complete Login + Char + Interop (incl. map→char) before any map gameplay work.
