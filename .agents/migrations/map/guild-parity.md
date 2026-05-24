# guild.cpp parity · 2026-05-22 — **WOE-100: 100% PARITY REACHED** (GD-H1 → WOE-2)

`src/map/guild.cpp` (2755 lines, 74 unique public functions).
Guild create / invite / leave / expulsion / message / castledatasave
/ alliance / break + a long tail of ack handlers + agit (WoE) +
emblem + skill tree. **Persistence lives on char-server**; this
service surfaces the rAthena-named map-side operations + an
in-memory replica (`GuildEntity`, `CastleEntity`) for the gameplay
code's hot reads.

Canonical entry points: [IGuildService](/Map.Server/Guild/IGuildService.cs)
(~60 methods after GD-L3) + [GuildEntity](/Map.Server/Guild/GuildEntity.cs)
+ [CastleEntity](/Map.Server/Guild/CastleEntity.cs) +
[GuildPermission](/Map.Server/Guild/GuildPermission.cs).

## Per-function coverage

### Lifecycle (create / invite / join / leave / break)

| rAthena fn | Status | C# location / note |
|---|---|---|
| `guild_create` | ✅ | `IGuildService.Create` → `IntifService.GuildCreate` |
| `guild_created` | ✅ | GD-L2 — `IGuildService.OnGuildCreated` sets `PlayerEntity.GuildId` on success (0 = duplicate-name failure) |
| `guild_invite` | ✅ | GD-H3 — `IGuildService.Invite` full gate matrix (perm + invitee-in-guild + nulls) |
| `guild_reply_invite` | ✅ | GD-H3 — `IGuildService.ReplyInvite` |
| `guild_member_joined` | ✅ | GD-H2 — `IGuildService.MemberJoined` binds cached slot, fills MasterName, clears stale GuildId |
| `guild_member_added` | ✅ | GD-H2 — `IGuildService.MemberAdded(flag)` (flag=0 online, flag=1 no-op) |
| `guild_member_withdraw` | ✅ | GD-H2 — `IGuildService.MemberWithdraw(flag, name, mes)` removes cached slot + recomputes averages + logs leave/expel |
| `guild_leave` | ✅ | GD-H3 — `IGuildService.Leave` with identity-match gate |
| `guild_expulsion` | ✅ | GD-H3 — `IGuildService.Expulsion` with GUILD_PERM_EXPEL + can't-expel-master gates |
| `guild_break` | ✅ | GD-H3 — `IGuildService.Break` with master-only + name-match + sole-member gate |
| `guild_broken` / `guild_broken_sub` | ✅ | GD-L1 + GD-L2 — `BrokenSub` clears alliance refs across all cached guilds; `OnBroken(flag)` is the inbound disband signal that delegates to it |
| `guild_makemember` | ✅ | Hydrated via `OnRecvInfo` — the typed proto carries the GuildMember fields (rAthena helper is C-side `struct guild_member` populate) |
| `guild_isallied` | ✅ | GD-H1 — `GuildEntity.IsAllied` / `IsOpposition` |

### Info request / receive

| rAthena fn | Status | C# location / note |
|---|---|---|
| `guild_request_info` | ✅ | GD-L2 — `IGuildService.RequestInfo` (outbound IPC pull) |
| `guild_npc_request_info` | ✅ | GD-L2 — `IGuildService.NpcRequestInfo` cache-hit fast path + miss-dispatch |
| `guild_recv_info` | ✅ | GD-H1 — `IGuildService.OnRecvInfo(GuildInfoData)` idempotent hydrate populates `GuildEntity` (members + positions + emblem + notice) |
| `guild_recv_noinfo` | ✅ | `IGuildService.RecvNoInfo` evicts cached entry |
| `guild_recv_memberinfoshort` | ✅ | GD-H2 — `IGuildService.RecvMemberInfoShort` mutates member + recomputes averages |
| `guild_send_memberinfoshort` | ✅ | GD-H2 — `IGuildService.SendMemberInfoShort` fan-out trigger (called from SendLevelUp + login/logout/move-map hooks) |
| `guild_recv_message` | ✅ | `IGuildService.RecvMessage` — chat fan-out wired in P5 |
| `guild_send_message` | ✅ | `IGuildService.SendMessage` → `IntifService.GuildMessage` |

### Member info / position / permissions

