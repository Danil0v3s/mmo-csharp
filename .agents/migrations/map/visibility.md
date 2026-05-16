# MS1 · Visibility — view range and area notifications

**Phase:** MS1
**Depends on:** [entities.md](entities.md) (spatial index), [packets.md](packets.md)
**Used by:** [movement.md](movement.md), [session.md](session.md), all MS2/MS3 work

Visibility decides who sees whom and triggers the right entry/exit packets. In rAthena every gameplay event that needs broadcast (a player walks, a mob spawns, an item drops) calls one of the `clif_send_*` area helpers that walk the spatial index and emit per-recipient.

## Source of truth

- [rathena/src/map/map.cpp](/Volumes/1TB/Projetos/rathena/src/map/map.cpp) — `map_foreachinrange`, `map_foreachinarea`, `map_foreachinmap`
- [rathena/src/map/clif.cpp](/Volumes/1TB/Projetos/rathena/src/map/clif.cpp) — `clif_send`, `clif_spawn`, `clif_clearunit_area`, `clif_set_unit_idle`, `clif_set_unit_walking`. The `enum send_target` (SELF, AREA, AREA_WOS, AREA_WOSC, AREA_WOC, ALL_CLIENT, GUILD, PARTY, …)
- [rathena/src/map/battle.cpp](/Volumes/1TB/Projetos/rathena/src/map/battle.cpp) `battle_config.area_size` — the default range constant.

## Scope (MS1)

**In scope:**
- Constants: `AREA_SIZE = 14` (rAthena default), `MAX_VIEW_RANGE = 16`. These are cells; view = `(2*range+1)²`.
- `send_target` enum: `SELF`, `AREA`, `AREA_WOS` (without source), `AREA_WOSC` (without source if cloaked), `ALL_SAMEMAP`.
- Helpers:
  - `SendToSelf(entity, packet)`
  - `SendToArea(entity, packet, target = AREA)` — broadcasts to all PCs in view range of `entity`
  - `SendToAreaOnExit(entity, packet)` — to all observers as the entity is leaving (vanish)
- Entry / exit hooks called from movement, spawn, despawn, map-change.

**Out of scope:**
- Cloaking, hiding, fog-of-war — needs status changes in MS3.
- Guild-only and party-only filtering — MS3 chat / guild.
- Trickwall-of-sight (true LOS checks via `path_search_long`) — usable but not enforced for visibility itself; rAthena ignores LOS for "what's in view" purposes.

## Done

- [VisibilityConfig](../../../Map.Server/Visibility/VisibilityConfig.cs) — `AreaSize = 14`.
- [SendTarget](../../../Map.Server/Visibility/SendTarget.cs) — `Self`, `Area`, `AreaWos`.
- [IPacketDispatcher](../../../Map.Server/Visibility/IPacketDispatcher.cs) + [SessionPacketDispatcher](../../../Map.Server/Visibility/SessionPacketDispatcher.cs) — abstraction so visibility doesn't take a hard dep on `SessionManager` (and tests don't need a real socket stack).
- [IVisibilityService](../../../Map.Server/Visibility/IVisibilityService.cs) / [VisibilityService](../../../Map.Server/Visibility/VisibilityService.cs):
  - `SendToSelf`, `SendToArea(target)`.
  - `NewlyVisible` / `NewlyInvisible` view-diff over the spatial index.
  - `NotifySpawnedToArea` (PC only for MS1; throws on mob/npc), `NotifyVanishedToArea`, `NotifyMoveToArea`.
- 9 tests in [Map.Server.Tests/Visibility/](../../../Map.Server.Tests/Visibility/) covering edge-of-range, source-exclusion, view-diff, and packet shape.

Pending: integration with [session.md](session.md) (spawn/despawn) and per-step view diff in [movement.md](movement.md). The visibility primitives are ready; the callers wire them in as MS1.session and the movement step callback land.

## Pending

### Items, in suggested order

1. **Constants.** Pin `AREA_SIZE = 14` in a `VisibilityConfig` static class. Anything below 14 will visibly de-sync mobile clients; anything significantly above will balloon packet counts.

