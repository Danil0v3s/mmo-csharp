# guild.cpp parity · 2026-05-20 (refreshed 2026-05-22 — T8.5 per-fn table)

`src/map/guild.cpp` (2755 lines, 74 unique public functions — the
prior "79 public functions" claim was an over-count from including
forward-declared `static` helpers).
Guild create / invite / leave / expulsion / message / castledatasave
/ alliance / break + a long tail of ack handlers + agit (WoE) +
emblem + skill tree. **Persistence lives on char-server**; this
service surfaces the rAthena-named map-side operations.

Canonical entry points: [IGuildService](/Map.Server/Guild/IGuildService.cs)
(27 methods).

## Per-function coverage

### Lifecycle (create / invite / join / leave / break)

| rAthena fn | Status | C# location / note |
|---|---|---|
| `guild_create` | ✅ | `IGuildService.Create` → `IntifService.GuildCreate` |
| `guild_created` | ⚠️ | C# `CreateGuildResponse` returned; map-side ack ("guild created" emote + ZC packet) is the gap |
| `guild_invite` | ✅ | `IGuildService.Invite` |
| `guild_reply_invite` | ✅ | `IGuildService.ReplyInvite` |
| `guild_member_joined` | ❌ | No map-side onJoin handler (session-enter doesn't ping guild HUD) |
| `guild_member_added` | ✅ | `IntifService.GuildAddMember` covers the IPC; map-side broadcast missing |
| `guild_member_withdraw` | ❌ | No map-side handler — when char-side notifies the withdraw, map doesn't refresh the member list |
| `guild_leave` | ✅ | `IGuildService.Leave` → `IntifService.GuildLeave` |
| `guild_expulsion` | ✅ | `IGuildService.Expulsion` → `IntifService.GuildExpulsion` |
| `guild_break` | ✅ | `IGuildService.Break` → `IntifService.GuildBreak` |
| `guild_broken` / `guild_broken_sub` | ⚠️ | Char-side disband broadcasts; map-side cleanup gap |
| `guild_makemember` | ❌ | rAthena helper that hydrates a `guild_member` struct from `mmo_charstatus`; not needed map-side (we get the typed proto) |
| `guild_isallied` | ❌ | No map-side helper; callers that need alliance checks do ad-hoc lookups |

### Info request / receive

| rAthena fn | Status | C# location / note |
|---|---|---|
| `guild_request_info` | ⚠️ | `IntifService.GuildRequestInfo` dispatches; map-side `OnRecvInfo` hydrate is the gap |
| `guild_npc_request_info` | ❌ | NPC-script-driven guild lookup (`getguildname`, etc.) — script-engine consumer (Phase 4 of scripting/) |
| `guild_recv_info` | ⚠️ | Char-side returns `GuildInfo` proto; map-side `IGuildService.RecvInfo` stub exists, doesn't populate a GuildEntity |
| `guild_recv_noinfo` | ⚠️ | Same — stub returns 0 |
| `guild_recv_memberinfoshort` | ❌ | Short-form member status broadcast (online/offline / class / level / mapid). Used by guild HUD + member list. Map-side handler missing |
| `guild_send_memberinfoshort` | ❌ | Outbound counterpart on PC level-up / job-change / move-map |
| `guild_recv_message` | ✅ | `IGuildService.RecvMessage` — chat fan-out wired in P5 |
| `guild_send_message` | ✅ | `IGuildService.SendMessage` → `IntifService.GuildMessage` |

### Member info / position / permissions

| rAthena fn | Status | C# location / note |
|---|---|---|
| `guild_change_position` | ✅ | `IGuildService.ChangePosition` → `IntifService.GuildSavePosition` |
| `guild_change_memberposition` | ✅ | `IGuildService.ChangeMemberPosition` |
| `guild_position_changed` | ⚠️ | Ack handler — char-side broadcasts; map-side refresh of member-list UI missing |
| `guild_memberposition_changed` | ⚠️ | Same |
| `guild_getposition` | ❌ | Helper that returns the position struct for a member; missing |
| `guild_getindex` | ❌ | Helper that returns the member's index in `guild.member[]`; missing |
| `guild_check_member` | ✅ | `IGuildService.CheckMember` (true/false in-party gate) |
| `guild_has_permission` | ❌ | rAthena 16-bit `gperm_mask` enforcement (invite / expel / position / emblem / etc.). Map-side perm check missing — GM tooling bypasses, but client-driven actions don't enforce |
| `guild_check_skill_require` | ❌ | Stat / level prereq check for guild skill upgrades. Missing |

### Skills / aura (guild_skill_*)

| rAthena fn | Status | C# location / note |
|---|---|---|
| `guild_skillup` | ⚠️ | `IGuildService.SkillUp` stub → `IntifService.GuildSetSkill`; full skill-tree validation gap |
| `guild_skillupack` | ⚠️ | Char-side ack; map-side broadcast refresh missing |
| `guild_skill_get_max` | ❌ | Per-skill max-level lookup (e.g. Battle Orders = 5). Missing |
| `guild_block_skill` | ❌ | Guild-skill cooldown (Battle Orders / Regeneration / Resto). Missing |
| `guild_check_skill_require` | ❌ | (Same as above; listed under permissions too) |
| `guild_guildaura_refresh` | ❌ | Guild-aura SC reapply (e.g. Earth Charm of the Lord, Lex Mighty). No map-side handler — aura buffs don't tick on members |

### Alliance / opposition

| rAthena fn | Status | C# location / note |
|---|---|---|
| `guild_reqalliance` | ❌ | Inbound request from another guild leader; no map-side handler (no `ZC_REQ_ALLIANCE` emitter) |
| `guild_reply_reqalliance` | ❌ | Outbound reply; missing |
| `guild_allianceack` | ✅ | `IGuildService.AllianceAck` → `IntifService.GuildAllianceAck` |
| `guild_delalliance` | ❌ | Map-side break-alliance entry; missing |
| `guild_opposition` | ❌ | Declare-opposition entry; missing |
| `guild_check_alliance` | ✅ | `IGuildService.CheckAlliance` |
| `guild_get_alliance_count` | ❌ | Helper used by `pc_isAllied` etc.; missing |

### Castle data (WoE)

| rAthena fn | Status | C# location / note |
|---|---|---|
| `guild_castledatasave` | ✅ | `IGuildService.CastleDataSave` |
| `guild_castledataloadack` | ✅ | `IGuildService.CastleDataLoadAck` |
| `guild_castle_map_init` | ❌ | Iterate `guild_castle.yml` at boot and bind castle-id→map-id; missing (WoE pre-port) |
| `guild_castle_reconnect` | ❌ | On char-server reconnect, refresh castle ownership; missing |
| `guild_castle_reconnect_sub` | ❌ | Helper for above |
| `guild_checkcastles` | ❌ | Per-castle online-count refresh; missing |

### Emblem

| rAthena fn | Status | C# location / note |
|---|---|---|
| `guild_emblem_changed` | ✅ | `IGuildService.EmblemChanged` |
| `guild_change_emblem` | ❌ | Inbound `CZ_GUILD_EMBLEM` handler; missing |
| `guild_change_emblem_version` | ❌ | Emblem-version bump (PACKETVER ≥ 20200716 client-bound trigger) |
| `guild_check_emblem_change_condition` | ❌ | Castle-owner / GM-permission gate for emblem change; missing |

### Notice

| rAthena fn | Status | C# location / note |
|---|---|---|
| `guild_change_notice` | ❌ | Inbound notice-update handler; missing |
| `guild_notice_changed` | ❌ | Outbound broadcast on notice change; missing |

### GM / Leader

| rAthena fn | Status | C# location / note |
|---|---|---|
| `guild_gm_change` | ❌ | Inbound `CZ_REQ_CHANGE_MEMBERS_POSITION` for GM transfer; missing |
| `guild_gm_changed` | ❌ | Ack broadcast; missing |

### Agit (War of Emperium)

| rAthena fn | Status | C# location / note |
|---|---|---|
| `guild_agit_start` / `guild_agit_end` | ❌ | WoE 1.0 window timer + state broadcast. WoE is pre-port (deferred to a WoE wave) |
| `guild_agit2_start` / `guild_agit2_end` | ❌ | WoE 2.0 (Schwartzwald) timer; same |
| `guild_agit3_start` / `guild_agit3_end` | ❌ | WoE TE; same |

### EXP / payout

| rAthena fn | Status | C# location / note |
|---|---|---|
| `guild_payexp` (`t_guild_payexp`) | ❌ | rAthena: PC pays guild tax → guild gets exp. Missing — tax rate UI works (`@guildexp`) but the payout pipe is no-op |
| `guild_getexp` (`t_guild_getexp`) | ❌ | Counterpart |
| `guild_payexp_timer_sub` | ❌ | Timer that flushes accumulated exp once-per-minute; missing |

### Misc helpers (map-side iterators)

| rAthena fn | Status | C# location / note |
|---|---|---|
| `guild_send_xy_timer_sub` | ❌ | Mini-map dot refresh per N seconds; missing |
| `guild_send_dot_remove` | ✅ | `IGuildService.SendDotRemove` (existing) — runs on logout |
| `guild_checkskill` | ❌ | Helper: does this guild have skill X learned? Missing |
| `guild_flag_add` / `guild_flag_remove` / `guild_flags_clear` | ❌ | Guild emblem-flag-NPC registry (the flag NPCs around a castle); missing |
| `guild_retrieveitembound` | ❌ | When a member is expelled with bound items, char-side mails them back. Map-side trigger missing |
| `map_session_guild_getavailablesd` | ❌ | Helper that returns any online member of a given guild; missing |
| `guild_send_levelup` | ✅ | `IGuildService.SendLevelUp` |

## Coverage summary

| Bucket | ✅ | ⚠️ | ❌ | Total |
|---|---|---|---|---|
| Lifecycle | 7 | 3 | 3 | 13 |
| Info request / receive | 2 | 3 | 3 | 8 |
| Member info / permissions | 3 | 2 | 4 | 9 |
| Skills / aura | 0 | 2 | 4 | 6 |
| Alliance / opposition | 2 | 0 | 5 | 7 |
| Castle data (WoE) | 2 | 0 | 4 | 6 |
| Emblem | 1 | 0 | 3 | 4 |
| Notice | 0 | 0 | 2 | 2 |
| GM / Leader | 0 | 0 | 2 | 2 |
| Agit (WoE) | 0 | 0 | 6 | 6 |
| EXP / payout | 0 | 0 | 3 | 3 |
| Misc | 2 | 0 | 6 | 8 |
| **Totals** | **19** | **10** | **45** | **74** |

## Gaps in priority order

**High** (player-facing, blocks gameplay):
1. **`guild_recv_info` hydrate** — guild HUD shows empty / "Press [Apply] to refresh" until the map-side hydrate populates a real GuildEntity from the char-side response.
2. **`guild_member_joined` / `guild_member_withdraw` / `guild_recv_memberinfoshort`** — member list never refreshes on the client; staffing changes invisible.
3. **`guild_has_permission`** — anyone in a guild can invite/expel today (no perm-bit check); GM tooling is fine, client-driven actions are not gated.
4. **`guild_change_emblem` / `guild_change_notice`** — emblem + notice updates have no client→server handler.

**Medium** (WoE / Endgame):
5. Castle data: `castle_map_init`, `checkcastles`, `castle_reconnect` — WoE setup pre-port, but the basic ownership tracking is the foundation.
6. Guild aura (`guild_guildaura_refresh`) — Earth Charm of the Lord, Lex Mighty, etc. — passive buffs on members.
7. Alliance ops: `reqalliance` / `reply_reqalliance` / `delalliance` / `opposition` — diplomacy UI inert today.

**Low** (engine completeness):
8. Agit start/end (×3 WoE versions) — endgame system.
9. EXP payout pipe — guild tax flowing into guild EXP.
10. Misc helpers (`getindex`, `getposition`, `flag_*`, `getavailablesd`).

## Implementation plan

Tracked separately as the **GD (Guild)** wave. Estimated 5 sub-waves:

1. **GD-H1** — `GuildEntity` in-memory model + `IGuildService.OnRecvInfo` hydrate.
2. **GD-H2** — Member tracking: `member_joined` / `member_withdraw` / `recv_memberinfoshort` + `send_memberinfoshort` triggers (level-up / job-change / move-map / login / logout).
3. **GD-H3** — Permissions: `gperm_mask` enum + `has_permission` gate on Invite / Expulsion / ChangePosition / Emblem.
4. **GD-M1** — Notice + Emblem update inbound + ack broadcasts.
5. **GD-M2** — Alliance / opposition req-reply flow.

WoE-related rows (Agit, Castle init, EXP-tax) defer to a dedicated WoE wave.

## History

### 2026-05-22 — T8.5 per-function audit

Replaced the prior "79 public functions covered (canonical entry
points)" claim — which was an over-count + the C# surface only has
27 methods — with a per-function table covering all 74 unique
rAthena guild_* functions.

**New baseline:** 19 ✅ / 10 ⚠️ / 45 ❌. The previous doc's framing
(everything's covered as canonical entry points) was misleading; the
truth is that 45 functions have no map-side entry at all. GD-H1..GD-M2
wave plan captures the implementation backlog.

Char-side guild RPCs all ✅ (P1-P8 audits); see
[inter/modules.md § Guild](../inter/modules.md#guild-int_guildcpp).

### 2026-05-20 — initial audit + service (superseded)
- (Prior) 79 public functions covered (canonical entry points).
- Refresh above corrects the count to 74 and the coverage shape.
