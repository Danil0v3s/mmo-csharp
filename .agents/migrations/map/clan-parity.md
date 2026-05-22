# clan.cpp parity · 2026-05-22 (T9.F — per-fn rollup)

`src/map/clan.cpp` (235 lines, 13 functions) — clan membership (pre-
built rosters, no player creation) + per-clan chat fan-out.

## Subsystem coverage

| rAthena fn | Status | C# location |
|---|---|---|
| `clan_member_join` | ✅ | [ClanService.MemberJoin](/Map.Server/Clan/ClanService.cs) |
| `clan_member_leave` | ✅ | `ClanService.MemberLeave` |
| `clan_member_joined` | ⚠️ | `ClanService.MemberJoined` — broadcast pending |
| `clan_member_left` | ⚠️ | `ClanService.MemberLeft` — broadcast pending |
| `clan_load_clandata` | ⚠️ | `ClanService.LoadClanData` — IPC pending |
| `clan_getMemberIndex` | ⚠️ | `ClanService.GetMemberIndex` — roster pending |
| `clan_getNextFreeMemberIndex` | ⚠️ | `ClanService.GetNextFreeMemberIndex` |
| `clan_get_alliance_count` | ⚠️ | `ClanService.GetAllianceCount` |
| `clan_getavailablesd` | ✅ | `ClanService.GetAvailableMember` — entity-registry scan |
| `clan_send_message` | ⚠️ | `ClanService.SendMessage` — packet emitter pending |
| `clan_recv_message` | ⚠️ | `ClanService.RecvMessage` — inter routing pending |
| `do_init_clan` / `do_final_clan` | ✅ | DI lifecycle |

## Coverage summary

| Bucket | ✅ | ⚠️ | ❌ | Total |
|---|---|---|---|---|
| Clan membership / chat / lifecycle | 4 | 9 | 0 | 13 |
| **Totals** | **4** | **9** | **0** | **13** |

## History

### 2026-05-22 — T9.F per-fn rollup

Per-function audit. Baseline: **4 ✅ / 9 ⚠️ / 0 ❌**. Core
member_join / member_leave / getavailablesd / DI lifecycle ✅.
9 ⚠️ rows are broadcast (member_joined/_left), IPC sync
(load_clandata), roster ops (getMemberIndex / getNextFreeMemberIndex
/ get_alliance_count), and chat fan-out (send/recv message) —
all pending char-server clan roster sync + ZC_NOTIFY_CHAT_CLAN
packet emitter.

### 2026-05-20 — initial audit + service
- 13 functions covered by `IClanService` / `ClanService`.
- `PlayerEntity.ClanId` added.
- Roster + IPC + wire packets land alongside the char-server side.
