# Code completeness roadmap · 2026-05-20

Axis: **per-file parity sweep across every rAthena map-side .cpp**.
Not vertical gameplay slices — full enumeration of public functions in
each file, mapped to a C# canonical entry point (real, partial, or
documented "data-pending").

Driver skill: `/rathena-parity <path>` (`.claude/skills/rathena-parity/`).
Per-file audit docs live under `.agents/migrations/map/<file>-parity.md`.

## Status legend (per file)

- ✅ **audited** — full per-function table + wave plan + at least one
  pass of implementation work, with a `History` section.
- 🟡 **in flight** — table written but waves not all done.
- ❌ **pending** — no audit doc yet.

## File inventory (41 rAthena map .cpp)

Ordered by line count + dependency. Big consumers first, isolated leaf
modules last. The "C# surface" column gives the entry folder under
`Map.Server/` that's already established.

| # | rAthena file | Lines | Status | Audit doc | C# surface |
|---|---|---:|---|---|---|
| 1  | `script.cpp`       | 28 422 | ❌ | — | `Scripting/` (TS+Jint runtime) |
| 2  | `skill.cpp`        | 26 438 | ✅ | [skill-parity.md](map/skill-parity.md) | `Skills/` |
| 3  | `clif.cpp`         | 25 817 | ❌ | — | `Handlers/`, `Core.Server/Packets/` |
| 4  | `status.cpp`       | 16 047 | ❌ | — | `Status/` |
| 5  | `pc.cpp`           | 15 989 | ✅ | [pc-parity.md](map/pc-parity.md) | `Entities/PlayerEntity*`, `Session/` |
| 6  | `battle.cpp`       | 12 432 | ✅ | [battle-parity.md](map/battle-parity.md) | `Combat/` |
| 7  | `atcommand.cpp`    | 12 068 | ✅ | [atcommand-parity.md](map/atcommand-parity.md) | `Gm/` |
| 8  | `mob.cpp`          |  6 967 | 🟡 | [mob-db.md](map/mob-db.md) | `Mob/`, `MobDb/`, `Spawn/` |
| 9  | `npc.cpp`          |  6 341 | 🟡 | [npc.md](map/npc.md) (superseded) | `Scripting/`, `Spawn/` |
| 10 | `map.cpp`          |  5 356 | 🟡 | [world.md](map/world.md) | `World/` |
| 11 | `itemdb.cpp`       |  4 948 | ❌ | — | `Items/`, `MobDb/` (catalogs) |
| 12 | `unit.cpp`         |  4 010 | 🟡 | [movement.md](map/movement.md) | `Movement/`, `Combat/AttackService.cs` |
| 13 | `intif.cpp`        |  3 900 | 🟡 | [ipc-integration.md](map/ipc-integration.md) | `Services/CharServerIpcService.cs` |
| 14 | `guild.cpp`        |  2 755 | ❌ | — | `Party/` (guild folder TBD) |
| 15 | `pet.cpp`          |  2 504 | ❌ | — | `Pet/` |
| 16 | `homunculus.cpp`   |  2 064 | ❌ | — | — (folder TBD) |
| 17 | `chrif.cpp`        |  1 974 | 🟡 | [ipc-integration.md](map/ipc-integration.md) | `Services/CharServerIpcService.cs` |
| 18 | `battleground.cpp` |  1 617 | ❌ | — | — |
| 19 | `party.cpp`        |  1 575 | ❌ | — | `Party/` |
| 20 | `channel.cpp`      |  1 526 | ❌ | — | `Chat/` |
| 21 | `instance.cpp`     |  1 316 | ❌ | — | — |
| 22 | `achievement.cpp`  |  1 219 | ❌ | — | — |
| 23 | `storage.cpp`      |  1 206 | ❌ | — | `Storage/` |
| 24 | `elemental.cpp`    |  1 149 | ❌ | — | — |
| 25 | `quest.cpp`        |    995 | ❌ | — | — |
| 26 | `mercenary.cpp`    |    956 | ❌ | — | — |
| 27 | `buyingstore.cpp`  |    832 | ❌ | — | `Shop/` |
| 28 | `vending.cpp`      |    768 | ❌ | — | `Shop/` |
| 29 | `log.cpp`          |    718 | ❌ | — | (DI logger) |
| 30 | `cashshop.cpp`     |    672 | ❌ | — | `Shop/` |
| 31 | `trade.cpp`        |    666 | ❌ | — | `Trade/` |
| 32 | `navi.cpp`         |    655 | ❌ | — | — |
| 33 | `mail.cpp`         |    535 | ❌ | — | — |
| 34 | `path.cpp`         |    522 | ❌ | — | `Movement/` |
| 35 | `chat.cpp`         |    507 | ❌ | — | `Chat/` |
| 36 | `npc_chat.cpp`     |    443 | ❌ | — | — |
| 37 | `pc_groups.cpp`    |    406 | ❌ | — | `Gm/` |
| 38 | `searchstore.cpp`  |    361 | ❌ | — | `Shop/` |
| 39 | `mapreg.cpp`       |    355 | ❌ | — | `Scripting/` |
| 40 | `duel.cpp`         |    311 | ❌ | — | — |
| 41 | `clan.cpp`         |    235 | ❌ | — | `Party/` |
| 42 | `date.cpp`         |    155 | ❌ | — | — |

