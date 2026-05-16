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

## Pending ⚠️

### Pincode state machine — partial

- **`MustChange` state (3) never emitted.** rAthena sets state 3 when `(now - pincode_change) > pincode_change_interval`. C# loads `PincodeChangeUnixTime` ([ClientConnectHandler.cs:180](../../../Char.Server/Handlers/ClientConnectHandler.cs)) but never compares it to a threshold or emits state 3. Players with expired pincodes are not forced to change.
- **`NewV2` state (4) never emitted.** No code path uses this state value.
- **`pincode_force` config not honored.** rAthena forces a new pincode on accounts with `PINCODE_NOTSET` when this config is true. C# has no check for the config flag.

### Duplicate-online — cross-server gap

- C# only checks the local char server's live sessions ([ClientConnectHandler.cs:213-223](../../../Char.Server/Handlers/ClientConnectHandler.cs) `HasDuplicateLiveAccountSession`). rAthena additionally queries the login server's `online_char_db` to catch the same account online on a *different* char server. Add an RPC to login to query global online state, or have the login server reject the auth at step 4.

### Reject codes — richer mapping deferred

Current mapping (per old Phase C10):

| Reason | Code |
|---|---|
| Server closed | `SC_NOTIFY_BAN = 1` |
| Duplicate session | `SC_NOTIFY_BAN = 8` |
| Maintenance / capacity | `SC_NOTIFY_BAN = 7` |
| Generic hard failure | `HC_REFUSE_ENTER = 0` |

rAthena uses richer `HC_REFUSE_ENTER` codes (2 = already online, 3 = incorrect ID/PW, etc.). Decide whether parity matters for client UX.

### PC-ban check missing

rAthena consults `login_log` for an active PC ban during connect; C# has no equivalent. (See also [../login/status.md](../login/status.md).)

### Test gaps

- **Replayed `LoginId1/LoginId2` on a new TCP connection** — claimed by old Phase D13 but no test exists.
- **Out-of-order sequences beyond charlist** (e.g. char-select before charlist) — only one of several scenarios covered.

## History

- **2026-05-15** — Audit confirmed Phase A-D items mostly land. Three pincode-related gaps reclassified as Pending (`MustChange`, expiration, `pincode_force`). Cross-server duplicate-online check and replayed-login-id tests added to Pending.
- **(pre-2026-05) Phase D** — Added log markers for receive 0x65 / auth / account-data / charlist. Added 3000ms `CancellationTokenSource` timeout on login RPCs.
- **(pre-2026-05) Phase C** — Pincode handlers added; charlist gates on `PincodeVerified`; duplicate-live-session reject; refined reject result codes.
- **(pre-2026-05) Phase B** — `RequestFullAccountDataAsync` wired; account-data fields bound to `CharSessionData`; maintenance/capacity/group gates enforced before charlist send.
- **(pre-2026-05) Phase A** — `CH_REQ_CHARLIST` handler added; charlist emission backed by `ICharacterRepository.GetByAccountIdAsync`.
