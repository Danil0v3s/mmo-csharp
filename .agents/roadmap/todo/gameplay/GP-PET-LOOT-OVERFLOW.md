# GP-PET-LOOT-OVERFLOW — pet loot that won't fit drops on the ground

> **Epic:** gameplay · **Status:** ❌ Not started · **Size:** S · **Player-visible:** yes (edge case)
> **Depends on:** none · **Unlocks:** none

## The deliverable

> When a pet deposits its loot bag and an item can't be added to the owner (full/overweight
> inventory), that item drops on the ground at the pet — matching rAthena `pet_lootitem_drop` — and the
> pet's re-loot cooldown (`canact_tick + 10s`) is applied.

## Player story / why it matters

GP-PET (turn 5, FEATURE-28) implemented the pet loot bag + deposit (`pet_lootitem_drop`): the pet
hunts floor items into its bag, and on return-to-egg the bag is handed to the owner's inventory.
Items that fit are added; items that **don't** fit currently stay in the pet's loot bag (so they're
not lost). rAthena instead drops the un-addable items on the ground at the pet's tile
(`map_addflooritem` via the delayed-drop list) and empties the bag, then sets
`pd.ud.canact_tick = gettick() + 10000` so the pet can't immediately re-loot what it just dropped.

The common path (room in the bag) is correct; this is the full-inventory overflow + cooldown.

## Current state — per layer

| Layer | Exists? | Where / what's missing |
|---|---|---|
| Deposit | ✅ (partial) | `Map.Server/Pet/PetOps/PetOpsService.cs` `LootItemDrop` — delivers what fits, keeps the rest in the bag |
| Ground drop on overflow | ☐ | should `IItemDropService`-drop the un-addable items at the pet, then clear the bag |
| Re-loot cooldown | ☐ | no `canact_tick + 10s` gate after a drop |

## rAthena reference

- `rathena/src/map/pet.cpp` `pet_lootitem_drop` — `pc_additem`; on failure push to the delayed
  `s_item_drop_list` dropped at the pet's tile; then `memset` the bag, `count = 0`, `weight = 0`,
  `canact_tick = gettick()+10000`.

## Scope — every layer

- [ ] On `GiveItem` failure, drop the item at the pet's position (`IItemDropService`) instead of
      keeping it in the bag; always empty the bag after a deposit.
- [ ] Apply a 10s re-loot cooldown to the pet after a deposit (a `canact`-style tick the AI loot step
      checks).

## Done criteria

- A pet depositing into a full inventory drops the overflow on the ground (not retained in the bag),
  and doesn't immediately re-loot it.

## Test plan

- Service test: full inventory → overflow items dropped via the item-drop service; bag emptied.
- AI test: a pet that just deposited doesn't loot for the cooldown window.

## Notes

- Filed by GP-PET (turn 5). The non-overflow path is correct; this is the full-bag edge + cooldown.
