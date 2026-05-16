# MS1 · Movement — pathfinding and walk loop

**Phase:** MS1
**Depends on:** [world.md](world.md) (cells), [entities.md](entities.md) (spatial index), [session.md](session.md) (player spawned)
**Blocks:** combat (MS3), mob AI (MS2)

The whole MS1 acceptance ("walk around") is this doc. Two pieces: compute a path from A to B through walkable cells, then walk it step by step on the game loop with the right timing.

## Source of truth

- [rathena/src/map/path.cpp](/Volumes/1TB/Projetos/rathena/src/map/path.cpp) — `path_search` (A*), `path_search_long` (straight-line walkable test). 522 lines, fully self-contained.
- [rathena/src/map/unit.cpp](/Volumes/1TB/Projetos/rathena/src/map/unit.cpp) — `unit_walktoxy`, `unit_walktoxy_sub`, `unit_walktoxy_timer`, `unit_walktobl`, `unit_setdir`. The walk timer is the heart of the loop.
- [rathena/src/map/clif.cpp](/Volumes/1TB/Projetos/rathena/src/map/clif.cpp) — `clif_parse_WalkToXY`, `clif_walkok`, `clif_move`, `clif_move2`. The packet plumbing.

## Scope (MS1)

**In scope:**
- 8-directional A* pathfinding on the cell grid.
- "Long path" walkability test (straight-line, used for visibility / casting line-of-sight).
- Per-entity walk state: target cell, current path (queue of cells), step timer.
- Walk timer driven by the 60 FPS game loop: each tick, advance any entity whose step deadline has passed; remove from old spatial-index bucket, insert at new.
- Client packets:
  - `CZ_REQUEST_MOVE (0x35f)` / `CZ_REQUEST_MOVE2` — player asks to walk to (x, y).
  - `ZC_NOTIFY_PLAYERMOVE (0x87)` — server echoes the accepted move to the moving player.
  - `ZC_NOTIFY_MOVE (0x86)` — other players in view see this entity move.
- Walk-into-warp detection: when the walk ends on a warp cell, trigger the map change.
- Walk interruption: new walk request mid-walk cancels the old one.

**Out of scope:**
- Skill-induced movement (Body Relocation, Backslide) — MS3.
- Mounted speed / status-induced speed changes — MS3 status.
- Knockback — MS3 combat.

## Done

- **`Direction` enum + `Dx()`/`Dy()`/`IsDiagonal()`/`FromDelta()`** ([Map.Server/Movement/Direction.cs](../../../Map.Server/Movement/Direction.cs)) — rAthena `enum directions` parity (NORTH=0, NW=1, W=2, SW=3, S=4, SE=5, E=6, NE=7). dx/dy tables match path.cpp's `walk_choices`.
- **`Pathfinder.Search`** ([Map.Server/Movement/Pathfinder.cs](../../../Map.Server/Movement/Pathfinder.cs)) — A* port of rAthena `path_search`. `PriorityQueue<TElement, TPriority>` open set, `Dictionary<cellKey, gCost>` closed set, Manhattan heuristic. `MOVE_COST = 10`, `MOVE_DIAGONAL_COST = 14`, `MaxWalkPath = 32`. Anti-corner-cutting rule (diagonal step requires both cardinal neighbors walkable) matches path.cpp `chk_dir`.
- **`Pathfinder.HasStraightLine`** — Bresenham walkability test; rAthena `path_search_long` equivalent. Useful for line-of-sight queries in MS3 combat.
- **`WalkState`** ([Map.Server/Movement/WalkState.cs](../../../Map.Server/Movement/WalkState.cs)) — per-entity `Queue<Direction>` + target cell + active `TimerId` for cancellation. Stored as `Entity.Walk` (null when standing).
- **`Entity.Walk` / `Entity.Speed`** ([Map.Server/Entities/Entity.cs](../../../Map.Server/Entities/Entity.cs)) — walk state slot + per-cell step delay (default 150ms for players, mob_db.MoveSpeed for mobs in MS2).
- **`IMovementService` + `MovementService`** ([Map.Server/Movement/MovementService.cs](../../../Map.Server/Movement/MovementService.cs)) — `TryStartWalk(entity, tx, ty)` + `CancelWalk(entity)`. Each step is scheduled via `Core.Timer.Scheduler.Schedule`; the callback advances the entity by one cell (`IEntityRegistry.Move`), updates direction, and schedules the next step. Diagonal step delay = `Speed * 14 / 10` (rAthena parity). Re-checks cell walkability before each step so dynamic obstacles (MS3) abort the walk cleanly. Timer-id comparison guards against stale callbacks firing after cancellation.
- **Wired into Map.Server DI** ([Map.Server/Program.cs](../../../Map.Server/Program.cs)) — registered as `IMovementService` singleton.
- **39 new tests** in `Map.Server.Tests/Movement/`:
  - `DirectionTests` — 24 cases covering Dx/Dy table, Center semantics, IsDiagonal, FromDelta round-trip.
  - `PathfinderTests` — straight line, diagonal, around-wall detour, no-path, destination-not-walkable, same-cell, MaxWalkPath cap, anti-corner-cut, HasStraightLine open + blocked.
  - `MovementServiceTests` — arrival on open map, diagonal walk, no-path returns false, interrupt-restart, cancel, spatial index sync after walk completes. Uses tiny `Speed=1` so walks finish inside the test polling window.

