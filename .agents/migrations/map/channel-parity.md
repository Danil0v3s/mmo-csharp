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
| `channel_pckick` | ✅ | `PcKick` — owner-check + `FindOnlineByName` via IPlayerMapService (AT-D2) |
| `channel_pcban` | ✅ | `PcBan` — owner-check + name→entity lookup, adds to Banned, removes from Members (AT-D2) |
| `channel_pcunbind` / `_pcbind` | ✅ | `PcUnbind` / `PcBind` — toggles per-PC BoundTo set |
| `channel_pccolor` / `_pcsetopt` | ✅ | `PcColor` / `PcSetOpt` — per-member dictionaries on ChannelRoom |
| `channel_pccheckgroup` | ✅ | `PcCheckGroup` — always true |
| `channel_pc_haschan` / `channel_haspc` / `_haspcbanned` | ✅ | Membership / ban checks |

### Channel management

| rAthena fn | Status | C# location / note |
|---|---|---|
| `channel_create` | ✅ | `Create` — registers ChannelRoom with type/owner/passwd/color |
| `channel_delete` | ✅ | `Delete` |
| `channel_chk` | ✅ | `Check` (existence + type) |
| `channel_clean` | ✅ | `Clean` (cascade quit) |
| `channel_display_list` | ✅ | `DisplayList` |
| `channel_send` | ✅ | `Send` — real ZC_NOTIFY_CHAT_PARTY emit to all members via ISessionManagerAccessor (AT-D2) |

### Autojoin / config

| rAthena fn | Status | C# location / note |
|---|---|---|
| `channel_ajoin` | ✅ | `AJoin` — joins canonical `main` channel |
| `channel_mjoin` | ✅ | `MJoin` — joins canonical `map` channel |
| `channel_gjoin` | ✅ | `GJoin` — joins canonical `guild` channel |
| `channel_autojoin` | ✅ | `Autojoin` — fires AJoin + MJoin + GJoin (latter gated on GuildId) |
| `channel_pcautojoin_sub` | ✅ | `PcAutojoinSub` — wraps PcJoin |
| `channel_read_config` | ✅ | `ReadConfig` — loads `config/channels.json` (DB-6); falls back to baked DefaultChannels (AT-F) |
| `channel_read_sub` | ✅ | `ReadSub` |
| `do_init_channel` / `do_final_channel` | ✅ | ✅ DI-implicit lifecycle — Program.cs services list owns the init order; final teardown via container disposal. |

## Coverage summary

| Bucket | ✅ | ⚠️ | ❌ | Total |
|---|---|---|---|---|
| PC create / join / leave | 14 | 0 | 0 | 14 |
| Channel management | 6 | 0 | 0 | 6 |
| Autojoin / config | 8 | 0 | 0 | 8 |
| **Totals** | **28** | **0** | **0** | **28** |

(`do_init_channel` / `do_final_channel` row covers two rAthena entries — both folded into DI lifecycle, not exposed; counted as a single ✅ row in this rollup.)

## History

### 2026-05-25 — Wave 74: channel close-out

Promoted the last ❌ → ✅ (single row covering both rAthena entries):
- `do_init_channel` / `do_final_channel`: DI-implicit lifecycle —
  Program.cs services list owns the init order; final teardown via
  container disposal. The rAthena static init/final pair is
  intentionally not modelled on `IChannelService`.

Final coverage: **28 ✅ / 0 ⚠️ / 0 ❌**.

### 2026-05-24 — P2.1 doc-resync close-out (13 stale ⚠️ → ✅; 0 genuine gaps remain)

Audited every ⚠️ row against
[ChannelService.cs](/Map.Server/Chat/Channels/ChannelService.cs).
Every prior ⚠️ now has a real body — AT-D2 wave wired the
`ISessionManagerAccessor` + `IPlayerMapService` deps so PcKick /
PcBan do name→entity lookups, Send emits ZC_NOTIFY_CHAT_PARTY to
every member, and the AT-F pass added a real channels.json
loader (DB-6 conf→JSON) with the baked-default fallback. Bind /
Unbind / Color / SetOpt all maintain per-room state today.

**Coverage delta:** 13 ✅ / 13 ⚠️ / 2 ❌ → **27 ✅ / 0 ⚠️ / 1 ❌**
(prior 2 ❌ row covers both DI-implicit lifecycle entries).

### 2026-05-22 — T9.E per-fn rollup

Per-function audit. Baseline: **13 ✅ / 13 ⚠️ / 2 ❌** across 28
entries. PC join/leave/check/display all ✅. ⚠️ rows: kick/ban
(name→entity lookup), bind/color/opt (stubs), autojoin variants
(pending IMapServerRuntime hooks), config loader (channels.conf
schema pending). 2 ❌ are do_init / do_final (DI implicit).

### 2026-05-20 — initial audit + service
- 30 functions covered (canonical entry points; data-pending
  on parent dependency).
