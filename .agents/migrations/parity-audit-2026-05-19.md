# Login + Char parity audit · 2026-05-19

Full scan of `Login.Server/` and `Char.Server/` against rAthena `src/login/`,
`src/char/`, and their `conf/*_athena.conf` files. Excludes items closed
earlier in this session (IpSyncInterval, login_get_usercount, disable_webtoken_delay
on the Login side; mail return/delete timers, clan-inactive cleanup,
mail-retrieve gate, allowed-job-flag, char-rename party/guild, guild-exp-rate
on the Char side).

Findings are grouped by impact. **High** = user-visible / data-loss
potential. **Medium** = parity drift that won't break gameplay today but
matters for correctness once the dependent feature ships. **Low** =
ergonomic / operator-side.

---

## LOGIN

### High

**L-H1. `login_online_data_cleanup` timer (login.cpp:201, 600 s interval).**
rAthena scans `online_db` every 10 min and removes entries whose
`char_server == -2` (a char server crashed without notifying). C# `LoginDataRepository`
has `RemoveOnlineUsersByCharServer` but no periodic sweep — accounts of
players on a crashed char server stay marked "online forever" until login
restarts. Wire into `LoginServerImpl.UpdateGameLogicAsync`.

### Medium

**L-M1. VIP timeout automatic downgrade (login.cpp:93 `vip_timeout_tid`).**
rAthena schedules a timer when an account's `vip_time` is in the future;
when it fires, the account's group is downgraded. C# has
`VipConfiguration` + `RequestVipData` RPC but no timer service. When a
player's VIP expires while they're online, their group never refreshes.

**L-M2. `allowed_regs` + `time_allowed` registration rate limiting**
(login.cpp:1093, `auth_new` path). rAthena enforces "max N new accounts
per IP per T seconds". C# config has both knobs (default 3 / 3600 s) but
`LoginMmoAuth.cs` never consults them. Wire a per-IP counter cache and
reject when the threshold trips.

**L-M3. `client_hash_check` linked-list enforcement (login.hpp:108–111).**
Config declares `ClientHashCheck` (int) + `ClientHashNodes` (list of
{groupId, md5Hash}). rAthena enforces the right hash per group; C#
`ExeHashCheckHandler.cs` stores the client hash but never cross-checks
against the node list at auth time.

**L-M4. `loginlog.rcode` differentiation (loginlog.cpp:63).**
rAthena writes `rcode = 0` for bad password, `1` for blocked/banned,
`100` for OK. C# `LoginSecurityService.LogLoginAttemptAsync` accepts a
result code but most failure paths pass generic values — the differentiation
is sometimes lost. Audit each call site; ensure 0/1/100 are the values used.

### Low

**L-L1. `bind_ip` (login.cpp:607) — selective interface binding.**
rAthena lets the operator pin the listen socket to a specific NIC.
C# binds to `IPAddress.Any`. Knob isn't declared in C# config. Trivial
to add; default keeps current behavior.

**L-L2. Console commands (logincnslif.cpp:45–99).**
`server:shutdown`, `server:alive`, `server:reloadconf`, interactive
account creation. C# `Console` config flag is declared but no stdin
loop exists. Skipping unless operations need it.

---

## CHAR

### High

**C-H1. Starter items not inserted into inventory on character create.**
`CharServerConfiguration.StartItems` is parsed from config (default
includes Knife×1 + Cotton_Shirt×1). rAthena `make_new_char_sql`
inserts each starter item into the new char's inventory. C#
`CharacterCreateHandler` skips this entirely — every newly-created
character starts with an empty bag.

**C-H2. Hotkey load/save never wired.**
`hotkey` table exists in DB; `CharGrpcService` has no `LoadHotkeys` /
`SaveHotkeys` RPC handler even though the schema is there. Map server
can't restore the player's hotkey bar across sessions — data-loss-adjacent.

### Medium

**C-M1. `char_online_data_cleanup` timer (char.cpp:2190, 600 s interval).**
Mirrors L-H1 on the char side: cleans up `online_char_db` entries where
`server` field is `-2` (stale after map-server crash). Wire into
`CharMaintenanceService`.

**C-M2. `char_new_display` flag (char.cpp:2820).**
Config has `CharNewDisplay` declared but never consulted. rAthena uses
it to flip the "New" badge on the char-server entry of `AC_ACCEPT_LOGIN`.
Wire into `LoginAuthUseCase` or push into `LoginGrpcService.RegisterCharacterServer`.

**C-M3. `char_name_min_length` not read from config.**
Char-create + rename name-validation uses a hardcoded minimum (4 by
constant default). The config field exists but the validator never
reads it. Verify by changing config and confirming the gate moves.

### Low

**C-L1. Console commands (char_cnslif.cpp).** Same scope/skip as L-L2.

**C-L2. `char_check_db` boot-time schema probe.** Structurally absent —
EF migrations enforce the schema, so the probe would be redundant.
Already documented as won't-fix.

**C-L3. `log_inter` flag.** Config knob present but no code path
toggles it. Won't-fix — Serilog sinks handle all logging.

---

## Implementation plan

Action in priority order. Each item becomes its own task.

1. **C-H1**: Insert starter items in `CharacterCreateHandler`
2. **C-H2**: Hotkey load/save RPC + wiring
3. **L-H1**: Login `online_data_cleanup` periodic sweep
4. **C-M1**: Char `online_data_cleanup` periodic sweep (via `CharMaintenanceService`)
5. **L-M1**: VIP timeout automatic downgrade
6. **L-M2**: `allowed_regs` + `time_allowed` registration rate limit
7. **C-M2**: `char_new_display` wired into server-list flag
8. **C-M3**: `char_name_min_length` honored
9. **L-M3**: `client_hash_check` per-group enforcement
10. **L-M4**: `loginlog.rcode` audit + fix-up

Items L-L1, L-L2, C-L1, C-L2, C-L3 deferred or documented as won't-fix.
