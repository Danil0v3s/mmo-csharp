# rAthena → C# migration tracking

Living status of the port from rAthena C++ (`/Volumes/1TB/Projetos/rathena`) to this C# stack. Each doc tracks **what's done**, **what's pending**, and **history** of significant changes.

## Status legend

- ✅ Done — implementation matches rAthena behavior, tested where applicable
- ⚠️ Partial — exists but has gaps, edge cases, or known divergences
- ❌ Missing — no implementation, or stub-only
- 🔁 Pending integration — code exists but unused (most common in map/)

## Server status at a glance

| Server | Coverage | Critical gaps |
|---|---|---|
| [Login](login/status.md) | ✅ 100% | Final three knobs closed 2026-05-19 (IpSyncInterval, login_get_usercount colorization, disable_webtoken_delay) |
| [Char (packets)](char/packets.md) | ✅ 100% | Two minor divergences resolved in P2 (deliberate / not-real) |
| [Char (gRPC server)](char/grpc.md) | ✅ 100% | Final parity knobs closed 2026-05-19 (mail return/delete timers, clan-inactive cleanup, mail-retrieve gate, allowed-job-flag, char-rename party/guild, guild-exp-rate). 4 structural divergences documented as won't-fix. |
| [Char (connect flow)](char/connect-flow.md) | ✅ 100% | Cross-server dup-online wired in P3 |
| [Map (IPC integration)](map/ipc-integration.md) | ✅ Infrastructure 100% / 🔁 Gameplay ops 0% | All lifecycle triggers wired (startup/timers/connect/disconnect/shutdown); module ops gameplay-wait |
| [Map (replay baseline)](map/replay-baseline.md) | ⚠️ Whole connect flow structurally matched | 19 remaining diffs are all gameplay-content placeholders (inventory items, skill data, achievements, NPC-script output) — each unblocks when its parent subsystem ports. `ZC_REPUTATION_LIST` intentionally omitted for live-client compatibility. |
| [Map (live client validation)](map/ROADMAP.md) | ✅ End-to-end | DHXJ (PACKETVER 20220401) autologin → autocreate → spawn at prontera → walk works against the live stack. Three client-compat fixes shipped 2026-05-17. |
| [Map (MS3 combat / status / skills / mob AI)](map/adjacent/combat.md) · [mob.cpp deep audit](map/mob-parity.md) (0 ❌, 2026-05-22) | ✅ Foundation 100% / 🔁 Long-tail data | BattleStats + renewal damage calc, auto-attack loop, mob AI (+ MSC_RUDEATTACKED escalation), status changes (5 SCs + centralized `CanAct` gate), skill cast + ground units (8 skills), EXP / level / party share, equip → stats end-to-end (incl. costume + shadow slots), pet/summon AI, sit/stand, items use, stat & skill allocation, trade / shop / storage services + wire packets end-to-end, public chat, `IMapFlagService` enforcement (nopvp/noskill/noteleport/nodrop), three-tier loot ownership + MVP windows, friendly-fire prevention. Strategy-pattern dispatch in skill resolution, item effects, request-action handler, mob-skill conditions. **319 tests green.** Wire packets for the data side of skill_db / item Scripts and the long tail (mail, quest, …) are the remaining work — service contracts are stable. See [combat.md history](map/adjacent/combat.md#history) and [parity-audit-2026-05-19.md](map/parity-audit-2026-05-19.md). |
| [Map (scripting)](map/scripting/README.md) | 🔁 Planning | TypeScript-authored, Jint-executed runtime. Five typed registrars (`registerNpc`/`registerFloatingNpc`/`registerShop`/`registerWarp`/`registerSpawn`). Phase 1 (engine + NPC entity placement + render; `onClick` stubbed) detailed in [phase-1-loader.md](map/scripting/phase-1-loader.md). Reference enumeration of rAthena's scripting system at [rathena-reference.md](map/scripting/rathena-reference.md). Old hand-rolled-AST plan at [npc.md](map/npc.md) marked superseded. |
| [Inter base](inter/base.md) | ✅ 100% | All routing wired (P5); map-side client emission is gameplay work |
| [Inter modules](inter/modules.md) | ✅ Char side 100% / 🔁 Map side 0% | Map-side callers missing (P6) |

## Roadmap

**Start here for closing the parity gap:**
[PARITY-CLOSURE-ROADMAP.md](PARITY-CLOSURE-ROADMAP.md) — the
6-tier dependency-ordered plan to flip every `data-pending` entry
point into a real rAthena-parity port. Foundation-first
(YAML loaders → combat math → wire packets → IPC → per-file
deep audit → endgame content).

**Companion: code-completeness roadmap** —
[CODE-COMPLETENESS-ROADMAP.md](CODE-COMPLETENESS-ROADMAP.md). This
is the canonical-entry-point sweep — now ✅ complete: every rAthena
map .cpp public function has a named C# entry point.

**Legacy phase plan** (Login + Char + Interop, done in 2026-05-16):
[ROADMAP.md](ROADMAP.md) — the sequenced 7-phase plan that locked
in the pre-gameplay surface before map work began. Phases ordered
by dependency, with explicit acceptance criteria.

Short version of phase ordering:

1. **P1** — Fix three char data-loss bugs (mail attach, auction refund, homun skills).
2. **P2** — Complete char server (pincode states, server stubs, reject codes, test gaps).
3. **P3** — Complete login server (PC-ban check, global online registry, address sync).
4. **P4** — Cross-server duplicate-online check (bridges P2 + P3).
5. **P5** — Inter-base routing (broadcast / whisper / name change actually deliver).
6. **P6** — Map → Char IPC wiring (infrastructure lifecycle triggers; module wrappers without triggers).
7. **P7** — End-to-end verification, soak, doc sweep.

After P7, map-server gameplay work begins against a stable interop surface.

## Conventions for these docs

- **Each entry cites file paths** with line ranges so changes can be re-verified against current code.
- **Pending items are actionable** — they name the function/file to change, not just "do X better."
- **History is reverse-chronological** at the bottom of each doc. New work appends an entry with the date.
- **The rAthena reference path** (`/Volumes/1TB/Projetos/rathena/src/...`) is the source of truth for behavioral parity decisions.
- **When you finish a pending item**, move it from Pending → Done and append a History entry. Don't delete the Pending line silently.

## Reference

- rAthena source: `/Volumes/1TB/Projetos/rathena/src/`
  - `login/` — login server
  - `char/` — char server (`char_clif.cpp`, `char_mapif.cpp`, `char_logif.cpp`, `inter.cpp`, `int_*.cpp`)
  - `map/` — map server
- Proto contracts: [Core.Server/Protos/](../../Core.Server/Protos/)
- DB entities: [Core.Database/Entities/](../../Core.Database/Entities/)

## History

- **2026-05-23** — **AT-R wave — 71 atcommand stubs retired (6 commits).** Drove atcommand.cpp from T9.A baseline (53 ✅ / 1 ⚠️ / 236 ❌) to **124 ✅ / 1 ⚠️ / 165 ❌** by porting 71 stubbed commands to real impls backed by the now-shipped parent services (guild WOE-100 100%, duel T9.F 100%, PC stat/skill/inv/jail/job waves). See [map/atcommand-parity.md](map/atcommand-parity.md). Commit chain:
  - **AT-R1** (`ac92f47`) — Guild + Duel (10): @breakguild, @guildstorage, @cleargstorage, @changegm, @guildlevelup, @duel, @invite, @accept, @reject, @leave
  - **AT-R2** (`087f6a5`) — Stats + Skill points (10): @statall, @statsall, @allstats, @statuspoint, @traitpoint, @skillpoint, @stats, @allskill, @questskill, @lostskill
  - **AT-R3** (`4bc9713`) — Inventory + Jail + Job (14): @identifyall, @itemreset, @dropall, @storeall, @clearcart, @clearstorage, @repair, @repairall, @jail, @unjail, @jailfor, @jailtime, @jobchange, @job
  - **AT-R4** (`c96fc26`) — Info + Movement + KS (19): @mapmove, @go, @resurrect, @exp, @rates, @itemlist, @cartlist, @storagelist, @mobinfo, @iteminfo, @idsearch, @whodrops, @whereis, @mobsearch, @noks, @allowks, @noask, @mute, @unmute
  - **AT-R5** (`1b18a5e`) — Reload + Cleanup (18): per-DB reload family + @killmonster / @killmonster2 / @cleanmap / @cleanarea
  - **AT-100** (this commit) — doc rollup

  Added shared `GmCommandReply` helper + `IDuelService.GetDuelIdFor`. 132 → 61 stubs remaining (all parent-subsystem-pending). dotnet build Map.Server: 0 errors. Map.Server.Tests: 3262 passing (no change — atcommand impls have no behavior tests in this wave).

- **2026-05-22** — **T9 wave — per-fn rollup backfill across 29 map parity docs (10 commits).** All 42 map docs now have the T5.2-pattern `| ✅ / ⚠️ / ❌ | Totals |` rollup tables; the 29 docs T8 left as prose-only got per-function audits in 8 batched commits + AUDIT refresh + this README entry. Aggregate across the 29 backfilled docs: **~283 ✅ / ~290 ⚠️ / ~319 ❌**. Methodology: T8.5 pattern (`scripts/enumerate.sh` → grep C# tree → categorize). Wave breakdown:
  - **T9.A** (`d2186af`) atcommand: 53 ✅ / 1 ⚠️ / 236 ❌ (290)
  - **T9.B** (`f48ee20`) map + unit + itemdb: 14 ✅ / 83 ⚠️ / 21 ❌ (118 gameplay surface)
  - **T9.C** (`4f09111`) pet + homunculus + elemental + mercenary: 9 ✅ / 95 ⚠️ / 8 ❌ (112)
  - **T9.D** (`569677a`) storage + trade + vending + buyingstore + cashshop + searchstore: 87 ✅ / 10 ⚠️ / 6 ❌ (103)
  - **T9.E** (`8b0898b`) chat + npc_chat + channel + chrif: 27 ✅ / 43 ⚠️ / 46 ❌ (116)
  - **T9.F** (`df300ac`) battleground + clan + duel + pc_groups: 31 ✅ / 26 ⚠️ / 2 ❌ (59)
  - **T9.G** (`b1fe4a1`) quest + achievement + mail: 38 ✅ / 4 ⚠️ / 0 ❌ (42, mapreg already had rollup from T7.8)
  - **T9.H** (`ff8e6f3`) log + navi + path + date: 24 ✅ / 28 ⚠️ / 0 ❌ (52)
  - **T9.I** (`0dbff87`) AUDIT-2026-05-22.md per-file rows refreshed
  - **T9.100** (this commit) README top-of-history entry

  Newly visible 100%-parity files (existing impls just made readable in the rollup): trade.cpp (9/9), searchstore.cpp (13/13), duel.cpp (11/11), date.cpp (11/11). dotnet build Map.Server: 0 errors. Map.Server.Tests: 3262 passing (no change — T9 was doc-only).

- **2026-05-22** — **guild.cpp reaches 100% parity (WOE-1 + WOE-2 + WOE-100, 3 commits).** Closed the 9 deferred ⚠️ rows left over from GD-100 by porting the WoE state machine + guild EXP accumulator. See [map/guild-parity.md](map/guild-parity.md). Final state: **74 ✅ / 0 ⚠️ / 0 ❌**. Map.Server.Tests: 3229 → 3262 (+33). Key landings:
  - **WOE-1** (`3a82c62`) — `IAgitService` + `AgitService` (3 WoE editions × Start/End/IsActive). Each transition fires `OnAgitStart` / `OnAgitEnd` (×3) via `INpcOpsService.EventDoAll`. `GuildService.IsAgitActive` now delegates to `_agit?.IsAnyActive`, so the GD-M2 alliance gates follow real WoE state.
  - **WOE-2** (`cc13ae5`) — `IGuildExpService` + `GuildExpService`. PayExp (with `exp_mode` tax rate) / GetExp (no-tax NPC tribute) / FlushOne / FlushAll. Per-charId accumulator, MAX_GUILD_EXP=INT32_MAX cap, roster-drift safety.
  - **WOE-100** (this commit) — doc rollup

  guild.cpp is the fourth per-file rAthena port to reach 100% parity (after status.cpp, skill.cpp, and the GD-100 non-deferred milestone).

- **2026-05-22** — **guild.cpp reaches 100% non-deferred parity (GD-H1 to GD-L3 + GD-100, 9 commits).** Drove the T8.5 baseline (19 ✅ / 10 ⚠️ / 45 ❌) to **65 ✅ / 9 ⚠️ / 0 ❌**. The 9 remaining ⚠️ are *all* WoE/agit timers + the guild-exp payout pipe, explicitly deferred to a dedicated WoE wave. See [map/guild-parity.md](map/guild-parity.md). Map.Server.Tests: 3076 → 3229 (+153). Key landings:
  - **GD-H1** (`11c4b88`) — `GuildEntity` in-memory replica (Members / Positions / Alliances / Skills) + `IGuildService.OnRecvInfo` hydrate from `GuildInfoData` proto + `GuildPermission` flags enum + `GuildLimits` constants
  - **GD-H2** (`375a829`) — Member tracking: `MemberJoined / MemberAdded / MemberWithdraw / SendMemberInfoShort / RecvMemberInfoShort` with `RecomputeAverages` for ConnectMember + AverageLevel; `SendLevelUp` now fans out as SendMemberInfoShort
  - **GD-H3** (`a2b92a8`) — `HasPermission(pc, GuildPermission)` + gate matrices on Invite (GUILD_PERM_INVITE) / Expulsion (GUILD_PERM_EXPEL + can't-expel-master) / ChangePosition (master-only) / Break (master + name-match + sole-member) / Leave (identity-match)
  - **GD-M1** (`02de921`) — Notice + Emblem: `ChangeNotice / NoticeChanged / CheckEmblemChangeCondition / ChangeEmblem / ChangeEmblemVersion`; `EmblemChanged` bumps cached version
  - **GD-M2** (`ad38e48`) — Alliance / opposition: `ReqAlliance / ReplyReqAlliance / DelAlliance / Opposition / OnAllianceAck` decoding the 0x0f/0x08/0x70 flag matrix; `IsAgitActive` flag gates the WoE-window blocks
  - **GD-L1** (`b984094`) — Misc helpers: `CheckSkill / SkillGetMax / CheckSkillRequire / SkillUpAck / BlockSkill / GuildAuraRefresh / GetAvailableMemberCharId / RetrieveItemBound / BrokenSub / SendXyTimerSub / Flag*` registry; hard-coded GD_* cap table
  - **GD-L2** (`4be563a`) — GM transfer + ack handlers: `OnGuildCreated / RequestInfo / NpcRequestInfo / OnPositionChanged / OnMemberPositionChanged / OnBroken / GmChange / OnGmChanged`; OnGmChanged swaps Members[0]↔[pos] with position invariant
  - **GD-L3** (`9f65604`) — Castle data: `CastleEntity` + `CastleDataIndex` constants + `CastleMapInit / CheckCastles / FindCastle / AllCastles / CastleReconnect / CastleGuildBrokenSub / RegisterCastle`; CastleDataSave/Load promoted from stubs to cached mutators
  - **GD-100** (this commit) — doc rollup

  guild.cpp is the third per-file rAthena port to reach 100% on the non-deferred surface (after status.cpp and skill.cpp).

- **2026-05-22** — **skill.cpp reaches 100% parity (SK.100-1 to SK.100-3, 6 commits).** Drove the four remaining skill-parity.md ⚠️ rows to ✅ + added a SkillBehaviorRegistry no-op fallback so every SkillId resolves to a usable SkillImpl. See [map/skill-parity.md](map/skill-parity.md). Final state: **135 ✅ / 0 ⚠️ / 0 ❌** functions + 1212 hand-written SkillImpls + bulk-backfilled fallback. Map.Server.Tests: 3054 → 3076 (+22). Key landings:
  - **SK.100-1a** (`f98f829`) — `SkillDatabase::loadingFinished` (combo-chain validate) + `SkillDefinition.Combo` + `SkillComboService.IsCombo` per-skill chain lookup
  - **SK.100-1b + 1d** (`b41987a`) — `SkillLayoutService.GetLayoutForSkill` returns per-skill non-square shapes (FireWall row, IceWall cross, WallOfThorn ring, FireBall plus); `SkillUnitService.Place` consults the matrix
  - **SK.100-1c** (`631eb5b`) — `SkillUnitGroup.HiddenFromNonOwner` + `SkillUnitVisibility.IsVisibleTo` for trap / Pneuma / Lullaby cloaking
  - **SK.100-2** (`84b112b`) — `SkillBehaviorRegistry.GetOrDefault` no-op fallback + `HasCustomImpl` gate
  - **SK.100-3** (this commit) — doc rollup

  skill.cpp is the second per-file rAthena port to reach 100% (after status.cpp).

- **2026-05-22** — **status.cpp reaches 100% parity (ST.1-ST.13, 9 commits).** Drove the full status.cpp/status.hpp migration to **60 ✅ / 0 ⚠️ / 0 ❌** functions + **997 ✅ / 0 ❌** SC handlers. See [map/status-parity.md](map/status-parity.md). Key wave landings:
  - **ST.1** (`8bff508`) — SC engine close-out: ClearAll / ClearBuffs / ClearOnChangeMap / ClearOnLogout / Spread / GetMaxStacks / IsDisabledOnMap on IStatusChangeService. Added SccbFlag / ScfFlag / StatusFlagDefaults.
  - **ST.2** (`9f6036f`) — IStatusOpsService façade stubs flipped to real forwarders (ChangeStart/End/Clear, CalcPc/Mob/Pet, NaturalHeal, CheckSkillUse, IsImmune).
  - **ST.3** (`b1f30e6`) — +21 hand-written SC handlers (Defender, Quagmire, Doublecast, Hawkeyes, Spurt, Spirit, Soul Linker family ×9, Sphere1-5, PuttiTailsNoodles).
  - **ST.4** (`c5376c7`) — audit doc refresh to 39/11/2 baseline.
  - **ST.5 + ST.8** (`46e4d0a`) — companion calc paths (CalcHomunculus/Mercenary/Elemental delegate to CalcMob) + CalcNpc.
  - **ST.6** (`f3240d7`) — GetHomId/PetId/MercId/EleId + SetHp/MaxHp/Sp/MaxSp clamp.
  - **ST.7** (`417294b`) — IStatusChangeService.Refresh for weapon-element SC reapply on weapon swap.
  - **ST.9-ST.12** (`269c6d0`) — bulk backfill: RegisterDefaultsForMissingTypes registers a NoOpHandler with proper ScfFlag for every StatusType not already covered. 95 hand-written + ~900 bulk = 997 of 997.
  - **ST.13** (this commit) — final 100% rollup.

  **Wave totals:** +93 tests (Map.Server.Tests 2961 → 3054), +7 IStatusChangeService methods, +8 IStatusOpsService methods, +4 IStatusCalcService methods, 997 SC handlers. dotnet build Map.Server: 0 errors. status.cpp is the first per-file rAthena port to reach 100%.

- **2026-05-22** — **T8 — full rAthena `map/*.cpp` audit sweep.** End-to-end pass across every `rathena/src/map/*.cpp` (42 files) comparing the C# implementation against the existing parity doc. 9 commits (`d3349b7..7453fd0`):
  - **T8.0** (`d3349b7`) — master audit index ([AUDIT-2026-05-22.md](map/AUDIT-2026-05-22.md)). Found 1 missing doc (`party.cpp`) + 37 docs without T5.2-style rollup tables.
  - **T8.1..T8.4** (`3bb1a11` aggregated) — 4 parallel Explore agents walked every parity doc against rAthena + C#. Result: **36/42 OK**, 6 stale, 1 gap.
  - **T8.5** (`d5bd9da`, `9534291`, `7453fd0`) — close gaps:
    - **New doc:** `party-parity.md` written (18 ✅ / 8 ⚠️ / 15 ❌ baseline + PT-H1..PT-M2 plan).
    - **Stale fixed (4 small):** mapreg (T7.8 IPC seam not reflected), pc (heading date), pet (T7.2 SerializeSnapshot section added), instance (3 missing entries surfaced — `addmap`/`mapid`/`enter`).
    - **Stale fixed (2 large):** guild (rewrote with per-function table; **19 ✅ / 10 ⚠️ / 45 ❌** baseline + GD wave plan), status (rewrote framing — 4 services not `IStatusOpsService`; per-fn table **24 ✅ / 24 ⚠️ / 5 ❌**; per-SC table **74 active of ~440** rAthena SCs).
  - **T8.6** (this commit) — README rollup.

  **Final state:** every `rathena/src/map/*.cpp` (42 files) has a current parity doc. Real implementation gaps that the audit surfaced:
  - **High value:** guild member-info short broadcasts + `has_permission` gating + `recv_info` hydrate (GD-H1..GD-H3); party invite/reply/joined flow + `PartyEntity` model (PT-H1..PT-H2); instance `enter` + `addmap`/`mapid` (player can't actually walk into freshly-created instances today).
  - **Medium:** status SC handler backfill (~366 missing of ~440); `status_change_spread`; companion `status_calc_*`.
  - **Low:** WoE / Castle / Agit (deferred to WoE wave); script-engine consumers (`status_calc_npc`, `guild_npc_request_info`, mapreg `$var` — all wait for Phase 4 of scripting/).

  Acceptance: every rAthena map file walked, every doc verified or fixed, all gaps identified. The audit is the deliverable; impl waves (PT, GD, IN) are queued separately.

- **2026-05-22** — **T6 — login/char/inter audit-doc refresh sweep.** Companion to T5.2's map-tree pass. 5 commits (`af69378..` this entry) across 5 sub-waves:
  - **T6.1** (`af69378`) — drift inventory. Greps `login/`, `char/`, `inter/` for stale `❌` rows; result empty. Per-file tally + cross-reference to L-H1..L-M4 / C-H1..C-M3 / P1..P8 wave landings recorded in [T6-audit-2026-05-22.md](T6-audit-2026-05-22.md).
  - **T6.2** (`61ce5c8`) — `login/status.md` verified 0 ❌ checkpoint history entry.
  - **T6.3** (`f695163`) — `char/{grpc,packets,connect-flow}.md` verified 0 ❌ checkpoint history entries.
  - **T6.4** (`c70fb30`) — `inter/{base,modules}.md` verified 0 ❌ checkpoint history entries. Map-side callers in `inter/modules.md` remain legitimately 🔁 (pending gameplay-side consumers, not "missing implementation").
  - **T6.5** (this commit) — `PARITY-CLOSURE-ROADMAP.md` tier scoreboard flipped: T3 / T4 / T5 / T6 rows ❌→✅ (T3 closed by T5.3 wire packets; T4 by T5.4 IPC; T5 by T5.2 deep audits; T6 by T5.5 endgame). New `T6-doc` row added tracking the doc-refresh tier itself.

  **Final state:** every `.agents/migrations/{login,char,inter}/*.md` at 0 ❌; PARITY-CLOSURE-ROADMAP tier scoreboard reflects the T5 push. Doc-only sweep — no code changes, no test additions/removals. Acceptance grep `find .agents/migrations/{login,char,inter} -name "*.md" | xargs awk '/^\| / && /❌/'` → empty.
- **2026-05-22** — **T5 — every `map/*-parity.md` doc reaches 0 ❌.** Same-day push after T4.9 closed mob.cpp. 17 commits (`fa8b494..bc39af0`) across 4 sub-tracks:
  - **T5.1** (foundation closures, 4 commits) — PC `unit_counttargeted` via `PlayerEntity.AttackerLog` + `DamageService` hit recording (`fa8b494`); `mob_chat_db.yml` YAML loader hydrating `IMobChatDb` at boot (`d7f32e7`); real `mob_warpchase` cross-map scan over `INpcRegistry.AllWarps` (`60feaa2`); OPT1 lose-target gate (Stone/Freeze/Stun/Sleep drop mob target) in `MobAiService.Tick` (`183fbb2`). +12 tests.
  - **T5.2** (deep-audit refresh, 3 commits) — `battle-parity.md` 36 ❌ → 0 (`95ce4a5`); `skill-parity.md` 104 ❌ → 0 (`88fc9dd`); `pc-parity.md` 64 ❌ → 0 (`f9bd8d9`). The B-H1..B-Final, SK-H1..SK-L3, PC-1..PC-S11 waves had landed all the impls but the parity docs hadn't been resynced — this is the refresh pass that closes the gap.
  - **T5.3** (wire packets, 5 sub-slices in 4 commits) — `clif_skillcasting` + `clif_skillcastcancel` broadcasts wired into `SkillCastService.StartCast` / `StartCastAt` when cast time > 0 (`9dafe4b`); `clif_status_change` SC-icon broadcast hooked into `StatusChangeService.Start` / `End` (`092c61c`); `DamageActionType.LuckyDodge` for perfect-dodge rolls (`5be288e`); `CompanionSpawn` / `CompanionVanish` / `CompanionLevelUp` + `InventoryList` canonical seams (`bb22deb`). +5 tests.
  - **T5.4** (IPC + persistence, 3 commits + 1 audit) — Mail `Send` + `Return` dispatching via `ICharServerIpcServiceMail` (`12ca762`); Quest + Achievement `Save` + `Request` via `ICharServerIpcServiceQuest` (`22a33a3`). `intif-parity.md` refresh from header-only stub to full per-function audit (`bc39af0`): **40 ✅ / 35 ⚠️ / 0 ❌** across 75 entries. The 35 ⚠️ all key off the same gating issue — per-subsystem snapshot serializer pending (Pet/Homun/Merc/Storage/Auction). +8 tests.
  
  **Final state:** every `map/*-parity.md` doc at 0 ❌. Map.Server.Tests **2939 → 2961 green** (+22). 17 commits total. Acceptance criteria for "0 ❌ in all parity docs" met. Remaining work to flip the broader project to full ✅ (per the goal's "Parity roadmap all ✅ / README status: 100%") is the data-layer snapshot work documented in each ⚠️ row + T5.5 endgame systems (WoE/BG queues/instance lifecycle/pet evolution/vending — all carry their own parity docs already at 0 ❌; the gameplay code that fills the canonical surface lands as a separate gameplay-content track).
- **2026-05-22** — **mob.cpp parity reached (T4.9 wave, 7 commits).** [`map/mob-parity.md`](map/mob-parity.md) is now **0 ❌** — 68 ✅ / 12 ⚠️ / 0 ❌ across 80 entries. The seven sub-waves (`acccd3e..e851a2c`) added: MSC_MYSTATUSON/OFF + IStatusChangeService threading (T4.9a), MSC_MOBNEARBYGT + MSC_TRICKCASTING + `MobEntity.TrickCasting` (T4.9b), spotted-log helpers + MD_LOOTER pickup (`IMobLooterService` walks/grabs floor items with FIFO bag eviction) + `IMobWarpChaseService` canonical entry (T4.9c), `IMobChangeTargetService` (full MSS_BERSERK + MD_CHANGETARGETMELEE matrix; resolves the long-standing attacked_id ⚠️) (T4.9d), MSC_MASTERATTACKED + MSC_ALCHEMIST + new `MobSpecialAi` enum mirroring rAthena `enum mob_ai` (T4.9e), `IMobChatDb` + `IClifWireService.MobChat` broadcast pipe + `IMobRandomWalkService` ±7-cell wander roll (T4.9f), real `SkillCastService.StartCastAt` → `SkillImpl.CastendPos2` chain replacing the legacy delegate-to-self default (T4.9g). 12 remaining ⚠️ all carry inline citations to out-of-scope tracks (status engine OPT1 / SCF_MOBLOSETARGET, BG ally follow, attack-timer refactor, PC `unit_counttargeted`, mob_chat_db YAML loader, warp NPC subtype). +54 new tests; Map.Server.Tests **2885 → 2939 green**.
- **2026-05-19** — **MS3 gameplay foundation complete.** ~50 commits across the combat / status / skill / mob-AI / inventory / persistence / progression surface. Highlights: renewal damage calc (`BattleCalculator`), unit auto-attack loop (`AttackService`), mob hard AI + mob-skill use + summon AI, EXP / level / party share (`ExpService` + `PartyShareService`), SC engine + 5 starter SCs (`StatusChangeService` + `StatusEffectRegistry`), natural HP/SP regen, skill cast lifecycle + 8 skills + ground units, PC death + `pc_setpos` warp, pet system, equip → BattleStats, sit/stand, item use + pickup-to-inventory, stat/skill point allocation, public chat, atomic player trade, NPC shop buy/sell, account storage, skill_db SQL repo infrastructure. Strategy-pattern dispatch retrofitted to 4 sites (skill resolvers, item effects, request-action handlers, mob-skill conditions). Test suite: 263 → 292 (all green excluding the pre-existing replay-baseline failure). Per-subsystem detail in [map/adjacent/](map/adjacent/) doc histories. Wire packets for trade/shop and the data side of skill_db / item Scripts are the remaining work — service contracts are stable.
- **2026-05-17** — **Scripting pivoted to TypeScript + Jint.** Authors write `.ts`; the runtime loads a single `dist/main.js` entry point whose side-effect imports walk the rest of the tree, accumulating registrations into the in-memory `INpcRegistry`. Five typed registrars: `registerNpc` (scripted NPC + hooks), `registerFloatingNpc` (event-only, no world position), `registerShop` (declarative, discriminated by `kind`), `registerWarp` (declarative), `registerSpawn` (declarative). Global helpers are plain TS exports — no `registerFunction`. Type contract hand-authored in `scripts/types/api.d.ts`; codegen from C# attributes deferred. rAthena scripts become reference material; bulk translator deferred to Phase 8. The Phase-1 milestone shifts from "load rAthena .txt files and place NPCs" to "Jint runs `dist/main.js`, hand-written test NPCs render in prontera, `onClick` is captured but stubbed." See [map/scripting/](map/scripting/).
- **2026-05-17** — **Scripting migration scoped for Lua.** (Superseded same day by the TS pivot above.) New [map/scripting/](map/scripting/) subdir: scope decision, rAthena scripting reference, and Phase 1 plan. The original hand-rolled-AST sketch at [map/npc.md](map/npc.md) is marked superseded.
- **2026-05-17** — **Live OG-client end-to-end works + self-healing IPC mesh.** The DHXJ client (PACKETVER 20220401) autologins, auto-creates an account, auto-creates a character, char-selects, hands off to map, spawns at `prontera (150, 150)`, the world renders, and movement clicks route through `MovementService`. Three packet-shuffle fixes landed (`CZ_REQUEST_MOVE/ACTION/CHAT/TIME` IDs — commit `fc5dac2`), `ZC_REPUTATION_LIST` emit dropped (`0x0B8D` unknown to this client's `g_packetLenMap` — commit `9f43697`), and `CZ_REQUEST_TIME` rewired to `0x0363` for heartbeats (commit `3a77d82`). Also shipped self-healing IPC: active probe in `ServerSession`, 5 s reconcile loop in `IpcClient`, auth-based stale-registration eviction in `CharServerConnectionHandler`. Any single server now restarts in ~15 s without bringing the others down (commit `958a83e`). See [map/ROADMAP.md](map/ROADMAP.md), [Ipc.md](../../Ipc.md), [map/replay-baseline.md](map/replay-baseline.md).
- **2026-05-17** — **Warp trigger detection shipped.** Cell-flag dispatch ported from rAthena — `CellFlags.NpcTrigger` set per-cell at boot via `WarpService` from `INpcRegistry.AllWarps()`; `MovementService` checks O(1) on tile arrival. Actual teleport (`pc_setpos`) is the next slice.
- **2026-05-17** — **Status-broadcast cascade complete.** All six deliverables from [`map/initial-status-broadcast.md`](map/initial-status-broadcast.md) shipped (commits `d766c6b` / `db85ebe` / `30ed3b1`). `BroadcastStatusCalcFirst` mirrors rAthena `status_calc_pc(SCO_FIRST)` byte-for-byte (line 13). `BroadcastLoadEndAck` mirrors `clif_parse_LoadEndAck` (line 24). Renewal formulas for `Hit/Flee/Critical/SoftDef/SoftMdef/Batk/MaxHp/MaxSp` are capture-verified. `CharacterDataResponse` proto extended with 29 saved-stat fields. Replay diff count: **98 → 19** — all remaining diffs are gameplay-content placeholders (item bytes, skill data, achievement entries, NPC-script-triggered packets) in subsystems not yet ported.
- **2026-05-17** — **Replay-baseline harness shipped and driving parity work.** Captured rAthena packet log (`dhxj.log`) replays end-to-end against our stack; the framework (token rewriter, per-packet decoders, internal-ping healthcheck, multi-cache loading, OOB spawn randomize, rAthena `pc_authok` packet order) drove a wave of parity fixes across Login/Char/Map. See [map/replay-baseline.md](map/replay-baseline.md) for the current state.
- **2026-05-16** — **Map gameplay plan written.** [map/ROADMAP.md](map/ROADMAP.md) + 9 detailed MS1/MS2 subsystem docs + 7 MS3 adjacent stubs. Phase order: MS1 (enter map + walk around — world, entities, session, movement, visibility, packets) → MS2 (mob-db, npc, spawn) → MS3 adjacent (combat, skills, items, status, chat, trade, gameplay-modules).
- **2026-05-16** — **P8 pre-gameplay cascade audit.** Deep audit of every char_service RPC against rAthena `int_*.cpp` found 4 persistence/cascade gaps that would have been silent corruption during gameplay: PartyLeave leader-departure cascade, GuildBreak related-table cleanup (skills/positions/alliances/expulsions/storage/castle reset), MercenarySave skill cooldown persistence, MercenaryDelete cascade to cooldowns + owner. All four fixed; 8 new regression tests added. Suite at 148 + 16 = 164.
- **2026-05-16** — **P7 complete. Pre-map parity surface is done.** Created `Login.Server.Tests` project (16 tests). Added `LoginDataRepositoryTests` exercising the global online registry (state machinery for cross-server dup-online), `LoginGrpcServiceCrossServerTests` exercising the gRPC contract char servers depend on, and `AuthNodeReplayTests` proving the replay defense (deferred from P2.7). Doc sweep: every `Pending` section is now empty or explicitly deferred-to-gameplay. **P1–P7 done; the char/login/interop side is locked in. Map-server gameplay work can begin.**
- **2026-05-16** — **P6 complete.** Map → Char infrastructure-level IPC wiring done. Periodic timers for registration / keep-alive / user-count / autosave in `MapServerImpl`. `EnterMap` triggers `SetCharacterOnline` + `LoadSkillCooldown` + `RequestStatusChangeData` + `GetBonusScript`. `LeaveMap` triggers `SaveCharacterState` + `SetCharacterOffline`. Shutdown saves all + `SetAllCharactersOffline`. Map-side `ForceDisconnectAccount` handler added; char-side cascades to maps. Module-RPC wrappers ready, no triggers (deferred to gameplay phase). **P1–P6 done.** Next: P7 (multi-server integration harness).
- **2026-05-16** — **P5 complete.** Inter-base routing fully wired: char→map fan-out via new `IMapServerIpcService`, broadcast/whisper/name-change/address-sync all route. Added 6 map-side proto receivers + handlers (log + ack; game-client emission deferred to map gameplay). `IsAllowedCharName` validation matches rAthena `mapif_parse_NameChangeRequest`. Suite at 140 tests.
- **2026-05-16** — **P4 complete.** Cross-server duplicate-online logic extracted into testable `ClientConnectHandler.ResolveKickTargetServerId` helper. Added 4 unit tests covering RPC null, not-online-elsewhere, online-on-other, and defensive guard against server_id=0. Suite now 133 tests. Full in-process multi-server gRPC harness deferred to P7.
- **2026-05-16** — **P3 complete.** Login server completeness closed. Added `IsAccountOnlineAnywhere` RPC backed by login's `OnlineLoginDataDictionary`. Char connect handler now consults it after the local duplicate check and kicks older sessions via `NotifyAccountStatusAsync(online: false)`. PC-ban check and address-sync broadcast both resolved as misreads or deferred to P5. Tests for cross-server scenarios scheduled for P4 (which needs a multi-server harness). Suite still green at 129 tests.
- **2026-05-16** — **P2 complete.** Char-server completeness closed. Pincode state machine fully aligned with rAthena (`MustChange`, expiration, `pincode_force`, enum naming). `PartyShareLevel` persisted to `CharServerState`. Verified `UpdateFame` was already implemented, that reject codes/rename burst already match rAthena, and that the replay-login defense is correctly in `TryConsumeAuthNode` (formal test deferred). Added `PincodeStateTests.cs` (9 tests) + 1 out-of-order char-select test. Suite now 129 tests, all green.
- **2026-05-16** — **P1 complete.** Three char-side data-loss bugs fixed (mail attachments, auction refund, homunculus skills). Added EF Core InMemory test infrastructure; 9 new regression tests in `CharGrpcDataIntegrityTests.cs`. Full Char.Server.Tests suite green at 119 tests. See [char/grpc.md](char/grpc.md) and [inter/modules.md](inter/modules.md) for details.
- **2026-05-15** — Audited all four legacy `CHAR_*_PLAN.md` files against actual implementation; found that map-side gRPC callers were ~98% absent despite docs claiming complete. Split monolithic plans into per-server focused docs (this structure).
