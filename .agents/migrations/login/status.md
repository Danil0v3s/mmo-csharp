# Login server status

Migration of rAthena's login server (`rathena/src/login/`) to [Login.Server/](../../../Login.Server/).

**Coverage:** ✅ ~95%. The original `LoginServerMigrationAnalysis.md` (deleted 2026-05-15) listed many "MISSING" features that are now implemented.

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

## Pending ⚠️

### char→map address-sync fan-out (deferred to P5)

When a char server's address changes, rAthena's 0x2b1e propagates to all map servers (chained off 0x2735 from login). C# currently has the char→login leg working ([CharGrpcService.cs:4418-4427](../../../Char.Server/CharGrpcService.cs) calls `TriggerAddressSync()`), but the char→map fan-out needs a new `map_service.proto` RPC plus a map-side receiver — folded into P5 (inter-base routing) since maps need similar receivers for broadcast/whisper.

## History

- **2026-05-16** — **P3 complete.**
  - Added `IsAccountOnlineAnywhere` RPC to `login_service.proto`. Server-side handler in [LoginGrpcService.cs](../../../Login.Server/LoginGrpcService.cs) queries the existing `OnlineLoginDataDictionary` (LoginDataRepository), excluding the calling char server. This gives any char server a global view of online accounts.
  - Wired the char-side caller into [ClientConnectHandler.cs](../../../Char.Server/Handlers/ClientConnectHandler.cs): after local duplicate check, if the account is online on a *different* char server, ask login to broadcast force-disconnect via `NotifyAccountStatusAsync(online: false)` — matches rAthena's "kick older session" behavior in `char_auth_ok`.
  - **PC-ban check from `login_log` resolved as won't-fix:** rAthena has no such check. The actual rAthena IP-ban gate is `ipban_check` (ipban.cpp:40) which queries the `ipbanlist` table; C# `IsIpBannedAsync` already mirrors this. Plan item was based on a misread of the audit doc.
  - **`RequestAddressSync` (0x2735):** char→login leg already works (TriggerAddressSync). char→map fan-out deferred to P5.
- **2026-05-15** — Migration analysis from the original `LoginServerMigrationAnalysis.md` was largely outdated. Audit confirmed all listed "MISSING" features (char-server registration, IP ban, DNSBL, client hash, inter-server comms, MD5 passwords) are implemented. Original analysis doc deleted; this status doc replaces it.
- **(pre-2026-05)** — Login server packet/auth/security surface brought to ~95% parity. Open work: cross-server account-online sync, PC-ban read, address-sync broadcast.
