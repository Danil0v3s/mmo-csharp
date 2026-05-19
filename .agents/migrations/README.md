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
| [Login](login/status.md) | ✅ ~100% | Cross-server online registry done (P3); char→map address fan-out folded into P5 |
| [Char (packets)](char/packets.md) | ✅ 100% | Two minor divergences resolved in P2 (deliberate / not-real) |
| [Char (gRPC server)](char/grpc.md) | ✅ ~98% | Remaining: KeepAlive + RequestAddressSync stubs (deferred to P6 map wiring) |
| [Char (connect flow)](char/connect-flow.md) | ✅ 100% | Cross-server dup-online wired in P3 |
| [Map (IPC integration)](map/ipc-integration.md) | ✅ Infrastructure 100% / 🔁 Gameplay ops 0% | All lifecycle triggers wired (startup/timers/connect/disconnect/shutdown); module ops gameplay-wait |
| [Map (replay baseline)](map/replay-baseline.md) | ⚠️ Whole connect flow structurally matched | 19 remaining diffs are all gameplay-content placeholders (inventory items, skill data, achievements, NPC-script output) — each unblocks when its parent subsystem ports. `ZC_REPUTATION_LIST` intentionally omitted for live-client compatibility. |
| [Map (live client validation)](map/ROADMAP.md) | ✅ End-to-end | DHXJ (PACKETVER 20220401) autologin → autocreate → spawn at prontera → walk works against the live stack. Three client-compat fixes shipped 2026-05-17. |
| [Map (MS3 combat / status / skills / mob AI)](map/adjacent/combat.md) | ✅ Foundation 100% / 🔁 Long-tail data | BattleStats + renewal damage calc, auto-attack loop, mob AI, status changes (5 SCs), skill cast + ground units (8 skills), EXP / level / party share, equip → stats, pet/summon AI, sit/stand, items use, stat & skill allocation, trade / shop / storage services + wire packets end-to-end, public chat. Strategy-pattern dispatch in skill resolution, item effects, request-action handler, mob-skill conditions. **303 tests green.** Wire packets for the data side of skill_db / item Scripts and the long tail (equip/unequip, drop, whisper/party/guild chat, mail, quest, …) are the remaining work — service contracts are stable. See [combat.md history](map/adjacent/combat.md#history). |
| [Map (scripting)](map/scripting/README.md) | 🔁 Planning | TypeScript-authored, Jint-executed runtime. Five typed registrars (`registerNpc`/`registerFloatingNpc`/`registerShop`/`registerWarp`/`registerSpawn`). Phase 1 (engine + NPC entity placement + render; `onClick` stubbed) detailed in [phase-1-loader.md](map/scripting/phase-1-loader.md). Reference enumeration of rAthena's scripting system at [rathena-reference.md](map/scripting/rathena-reference.md). Old hand-rolled-AST plan at [npc.md](map/npc.md) marked superseded. |
| [Inter base](inter/base.md) | ✅ 100% | All routing wired (P5); map-side client emission is gameplay work |
| [Inter modules](inter/modules.md) | ✅ Char side 100% / 🔁 Map side 0% | Map-side callers missing (P6) |

## Roadmap

**Start here:** [ROADMAP.md](ROADMAP.md) — the sequenced 7-phase plan to complete Login + Char + Interop parity before any map-server gameplay work. Phases ordered by dependency, with explicit acceptance criteria.

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