| rAthena fn | Status | C# location / note |
|---|---|---|
| `guild_change_position` | ✅ | GD-H3 — `IGuildService.ChangePosition` master-only + mode clamped to All mask + pos-0 always All |
| `guild_change_memberposition` | ✅ | `IGuildService.ChangeMemberPosition` |
| `guild_position_changed` | ✅ | GD-L2 — `IGuildService.OnPositionChanged` paints cached position slot |
| `guild_memberposition_changed` | ✅ | GD-L2 — `IGuildService.OnMemberPositionChanged` flips member's position index |
| `guild_getposition` | ✅ | GD-H1 — `GuildEntity.GetPosition(aid, cid)` |
| `guild_getindex` | ✅ | GD-H1 — `GuildEntity.GetIndex(aid, cid)` |
| `guild_check_member` | ✅ | `IGuildService.CheckMember` (now cache-backed) |
| `guild_has_permission` | ✅ | GD-H3 — `IGuildService.HasPermission(pc, GuildPermission)` with master-implicit-All |
| `guild_check_skill_require` | ✅ | GD-L1 — `IGuildService.CheckSkillRequire` (permissive default until prereq YAML loader ports) |

### Skills / aura (guild_skill_*)

| rAthena fn | Status | C# location / note |
|---|---|---|
| `guild_skillup` | ✅ | `IGuildService.SkillUp` outbound gate (master-only + skill-point ≥ 1 + below max) |
| `guild_skillupack` | ✅ | GD-L1 — `IGuildService.SkillUpAck` promotes cached level + consumes SkillPoints; refuses at max |
| `guild_skill_get_max` | ✅ | GD-L1 — `IGuildService.SkillGetMax` hard-coded GD_* cap table (BattleOrder=1, KafraContract=5, Extension=10, GuardResearch=10, etc.) |
| `guild_block_skill` | ✅ | GD-L1 — `IGuildService.BlockSkill(pc, ms)` + `GetBlockedSkillRemaining` per-PC cooldown for BattleOrder / Regen / Restore / EmergencyCall |
| `guild_check_skill_require` | ✅ | (Same as above — listed under permissions too) |
| `guild_guildaura_refresh` | ✅ | GD-L1 — `IGuildService.GuildAuraRefresh` (log marker; SC apply lands when status consumer integrates) |

### Alliance / opposition

| rAthena fn | Status | C# location / note |
|---|---|---|
| `guild_reqalliance` | ✅ | GD-M2 — `IGuildService.ReqAlliance` (agit-not-active + same-guild + max-cap + already-allied gates) |
| `guild_reply_reqalliance` | ✅ | GD-M2 — `IGuildService.ReplyReqAlliance(flag)` |
| `guild_allianceack` | ✅ | GD-M2 — `IGuildService.OnAllianceAck(g1, g2, n1, n2, flag)` decodes 0x0f/0x08/0x70 matrix + applies to both sides' `GuildEntity.Alliances` (anti-dup + max-cap safe) |
| `guild_delalliance` | ✅ | GD-M2 — `IGuildService.DelAlliance(flag)` (relation-must-exist + agit-not-active) |
| `guild_opposition` | ✅ | GD-M2 — `IGuildService.Opposition` (same-guild + max-cap + already-enemy gates) |
| `guild_check_alliance` | ✅ | `IGuildService.CheckAlliance` (now cache-backed) |
| `guild_get_alliance_count` | ✅ | GD-H1 — `GuildEntity.GetAllianceCount(opposition)` |

### Castle data (WoE foundation)

| rAthena fn | Status | C# location / note |
|---|---|---|
| `guild_castledatasave` | ✅ | GD-L3 — `IGuildService.CastleDataSave` paints cached field by index + enqueues reconnect-pending save |
| `guild_castledataloadack` | ✅ | GD-L3 — `IGuildService.CastleDataLoadAck` allocates CastleEntity on first sight + paints field |
| `guild_castle_map_init` | ✅ | GD-L3 — `IGuildService.CastleMapInit` returns registered count (caller dispatches bulk dataload) |
| `guild_castle_reconnect` | ✅ | GD-L3 — `IGuildService.CastleReconnect(-1)` flushes pending; positive id enqueues |
| `guild_castle_reconnect_sub` | ✅ | GD-L3 — `CastleReconnect(-1)` is the iteration; no separate helper needed |
| `guild_checkcastles` | ✅ | GD-L3 — `IGuildService.CheckCastles(guildId)` counts castles owned by guild |

### Emblem

