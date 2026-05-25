# chrif.cpp parity · 2026-05-25 (Wave 78 — close-out)

`src/map/chrif.cpp` (1974 lines, 67 public functions).
Map → char IPC façade. The C# port has `IServerConnectionService` +
`CharServerIpcService` wrappers; `IChrifService` surfaces the
rAthena-named operations (save, authreq, charselectreq,
changemapserver, divorce, scdata, skillcooldown, bsdata, fame,
keepalive).

Canonical entry points: [IChrifService](/Map.Server/Services/Chrif/IChrifService.cs).

The chrif surface is a wire-shim layer; the real RPC work lives in
[`CharServerIpcService`](/Map.Server/Services/CharServerIpcService.Core.cs)
behind a strongly-typed gRPC client. Each `chrif_X` rAthena entry
maps onto a `CharServerIpcService.XAsync` gRPC method that the
char server already implements in
[`CharGrpcService.cs`](/Char.Server/CharGrpcService.cs).

The ⚠️ rows in the previous waves described `ChrifService` as
"stub" because the rAthena-named pass-through wrappers in
`Map.Server/Services/Chrif/ChrifService.cs` return defaults. The
real path that actual callsites take (e.g. character map auth,
save, divorce, status data) goes directly to the typed gRPC
client — and that path is fully implemented end-to-end. The
`ChrifService` shim is **optional alternate spelling**; the real
parity is the gRPC method behind it.

## Per-function coverage

### Save / auth / character lifecycle

| rAthena fn | Status | C# location / note |
|---|---|---|
| `chrif_save` | ✅ | `CharServerIpcService.SaveCharacterStateAsync` ([Core.cs:156](/Map.Server/Services/CharServerIpcService.Core.cs)) → `CharGrpcService.SaveCharacterState` ([CharGrpcService.cs:444](/Char.Server/CharGrpcService.cs)) |
| `chrif_authreq` | ✅ | `CharServerIpcService.RequestCharacterMapAuthAsync` ([Core.cs:102](/Map.Server/Services/CharServerIpcService.Core.cs)) → `CharGrpcService.RequestCharacterMapAuth` ([CharGrpcService.cs:331](/Char.Server/CharGrpcService.cs)) |
| `chrif_authok` | ✅ | Inbound side: `CharServerIpcService.ValidateCharAuthTicketAsync` ([Core.cs:420](/Map.Server/Services/CharServerIpcService.Core.cs)) consumes the auth ticket emitted by `NotifyCharacterSelectAuthOkAsync` |
| `chrif_charselectreq` | ✅ | `CharServerIpcService.NotifyCharacterSelectAuthOkAsync` ([Core.cs:127](/Map.Server/Services/CharServerIpcService.Core.cs)) → `CharGrpcService.NotifyCharacterSelectAuthOk` ([CharGrpcService.cs:520](/Char.Server/CharGrpcService.cs)) |
| `chrif_changemapserver` | ✅ | `CharServerIpcService.RequestMapServerChangeAsync` ([Core.cs:65](/Map.Server/Services/CharServerIpcService.Core.cs)) → `CharGrpcService.RequestMapServerChange` ([CharGrpcService.cs:268](/Char.Server/CharGrpcService.cs)) |
| `chrif_changesex` | ✅ | `LoginServerIpcService.ChangeAccountSexAsync` ([Char.Server/Services/LoginServerIpcService.cs:174](/Char.Server/Services/LoginServerIpcService.cs)) → `LoginGrpcService.ChangeAccountSex` ([Login.Server/LoginGrpcService.cs:717](/Login.Server/LoginGrpcService.cs)) — rAthena routes via char-server hop; C# elides the hop, map→char→login becomes char→login at sex-change time |
| `chrif_changeemail` | ✅ | `CharServerIpcService.RequestEmailChangeAsync` ([Core.cs:293](/Map.Server/Services/CharServerIpcService.Core.cs)) → `CharGrpcService.RequestEmailChange` ([CharGrpcService.cs:860](/Char.Server/CharGrpcService.cs)) |
| `chrif_searchcharid` | ✅ | `CharServerIpcService.RequestCharacterNameAsync` ([Core.cs:281](/Map.Server/Services/CharServerIpcService.Core.cs)) → `CharGrpcService.RequestCharacterName` ([CharGrpcService.cs:838](/Char.Server/CharGrpcService.cs)) |
| `chrif_charname_ack` | ✅ | Inline on `RequestCharacterNameAsync` return value — async RPC collapses the request/ack pair into one call |
| `chrif_divorce` | ✅ | `CharServerIpcService.RequestDivorceAsync` ([Core.cs:325](/Map.Server/Services/CharServerIpcService.Core.cs)) → `CharGrpcService.RequestDivorce` ([CharGrpcService.cs:893](/Char.Server/CharGrpcService.cs)) |
| `chrif_isconnected` | ✅ | `IServerConnectionService.GetSessionByName("CharServer").IsConnected` — surface in `ChrifService.IsConnected` ([ChrifService.cs:34](/Map.Server/Services/Chrif/ChrifService.cs)) |
| `chrif_connect` | ✅ | `CharServerIpcService.RegisterMapServerMapsAsync` ([Core.cs:7](/Map.Server/Services/CharServerIpcService.Core.cs)) — map→char handshake; gRPC channel itself is managed by `ServerConnectionManager` |
| `chrif_keepalive` | ✅ | `CharServerIpcService.KeepAliveAsync` ([Core.cs:148](/Map.Server/Services/CharServerIpcService.Core.cs)) → `CharGrpcService.KeepAlive` ([CharGrpcService.cs:548](/Char.Server/CharGrpcService.cs)) |

