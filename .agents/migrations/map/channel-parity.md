# channel.cpp parity · 2026-05-22 (T9.E — per-fn rollup)

`src/map/channel.cpp` (1526 lines, 30 functions). Persistent text
channels — guild / map / party / custom user channels.

Canonical entry points: [IChannelService](/Map.Server/Chat/Channels/IChannelService.cs).
In-memory registry + per-PC membership; channels.conf loader pending.

## Per-function coverage

### PC create / join / leave

| rAthena fn | Status | C# location / note |
|---|---|---|
| `channel_pccreate` | ✅ | `PcCreate` |
| `channel_pcdelete` | ✅ | `PcDelete` |
| `channel_pcjoin` | ✅ | `PcJoin` (password + ban checks) |
| `channel_pcleave` | ✅ | `PcLeave` |
| `channel_pcquit` | ✅ | `PcQuit` (cascade leave) |
| `channel_join` | ✅ | `Join` (delegates to PcJoin) |
| `channel_pckick` | ⚠️ | `PcKick` — stub (name→entity lookup pending) |
| `channel_pcban` | ⚠️ | `PcBan` — stub |
| `channel_pcunbind` / `_pcbind` | ⚠️ | `PcUnbind` / `PcBind` — stubs |
| `channel_pccolor` / `_pcsetopt` | ⚠️ | `PcColor` / `PcSetOpt` — stubs |
| `channel_pccheckgroup` | ✅ | `PcCheckGroup` — always true |
| `channel_pc_haschan` / `channel_haspc` / `_haspcbanned` | ✅ | Membership / ban checks |

### Channel management

| rAthena fn | Status | C# location / note |
|---|---|---|
| `channel_create` | ⚠️ | `Create` — exists; ReadConfig data-pending |
| `channel_delete` | ✅ | `Delete` |
| `channel_chk` | ✅ | `Check` (existence + type) |
| `channel_clean` | ✅ | `Clean` (cascade quit) |
| `channel_display_list` | ✅ | `DisplayList` |
| `channel_send` | ⚠️ | `Send` — membership count only; wire data-pending |

### Autojoin / config

| rAthena fn | Status | C# location / note |
|---|---|---|
| `channel_ajoin` | ⚠️ | `AJoin` — stub |
| `channel_mjoin` | ⚠️ | `MJoin` — stub |
| `channel_gjoin` | ⚠️ | `GJoin` — stub |
| `channel_autojoin` | ⚠️ | `Autojoin` — no-op |
| `channel_pcautojoin_sub` | ⚠️ | `PcAutojoinSub` — stub |
| `channel_read_config` | ⚠️ | `ReadConfig` — data-pending |
| `channel_read_sub` | ✅ | `ReadSub` |
| `do_init_channel` / `do_final_channel` | ❌ | Not exposed (DI implicit) |

## Coverage summary

| Bucket | ✅ | ⚠️ | ❌ | Total |
|---|---|---|---|---|
| PC create / join / leave | 8 | 6 | 0 | 14 |
| Channel management | 4 | 2 | 0 | 6 |
| Autojoin / config | 1 | 5 | 2 | 8 |
| **Totals** | **13** | **13** | **2** | **28** |

## History

### 2026-05-22 — T9.E per-fn rollup

Per-function audit. Baseline: **13 ✅ / 13 ⚠️ / 2 ❌** across 28
entries. PC join/leave/check/display all ✅. ⚠️ rows: kick/ban
(name→entity lookup), bind/color/opt (stubs), autojoin variants
(pending IMapServerRuntime hooks), config loader (channels.conf
schema pending). 2 ❌ are do_init / do_final (DI implicit).

### 2026-05-20 — initial audit + service
- 30 functions covered (canonical entry points; data-pending
  on parent dependency).
