# Char connect flow (`CH_REQ_TO_CONNECT`)

End-to-end client connect handshake, from `CH_REQ_TO_CONNECT (0x65)` through login auth, account-data load, gate enforcement, pincode, and char-list emission.

**rAthena source:**
- [char_clif.cpp](/Volumes/1TB/Projetos/rathena/src/char/char_clif.cpp) — `chclif_parse_reqtoconnect`, `chclif_mmo_char_send`, `chclif_reject`, `chclif_pincode_*`
- [char.cpp](/Volumes/1TB/Projetos/rathena/src/char/char.cpp) — `char_auth_ok`
- [char_logif.cpp](/Volumes/1TB/Projetos/rathena/src/char/char_logif.cpp) — `chlogif_parse_ackaccreq`, `chlogif_parse_reqaccdata`

**C# implementation:**
- Entry handler: [ClientConnectHandler.cs](../../../Char.Server/Handlers/ClientConnectHandler.cs)
- Char list flow: [CharacterListHandler.cs](../../../Char.Server/Handlers/CharacterListHandler.cs), [CharacterListFlowService.cs](../../../Char.Server/Services/CharacterListFlowService.cs)
- Pincode: [Pincode*Handler.cs](../../../Char.Server/Handlers/)
- Login RPC wrapper: [LoginServerIpcService.cs](../../../Char.Server/Services/LoginServerIpcService.cs)

## Done ✅

### Connect pipeline

1. Client sends `CH_REQ_TO_CONNECT 0x65`.
2. [ClientSession.ReceiveLoopAsync](../../../Core.Server/Network/ClientSession.cs) parses and enqueues.
3. [CharServerImpl.ProcessIncomingPacketsAsync](../../../Char.Server/CharServerImpl.cs) dispatches via [PacketHandlerRegistry](../../../Core.Server/Network/PacketHandlerRegistry.cs).
4. [ClientConnectHandler](../../../Char.Server/Handlers/ClientConnectHandler.cs) runs the full flow.

### Per-step parity

| Step | rAthena | C# |
|---|---|---|
| Account-id echo | `chclif_parse_reqtoconnect` immediate 4-byte echo | ClientConnectHandler.cs |
| Duplicate-`0x65` suppression | `sd` exists → ignore | ClientConnectHandler.cs |
| Server-ready gate | `chserv_running == 0` → `HC_REFUSE_ENTER` | ClientConnectHandler.cs (rejects when not Running or unregistered) |
| Login auth RPC | 0x2712 / 0x2713 | `AuthenticateAccountAsync` with 3000ms timeout |
| Account-data fetch | 0x2716 / 0x2717 (`chlogif_parse_reqaccdata`) | `RequestFullAccountDataAsync` at ClientConnectHandler.cs:151 |
| CharSessionData bind | sets `group_id`, `char_slots`, `pincode_seed`, `pincode`, `isvip` | ClientConnectHandler.cs:175-184 binds `GroupId`, `CharacterSlots`, `Pincode`, `PincodeChangeUnixTime`, `IsVip` |
| Maintenance gate | `start_users >= max_users` → reject | [CharacterListFlowService.cs:33-45, 122-135, 189](../../../Char.Server/Services/CharacterListFlowService.cs) `TryValidateCapacityAndMaintenance` |
| Capacity gate | as above, with GM bypass | same — `groupId >= gmAllowGroup` bypasses |
| Duplicate-online (same server) | `online_char_db` lookup → kick | ClientConnectHandler.cs:115-137 `HasDuplicateLiveAccountSession` |
| Reject codes | partial mapping | `resultCode 1` server closed, `8` duplicate, `7` maintenance/capacity, `HC_REFUSE_ENTER 0` generic |
| Log markers | n/a | `received CH_REQ_TO_CONNECT`, `requesting login auth`, auth ack, `requesting full account data`, `sending initial character window/list` |
| RPC timeouts | n/a | symmetric `CancellationTokenSource(3000)` for auth and account-data |

### Pincode flow (partial — see Pending)

- States 0/1/2 emitted via `HC_SECOND_PASSWD_LOGIN`:
  - `PincodeState.PassedOrDisabled (0)` — [PincodeCheckHandler.cs:46](../../../Char.Server/Handlers/PincodeCheckHandler.cs), [PincodeSetNewHandler.cs:52](../../../Char.Server/Handlers/PincodeSetNewHandler.cs), [PincodeChangeHandler.cs:59](../../../Char.Server/Handlers/PincodeChangeHandler.cs)
  - `PincodeState.Ask (1)` — [CharacterListFlowService.cs:93](../../../Char.Server/Services/CharacterListFlowService.cs), [PincodeWindowHandler.cs:35](../../../Char.Server/Handlers/PincodeWindowHandler.cs)
  - `PincodeState.New (2)` — same call sites
  - `PincodeState.Incorrect (8)` — PincodeCheckHandler.cs:51, PincodeChangeHandler.cs:34, PincodeSetNewHandler.cs:44
