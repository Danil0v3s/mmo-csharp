# Map server gameplay migration roadmap

Pre-map IPC parity is locked in (commits `ae80d3a` → `8f66d5e`, tag `pre-map-parity-complete`). Now we port the rAthena map server's gameplay code into `Map.Server`. This roadmap orders the work by user-priority: enter a map and walk around first, then NPCs and mobs, then everything that hangs off of those.

**rAthena source:** [/Volumes/1TB/Projetos/rathena/src/map/](/Volumes/1TB/Projetos/rathena/src/map/) — 153K lines across the core gameplay files. Don't underestimate the scope; the goal is parity, not invention.

## Phase ordering

```
MS1 — Enter map, walk around  ──┐
                                ├─ Independent within phase; doable in parallel
MS2 — NPCs + mobs               │  with explicit ordering inside each phase.
                              ──┘

MS3 — Adjacent systems
    (combat, skills, items, status, chat, trade, gameplay modules)
    ── triggered after MS1/MS2 land; many of these can also parallelize.
```

The user's stated priority: **MS1 first, then MS2, then plan MS3 in parallel-friendly pieces.**

---

## MS1 — Enter the map and walk around

Goal: a player who has completed char-select can TCP-connect to map, walk anywhere on a loaded map, and other players in view range see them moving. No combat, no NPCs (yet), no skills.

Subsystems (each has its own dedicated plan file, all under `.agents/migrations/map/`):

| Subsystem | Doc | Depends on | rAthena ref |
|---|---|---|---|
| World data (cell grid, warps, map list) | [world.md](world.md) | — | `mapcache.cpp`, `map.cpp` |
| Entity model (block list, lifetime) | [entities.md](entities.md) | world | `map.cpp` (block list), `unit.cpp` |
| Player session (TCP enter → spawn) | [session.md](session.md) | entities, IPC (done) | `clif.cpp` (enter handlers), `pc.cpp` |
| Movement (pathfinding, walk loop) | [movement.md](movement.md) | world, entities | `path.cpp`, `unit.cpp` |
| Visibility (view range, area notify) | [visibility.md](visibility.md) | entities | `map.cpp` `map_foreachinrange`, `clif.cpp` |
| Packet inventory (CZ_/ZC_ for MS1) | [packets.md](packets.md) | cross-cutting | `clif.cpp` |

**Dependency graph within MS1:**

```
packets.md (reference, can start anytime)
world.md ──┐
entities.md ┼── session.md ──── movement.md ── visibility.md
            └────────────────────┘
```

**MS1 acceptance:**
1. Two players connect to map, both visible on the same map, can move and see each other walk.
2. Movement validates path against cell grid (no walking through walls).
3. Disconnect cleanly removes the player from view of others (already wired via IPC P6).
4. Server can handle ≥50 concurrent walking players without dropped packets or pathfinding stalls.

---

## MS2 — Parse and spawn entities

Goal: NPCs and mobs appear on map at the right locations, players see them on `ZC_NOTIFY_STANDENTRY`, NPCs respond to click (basic dialog), mobs respawn after death timer (death-via-GM-command for now; real combat is MS3).

| Subsystem | Doc | Depends on | rAthena ref |
|---|---|---|---|
| mob_db catalog (DB-backed via `IMobRepository`) | [mob-db.md](mob-db.md) | entities, Core.Database | `mob.cpp` `mob_read_sqldb`, `mob_db_re` table |
| NPC system (parser, dialog) | [npc.md](npc.md) | world, visibility | `npc.cpp`, `script.cpp` |
| Spawn manager + respawn timers | [spawn.md](spawn.md) | mob-db, world | `mob.cpp` `mob_spawn`, `db/mob_spawn.txt` |

**MS2 acceptance:**
1. Map loads its mob spawns from config; mobs appear at the right cells with the right counts.
2. Player walking onto a map sees NPCs/mobs in view range via `ZC_NOTIFY_STANDENTRY`.
3. Clicking an NPC opens a dialog (server emits `ZC_SAY_DIALOG` + buttons).
4. A killed mob (via GM command) respawns after the configured delay.

**Script engine scope decision** (deferred until reading [npc.md](npc.md)): rAthena's `script.cpp` is 28K lines. We need to decide on either (a) port a minimal subset of the script VM, (b) port script parsing to a typed AST and re-implement common builtins, or (c) DB-driven NPCs only (no script). See [npc.md](npc.md).

---

## MS3 — Adjacent gameplay systems

Everything else, after MS1+MS2 are usable. These can largely run in parallel. Detailed plans in [adjacent/](adjacent/):

