# Char Connect Flow Migration Plan (CH_REQ_TO_CONNECT parity)

Source of truth:
- `rathena/src/char/char_clif.cpp` (`chclif_parse_reqtoconnect`, `chclif_mmo_char_send`, `chclif_reject`)
- `rathena/src/char/char.cpp` (`char_auth_ok`)
- `rathena/src/char/char_logif.cpp` (`chlogif_parse_ackaccreq`, `chlogif_parse_reqaccdata`)

Target:
- `mmo-csharp` end-to-end char connect flow from `CH_REQ_TO_CONNECT` through auth, account-data gating, and charlist/selection handoff.

## Scope

This document traces every current path starting at `CH_REQ_TO_CONNECT` and defines the remaining migration units to reach rAthena parity.

In-scope entrypoint:
- Client packet `CH_REQ_TO_CONNECT (0x65)` in `Core.Server/Packets/In/CH/CH_REQ_TO_CONNECT.cs`
- Handler `Char.Server/Handlers/ClientConnectHandler.cs`

## 1. Current C# flow trace (as implemented)

### 1.1 Dispatch pipeline to handler
1. Client sends packet `0x65`.
2. `ClientSession.ReceiveLoopAsync` parses header/body and enqueues `IncomingPacket`.
3. `CharServerImpl.ProcessIncomingPacketsAsync` calls `PacketHandlerRegistry.ProcessSessionPacketsAsync`.
4. `PacketHandlerRegistry` resolves `ClientConnectHandler` via DI and invokes `HandleAsync`.

Failure behavior around dispatch:
- No handler for header => disconnect with `UnhandledPacket`.
- Handler throws => disconnect with `PacketHandlerError`.

### 1.2 `ClientConnectHandler` branch map
Current handler behavior in `Char.Server/Handlers/ClientConnectHandler.cs`:

1. Immediately sends 4-byte account-id echo to client.
2. If session already has `AccountId`:
   - Logs duplicate `CH_REQ_TO_CONNECT`.
   - Returns (no additional auth call).
3. Pre-binds session fields from packet:
   - `AccountId`, `LoginId1`, `LoginId2`, `Sex`, `IsAuthenticated=false`.
4. Server readiness gate:
   - If char server is not `Running` or not registered to login server:
   - Sends `HC_REFUSE_ENTER` (`ErrorCode=0`) and disconnects.
5. Calls login IPC:
   - `ILoginServerIpcService.AuthenticateAccountAsync(...)`.
6. If auth fails / null response:
   - Sends `HC_REFUSE_ENTER` (`ErrorCode=0`) and disconnects.
7. If auth succeeds:
   - Binds response fields (`AccountId`, `LoginId1`, `LoginId2`, `Sex`, `ClientType`).
   - Sets `IsAuthenticated=true`.
   - No charlist/accept-enter packets are sent here.

### 1.3 Login-server side auth path (`AuthenticateAccountForCharServer`)
1. Char server RPC hits `LoginGrpcService.AuthenticateAccountForCharServer`.
2. Login server consumes auth node (`TryConsumeAuthNode`).
3. On success:
   - Marks account online on requested char server (`SetOnlineUserCharServer`).
   - Returns success with auth/session metadata.
4. On failure:
   - Returns failed auth response (`Success=false`).

### 1.4 Post-auth behavior currently present/missing
Present:
- Session is authenticated in-memory.
- Account online state is tracked in login server online DB via auth consume path.

Missing:
- No account-data fetch/load step equivalent to rAthena `0x2717` path.
- No `HC_ACCEPT_ENTER`/charlist burst on successful auth.
- No pincode gate start/verification chaining.
- `CH_REQ_CHARLIST` handler is missing (no handler registered for `0x9a1`), so client can be disconnected by unhandled packet after connect depending on client behavior.

## 2. rAthena reference flow trace (0x65)

## 2.1 Connect/auth flow
1. `chclif_parse_reqtoconnect` reads `account_id/login_id1/login_id2/sex`.
2. If session already has auth data (`sd` exists), packet is ignored.
3. Creates `char_session_data` and pre-binds account/login ids.
4. Sends immediate account-id echo (4 bytes) to client.
5. If char server not running => `HC_REFUSE_ENTER`.
6. Auth source decision:
   - If auth node is already in char auth DB (map-change path) => `char_auth_ok` immediately.
   - Else asks login server (`0x2712`) and waits for ack (`0x2713`).
7. On login ack success => `char_auth_ok`.
8. On login ack failure => `HC_REFUSE_ENTER`.

## 2.2 `char_auth_ok` continuation
1. Performs duplicate-online checks (already online / pending select session).
2. Requests full account data from login server (`0x2716` -> `0x2717`).
3. Marks char session as authenticated/charselect state.
4. Final continuation occurs when account data returns:
   - Enforces max-user/maintenance/gm-group gates.
   - Sends char window/list payloads (`chclif_mmo_char_send`):
     - includes `HC_ACCEPT_ENTER` (`0x6b`) and related modern packets.
   - Starts pincode flow if enabled.

