# Login server status

Migration of rAthena's login server (`rathena/src/login/`) to [Login.Server/](../../../Login.Server/).

**Coverage:** ✅ 100%. The original `LoginServerMigrationAnalysis.md` (deleted 2026-05-15) listed many "MISSING" features that are now implemented. The final three knobs (`ip_sync_interval`, `login_get_usercount` colorization, `disable_webtoken_delay`) closed on 2026-05-19.

## Done ✅

### Client authentication packets

| Packet | rAthena | C# handler |
|---|---|---|
| `CA_LOGIN 0x64` (plaintext) | login `parse_login` | [LoginHandler.cs](../../../Login.Server/Handlers/LoginHandler.cs) |
| `CA_LOGIN2 0x1dd` (MD5) | same | [Login2Handler.cs](../../../Login.Server/Handlers/Login2Handler.cs) |
| `CA_LOGIN3 0x1fa` (MD5 + info) | same | [Login3Handler.cs](../../../Login.Server/Handlers/Login3Handler.cs) |
| `CA_LOGIN4 0x27c` (MD5 + MAC) | same | [Login4Handler.cs](../../../Login.Server/Handlers/Login4Handler.cs) |
| `CA_LOGIN_PCBANG 0x277` | same | [LoginPcbangHandler.cs](../../../Login.Server/Handlers/LoginPcbangHandler.cs) |
| `CA_LOGIN_CHANNEL 0x2b0` | same | [LoginChannelHandler.cs](../../../Login.Server/Handlers/LoginChannelHandler.cs) |
| `CA_SSO_LOGIN_REQ 0x825` | same | [SsoLoginHandler.cs](../../../Login.Server/Handlers/SsoLoginHandler.cs) |
| `CA_REQ_HASH 0x1db` | same | [ReqHashHandler.cs](../../../Login.Server/Handlers/ReqHashHandler.cs) (also sends `AC_ACK_HASH 0x1dc`) |
| `CA_EXE_HASHCHECK 0x204` | same | [ExeHashCheckHandler.cs](../../../Login.Server/Handlers/ExeHashCheckHandler.cs) |
| `CA_CONNECT_INFO_CHANGED 0x200` | same | [LoginKeepAliveHandler.cs](../../../Login.Server/Handlers/LoginKeepAliveHandler.cs) |
| OTP auth | OTP path | [OtpAuthHandler.cs](../../../Login.Server/Handlers/OtpAuthHandler.cs) |

### Server response packets

| Packet | C# emission |
|---|---|
| `AC_ACCEPT_LOGIN 0xac4` | LoginMmoAuth success path |
| `AC_REFUSE_LOGIN 0x83e` | LoginMmoAuth failure path |
| `SC_NOTIFY_BAN 0x81` | Various reject paths |
| `AC_ACK_HASH 0x1dc` | ReqHashHandler.cs:15 |

### Char-server registration

Replaces rAthena packets 0x2710 / 0x2711 with gRPC: [CharServerConnectionHandler.cs](../../../Login.Server/Handlers/CharServerConnectionHandler.cs) implements `RegisterCharacterServerAsync` ([login_service.proto:11](../../../Core.Server/Protos/login_service.proto)). Validates server ID, authenticates, registers in `ICharServerRegistry`.

### Inter-server gRPC (replaces rAthena 0x2712-0x2742)

[login_service.proto](../../../Core.Server/Protos/login_service.proto) defines 21 RPCs:
- `AuthenticateAccountForCharServer` (≈ 0x2712)
- `UpdateCharacterServerUserCount` (≈ 0x2714)
- `GetFullAccountData` (≈ 0x2716/0x2717)
- `GetAccountInfo` (≈ 0x2720/0x2721)
- `BanAccount` / `UnbanAccount` (≈ 0x2725 / 0x272a)
- `ChangeAccountSex` (≈ 0x2723)
- `UpdateAccountState` (≈ 0x2724)
- `NotifyAccountStatus` (online/offline, ≈ 0x272b / 0x272c)
- `PushVipData` (≈ 0x2743)
- `GetGlobalAccountRegisters` (≈ 0x2726)
- Pincode / state / VIP updates
- `ForceDisconnectAccount`

Client wrappers: [Login.Server/CharServerIpcService.cs](../../../Login.Server/CharServerIpcService.cs) for login-side push to char servers.

### Security features

