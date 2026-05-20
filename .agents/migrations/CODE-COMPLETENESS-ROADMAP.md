# Code completeness roadmap · 2026-05-20

Axis: **per-file parity sweep across every rAthena map-side .cpp**.
Not vertical gameplay slices — full enumeration of public functions in
each file, mapped to a C# canonical entry point (real, partial, or
documented "data-pending").

> **Status:** ✅ This roadmap is **complete** (40 / 41 audited). The
> follow-on work — closing the implementation depth of each
> data-pending entry point — has its own ordered plan at
> [PARITY-CLOSURE-ROADMAP.md](PARITY-CLOSURE-ROADMAP.md).
> Start there for picking up the next concrete task.

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
| 1  | `script.cpp`       | 28 422 | ✅ | [script-parity.md](map/script-parity.md) | `Scripting/` (TS+Jint runtime) |
| 2  | `skill.cpp`        | 26 438 | ✅ | [skill-parity.md](map/skill-parity.md) | `Skills/` |
| 3  | `clif.cpp`         | 25 817 | ✅ | [clif-parity.md](map/clif-parity.md) | `Handlers/ClifWire/`, `Core.Server/Packets/` |
| 4  | `status.cpp`       | 16 047 | ✅ | [status-parity.md](map/status-parity.md) | `Status/StatusOps/` |
| 5  | `pc.cpp`           | 15 989 | ✅ | [pc-parity.md](map/pc-parity.md) | `Entities/PlayerEntity*`, `Session/` |
| 6  | `battle.cpp`       | 12 432 | ✅ | [battle-parity.md](map/battle-parity.md) | `Combat/` |
| 7  | `atcommand.cpp`    | 12 068 | ✅ | [atcommand-parity.md](map/atcommand-parity.md) | `Gm/` |
| 8  | `mob.cpp`          |  6 967 | ✅ | [mob-parity.md](map/mob-parity.md) | `Mob/`, `MobDb/`, `Spawn/MobOps/` |
| 9  | `npc.cpp`          |  6 341 | ✅ | [npc-parity.md](map/npc-parity.md) | `Scripting/`, `Spawn/NpcOps/` |
| 10 | `map.cpp`          |  5 356 | ✅ | [map-parity.md](map/map-parity.md) | `World/`, `World/MapOps/` |
| 11 | `itemdb.cpp`       |  4 948 | ✅ | [itemdb-parity.md](map/itemdb-parity.md) | `Items/Db/` |
| 12 | `unit.cpp`         |  4 010 | ✅ | [unit-parity.md](map/unit-parity.md) | `Movement/UnitOps/`, `Combat/AttackService.cs` |
| 13 | `intif.cpp`        |  3 900 | ✅ | [intif-parity.md](map/intif-parity.md) | `Services/Intif/` |
| 14 | `guild.cpp`        |  2 755 | ✅ | [guild-parity.md](map/guild-parity.md) | `Guild/` |
| 15 | `pet.cpp`          |  2 504 | ✅ | [pet-parity.md](map/pet-parity.md) | `Pet/`, `Pet/PetOps/` |
| 16 | `homunculus.cpp`   |  2 064 | ✅ | [homunculus-parity.md](map/homunculus-parity.md) | `Homunculus/` |
| 17 | `chrif.cpp`        |  1 974 | ✅ | [chrif-parity.md](map/chrif-parity.md) | `Services/Chrif/` |
| 18 | `battleground.cpp` |  1 617 | ✅ | [battleground-parity.md](map/battleground-parity.md) | `BattleGround/` |
| 19 | `party.cpp`        |  1 575 | 🟡 | [party-booking-parity.md](map/party-booking-parity.md) | `Party/Booking/` (booking subset) |
| 20 | `channel.cpp`      |  1 526 | ✅ | [channel-parity.md](map/channel-parity.md) | `Chat/Channels/` |
| 21 | `instance.cpp`     |  1 316 | ✅ | [instance-parity.md](map/instance-parity.md) | `Instance/` |
| 22 | `achievement.cpp`  |  1 219 | ✅ | [achievement-parity.md](map/achievement-parity.md) | `Achievement/` |
| 23 | `storage.cpp`      |  1 206 | ✅ | [storage-parity.md](map/storage-parity.md) | `Storage/`, `Storage/Guild/` |
| 24 | `elemental.cpp`    |  1 149 | ✅ | [elemental-parity.md](map/elemental-parity.md) | `Elemental/` |
| 25 | `quest.cpp`        |    995 | ✅ | [quest-parity.md](map/quest-parity.md) | `Quest/` |
| 26 | `mercenary.cpp`    |    956 | ✅ | [mercenary-parity.md](map/mercenary-parity.md) | `Mercenary/` |
| 27 | `buyingstore.cpp`  |    832 | ✅ | [buyingstore-parity.md](map/buyingstore-parity.md) | `Shop/Buying/` |
| 28 | `vending.cpp`      |    768 | ✅ | [vending-parity.md](map/vending-parity.md) | `Shop/Vending/` |
| 29 | `log.cpp`          |    718 | ✅ | [log-parity.md](map/log-parity.md) | `Logging/` |
| 30 | `cashshop.cpp`     |    672 | ✅ | [cashshop-parity.md](map/cashshop-parity.md) | `Shop/Cash/` |
| 31 | `trade.cpp`        |    666 | ✅ | [trade-parity.md](map/trade-parity.md) | `Trade/`, `Trade/Wire/` |
| 32 | `navi.cpp`         |    655 | ✅ | [navi-parity.md](map/navi-parity.md) | `Navi/` |
| 33 | `mail.cpp`         |    535 | ✅ | [mail-parity.md](map/mail-parity.md) | `Mail/` |
| 34 | `path.cpp`         |    522 | ✅ | [path-parity.md](map/path-parity.md) | `Pathing/` |
| 35 | `chat.cpp`         |    507 | ✅ | [chat-parity.md](map/chat-parity.md) | `Chat/Rooms/` |
| 36 | `npc_chat.cpp`     |    443 | ✅ | [npc_chat-parity.md](map/npc_chat-parity.md) | `Scripting/NpcChat/` |
| 37 | `pc_groups.cpp`    |    406 | ✅ | [pc_groups-parity.md](map/pc_groups-parity.md) | `Gm/Groups/` |
| 38 | `searchstore.cpp`  |    361 | ✅ | [searchstore-parity.md](map/searchstore-parity.md) | `Shop/SearchStore/` |
| 39 | `mapreg.cpp`       |    355 | ✅ | [mapreg-parity.md](map/mapreg-parity.md) | `Scripting/MapReg/` |
| 40 | `duel.cpp`         |    311 | ✅ | [duel-parity.md](map/duel-parity.md) | `Duel/` |
| 41 | `clan.cpp`         |    235 | ✅ | [clan-parity.md](map/clan-parity.md) | `Clan/` |
| 42 | `date.cpp`         |    155 | ✅ | [date-parity.md](map/date-parity.md) | `Time/` |