2. **`SendTarget` enum** matching rAthena's `enum send_target`. Only the MS1-relevant entries; add more in MS3.

3. **`IVisibilityService`** (singleton):
   - `void SendToSelf(PlayerEntity to, IOutgoingPacket packet)` — trivial; `to.Session.EnqueuePacket(packet)`.
   - `void SendToArea(Entity src, IOutgoingPacket packet, SendTarget target)` — iterate `IEntityRegistry.ForEachInRange(src.MapId, src.X, src.Y, AREA_SIZE, EntityType.PC)` and enqueue per-player; respect target's source-exclusion rules.
   - `void NotifyEntered(Entity entered)` — called from movement / spawn when an entity becomes visible to a new viewer. Emits `ZC_NOTIFY_STANDENTRY` per viewer.
   - `void NotifyVanished(Entity gone, int reason)` — emits `ZC_NOTIFY_VANISH` to viewers.

4. **Cell-bucket view set.** When a player walks from cell A to cell B, two sets change:
   - **Newly visible:** entities in B's view that weren't in A's view → emit `ZC_NOTIFY_STANDENTRY` to the walker for each.
   - **Newly invisible:** entities in A's view that aren't in B's view → emit `ZC_NOTIFY_VANISH` to the walker.
   - Symmetric: from each side. Players in B's view now see the walker if they didn't see them in A.

   Implementation: compute the two view bounding boxes, scan the difference. Optimization later: if the move is one cell, the boxes overlap heavily — only the edge rows/cols differ.

5. **`ZC_NOTIFY_STANDENTRY` payload.** This packet has many fields (HP, status, equipment, view angle, monster_id vs char_id, weapon ids, etc.). For MS1, populate only what's needed for a standing player: type, char_id, position, direction, name, head dir, sex. The mob-/npc-specific fields come in MS2.

6. **Integration points.** Wire `IVisibilityService` into:
   - **Movement** ([movement.md](movement.md)): each accepted step calls the entered/vanished diff (cheap for 1-cell steps).
   - **Spawn** ([session.md](session.md), [spawn.md](spawn.md)): on initial entry, send STANDENTRY to all viewers.
   - **Despawn** (disconnect, mob death): send VANISH to all viewers.
   - **Map change**: send VANISH to old map's view, STANDENTRY to new map's view.

7. **Walk-during-view packet (`ZC_NOTIFY_MOVE`).** When a viewer sees an entity start walking, they get `ZC_NOTIFY_MOVE` with the start+end coords + start time. The client locally interpolates. Server only sends one packet per walk-start (not per step), unless the walk gets interrupted or re-pathed.

### File layout

```
Map.Server/Visibility/
├── VisibilityConfig.cs       — constants
├── SendTarget.cs             — enum
├── IVisibilityService.cs
└── VisibilityService.cs      — singleton implementation
```

### Tests (Map.Server.Tests)

1. `VisibilityServiceTests`:
   - Two players in the same cell → each appears in the other's `ForEachInRange` and gets `SendToArea` packets.
   - Two players 100 cells apart → neither in view, no cross-broadcast.
   - Player on edge of range (exactly 14 cells away) → in view; 15 cells away → out of view.
   - `SendTarget.AREA_WOS`: source is excluded from the recipient list.
2. View-diff edge cases:
   - Walker moves north 1 cell: row furthest south is "vanished", row furthest north is "entered". Verify only that diff is computed (no full view rescan).
   - Cross-map-boundary walk (warps): full vanish from old, full standentry on new.

### Acceptance

- Player A walks into range of player B; both receive `ZC_NOTIFY_STANDENTRY` for the other.
- Player A walks out of range of player B; both receive `ZC_NOTIFY_VANISH`.
- Player C, who is 50 cells away from both, sees nothing.

## History

- **2026-05-16** — Plan written. No implementation yet.
- **2026-05-16** — Service + tests delivered. AOI iteration, view-diff helpers, PC standentry/vanish/move broadcasts. Wired into DI in [Program.cs](../../../Map.Server/Program.cs). Callers (movement / session) wire-up still pending.
