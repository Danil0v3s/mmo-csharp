# GP-CASHSHOP-SLOT-WEIGHT-CODE — cash-shop buy reports slot-full vs over-weight distinctly

> **Epic:** gameplay · **Status:** ❌ Not started · **Size:** S · **Player-visible:** yes
> **Depends on:** GP-CASHSHOP · **Unlocks:** none

## The deliverable

> A cash-shop buy that fails for **no free inventory slot** reports
> `CASHSHOP_RESULT_ERROR_INVENTORY_ITEMCNT` (5), and one that fails for **over the weight limit**
> reports `CASHSHOP_RESULT_ERROR_INVENTORY_WEIGHT` (4) — distinct codes, matching rAthena.

## Player story / why it matters

GP-CASHSHOP wired the cash-shop-button buy end-to-end. The service `CashShopService.BuyList`
returns a single `CashShopResult.InventoryWeight` for **both** the over-weight gate
(`CurrentWeight + total > MaxWeight`) and the no-free-slot gate
(`MaxInventory - inv.Count < freshSlots`) — it mirrors the NPC-shop `e_CASHSHOP_ACK` enum, which
collapses them into one code. `BuyCashItemHandler.Map` therefore always emits `INVENTORY_WEIGHT`
(4) even when the real cause was a full inventory (rAthena emits `INVENTORY_ITEMCNT`, 5). The
client shows a slightly wrong reason. Functionally the buy is still correctly rejected with no
mutation; only the reported code differs.

## Current state — per layer

| Layer | Exists? | Where / what's missing |
|---|---|---|
| Service | partial | `Map.Server/Shop/Cash/CashShopService.cs:BuyList` — both gates `return CashShopResult.InventoryWeight` |
| Wire enum | ✅ | `Core.Server/Packets/Out/ZC/ZC_PC_BUY_CASHITEM_RESULT.cs` — both `InventoryWeight=4` + `InventoryItemCnt=5` exist |
| Handler map | partial | `Map.Server/Handlers/Shop/BuyCashItemHandler.cs:Map` — InventoryWeight → InventoryWeight only |

## rAthena reference

- `rathena/src/map/cashshop.cpp:cashshop_buylist` — emits
  `CASHSHOP_RESULT_ERROR_INVENTORY_WEIGHT` for `totalweight + sd->weight > sd->max_weight` and
  `CASHSHOP_RESULT_ERROR_INVENTORY_ITEMCNT` for `pc_inventoryblank(sd) < new_`.

## Scope

- [ ] **Service**: split the two gates into distinct `CashShopResult` values (add an
      `InventorySpace` value, or thread a richer result), so the weight vs slot cause survives.
- [ ] **Handler**: map the new value to `CashShopBuyResult.InventoryItemCnt` (5).
- [ ] **Test**: a buy rejected for a full inventory asserts code 5; an over-weight buy asserts code 4.

## Done criteria

- No-free-slot rejection → `ZC_PC_BUY_CASHITEM_RESULT` result = 5; over-weight → result = 4.

## Test plan

- Extend `CashShopServiceTests` / the bridge tests with the two distinct rejection codes.

## Notes

- Filed by GP-CASHSHOP (turn 1). The collapse is pre-existing FEATURE-13 behavior; this only
  refines the reported failure code — the rejection itself (no mutation) is already correct.
