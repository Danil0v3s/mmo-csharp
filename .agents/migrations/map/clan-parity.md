# clan.cpp parity · 2026-05-22 (T9.F — per-fn rollup)

`src/map/clan.cpp` (235 lines, 13 functions) — clan membership (pre-
built rosters, no player creation) + per-clan chat fan-out.

## Subsystem coverage

| rAthena fn | Status | C# location |
|---|---|---|
| `clan_member_join` | ✅ | [ClanService.MemberJoin](/Map.Server/Clan/ClanService.cs) |
| `clan_member_leave` | ✅ | `ClanService.MemberLeave` |
| `clan_member_joined` | ✅ | `ClanService.MemberJoined` — broadcasts "X has connected" via Fanout to clan members (AT-D2) |
| `clan_member_left` | ✅ | `ClanService.MemberLeft` — broadcasts "X has logged out" via Fanout (AT-D2) |
| `clan_load_clandata` | ✅ | `ClanService.LoadClanData` — hydrates ClanRoom + adds member to roster |
| `clan_getMemberIndex` | ✅ | `ClanService.GetMemberIndex` — walks ClanRoom.Members |
| `clan_getNextFreeMemberIndex` | ✅ | `ClanService.GetNextFreeMemberIndex` — returns roster count |
| `clan_get_alliance_count` | ✅ | `ClanService.GetAllianceCount` — reads ClanRoom.Alliances |
| `clan_getavailablesd` | ✅ | `ClanService.GetAvailableMember` — entity-registry scan |
| `clan_send_message` | ✅ | `ClanService.SendMessage` — real ZC_NOTIFY_CHAT_PARTY emit to clan members on this map (AT-D2) |
| `clan_recv_message` | ✅ | `ClanService.RecvMessage` — Fanout with sender=0 for cross-server inbound (AT-D2) |
| `do_init_clan` / `do_final_clan` | ✅ | DI lifecycle |

## Coverage summary

| Bucket | ✅ | ⚠️ | ❌ | Total |
|---|---|---|---|---|
| Clan membership / chat / lifecycle | 13 | 0 | 0 | 13 |
| **Totals** | **13** | **0** | **0** | **13** |

## History

### 2026-05-24 — P2.1 doc-resync close-out (9 stale ⚠️ → ✅; 0 genuine gaps remain)

Audited every ⚠️ row against
[ClanService.cs](/Map.Server/Clan/ClanService.cs). All 9 prior
⚠️ rows now have real bodies — AT-D2 wave wired
`IEntityRegistry` + `ISessionManagerAccessor` so SendMessage /
RecvMessage emit ZC_NOTIFY_CHAT_PARTY to clan members,
MemberJoined / MemberLeft broadcast connect/logout system lines,
LoadClanData hydrates the in-memory ClanRoom on session-enter,
and the roster helpers (GetMemberIndex / GetNextFreeMemberIndex /
GetAllianceCount) walk the ClanRoom member + alliance sets.

**Coverage delta:** 4 ✅ / 9 ⚠️ / 0 ❌ → **13 ✅ / 0 ⚠️ / 0 ❌**.

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