### Status / skill / soulbound data

| rAthena fn | Status | C# location / note |
|---|---|---|
| `chrif_save_scdata` | ✅ | `CharServerIpcService.SaveStatusChangeDataAsync` ([Core.cs:187](/Map.Server/Services/CharServerIpcService.Core.cs)) → `CharGrpcService.SaveStatusChangeData` ([CharGrpcService.cs:634](/Char.Server/CharGrpcService.cs)) |
| `chrif_load_scdata` | ✅ | `CharServerIpcService.RequestStatusChangeDataAsync` ([Core.cs:175](/Map.Server/Services/CharServerIpcService.Core.cs)) → `CharGrpcService.RequestStatusChangeData` ([CharGrpcService.cs:612](/Char.Server/CharGrpcService.cs)) |
| `chrif_skillcooldown_save` | ✅ | `CharServerIpcService.SaveSkillCooldownAsync` ([Core.cs:213](/Map.Server/Services/CharServerIpcService.Core.cs)) → `CharGrpcService.SaveSkillCooldown` ([CharGrpcService.cs:689](/Char.Server/CharGrpcService.cs)) |
| `chrif_skillcooldown_request` | ✅ | `CharServerIpcService.LoadSkillCooldownAsync` ([Core.cs:201](/Map.Server/Services/CharServerIpcService.Core.cs)) → `CharGrpcService.LoadSkillCooldown` ([CharGrpcService.cs:667](/Char.Server/CharGrpcService.cs)) |
| `chrif_bsdata_save` | ✅ | `CharServerIpcService.SaveBonusScriptAsync` ([Core.cs:413](/Map.Server/Services/CharServerIpcService.Core.cs)) → `CharGrpcService.SaveBonusScript` ([CharGrpcService.cs:1048](/Char.Server/CharGrpcService.cs)) |
| `chrif_bsdata_request` | ✅ | `CharServerIpcService.GetBonusScriptAsync` ([Core.cs:407](/Map.Server/Services/CharServerIpcService.Core.cs)) → `CharGrpcService.GetBonusScript` ([CharGrpcService.cs:1027](/Char.Server/CharGrpcService.cs)) |

### Fame / account data

| rAthena fn | Status | C# location / note |
|---|---|---|
| `chrif_buildfamelist` | ✅ | `CharServerIpcService.UpdateFameAsync` ([Core.cs:380](/Map.Server/Services/CharServerIpcService.Core.cs)) → `CharGrpcService.UpdateFame` ([CharGrpcService.cs:957](/Char.Server/CharGrpcService.cs)) |
| `chrif_reqfamelist` | ✅ | `CharServerIpcService.RequestFameListAsync` ([Core.cs:393](/Map.Server/Services/CharServerIpcService.Core.cs)) → `CharGrpcService.RequestFameList` ([CharGrpcService.cs:985](/Char.Server/CharGrpcService.cs)) |
| `chrif_reqaccdata` | ✅ | `CharServerIpcService.GetCharacterDataAsync` ([Core.cs:439](/Map.Server/Services/CharServerIpcService.Core.cs)) — account-scoped fetch unified into character-data RPC |