**Audited: 4 / 41 (pc, battle, atcommand, skill). Partial: 5. Pending: 33.**

## Sweep order (dependency + impact)

The big four (`skill`, `clif`, `status`, `script`) feed almost every
other file. Doing them first means the smaller files can audit against
established surface area instead of trying to inflate it.

### Wave 1 — core gameplay engines

1. **skill.cpp** (26 438 lines) — most-depended-on. Resolver dispatch
   already exists; need full per-skill coverage table + status icon
   per skill_db entry. Will dominate this wave.
2. **status.cpp** (16 047 lines) — `status_change_*` engine + every
   SC apply/end function. Heavily consumed by skill.cpp and the
   `Status/` folder.
3. **clif.cpp** (25 817 lines) — outbound packets only (`clif_*`).
   Already-implemented packets live in `Core.Server/Packets/Out/`;
   need a per-`clif_*` table.

### Wave 2 — world + content data

4. **script.cpp** (28 422 lines) — script-command parity sweep against
   the TS runtime registrars. Different shape from rest because the
   engine is replaced, not 1:1 ported; audit doc should map every
   `BUILDIN(...)` to a TS API call (or stub).
5. **map.cpp** (5 356) — promote [world.md](map/world.md) to full
   per-function table.
6. **mob.cpp** (6 967) — promote [mob-db.md](map/mob-db.md) to full
   per-function table.
7. **npc.cpp** (6 341) — write fresh `npc-parity.md`; old [npc.md](map/npc.md)
   is superseded.
8. **unit.cpp** (4 010) — promote [movement.md](map/movement.md).
9. **itemdb.cpp** (4 948) — fresh `itemdb-parity.md`.

### Wave 3 — IPC + group systems

10. **chrif.cpp** + **intif.cpp** — already 100% structurally wired
    via `IServerConnectionService`. Audit doc should map every
    `chrif_*` / `intif_*` to its gRPC RPC name.
11. **guild.cpp** (2 755), **party.cpp** (1 575), **clan.cpp** (235)
    — three group-relation files; share a single audit pass.
12. **channel.cpp** (1 526), **chat.cpp** (507) — chat surfaces.

### Wave 4 — companions

13. **pet.cpp** (2 504), **homunculus.cpp** (2 064),
    **mercenary.cpp** (956), **elemental.cpp** (1 149) — four
    companion families. Each maps to a `<X>Entity` + `<X>Service`.

### Wave 5 — economy

14. **storage.cpp** (1 206), **mail.cpp** (535), **cashshop.cpp** (672),
    **vending.cpp** (768), **buyingstore.cpp** (832),
    **searchstore.cpp** (361), **trade.cpp** (666).

### Wave 6 — endgame / progression

15. **achievement.cpp** (1 219), **quest.cpp** (995),
    **instance.cpp** (1 316), **battleground.cpp** (1 617),
    **duel.cpp** (311).

### Wave 7 — utilities

16. **path.cpp** (522), **navi.cpp** (655), **mapreg.cpp** (355),
    **log.cpp** (718), **pc_groups.cpp** (406), **npc_chat.cpp** (443),
    **date.cpp** (155).

## Per-file procedure

Each entry runs the `/rathena-parity` workflow:

1. **Enumerate** every public function in the rAthena file.
2. **Read** the existing audit doc (if any).
3. **Scan** the C# tree for matching symbols.
4. **Categorize** each function ✅ / ⚠️ / ❌ with a C# location.
5. **Update** the audit doc with the new table, coverage summary,
   and wave plan.
6. **Implement** the canonical entry point for every ❌ — either real
   logic or a documented "data-pending" service. Commit per wave.
7. **History** entry on the audit doc with the date + commit refs.

The bar for a file to flip ✅ **audited** is that every rAthena public
function has a canonical C# entry point — even if the entry point is
documented as data-pending on a specific upstream dependency.

## Snapshot of what's left after Wave 1

After skill/status/clif complete, ~70 % of the gameplay surface is
covered by an audit doc. The remainder is mostly leaf modules that
will land in a handful of medium commits each.

## History

### 2026-05-20 — roadmap written
- Inventory of 41 rAthena map .cpp files with status + line counts.
- 7-wave dependency-ordered plan: skill→status→clif first, then the
  smaller subsystems.
- Drives next-up work: `skill.cpp` parity audit.

### 2026-05-20 — skill.cpp ✅
- 162 functions enumerated; 15-wave sweep landed in 2 commits.
- 17 new `Map.Server/Skills/` service interfaces + impls; every
  rAthena `skill_*` public function now has a canonical C# entry
  point.
- Next up: `status.cpp` (16 047 lines, second-most depended-on).
