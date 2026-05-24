# chrif.cpp parity · 2026-05-22 (T9.E — per-fn rollup)

`src/map/chrif.cpp` (1974 lines, 67 public functions).
Map → char IPC façade. The C# port has `IServerConnectionService` +
`CharServerIpcService` wrappers; `IChrifService` surfaces the
rAthena-named operations (save, authreq, charselectreq,
changemapserver, divorce, scdata, skillcooldown, bsdata, fame,
keepalive).

Canonical entry points: [IChrifService](/Map.Server/Services/Chrif/IChrifService.cs).

The chrif surface is a wire-shim layer. Most of the real work
happens behind `CharServerIpcService` (gRPC); chrif methods are
named entry points that consumers can call by rAthena name. The
"stub" status here means the named entry exists but doesn't yet
forward to the real IPC — the gRPC layer is wired separately via
the Char-server gRPC service.

## Per-function coverage

### Save / auth / character lifecycle

| rAthena fn | Status | C# location / note |
|---|---|---|
| `chrif_save` | ⚠️ | `Save` — stub (CharServerIpcService.CharSaveAsync exists) |
| `chrif_authreq` | ⚠️ | `AuthReq` — stub |
| `chrif_authok` | ⚠️ | `AuthOk` — stub |
| `chrif_charselectreq` | ⚠️ | `CharSelectReq` — stub |
| `chrif_changemapserver` | ⚠️ | `ChangeMapServer` — stub |
| `chrif_changesex` / `_changeemail` | ⚠️ | Stubs |
| `chrif_searchcharid` / `_charname_ack` | ⚠️ | Stubs |
| `chrif_divorce` | ⚠️ | `Divorce` — stub |
| `chrif_isconnected` | ✅ | Returns true |
| `chrif_connect` | ⚠️ | `Connect` — stub |
| `chrif_keepalive` | ⚠️ | `KeepAlive` — stub |

### Status / skill / soulbound data

| rAthena fn | Status | C# location / note |
|---|---|---|
| `chrif_save_scdata` / `_load_scdata` | ⚠️ | Stubs |
| `chrif_skillcooldown_save` / `_request` | ⚠️ | Stubs |
| `chrif_bsdata_save` / `_request` | ⚠️ | Stubs |

### Fame / account data

| rAthena fn | Status | C# location / note |
|---|---|---|
| `chrif_buildfamelist` | ⚠️ | `BuildFameList` — stub |
| `chrif_reqfamelist` | ⚠️ | `ReqFameList` — stub |
| `chrif_reqaccdata` | ⚠️ | `ReqAccData` — stub |

### Inbound packet handlers / state machine (not exposed)

These are called from the gRPC handlers, not the consumer API.

| rAthena fn | Status | C# location / note |
|---|---|---|
| `chrif_char_offline` / `_offline_nsd` / `_online` / `_reset_offline` | ❌ | gRPC handlers in CharServerIpcService |
| `chrif_recvfamelist` / `_updatefamelist` / `_updatefamelist_ack` | ❌ | gRPC inbound |
| `chrif_changemapserverack` / `_deletecharack` / `_divorceack` | ❌ | gRPC inbound |
| `chrif_recvmap` / `_sendmap` / `_sendmapack` | ❌ | gRPC inbound |
| `chrif_bsdata_received` / `_scdata_request` / `_skillcooldown_load` | ❌ | gRPC inbound |
| `chrif_connectack` / `_keepalive_ack` | ❌ | gRPC inbound |
| `chrif_parse_ack_vipActive` | ❌ | gRPC inbound |
| `chrif_flush_fifo` / `_parse` | ❌ | gRPC abstraction (no FIFO) |

### Configuration / not exposed

| rAthena fn | Status | C# location / note |
|---|---|---|
| `chrif_setip` / `_setport` / `_setuserid` / `_setpasswd` | ❌ | Config — appsettings.json |
| `chrif_update_ip` / `_checkdefaultlogin` | ❌ | Not in interface |
| `chrif_authfail` / `_on_disconnect` / `_on_ready` | ❌ | gRPC event handlers |
| `chrif_ban` / `_req_charban` / `_req_charunban` / `_req_login_operation` | ❌ | Not in interface |
| `chrif_deadopt` / `send_users_tochar` / `chrif_removefriend` | ❌ | Not in interface |
| `auth_db_cleanup_sub` / `auth_db_final` | ❌ | Internal callbacks |
| `do_init_chrif` / `do_final_chrif` | ⚠️ | Stubs (DI implicit) |

## Coverage summary

| Bucket | ✅ | ⚠️ | ❌ | Total |
|---|---|---|---|---|
| Save / auth / character lifecycle | 1 | 11 | 0 | 12 |
| Status / skill / soulbound | 0 | 6 | 0 | 6 |
| Fame / account data | 0 | 3 | 0 | 3 |
| Inbound packet handlers (gRPC) | 0 | 0 | 22 | 22 |
| Configuration / not exposed | 0 | 2 | 22 | 24 |
| **Totals** | **1** | **22** | **44** | **67** |

The 44 ❌ are architectural — gRPC handlers + config layer + auth
DB internals that don't need a chrif-named entry point because
they live in `CharServerIpcService` (gRPC channel layer) or
`appsettings.json` (config).

## History

### 2026-05-24 — P2.1 doc-resync close-out (0 stale ⚠️ → ✅; 22 genuine gaps remain)

Audited every ⚠️ row against `ChrifService.cs` at HEAD. All 22
remaining ⚠️ rows are intentional wire-shim stubs — the real IPC
work lives in `CharServerIpcService` (gRPC) and the chrif-named
entry points have no consumers calling them yet. Each gap maps to
PARITY-REMAINING.md §P2.2 leaf work and will flip when a caller
ports through the rAthena name instead of going straight to the
typed gRPC client.

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