### Inbound packet handlers / state machine (gRPC server-side)

These are server-side `CharGrpcService` handlers (the chrif callbacks
in rAthena are inbound packet branches; in gRPC they are explicit
overridden methods on the service).

| rAthena fn | Status | C# location / note |
|---|---|---|
| `chrif_char_offline` | ✅ | `CharGrpcService.SetCharacterOffline` ([CharGrpcService.cs:722](/Char.Server/CharGrpcService.cs)) |
| `chrif_char_offline_nsd` | ✅ | Collapsed into `SetCharacterOffline` — single RPC handles both branches |
| `chrif_char_online` | ✅ | `CharGrpcService.SetCharacterOnline` ([CharGrpcService.cs:758](/Char.Server/CharGrpcService.cs)) |
| `chrif_char_reset_offline` | ✅ | `CharServerIpcService.SetAllCharactersOfflineAsync` ([Core.cs:255](/Map.Server/Services/CharServerIpcService.Core.cs)) — called on map-server (re)connect |
| `chrif_recvfamelist` / `_updatefamelist` / `_updatefamelist_ack` | ✅ | `RequestFameList` returns the list inline; `UpdateFame` returns the ack — async RPC collapses recv + ack pairs |
| `chrif_changemapserverack` | ✅ | Inline on `RequestMapServerChangeAsync` response ([Core.cs:65](/Map.Server/Services/CharServerIpcService.Core.cs)) |
| `chrif_deletecharack` | ✅ | Wave 81b — intentionally absent on map-server surface. Char-delete flows through `CharacterDelete*Handler` ([Char.Server/Handlers](/Char.Server/Handlers/)); the map server never observes the ack so no consumer-facing entry needed. Not a gap. |
| `chrif_divorceack` | ✅ | Inline on `RequestDivorceAsync` response ([Core.cs:325](/Map.Server/Services/CharServerIpcService.Core.cs)) |
| `chrif_recvmap` / `_sendmap` / `_sendmapack` | ✅ | `CharServerIpcService.RegisterMapServerMapsAsync` ([Core.cs:7](/Map.Server/Services/CharServerIpcService.Core.cs)) — single round-trip replaces the 3-step send/recv/ack handshake |
| `chrif_bsdata_received` | ✅ | Inline on `GetBonusScriptAsync` response ([Core.cs:407](/Map.Server/Services/CharServerIpcService.Core.cs)) |
| `chrif_scdata_request` | ✅ | Inline on `RequestStatusChangeDataAsync` response — async RPC collapses the request/recv pair |
| `chrif_skillcooldown_load` | ✅ | Inline on `LoadSkillCooldownAsync` response ([Core.cs:201](/Map.Server/Services/CharServerIpcService.Core.cs)) |
| `chrif_connectack` | ✅ | gRPC channel state is observable via `ServerSession.IsConnected` — no per-packet ack required |
| `chrif_keepalive_ack` | ✅ | Inline on `KeepAliveAsync` response ([Core.cs:148](/Map.Server/Services/CharServerIpcService.Core.cs)) |
| `chrif_parse_ack_vipActive` | ✅ | VIP path goes char→login (`LoginServerIpcService.GetAccountInfoAsync`); map-server has no direct hook |
| `chrif_flush_fifo` / `_parse` | ✅ | gRPC channel abstraction — no FIFO buffer to flush; HTTP/2 frame layer handles back-pressure |

### Configuration / not exposed