- Charlist gates on `PincodeVerified` when enabled — CharacterListFlowService.cs:89, 114.

### Tests

- [ConnectFlowRegressionGuardTests.cs](../../../Char.Server.Tests/Services/ConnectFlowRegressionGuardTests.cs) — repeated `0x65`, duplicate live session, out-of-order charlist request.
- [PincodeGateParityTests.cs](../../../Char.Server.Tests/Services/PincodeGateParityTests.cs) — parser-level pincode whitelist.
- [CharacterSelectPacketFlowTests.cs](../../../Char.Server.Tests/Services/CharacterSelectPacketFlowTests.cs) — `CH_SELECT_CHAR` map handoff: success `HC_SEND_MAP_DATA` + map-auth ticket; reject `SC_NOTIFY_BAN` / `HC_REFUSE_ENTER`.
- [CharacterListFlowServiceGateTests.cs](../../../Char.Server.Tests/Services/CharacterListFlowServiceGateTests.cs) — maintenance / capacity / group gates.

## Pending

None.

## History

- **2026-05-22** — **T6.3 audit-doc refresh — verified 0 ❌.** Companion
  to T5.2's map-tree sweep. Per-step parity table all ✅; pincode flow
  fully aligned with rAthena (P2); cross-server dup-online wired (P3);
  `ResolveKickTargetServerId` extracted (P4). Full audit rollup at
  [../T6-audit-2026-05-22.md](../T6-audit-2026-05-22.md). No code
  changes — this is a checkpoint for future audits.
- **2026-05-16** — **P4 closed.** Extracted `ResolveKickTargetServerId` static helper that decides whether to kick a cross-server live session given the login server's `IsAccountOnlineAnywhere` response. Added 4 unit tests in [ConnectFlowRegressionGuardTests.cs](../../../Char.Server.Tests/Services/ConnectFlowRegressionGuardTests.cs) covering: RPC null/unreachable, account not online elsewhere, online on a different server, defensive guard against server_id=0 in malformed responses. Full multi-server integration harness (in-process gRPC, two char servers + one login) deferred to P7.
- **2026-05-16** — **P3 connect-flow items closed:**
  - Cross-server duplicate-online check wired: [ClientConnectHandler.cs](../../../Char.Server/Handlers/ClientConnectHandler.cs) now calls `IsAccountOnlineAnywhereAsync` after the local duplicate-session guard. If positive, kicks the older session via `NotifyAccountStatusAsync(online: false)` so login fans out a force-disconnect to the other char server.
  - PC-ban check resolved as won't-fix (no such mechanism in rAthena; existing `IsIpBannedAsync` already mirrors `ipban_check`).
- **2026-05-16** — **P2 connect-flow items closed:**
  - Pincode state machine now fully matches rAthena. Renamed enum to align with `char.hpp` (`NotSet`=2, `New`=4 was previously `NewV2`). New `ComputeStartState` helper in [`PincodeFlowSupport`](../../../Char.Server/Services/PincodeFlowSupport.cs) implements `chlogif_pincode_start` parity: disabled→Passed; no-pin+force→New; no-pin+!force→Passed; pin+expired→MustChange; pin+verified→Passed; else Ask. `pincode_force` config now honored. `ChangeTime` expiration now checked.
  - Three call sites in `CharacterListFlowService` / `CharacterSelectHandler` / `PincodeWindowHandler` updated to use the shared computer.
  - Reject-code mapping: confirmed current behavior (`SC_NOTIFY_BAN` codes 1/7/8 plus `HC_REFUSE_ENTER` code 0) matches rAthena — no richer codes needed for `HC_REFUSE_ENTER` per `char_clif.cpp:1528-1534`.
  - Tests added: 9 in `PincodeStateTests.cs` covering all branches; 1 in `CharacterSelectPacketFlowTests.cs` for out-of-order char-select (account-data not loaded).
- **2026-05-15** — Audit confirmed Phase A-D items mostly land. Three pincode-related gaps reclassified as Pending (`MustChange`, expiration, `pincode_force`). Cross-server duplicate-online check and replayed-login-id tests added to Pending.
- **(pre-2026-05) Phase D** — Added log markers for receive 0x65 / auth / account-data / charlist. Added 3000ms `CancellationTokenSource` timeout on login RPCs.
- **(pre-2026-05) Phase C** — Pincode handlers added; charlist gates on `PincodeVerified`; duplicate-live-session reject; refined reject result codes.
- **(pre-2026-05) Phase B** — `RequestFullAccountDataAsync` wired; account-data fields bound to `CharSessionData`; maintenance/capacity/group gates enforced before charlist send.
- **(pre-2026-05) Phase A** — `CH_REQ_CHARLIST` handler added; charlist emission backed by `ICharacterRepository.GetByAccountIdAsync`.