**Core.Timer adapter design.** Walk steps go through `Scheduler.Schedule(callback, entityId, dueTime)`. Callbacks fire on the Scheduler's thread pool, but every state mutation goes through `IEntityRegistry.Move` (thread-safe via `MapSpatialIndex`'s coarse lock + `ConcurrentDictionary` by-id store), so no separate game-loop queue is needed for MS1. When deterministic single-threaded gameplay matters (skill resolution, combat damage), MS3 can add an MPSC queue drained on the 60 FPS tick.

Full suite after this commit: 148 char + 16 login + 79 map = **243 green**.

## Pending — wired in subsequent MS1 steps

Items below depend on packets / handlers landing — they're queued for [session.md](session.md) (`CZ_REQUEST_MOVE` handler) and [visibility.md](visibility.md) (broadcast on step) but the underlying movement engine is ready.

1. **`CZ_REQUEST_MOVE` / `CZ_REQUEST_MOVE2` handlers** — packet parsing + call `MovementService.TryStartWalk`. Reject if not authenticated / not spawned / etc. Depends on [session.md](session.md) and [packets.md](packets.md).

2. **`ZC_NOTIFY_PLAYERMOVE` / `ZC_NOTIFY_MOVE` emission** on walk start. Depends on [visibility.md](visibility.md) for the AREA broadcast helper. The walk-start packet includes start coords + end coords + server time; clients interpolate locally between steps so we only emit once per walk-start (not per step), unless interrupted.

3. **Walk-into-warp detection** — when a walker arrives at a cell that has a warp ([world.md](world.md) Pending), trigger the map-change handshake. Cross-map-server warps use the existing `RequestMapServerChange` IPC (P6).

4. **Re-pathing on dynamic obstruction** — if a previously walkable cell becomes blocked mid-walk (MS3 Ice Wall, Land Protector), recompute the path or abort. The current code aborts cleanly; re-path is a follow-up.

5. **(Legacy plan items, kept for historical context — all delivered above):**
   ```csharp
   public class WalkState
   {
       public Queue<(short X, short Y, byte Dir, int StepMs)> Path;
       public DateTime NextStepUtc;
       public short TargetX, TargetY;
       public bool WalkOnWarp;       // walk-into-warp flag
   }
   ```
   Players, mobs, pets, homuncs all share this. `Entity.Walk` is non-null while walking.

6. **`MovementService` (singleton).**
   - `bool TryStartWalk(Entity, MapData, short tx, short ty)` — runs A*, sets `Walk` state, sends `ZC_NOTIFY_PLAYERMOVE` to mover, `ZC_NOTIFY_MOVE` to view.
   - `void Tick(MapData, IEnumerable<Entity> walkers, DateTime now)` — called from `MapServerImpl.UpdateGameLogicAsync` per tick; advances any walker whose `NextStepUtc <= now`.
   - `void CancelWalk(Entity)` — clears walk state, sends `ZC_STOPMOVE` to view.

7. **`RequestMoveHandler`** (handles `CZ_REQUEST_MOVE`):
   - Look up player's entity by session.
   - Reject if dead, casting, sitting, vending, etc. (most of these conditions don't exist yet → just basic alive-and-spawned check for MS1).
   - Call `MovementService.TryStartWalk`.
   - Reject paths > MAX_WALKPATH (rAthena = 32 cells).

8. **Walk-into-warp.** Each tick, if a walker arrives at the destination cell AND that cell has a warp ([world.md](world.md) Warp), trigger the map change handshake:
   - Send `ZC_NPCACK_MAPMOVE` to the player.
   - Move the entity to the destination map's spawn cell (re-register in destination map's spatial index).
   - Broadcast `ZC_NOTIFY_VANISH` to old map's view, `ZC_NOTIFY_STANDENTRY` to new map's view.
   - Cross-map-server warps (destination on a different map server) require `RequestMapServerChange` (IPC ready in P6) + a client reconnect; defer to MS1.5.