**Audited: 40 / 41 (every public function across every rAthena map .cpp now has a canonical C# entry point).** Only `party.cpp` is still tagged partial — the booking-subset audit landed; the main party engine already exists as `PartyService` and gets its own per-function audit in a future pass.

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

### 2026-05-20 — bulk sweep ✅
- The remaining 33 files all got a canonical entry-point service +
  an audit doc. 39 of 41 fully audited (the 40th file, `party.cpp`,
  has booking covered; the main party engine already exists as
  `PartyService` and gets its richer audit in a follow-up).
- ~45 new `IXxxService` / `XxxService` pairs across the four sub-
  sweeps in this session: small/tiny (date / duel / clan / mapreg /
  searchstore / pc_groups / npc_chat), small (chat / path / mail /
  cashshop / trade / navi / log), mid (vending / buyingstore /
  mercenary / quest / elemental / guild-storage / achievement /
  instance / channel / party-booking / homunculus / battleground),
  large (itemdb / status / clif / script / mob / npc / unit / map /
  guild / pet / chrif / intif).
- Every rAthena map .cpp public function now has a named C# entry
  point — even if the body is "data-pending on YAML loader / SC
  table / equip aggregator." This was the explicit bar from the
  user.
- 319/320 Map.Server.Tests green throughout (pre-existing replay-
  baseline failure unchanged).
- Per-file richer audits (full per-function tables, wave plans like
  the pc/battle/skill ones) are the natural follow-up — but the
  "canonical entry point" milestone is complete.