| System | Doc | When |
|---|---|---|
| Combat (attack, damage calc) | [adjacent/combat.md](adjacent/combat.md) | needs MS1 movement + MS2 mobs |
| Skills (skill_db, skill_use) | [adjacent/skills.md](adjacent/skills.md) | needs combat |
| Items (inventory, drops, pickup) | [adjacent/items.md](adjacent/items.md) | needs entities; partly MS1 (item pickup as walking-into) |
| Status (status changes, buffs) | [adjacent/status.md](adjacent/status.md) | needs skills + combat |
| Chat (whisper / party / guild) | [adjacent/chat.md](adjacent/chat.md) | most IPC done (P5); needs map-side client emission |
| Trade + vending + buyingstore | [adjacent/trade.md](adjacent/trade.md) | needs items |
| Party / Guild / Mail / Quest gameplay | [adjacent/gameplay-modules.md](adjacent/gameplay-modules.md) | IPC done (P6 wrappers); wire gameplay triggers |

---

## Cross-cutting conventions

- **Renewal only.** All data follows rAthena renewal. Damage formulas, stat curves, defense behavior, MATK split, etc. mirror `db/re/` and the `_re` SQL tables. Pre-renewal is permanently out of scope.
- **Static catalogs come from the database.** mob_db, item_db, skill_db, and friends are loaded once at map-server startup from `Core.Database` repositories (`IMobRepository`, `IItemRepository`, …). This matches rAthena's `use_sql_db: yes` mode in `conf/inter_athena.conf`; the seeded SQL schema in [Core.Database/Seeds/Scripts/](../../../Core.Database/Seeds/Scripts/) is column-for-column parity with `item_db_re` / `mob_db_re`. The YAML parsers under `db/re/*.yml` are reference material, not a runtime source — the map server doesn't ship a YAML reader.
- **Source of truth is rAthena.** Every new subsystem file links to the relevant `.cpp` files. When in doubt, read those, not invent.
- **DB schemas are already in `Core.Database`.** 74 entities + configurations. No new entities for MS1/MS2 should be needed; for MS3 some new tables may be required (e.g. a server-side cell-aux table for mob spawn state — TBD).
- **Char-side IPC is the persistence boundary.** Map.Server must never write to the DB directly except via the gRPC surface owned by Char.Server. (P6 already wired the lifecycle calls; gameplay-triggered calls — party/guild/mail/etc. — are stub-call-ready in `Map.Server/Services/CharServerIpcService.*.cs`.)
- **Game loop is 60 FPS.** Movement, status ticks, AI, and packet flushing all run inside `MapServerImpl.UpdateGameLogicAsync`. Heavy DB work goes off the loop via the IPC service (async).
- **Single-threaded gameplay.** The map's game state is single-threaded by design (matches rAthena). Socket I/O is concurrent at the session edge, but game state mutation happens on the tick.
- **Packet versioning.** rAthena targets specific Ragnarok client versions. We need to pin the packet version this server targets (likely 2020-XX-XX or similar). See [packets.md](packets.md) for the version pinning discussion.

---

## What's already in place (from P1–P8)

- TCP listener for clients on map's port 5191 ([MapServerImpl.cs](../../../Map.Server/MapServerImpl.cs))
- gRPC server for IPC from char/login on port 6003
- Periodic timers (registration, keep-alive, user-count sync, autosave) ✅
- `EnterMap` gRPC trigger wired to `SetCharacterOnline` + load cooldowns/scdata/bonus ✅
- `LeaveMap` gRPC trigger wired to `SaveCharacterState` + `SetCharacterOffline` ✅
- Shutdown trigger (save all + `SetAllCharactersOffline`) ✅
- Inter-base routing receivers (broadcast/whisper/etc.) — log+ack stubs awaiting gameplay
- `ForceDisconnectAccount` map-side handler ✅
- Per-module typed IPC wrappers ready in [Map.Server/Services/CharServerIpcService.*.cs](../../../Map.Server/Services/)

## What's NOT in place

- No map cell data loading — see [world.md](world.md)
- No client TCP packet handlers beyond the placeholder `CZ_HEARTBEAT` — see [packets.md](packets.md)
- No spatial entity tracking — see [entities.md](entities.md)
- No pathfinding — see [movement.md](movement.md)
- No NPCs or mobs — see [mob-db.md](mob-db.md), [npc.md](npc.md), [spawn.md](spawn.md)
- No combat, skills, items, status — see [adjacent/](adjacent/)

---

## History

- **2026-05-16** — Renewal locked as the only supported mode. All data sources, formulas, and packet handling target rAthena renewal exclusively; pre-renewal removed from scope across `world.md`, `combat.md`, `packets.md`.
- **2026-05-16** — Roadmap created after P8 closed the pre-gameplay IPC audit. User priority: enter map + walk first, NPCs + mobs second, adjacent systems planned but lower priority. Subsystem files written same day.
- **2026-05-16** — Switched static-catalog source from rAthena's YAML to the seeded `Core.Database` tables (rAthena's `use_sql_db: yes` parity). The YAML mob-db reader shipped in MS2 is being replaced by an `IMobRepository`-backed loader; item_db / skill_db follow the same pattern. The 28K+ rows under `Core.Database/Seeds/Scripts/seed_{item,mob}_db_*.sql` become the runtime authority.