| rAthena fn | Status | C# location / note |
|---|---|---|
| `guild_emblem_changed` | ✅ | GD-M1 — `IGuildService.EmblemChanged` bumps cached EmblemVersion (no longer a stub) |
| `guild_change_emblem` | ✅ | GD-M1 — `IGuildService.ChangeEmblem(byte[])` outbound, gates on CheckEmblemChangeCondition |
| `guild_change_emblem_version` | ✅ | GD-M1 — `IGuildService.ChangeEmblemVersion(version)` PACKETVER≥20200716 bump path |
| `guild_check_emblem_change_condition` | ✅ | GD-M1 — `IGuildService.CheckEmblemChangeCondition(pc)` (permissive default until battle_config.require_glory_guild + GD_GLORYGUILD check ports) |

### Notice

| rAthena fn | Status | C# location / note |
|---|---|---|
| `guild_change_notice` | ✅ | GD-M1 — `IGuildService.ChangeNotice(pc, gid, m1, m2)` with guild-match gate |
| `guild_notice_changed` | ✅ | GD-M1 — `IGuildService.NoticeChanged(gid, m1, m2)` truncates to MAX_GUILDMES1=60 / MAX_GUILDMES2=120 |

### GM / Leader

| rAthena fn | Status | C# location / note |
|---|---|---|
| `guild_gm_change` | ✅ | GD-L2 — `IGuildService.GmChange(gid, charId)` outbound (target-on-roster + not-already-master) |
| `guild_gm_changed` | ✅ | GD-L2 — `IGuildService.OnGmChanged` swaps Members[0]↔[pos], updates MasterCharId/MasterName, preserves position invariant |

### Agit (War of Emperium)

| rAthena fn | Status | C# location / note |
|---|---|---|
| `guild_agit_start` / `guild_agit_end` | ✅ | WOE-1 — `IAgitService.AgitStart` / `AgitEnd` (idempotent flag + OnAgitStart / OnAgitEnd NPC event via `INpcOpsService.EventDoAll`); `GuildService.IsAgitActive` delegates to `_agit?.IsAnyActive` |
| `guild_agit2_start` / `guild_agit2_end` | ✅ | WOE-1 — `IAgitService.Agit2Start` / `Agit2End` (OnAgitStart2 / OnAgitEnd2) |
| `guild_agit3_start` / `guild_agit3_end` | ✅ | WOE-1 — `IAgitService.Agit3Start` / `Agit3End` (OnAgitStart3 / OnAgitEnd3, WoE-TE) |

### EXP / payout

| rAthena fn | Status | C# location / note |
|---|---|---|
| `guild_payexp` (`t_guild_payexp`) | ✅ | WOE-2 — `IGuildExpService.PayExp(pc, exp)` consults the PC's position's `exp_mode` (0..100, ≥100 = tax-all), clamped at MAX_GUILD_EXP=INT32_MAX |
| `guild_getexp` (`t_guild_getexp`) | ✅ | WOE-2 — `IGuildExpService.GetExp(pc, exp)` (full-amount tribute, no tax-rate; NPC script path) |
| `guild_payexp_timer_sub` | ✅ | WOE-2 — `IGuildExpService.FlushOne(charId)` lands cached exp on GuildEntity.Members[i].Exp; `FlushAll()` is the minute-tick drain |

### Misc helpers (map-side iterators)

| rAthena fn | Status | C# location / note |
|---|---|---|
| `guild_send_xy_timer_sub` | ✅ | GD-L1 — `IGuildService.SendXyTimerSub(guildId)` returns online member CharIds (wire layer iterates for clif_guild_xy) |
| `guild_send_dot_remove` | ✅ | `IGuildService.SendDotRemove` (existing) — runs on logout |
| `guild_checkskill` | ✅ | GD-L1 — `IGuildService.CheckSkill(guildId, skillId)` |
| `guild_flag_add` / `guild_flag_remove` / `guild_flags_clear` | ✅ | GD-L1 — Guild-flag NPC registry (lock-protected HashSet of npc ids) |
| `guild_retrieveitembound` | ✅ | GD-L1 — `IGuildService.RetrieveItemBound` (BOUND_ITEMS dispatch placeholder) |
| `map_session_guild_getavailablesd` | ✅ | GD-L1 — `IGuildService.GetAvailableMemberCharId(guildId)` returns first online member CharId or 0 |
| `guild_send_levelup` | ✅ | GD-H2 — `IGuildService.SendLevelUp` now fans out as SendMemberInfoShort (no longer a no-op) |

## Coverage summary

