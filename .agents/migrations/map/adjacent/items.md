# MS3 · Items

**Phase:** MS3 (adjacent)
**Depends on:** [entities.md](../entities.md), Char IPC (P6 done)
**Blocks:** combat (drops), trade, vending, NPCs (shop)

Items thread through every gameplay system. The Char server already owns persistent inventory; the map server's job is the live state — pickup, drop, equip, consume, send-to-cart, trade.

## Source of truth

- [rathena/src/map/itemdb.cpp](/Volumes/1TB/Projetos/rathena/src/map/itemdb.cpp) — item_db loader
- [rathena/db/re/item_db.yml](/Volumes/1TB/Projetos/rathena/db/re/item_db.yml) — 30,000+ items in renewal
- [rathena/src/map/pc.cpp](/Volumes/1TB/Projetos/rathena/src/map/pc.cpp) — `pc_additem`, `pc_delitem`, `pc_takeitem`, `pc_equipitem`, `pc_unequipitem`
- [rathena/src/map/clif.cpp](/Volumes/1TB/Projetos/rathena/src/map/clif.cpp) — inventory list, pickup, drop packets

## Scope (MS3 first pass)

**In scope:**
- `item_db.yml` parser → `IItemDb` catalog (name, type, weight, equipable slot, weapon/armor stats, script effect — but script effects are deferred to skills).
- `Inventory` model on `PlayerEntity` (mirrors `inventory` table; loaded from char on EnterMap via the existing IPC).
- Pickup: `CZ_ITEM_PICKUP (0x0093)` → walk to item → take, broadcast vanish.
- Drop: `CZ_ITEM_THROW (0x00a2)` → server creates `ItemEntity` on the map.
- Equip / unequip: `CZ_REQ_WEAR_EQUIP (0x00a9)` / `CZ_REQ_TAKEOFF_EQUIP (0x00ab)`.
- Use consumable: `CZ_USE_ITEM (0x00a7)` for potions, food, etc.
- Send / fetch cart / storage / guild storage — IPC already exists in P6.
- Mob drop emission (called from combat death).

**Out of scope:**
- Item scripts (e.g. Yggdrasil Leaf revives target). Need a minimal script subset for items, similar to NPC scope decision. Defer per-item; treat each item script as its own little C# handler initially.
- Refinement / enchant / bound items special rules — covered by inventory schema but UI flows are extra work.

## Done

- Char IPC wrappers exist for inventory operations (P6). Inventory is per-character in the DB and loaded on character data fetch.
- **Floor-item entity lifecycle (MS3 first slice):**
  - [`ItemEntity`](../../../../Map.Server/Entities/ItemEntity.cs) — `ItemId`, `Amount`, `Identified`, `SubX/SubY`, `DroppedAtTick`. EntityType.Item.
  - Packets: [`CZ_ITEM_PICKUP`](../../../../Core.Server/Packets/In/CZ/CZ_ITEM_PICKUP.cs) (0x0362, modern), [`ZC_ITEM_ENTRY`](../../../../Core.Server/Packets/Out/ZC/ZC_ITEM_ENTRY.cs) (0x009d, item already on floor entering view), [`ZC_ITEM_FALL_ENTRY`](../../../../Core.Server/Packets/Out/ZC/ZC_ITEM_FALL_ENTRY.cs) (0x0add, just-dropped animation; PACKETVER_RE ≥ 20180704), [`ZC_ITEM_DISAPPEAR`](../../../../Core.Server/Packets/Out/ZC/ZC_ITEM_DISAPPEAR.cs) (0x00a1).
  - [`IItemDropService`](../../../../Map.Server/Items/IItemDropService.cs) + [`ItemDropService`](../../../../Map.Server/Items/ItemDropService.cs):
    - `DropOnFloor(map, x, y, itemId, amount)` → registers the entity and broadcasts `ZC_ITEM_FALL_ENTRY` to PC viewers in AOI.
    - `TryPickup(picker, itemEntityId)` → range/map validation (2-cell pickup range matches rAthena), broadcasts `ZC_ITEM_DISAPPEAR`.
    - `Tick()` → per-map-tick despawn sweep at 60s TTL (rAthena's `flooritem_lifetime` default).
  - [`PickupHandler`](../../../../Map.Server/Handlers/PickupHandler.cs) routes `CZ_ITEM_PICKUP` → `TryPickup`.
  - Visibility refactor: `BuildEnterViewPacket` / `BuildExitViewPacket` now dispatch the right packet per entity type (STANDENTRY for PC/Mob, ITEM_ENTRY/DISAPPEAR for items). `SendCurrentViewToSelf` now surfaces all entity types on spawn (not just PCs).
  - 15 tests: 3 packet wire-shape, 6 `ItemDropService`, plus the existing visibility/spawn coverage still green.

### Pending (deferred)

- `item_db.yml` parser + `IItemDb` catalog (mirrors `IMobDb` pattern). Today `ZC_ITEM_FALL_ENTRY.ItemType` is hard-coded to 0; once item_db lands we'll populate it from the loaded catalog.
- `Inventory` model on the session + char-server IPC for persistence (already wired in P6, just needs the runtime model).
- Equip/unequip / consumable use / drop-from-inventory flows. Drop service is a building block for these.
- Loot protection (owner + party id), bound items, refined items.

## Pending

1. `IItemDb` singleton (mirrors mob_db pattern).
2. `ItemEntity : Entity` with `EntityType.ITEM`, holds `item_id, amount, dropped_by, drop_time` (auto-cleanup after 60s).
3. `InventoryService` per session: load on entity spawn, mutate on add/remove/equip, persist via `SaveCharacterStateAsync` IPC.
4. Equip-slot constraints: weapon slot, two-handed weapon blocks shield, etc.
5. Weight tracking: total inventory weight; exceeds `MaxWeight` → can't pickup / move slower.
6. Drop pool: when a mob dies, roll its drops; emit `ZC_NOTIFY_ITEM_LIST_BC` to viewers; create `ItemEntity` instances.

### Acceptance
- Mob drops items on death; players see them on the floor.
- Player walks over item + presses pickup key → item added to inventory + map item vanishes.
- Player drops an item → server creates the floor entity + sends VANISH from inventory.
- Equip/unequip changes stats (read by combat doc).

## History
- **2026-05-16** — Plan stub.
- **2026-05-16** — Floor-item lifecycle first slice shipped: ItemEntity + 4 packets + drop/pickup/despawn service + PickupHandler + visibility per-type packet dispatch. Inventory persistence remains queued.