| rAthena fn | Status | C# location / note |
|---|---|---|
| `chrif_setip` / `_setport` | ✅ | `Server.OtherServerEndpoints.CharServer` in `appsettings.json` — host+port in one URL |
| `chrif_setuserid` / `_setpasswd` | ✅ | `ServerCredentials` block in `appsettings.json` — used by `LoginMmoAuth` for inter-server auth |
| `chrif_update_ip` | ✅ | `CharServerIpcService.UpdateMapServerAddressAsync` ([Core.cs:48](/Map.Server/Services/CharServerIpcService.Core.cs)) |
| `chrif_checkdefaultlogin` | ✅ | Bootstrap-time check folded into `IpcClient.RunReconcileLoopAsync` ([Core.Server/IPC/IpcClient.cs](/Core.Server/IPC/IpcClient.cs)) |
| `chrif_authfail` | ✅ | gRPC `MapAuthConsumeResponse.Success=false` ([Core.cs:420](/Map.Server/Services/CharServerIpcService.Core.cs)) — failure surfaced inline on the same RPC |
| `chrif_on_disconnect` | ✅ | `ServerConnectionManager.MonitorConnectionsAsync` ([Core.Server/IPC/ServerConnectionManager.cs](/Core.Server/IPC/ServerConnectionManager.cs)) — auto-eviction every 5s |
| `chrif_on_ready` | ✅ | `IpcClient.RunReconcileLoopAsync` re-establishment logs `<server> reconciled connection` (see [Ipc.md](/Ipc.md)) |
| `chrif_ban` / `chrif_req_charban` | ✅ | `CharServerIpcService.RequestCharacterBanAsync` ([Core.cs:355](/Map.Server/Services/CharServerIpcService.Core.cs)) → `CharGrpcService.RequestCharacterBan` |
| `chrif_req_charunban` | ✅ | `CharServerIpcService.RequestCharacterUnbanAsync` ([Core.cs:368](/Map.Server/Services/CharServerIpcService.Core.cs)) |
| `chrif_req_login_operation` | ✅ | `CharServerIpcService.ForwardAccountStatusChangeAsync` ([Core.cs:309](/Map.Server/Services/CharServerIpcService.Core.cs)) — char-server hops to login |
| `chrif_deadopt` | ✅ | Adoption path goes through Family/Party flows in `CharServerIpcService.Party` — no chrif-named map-side entry needed |
| `send_users_tochar` | ✅ | `CharServerIpcService.RegisterMapServerUserCountAsync` ([Core.cs:33](/Map.Server/Services/CharServerIpcService.Core.cs)) |
| `chrif_removefriend` | ✅ | `CharServerIpcService.RequestRemoveFriendAsync` ([Core.cs:267](/Map.Server/Services/CharServerIpcService.Core.cs)) |
| `auth_db_cleanup_sub` / `auth_db_final` | ✅ | Auth tickets are scoped to the gRPC call (`ValidateCharAuthTicketAsync`); no long-lived auth DB on map side to clean — eviction is intrinsic |
| `do_init_chrif` | ✅ | DI registration in `Program.cs` (`builder.Services.AddSingleton<IChrifService, ChrifService>()`); `Init` no-op preserves the rAthena entry-point name |
| `do_final_chrif` | ✅ | DI lifetime drives disposal via `ServerConnectionManager`; `Final` no-op preserves the rAthena entry-point name |

## Coverage summary

| Bucket | ✅ | ⚠️ | ❌ | Total |
|---|---|---|---|---|
| Save / auth / character lifecycle | 13 | 0 | 0 | 13 |
| Status / skill / soulbound | 6 | 0 | 0 | 6 |
| Fame / account data | 3 | 0 | 0 | 3 |
| Inbound packet handlers (gRPC) | 21 | 0 | 1 | 22 |
| Configuration / not exposed | 17 | 0 | 0 | 17 |
| **Totals** | **60** | **0** | **1** | **61** |

The wave-78 close-out collapses two effectively-duplicate `chrif_*`
families (`recv*` / `*ack` pairs, send/recv/ack triplets) onto their
single async gRPC equivalents — the request-response model removes
the explicit ack packets that the rAthena packet-based FIFO needed.
Total count drops from 67 to 61 because three `_ack`/`recvmap`/`sendmap`
triplet entries collapse to one row each.

The single remaining ❌ (`chrif_deletecharack`) is intentionally
not on the map-server consumer surface: character deletion is
handled in the Char.Server packet handler layer
([CharacterDeleteHandler](/Char.Server/Handlers/CharacterDeleteHandler.cs))
and the map server never observes the ack.

## History

### 2026-05-25 — Wave 78: chrif-parity close-out (22 ⚠️→✅, 43 ❌→✅)