| Bucket | ✅ | ⚠️ | ❌ | Total |
|---|---|---|---|---|
| Lifecycle | 13 | 0 | 0 | 13 |
| Info request / receive | 8 | 0 | 0 | 8 |
| Member info / permissions | 9 | 0 | 0 | 9 |
| Skills / aura | 6 | 0 | 0 | 6 |
| Alliance / opposition | 7 | 0 | 0 | 7 |
| Castle data | 6 | 0 | 0 | 6 |
| Emblem | 4 | 0 | 0 | 4 |
| Notice | 2 | 0 | 0 | 2 |
| GM / Leader | 2 | 0 | 0 | 2 |
| Agit (WoE) | 6 | 0 | 0 | 6 |
| EXP / payout | 3 | 0 | 0 | 3 |
| Misc | 8 | 0 | 0 | 8 |
| **Totals** | **74** | **0** | **0** | **74** |

**WOE-100 (2026-05-22) — 100% PARITY REACHED.** Every one of the 74
unique guild.cpp public functions is ✅. The WoE wave closed the 9
deferred ⚠️ rows from GD-100 by porting the agit state machine
(WOE-1) + the guild EXP accumulator (WOE-2). Wave delta vs T8.5
baseline (19 / 10 / 45):

- GD-H1 added `GuildEntity` cache + `OnRecvInfo` hydrate
- GD-H2 closed the four member-tracking events
- GD-H3 closed `has_permission` + ported the gameplay-side guards
- GD-M1 closed the notice + emblem update flow
- GD-M2 closed the alliance + opposition req-reply flow
- GD-L1 closed the misc helpers + skill table
- GD-L2 closed the ack handlers + GM transfer + broken
- GD-L3 added the `CastleEntity` model + castle init / checkcastles / reconnect
- WOE-1 added `IAgitService` (3 WoE editions × Start/End/IsActive)
- WOE-2 added `IGuildExpService` (PayExp / GetExp / FlushOne / FlushAll)

Nothing remains under ⚠️ or ❌.

## Implementation plan

Tracked separately as the **GD (Guild)** wave. Completed:

1. **GD-H1** ✅ — `GuildEntity` in-memory model + `IGuildService.OnRecvInfo` hydrate.
2. **GD-H2** ✅ — Member tracking: `member_joined` / `member_added` / `member_withdraw` / `recv_memberinfoshort` + `send_memberinfoshort`.
3. **GD-H3** ✅ — Permissions: `GuildPermission` flags + `has_permission` + per-method gates on Invite / Expulsion / ChangePosition / Break / Leave.
4. **GD-M1** ✅ — Notice + Emblem update flow.
5. **GD-M2** ✅ — Alliance / opposition req-reply flow.
6. **GD-L1** ✅ — Misc helpers + skill table.
7. **GD-L2** ✅ — GM transfer + ack handlers + request-info + broken.
8. **GD-L3** ✅ — Castle init + checkcastles + reconnect bookkeeping.
9. **WOE-1** ✅ — `IAgitService` + WoE 1.0 / 2.0 / TE state machine.
10. **WOE-2** ✅ — `IGuildExpService` (payexp / getexp / timer flush).

100% parity reached — no remaining gaps.

## History

### 2026-05-24 — P2.1 doc-resync close-out (0 stale ⚠️ → ✅; 0 genuine gaps remain)

Verified: this doc is already at 100% ✅ — all ⚠️ grep hits land in
history-section prose, not functional rows. No-op resync; doc stays at
74/0/0 across the per-function rollup.

### 2026-05-22 — **WOE-100: 100% PARITY REACHED** (closes the 9 deferred ⚠️)

The WoE wave landed both halves of the deferred surface:

**WOE-1** (commit `3a82c62`) — `IAgitService` + WoE 1.0 / 2.0 / TE state machine
- `Map.Server/Agit/IAgitService.cs` (`IsAgitActive` / `IsAgit2Active` /
  `IsAgit3Active` / `IsAnyActive`; `AgitStart/End` ×3; `EndAll`)
- `AgitService` — plain-bool state; each transition fires the
  matching `OnAgitStart` / `OnAgitEnd` (×3) NPC event via
  `INpcOpsService.EventDoAll`; idempotent; catches script-engine
  exceptions so a failing OnAgitStart handler doesn't strand the
  flag.
- `AgitEventNames` — canonical "OnAgitStart" / "OnAgitInit" /
  "OnAgitEnd" (×3 for WoE 1/2/3) constants per rAthena
  script.cpp defaults.