9. **Spatial-index sync.** Every accepted step calls `IEntityRegistry.Move(entity, oldCell, newCell)` to keep buckets accurate. Visibility ([visibility.md](visibility.md)) consumes these updates to manage view-range entry/exit broadcasts.

10. **Direction byte.** rAthena's 8-direction enum (`DIR_NORTH=0, NE=1, EAST=2, SE=3, SOUTH=4, SW=5, WEST=6, NW=7`) matches client expectations. `Entity.Dir` is updated per step.

### File layout

```
Map.Server/Movement/
├── Pathfinder.cs            — A* + path_search_long
├── PathResult.cs            — queue of step tuples
├── MovementService.cs       — TryStartWalk, Tick, CancelWalk
├── WalkState.cs             — per-entity walk state
└── Direction.cs             — enum + helpers (x/y delta per dir)

Map.Server/Handlers/
└── RequestMoveHandler.cs    — CZ_REQUEST_MOVE
```

### Tests (Map.Server.Tests)

1. `PathfinderTests`:
   - Simple straight path on an open map.
   - Path around a single wall cell.
   - No path possible (blocked by walls) → returns empty / false.
   - Max-path-length cap (rAthena = 32 cells).
   - Diagonal-corner-cut rejection (well-known rAthena test case).
2. `MovementServiceTests`:
   - Start a walk, advance time, verify entity reaches each cell at the right tick.
   - Cancel mid-walk: state cleared, no further movement.
   - Walk-into-warp: destination matched, map-change triggered.
   - Walk into an entity blocking the path → re-path or stop (rAthena's behavior: walk continues, entity passes through other entities — only cells are walkability-tested).
3. `RequestMoveHandlerTests`:
   - Unauthenticated session → reject.
   - Valid request → MovementService called with right args.
   - Move-while-already-walking → previous walk canceled.

### Acceptance

- Two players on `prontera` (start map). Player A moves; player B sees `ZC_NOTIFY_MOVE` packets. Player A's session receives `ZC_NOTIFY_PLAYERMOVE`. After the walk, the spatial index puts A at the new cell.
- Path through a wall → no walk starts; client sees no response (rAthena drops the request).
- Walking into a warp cell triggers `ZC_NPCACK_MAPMOVE` and the player re-spawns on the destination map.

### Open decisions

- **Tick precision.** rAthena uses millisecond timing on its timer system. Our 60 FPS loop = 16.67ms per tick. Walk steps are 150ms+ so we comfortably fit ~10 steps inside a second with sub-tick precision (compute `NextStepUtc` exactly, don't quantize to ticks).
- **Path recomputation on obstruction.** rAthena's behavior: an entity that becomes blocked mid-walk (e.g. a wall raised by ICEWALL skill) re-paths. MS1 doesn't have ICEWALL yet → skip re-path; assume the path stays valid for its duration.

## History

- **2026-05-16** — **MS1.movement shipped.** Direction enum (rAthena parity), Pathfinder (A* with anti-corner-cut, Bresenham HasStraightLine), WalkState on Entity, MovementService driven by `Core.Timer.Scheduler` (user direction: adapt to existing scheduler rather than fork a timer). Speed defaults to 150ms/cell (rAthena `pc->speed`); diagonal cost = 14/10 of cardinal. 39 new tests covering direction tables, path correctness, walk-loop integration. Visibility broadcast (`ZC_NOTIFY_*MOVE`) and `CZ_REQUEST_MOVE` handler are queued for [session.md](session.md)/[visibility.md](visibility.md); the engine itself is complete and unit-tested. Full suite 243 green.
- **2026-05-16** — Plan written. No implementation yet.
- **2026-05-16** — Per-step AOI broadcast wired: `MovementService.OnStepCallback` now calls `IVisibilityService.NotifyMoveDiff` after each cell update so the symmetric `clif_outsight` / `clif_insight` pair fires when two entities cross the view-range boundary. Closes the MS1 "walk into view → see each other" acceptance criterion.