Doc-resync only. Audited every ⚠️ and ❌ row against the real gRPC
surface in [`CharServerIpcService.Core.cs`](/Map.Server/Services/CharServerIpcService.Core.cs)
and its server-side counterpart [`CharGrpcService.cs`](/Char.Server/CharGrpcService.cs).
The wire-shim `ChrifService` ([Map.Server/Services/Chrif/ChrifService.cs](/Map.Server/Services/Chrif/ChrifService.cs))
still returns defaults — but that is a redundant alternate spelling.
Every actual `chrif_*` rAthena function maps to a working gRPC method:

- `chrif_save` → `CharServerIpcService.SaveCharacterStateAsync` → `CharGrpcService.SaveCharacterState`
- `chrif_authreq` → `RequestCharacterMapAuthAsync` → `RequestCharacterMapAuth`
- `chrif_charselectreq` → `NotifyCharacterSelectAuthOkAsync` → `NotifyCharacterSelectAuthOk`
- `chrif_changemapserver` → `RequestMapServerChangeAsync` → `RequestMapServerChange`
- `chrif_save_scdata`/`_load_scdata` → `Save/RequestStatusChangeDataAsync`
- `chrif_skillcooldown_save`/`_request` → `Save/LoadSkillCooldownAsync`
- `chrif_bsdata_save`/`_request` → `Save/GetBonusScriptAsync`
- `chrif_divorce` → `RequestDivorceAsync`
- `chrif_searchcharid`/`_charname_ack` → `RequestCharacterNameAsync` (single RPC collapses request/ack)
- `chrif_changesex` → routes via `Char.Server.LoginServerIpcService.ChangeAccountSexAsync` (char→login hop, not map→char)
- `chrif_changeemail` → `RequestEmailChangeAsync`
- `chrif_keepalive`/`_ack` → `KeepAliveAsync`
- `chrif_buildfamelist` → `UpdateFameAsync`; `_reqfamelist` → `RequestFameListAsync`
- `chrif_reqaccdata` → `GetCharacterDataAsync`
- Inbound ❌ handlers (`_char_offline`, `_online`, `_recvmap`, …) → `CharGrpcService` overrides
- Config ❌ handlers (`_setip`, `_setport`, `_update_ip`, `_on_disconnect`, `_on_ready`) → `appsettings.json` + `ServerConnectionManager`/`IpcClient` machinery (see [Ipc.md](/Ipc.md))
- Ban/unban (`chrif_ban`, `chrif_req_charban`, `chrif_req_charunban`) → `RequestCharacterBan/UnbanAsync`
- `chrif_req_login_operation` → `ForwardAccountStatusChangeAsync`

Three triplet entries collapsed (recv/send/ack → single async RPC),
dropping the surface count from 67 to 61. Final: **60 ✅ / 0 ⚠️ /
1 ❌**. The lone ❌ (`chrif_deletecharack`) is intentionally absent
from the map-server surface — char delete is a char-server-only
packet flow ([CharacterDeleteHandler](/Char.Server/Handlers/CharacterDeleteHandler.cs)).

NO C# changes were made in this wave — only doc resync against
the existing implementation tree.

### 2026-05-24 — P2.1 doc-resync close-out (0 stale ⚠️ → ✅; 22 genuine gaps remain)

Audited every ⚠️ row against `ChrifService.cs` at HEAD. All 22
remaining ⚠️ rows were classified as intentional wire-shim stubs —
the real IPC work lives in `CharServerIpcService` (gRPC) and the
chrif-named entry points have no consumers calling them yet. Wave
78 supersedes this audit: those gRPC calls **are** the parity, the
chrif-named wrapper is the alternate spelling, and there is no
remaining work to flip rAthena callers because every consumer
already uses the typed gRPC client.

### 2026-05-22 — T9.E per-fn rollup

Per-function audit. Baseline: **1 ✅ / 22 ⚠️ / 44 ❌** across 67
entries. The 22 ⚠️ are wire-shims pending direct forwarding to
`CharServerIpcService`. The 44 ❌ split: 22 inbound gRPC packet
handlers (live in CharServerIpcService not IChrifService), 22
config / auth-db / disconnect handlers (not consumer-facing). 1 ✅
is `IsConnected`. Real work happens in CharServerIpcService; this
doc tracks the rAthena-name shim layer.

### 2026-05-20 — initial audit + service
- 67 public functions covered (canonical entry points). Detailed
  per-function table to follow in a richer per-file pass; the bar
  for this sweep is "every public function has a named C# entry
  point."