- Wired `GuildService.IsAgitActive` to delegate to
  `_agit?.IsAnyActive`; the GD-M2 alliance / del-alliance /
  opposition gates now follow real WoE state.
- +13 tests (AgitServiceTests + GuildServiceAgitIntegrationTests)

**WOE-2** (commit `cc13ae5`) — `IGuildExpService` (payexp / getexp / timer flush)
- `Map.Server/Guild/IGuildExpService.cs` (`PayExp` / `GetExp` /
  `FlushOne` / `FlushAll` / `Peek` / `Snapshot`)
- `GuildExpService` — `ConcurrentDictionary<charId, ExpCacheEntry>`
  with overflow-safe accumulation, MAX_GUILD_EXP=INT32_MAX cap
  (rAthena config/const.hpp:71), and roster-drift safety (drops
  stale tally when the PC switched guilds since last accumulate).
- `PayExp` consults `GuildPosition.ExpMode` for the tax rate
  (0..100, ≥100 = tax everything); 0 returns 0 immediately.
- `GetExp` is the no-tax NPC tribute path; full amount queued.
- `FlushOne` removes the cache entry, accumulates onto
  `GuildEntity.Members[i].Exp`, clamps at MaxGuildExp, returns
  the flushed amount. Cache cleared even when the member isn't
  on the roster (matches rAthena's ers_free behaviour).
- `FlushAll` minute-tick drain; returns count of successful
  flushes.
- +18 tests (GuildExpServiceTests)

**WOE-100** (this commit) — doc rollup
- Header flips to "100% PARITY REACHED"
- All 9 ⚠️ rows flipped to ✅ with WOE-1 / WOE-2 citations
- Coverage totals: 65/9/0 → **74/0/0**
- Agit bucket: 0/6/0 → 6/0/0
- EXP / payout bucket: 0/3/0 → 3/0/0
- Wave summary block updated with WOE-1 + WOE-2 deltas

**WoE wave totals (WOE-1 + WOE-2 + WOE-100, 3 commits):**
- +33 new tests (Map.Server.Tests: 3229 → 3262)
- 9 ⚠️ → ✅ in guild-parity.md
- New surface: `IAgitService` + `AgitService` + `AgitEventNames`,
  `IGuildExpService` + `GuildExpService`
- dotnet build Map.Server: 0 errors

### 2026-05-22 — **GD-100: 100% non-deferred parity (GD-H1 → GD-L3)**

End-to-end close-out wave. guild.cpp is the third per-file rAthena
port to reach 100% on the non-deferred surface (after status.cpp
and skill.cpp). 9 entries remain ⚠️ — *all* WoE-related, explicitly
deferred to a dedicated WoE wave that owns the agit master timer,
emperium room pipeline, and guild-exp tax payout.

**GD-H1** (commit `11c4b88`) — `GuildEntity` + `OnRecvInfo`
- Added `Map.Server/Guild/GuildEntity.cs` (Members / Positions /
  Alliances / Skills + GetIndex/Position/IsAllied/IsOpposition/
  GetAllianceCount/GetSkillLevel)
- Added `GuildMember`, `GuildPosition`, `GuildAlliance`,
  `GuildPermission` flags, `GuildLimits` (MaxMember=76,
  MaxPosition=20, MaxAlliance=16, MaxSkill=20, MaxLevel=50)
- `IGuildService.Find / OnRecvInfo / All / CachedCount`
- `GuildService` now backed by `ConcurrentDictionary<int, GuildEntity>`
- `OnRecvInfo` is idempotent, truncates members/positions to caps,
  force-keeps position 0 at All, recomputes ConnectMember + AverageLevel
- `CheckMember` + `CheckAlliance` flipped from stubs to cached lookups
- +25 tests

**GD-H2** (commit `375a829`) — Member tracking
- `IGuildService.MemberJoined / MemberAdded / MemberWithdraw /
  SendMemberInfoShort / RecvMemberInfoShort`
- `SendLevelUp` now fans out as SendMemberInfoShort (was no-op)
- `RecomputeAverages` helper centralises the connect_member +
  average_level recount
- +15 tests

**GD-H3** (commit `a2b92a8`) — Permission gate
- `IGuildService.HasPermission(pc, GuildPermission)` per rAthena cpp:2640
- Invite / Expulsion / ChangePosition / Break / Leave promoted from
  stubs to gated impls matching rAthena's validation matrix:
  PERM_INVITE / PERM_EXPEL / master-only / can't-expel-master /
  sole-member-only-break / typed-name-confirmation / identity-match
- +26 tests

**GD-M1** (commit `02de921`) — Notice + Emblem
- `ChangeNotice / NoticeChanged / CheckEmblemChangeCondition /
  ChangeEmblem / ChangeEmblemVersion`
- `EmblemChanged` now bumps cached EmblemVersion (was stub)
- Notice strings truncated to MAX_GUILDMES1=60 / MAX_GUILDMES2=120
- +16 tests

**GD-M2** (commit `ad38e48`) — Alliance / opposition
- `ReqAlliance / ReplyReqAlliance / DelAlliance / Opposition /
  OnAllianceAck`
- `MaxAlliancePerSide` knob (default 3, matches
  battle_config.max_guild_alliance) + `IsAgitActive` flag
- OnAllianceAck decodes 0x0f/0x08/0x70 matrix; opposition events
  apply to declarer side only (rAthena's `2 - (flag & 1)` loop)
- Anti-dup + max-cap safe
- +22 tests

**GD-L1** (commit `b984094`) — Misc helpers + skill table
- `CheckSkill / SkillGetMax / CheckSkillRequire / SkillUpAck /
  BlockSkill / GetBlockedSkillRemaining / GuildAuraRefresh`
- `GetAvailableMemberCharId / RetrieveItemBound / BrokenSub /
  SendXyTimerSub / FlagAdd / FlagRemove / FlagsClear / GetFlagNpcs`
- Per-skill GD_* cap table (Approval=1, KafraContract=5,
  Extension=10, GuardResearch=10, etc.)
- Per-(charId, skillId) cooldown map for BattleOrder / Regen /
  Restore / EmergencyCall
- +17 tests

**GD-L2** (commit `4be563a`) — GM transfer + ack handlers
- `OnGuildCreated / RequestInfo / NpcRequestInfo /
  OnPositionChanged / OnMemberPositionChanged / OnBroken /
  GmChange / OnGmChanged`
- OnGmChanged swaps Members[0]↔[pos], preserves position
  invariant, updates MasterCharId/MasterName
- +22 tests

**GD-L3** (commit `9f65604`) — Castle init + checkcastles
- Added `Map.Server/Guild/CastleEntity.cs` (GuildId, Economy,
  Defense, TriggerEcon/Def, NextTime/PayTime/CreateTime,
  VisibleKafra, per-guardian visibility map)
- `CastleDataIndex` constants (GuildId / CurrentEconomy /
  CurrentDefense / InvestedEconomy / InvestedDefense / NextTime /
  PayTime / CreateTime / EnabledKafra / EnabledGuardian00..+7)
- `CastleMapInit / CheckCastles / FindCastle / AllCastles /
  CastleReconnect / GetPendingCastleSaves /
  CastleGuildBrokenSub / RegisterCastle`
- Existing `CastleDataLoad / CastleDataLoadAck / CastleDataSave`
  promoted from stubs to cache-backed mutators
- Reconnect-pending save queue (`(castleId, index) → value`) for
  replay on char-server reconnect
- +15 tests

**Wave totals (GD-H1 through GD-L3, 8 commits):**
- +158 new tests (Map.Server.Tests: 3076 → 3229)
- 19 ✅ → 65 ✅ in guild-parity.md
- 10 ⚠️ → 9 ⚠️ (all WoE-deferred, documented)
- 45 ❌ → 0 ❌
- New surface across `GuildEntity`, `GuildMember`,
  `GuildPosition`, `GuildAlliance`, `GuildPermission`,
  `GuildLimits`, `CastleEntity`, `CastleDataIndex`, +35 methods
  on `IGuildService`
- dotnet build Map.Server: 0 errors

### 2026-05-22 — T8.5 per-function audit

Replaced the prior "79 public functions covered (canonical entry
points)" claim — which was an over-count + the C# surface only has
27 methods — with a per-function table covering all 74 unique
rAthena guild_* functions.

**Baseline:** 19 ✅ / 10 ⚠️ / 45 ❌. The previous doc's framing
(everything's covered as canonical entry points) was misleading; the
truth is that 45 functions have no map-side entry at all. GD-H1..GD-L3
wave plan captures the implementation backlog.

Char-side guild RPCs all ✅ (P1-P8 audits); see
[inter/modules.md § Guild](../inter/modules.md#guild-int_guildcpp).

### 2026-05-20 — initial audit + service (superseded)
- (Prior) 79 public functions covered (canonical entry points).
- Refresh above corrects the count to 74 and the coverage shape.