| Feature | Implementation |
|---|---|
| IP ban list (with wildcard CIDR) | [LoginSecurityService.cs:14-37](../../../Login.Server/Security/LoginSecurityService.cs) `IsIpBannedAsync` |
| Dynamic IP ban on password failure | LoginSecurityService.cs:59-104 `EnforceDynamicPasswordFailureBanAsync` |
| Expired IP ban cleanup | LoginSecurityService.cs:106-124 `CleanupExpiredIpBansAsync` |
| Session-creation IP gate | [LoginServerImpl.cs:111](../../../Login.Server/LoginServerImpl.cs) |
| DNSBL lookup | [LoginMmoAuth.cs:431-460](../../../Login.Server/UseCase/LoginMmoAuth.cs) `IsDnsBlacklistedAsync` |
| Client hash check (per-group MD5) | LoginMmoAuth.cs:150-177 |
| Password modes | LoginMmoAuth.cs:238-260 plaintext / `MD5(md5key+pw)` / `MD5(pw+md5key)`; mode flags from config |
| Char-server password hashing | LoginMmoAuth.cs:115-119 honors `UseMd5Passwords` |

### Configuration knobs (from [Login.Server/appsettings.json](../../../Login.Server/appsettings.json))

Comprehensive: `IpBan`, `DynamicPassFailureBan*`, `UseDnsbl`, `DnsblServers`, `UseMd5Passwords`, `ClientHashCheck`, `ClientHashNodes`, `AllowedRegistrations`, `RegistrationInterval`, `GroupIdToConnect`, `MinGroupIdToConnect`, `CharactersPerAccount`, VIP config, `UseWebAuthToken`.

## Pending

None — P5 closed the char→map address-sync fan-out by adding `NotifyAddressSync` to `map_service.proto` and wiring the fan-out via `MapServerIpcService.NotifyAddressSyncAsync`.

## History

- **2026-05-22** — **T6.2 audit-doc refresh — verified 0 ❌.** Companion
  to T5.2's map-tree sweep. Re-grepped this doc against the acceptance
  criterion `awk '/^\| / && /❌/'` — no stale rows. All L-H1 / L-M1 /
  L-M2 / L-M3 / L-M4 wave landings (2026-05-19 entry below) are cited
  in the per-feature tables above. Full audit rollup at
  [../T6-audit-2026-05-22.md](../T6-audit-2026-05-22.md). No code
  changes — this is a checkpoint for future audits.
- **2026-05-19** — **Final three parity knobs closed (100%).**
  - `IpSyncInterval` config knob wired into [LoginServerImpl.RequestCharServerAddressSyncAsync](../../../Login.Server/LoginServerImpl.cs); replaces hardcoded 60-second cadence. Mirrors rAthena `loginchrif.cpp:logchrif_sync_ip_addresses` (default 10 min; 0 disables sync). The C# side asks all connected char servers to re-resolve their address via the existing `RequestAddressSync` IPC.
  - `login_get_usercount` colorization (login.cpp:484) ported as [CharServerUserCountClassifier.Classify](../../../Login.Server/UseCase/CharServerUserCountClassifier.cs); the AC_ACCEPT_LOGIN char-server entries now ship the 0-4 status code (green/yellow/red/purple/hidden) that the client renders as a colored dot instead of the raw user count, per PACKETVER ≥ 20170726. 9 unit tests pin the boundary mapping.
  - `disable_webtoken_delay` (account.cpp:935 / 952) wired into [LoginDataRepository.RemoveOnlineUser](../../../Login.Server/Repository/Impl/LoginDataRepository.cs); the web-token disable now runs on a `Scheduler.Schedule` timer that re-checks the in-memory online-user dictionary at fire time and only flushes the SQL update if the account is still offline. Fast disconnect+reconnect inside the window preserves the token. 4 regression tests + a counting test subclass.
- **2026-05-16** — **P3 complete.**
  - Added `IsAccountOnlineAnywhere` RPC to `login_service.proto`. Server-side handler in [LoginGrpcService.cs](../../../Login.Server/LoginGrpcService.cs) queries the existing `OnlineLoginDataDictionary` (LoginDataRepository), excluding the calling char server. This gives any char server a global view of online accounts.
  - Wired the char-side caller into [ClientConnectHandler.cs](../../../Char.Server/Handlers/ClientConnectHandler.cs): after local duplicate check, if the account is online on a *different* char server, ask login to broadcast force-disconnect via `NotifyAccountStatusAsync(online: false)` — matches rAthena's "kick older session" behavior in `char_auth_ok`.
  - **PC-ban check from `login_log` resolved as won't-fix:** rAthena has no such check. The actual rAthena IP-ban gate is `ipban_check` (ipban.cpp:40) which queries the `ipbanlist` table; C# `IsIpBannedAsync` already mirrors this. Plan item was based on a misread of the audit doc.
  - **`RequestAddressSync` (0x2735):** char→login leg already works (TriggerAddressSync). char→map fan-out deferred to P5.
- **2026-05-15** — Migration analysis from the original `LoginServerMigrationAnalysis.md` was largely outdated. Audit confirmed all listed "MISSING" features (char-server registration, IP ban, DNSBL, client hash, inter-server comms, MD5 passwords) are implemented. Original analysis doc deleted; this status doc replaces it.
- **(pre-2026-05)** — Login server packet/auth/security surface brought to ~95% parity. Open work: cross-server account-online sync, PC-ban read, address-sync broadcast.