## 3. Parity gap matrix from `CH_REQ_TO_CONNECT`

- [x] Immediate account-id echo after `0x65`.
- [x] Duplicate `0x65` suppression per session.
- [x] Server-ready rejection path (`HC_REFUSE_ENTER`) for startup/unregistered state.
- [x] Login-server auth request + reject on failure.
- [ ] Char-local authdb fast-path equivalent (for same-charserver or map transition edge cases).
- [ ] Duplicate-online enforcement before finishing char auth (rAthena `char_auth_ok` checks).
- [ ] Explicit account-data request/response continuation step before char window emission.
- [ ] Full char window/list packet emission parity (`HC_ACCEPT_ENTER` + modern companion packets).
- [ ] Pincode start/check/set/change flow wiring from connect success branch.
- [ ] `CH_REQ_CHARLIST` handler path (`0x9a1`) to avoid unhandled disconnect and match client request-driven list behavior.
- [ ] Structured auth timeout/retry/error-code mapping parity (current rejects always use `HC_REFUSE_ENTER` code `0`).

## 4. Known branch outcomes today

Starting from `CH_REQ_TO_CONNECT`, current outcomes are:

1. Duplicate request on same session:
   - Account-id echo sent, then packet ignored.
2. Char server not ready:
   - Account-id echo, `HC_REFUSE_ENTER`, disconnect.
3. Login IPC unavailable / auth miss:
   - Account-id echo, `HC_REFUSE_ENTER`, disconnect.
4. Auth success:
   - Session authenticated in memory.
   - No success packet burst is emitted at this stage.
   - Next client packet behavior is partially undefined because `CH_REQ_CHARLIST` is unhandled.

## 5. Ordered migration plan (from here)

Follow one migration unit per commit.

### Phase A: Stabilize post-auth client progression
1. [x] Add `CH_REQ_CHARLIST` handler with explicit auth checks.
2. [x] On valid authenticated `CH_REQ_CHARLIST`, send current equivalent list packets (`HC_CHARACTER_LIST`/`HC_CHARLIST_NOTIFY`/`HC_ACK_CHARINFO_PER_PAGE`) in one consistent versioned path. (Now backed by `ICharacterRepository.GetByAccountIdAsync`; char slots still use session/fallback until account-data parity lands.)
3. [ ] Add integration tests for:
   - unauthenticated charlist request => reject/disconnect policy
   - authenticated charlist request => expected packet sequence

### Phase B: Add missing `char_auth_ok` continuation semantics
4. [x] Add account-data load step after auth success (`RequestFullAccountDataAsync`) and bind fields into `CharSessionData` (`GroupId`, slots, pincode, vip metadata).
5. [x] Enforce maintenance/max-user/group gates after account-data load, before charlist send.
6. [x] Trigger charlist/accept-enter emission only after step 4/5 success. (Current C# equivalent auto-triggers charlist payload emission after connect auth/account-data; `CH_REQ_CHARLIST` reuses the same gated flow service.)
7. [x] Add targeted tests for each gate (maintenance/full server/group restrictions). (`Char.Server.Tests/Services/CharacterListFlowServiceGateTests.cs`)

### Phase C: Pincode and duplicate-online parity
8. [x] Wire pincode state machine start from successful post-auth continuation. (Initial parity: `HC_SECOND_PASSWD_LOGIN` state packet + handlers for `CH_REQ_PINCODE_WINDOW`/`CH_PINCODE_CHECK`/`CH_PINCODE_SETNEW`/`CH_PINCODE_CHANGE`; charlist flow now gates on `PincodeVerified` when pincode is enabled.)
9. [x] Align duplicate-online checks with login/char online-db semantics before final auth success. (Initial parity: `CH_REQ_TO_CONNECT` now rejects duplicate live account sessions on the same char server before completing auth continuation.)
10. [x] Normalize reject result-code mapping (avoid always returning error code 0 where parity expects richer reasons). (Initial mapping: server closed -> `SC_NOTIFY_BAN=1`, duplicate session -> `SC_NOTIFY_BAN=8`, maintenance/capacity -> `SC_NOTIFY_BAN=7`; generic hard failures still use `HC_REFUSE_ENTER=0`.)

### Phase D: Hardening and observability
11. [x] Add connect-flow metrics/log markers:
    - receive `0x65`, auth request, auth ack, account-data ack, charlist sent.
12. [x] Add timeout + cancellation boundaries for login RPC calls in connect flow.
13. [x] Add regression tests for repeated `0x65`, replayed login ids, and out-of-order packet sequences. (`Char.Server.Tests/Services/ConnectFlowRegressionGuardTests.cs`)

## 6. Immediate next migration unit

Recommended next unit:
- Implement `CH_REQ_CHARLIST` handler and explicit post-auth character-list packet emission path.

Reason:
- It is the first user-visible continuation after `CH_REQ_TO_CONNECT` success.
- Current missing handler can cause disconnects on normal client behavior.
- It creates a stable baseline before adding deeper account-data and pincode parity.
