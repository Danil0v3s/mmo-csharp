# MS3 · Items

**Phase:** MS3 (adjacent)
**Depends on:** [entities.md](../entities.md), Char IPC (P6 done), `Core.Database.Repositories.Api.IItemRepository`
**Blocks:** combat (drops), trade, vending, NPCs (shop)

Items thread through every gameplay system. The Char server already owns persistent inventory; the map server's job is the live state — pickup, drop, equip, consume, send-to-cart, trade.

## Source of truth

- [rathena/src/map/itemdb.cpp](/Volumes/1TB/Projetos/rathena/src/map/itemdb.cpp) — `itemdb_read_yaml` (YAML loader) and **`itemdb_read_sqldb`** (SQL alternate path, lines 3988+) for `use_sql_db: yes`
- [Core.Database/Entities/ItemEntity.cs](../../../../Core.Database/Entities/ItemEntity.cs) — persisted schema (mirrors rAthena's `item_db_re` SQL table column-for-column)
- [Core.Database/Seeds/Scripts/seed_item_db_equip.sql](../../../../Core.Database/Seeds/Scripts/seed_item_db_equip.sql) / [_etc](../../../../Core.Database/Seeds/Scripts/seed_item_db_etc.sql) / [_usable](../../../../Core.Database/Seeds/Scripts/seed_item_db_usable.sql) — renewal seed (~28K rows total)
- [rathena/src/map/pc.cpp](/Volumes/1TB/Projetos/rathena/src/map/pc.cpp) — `pc_additem`, `pc_delitem`, `pc_takeitem`, `pc_equipitem`, `pc_unequipitem`
- [rathena/src/map/clif.cpp](/Volumes/1TB/Projetos/rathena/src/map/clif.cpp) — inventory list, pickup, drop packets

## Scope (MS3 first pass)

**In scope:**
- `IItemCatalog` — in-memory snapshot of the item_db table hydrated from `IItemRepository.GetAllAsync()` at map-server startup. Indexed by id and aegis name.
- `Inventory` model on `PlayerEntity` (mirrors the `inventory` DB table; loaded from char on EnterMap via the existing IPC).
- Pickup: `CZ_ITEM_PICKUP (0x0362)` → walk to item → take, broadcast vanish. (Shipped — see Done.)
- Drop: `CZ_ITEM_THROW (0x00a2)` → server creates `FloorItemEntity` on the map.
- Equip / unequip: `CZ_REQ_WEAR_EQUIP (0x00a9)` / `CZ_REQ_TAKEOFF_EQUIP (0x00ab)`.
- Use consumable: `CZ_USE_ITEM (0x00a7)` for potions, food, etc.
- Send / fetch cart / storage / guild storage — IPC already exists in P6.
- Mob drop emission (called from combat death; uses `IItemCatalog.GetByAegisName` to resolve `mob_db.DropNItem` strings to numeric ids).

**Out of scope:**
- Item scripts (e.g. Yggdrasil Leaf revives target). Need a minimal script subset for items, similar to NPC scope decision. Defer per-item; treat each item script as its own little C# handler initially.
- Refinement / enchant / bound items special rules — covered by inventory schema but UI flows are extra work.

## Done

- Char IPC wrappers exist for inventory operations (P6). Inventory is per-character in the DB and loaded on character data fetch.
- **Floor-item entity lifecycle (MS3 first slice):**
  - `FloorItemEntity` (renamed from the original `ItemEntity` after the DB shape-up, to avoid a name collision with `Core.Database.Entities.ItemEntity`) — `ItemId`, `Amount`, `Identified`, `SubX/SubY`, `DroppedAtTick`. `EntityType.Item`.
  - Packets: [`CZ_ITEM_PICKUP`](../../../../Core.Server/Packets/In/CZ/CZ_ITEM_PICKUP.cs) (0x0362, modern), [`ZC_ITEM_ENTRY`](../../../../Core.Server/Packets/Out/ZC/ZC_ITEM_ENTRY.cs) (0x009d, item already on floor entering view), [`ZC_ITEM_FALL_ENTRY`](../../../../Core.Server/Packets/Out/ZC/ZC_ITEM_FALL_ENTRY.cs) (0x0add, just-dropped animation; PACKETVER_RE ≥ 20180704), [`ZC_ITEM_DISAPPEAR`](../../../../Core.Server/Packets/Out/ZC/ZC_ITEM_DISAPPEAR.cs) (0x00a1).
  - [`IItemDropService`](../../../../Map.Server/Items/IItemDropService.cs) + [`ItemDropService`](../../../../Map.Server/Items/ItemDropService.cs):
    - `DropOnFloor(map, x, y, itemId, amount)` → registers the entity and broadcasts `ZC_ITEM_FALL_ENTRY` to PC viewers in AOI.
    - `TryPickup(picker, itemEntityId)` → range/map validation (2-cell pickup range matches rAthena), broadcasts `ZC_ITEM_DISAPPEAR`.
    - `Tick()` → per-map-tick despawn sweep at 60s TTL (rAthena's `flooritem_lifetime` default).
  - [`PickupHandler`](../../../../Map.Server/Handlers/PickupHandler.cs) routes `CZ_ITEM_PICKUP` → `TryPickup`.
  - Visibility refactor: `BuildEnterViewPacket` / `BuildExitViewPacket` now dispatch the right packet per entity type (STANDENTRY for PC/Mob, ITEM_ENTRY/DISAPPEAR for items). `SendCurrentViewToSelf` surfaces all entity types on spawn (not just PCs).
  - 15 tests: 3 packet wire-shape, 6 `ItemDropService`, plus existing visibility/spawn coverage.
- **`IItemCatalog` over `IItemRepository`** ([IItemCatalog.cs](../../../../Map.Server/Items/IItemCatalog.cs) / [ItemCatalog.cs](../../../../Map.Server/Items/ItemCatalog.cs)) — DB-backed snapshot loaded once at boot (~28K rows). `Get(uint)`, `GetByAegisName(string)`, `All()`, `Reload()`. Returns `Core.Database.Entities.ItemEntity` directly.
- **Mob drop rolling on death** ([MobSpawnService.RollAndDropLoot](../../../../Map.Server/Spawn/MobSpawnService.cs)) — `KillMob` now iterates `mob.DbEntry.Drops`, rolls `rng.Next(10_000) < drop.Rate` per entry, resolves the aegis name via `IItemCatalog.GetByAegisName`, and spawns the floor item with a random sub-cell offset. Closes the spawn → kill → drop → pickup loop end-to-end. Two new tests in `MobSpawnServiceTests` cover the always-drop, never-drop, and unknown-item-name branches.

## Pending

1. **Inventory model** on the session + char-server IPC for persistence (P6 wrappers already exist).
2. **Equip / unequip / consumable use / drop-from-inventory flows.** `ItemDropService` is a building block for these.
3. **MVP drops + party share + drop-rate modifiers.** Today `RollAndDropLoot` uses raw `mob_db` rates and only rolls the regular `Drops` list. MVP drops (top-damager-only) and rAthena's `battle_config.item_rate_*` modifiers land alongside the combat damage-tracking work.
4. **Loot protection** (owner + party id), bound items, refined items.

### Acceptance
- Mob drops items on death; players see them on the floor and can pick them up within 2 cells.
- Player walks over item + presses pickup key → item added to inventory + map item vanishes.
- Player drops an item → server creates the floor entity + sends VANISH from inventory.
- Equip/unequip changes stats (read by combat doc).

## History
- **2026-05-16** — Plan stub.
- **2026-05-16** — Floor-item lifecycle first slice shipped: ItemEntity + 4 packets + drop/pickup/despawn service + PickupHandler + visibility per-type packet dispatch. Inventory persistence remains queued.
- **2026-05-16** — Plan re-aimed at DB-backed catalog (`IItemCatalog` over `IItemRepository`) instead of a YAML parser, matching rAthena's `use_sql_db` alternate path and the existing 28K-row seed in `Core.Database/Seeds/Scripts/seed_item_db_*.sql`. The floor-item runtime class is being renamed `ItemEntity` → `FloorItemEntity` to avoid a collision with the DB row.
- **2026-05-16** — Rename + ItemCatalog shipped. Mob death now rolls drops via `IItemCatalog.GetByAegisName(...)`; the spawn → kill → drop → pickup loop is end-to-end demonstrable. 177 Map.Server tests green.
