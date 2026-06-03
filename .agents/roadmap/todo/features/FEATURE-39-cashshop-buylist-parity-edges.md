# FEATURE-39 — Cash-shop buylist parity edges (pet-egg grant, trading gate, purchase log)

> **Epic:** Gameplay-Shop · **Status:** ❌ Not started · **Size:** S · **Player-visible:** yes
> **Depends on:** FEATURE-13 · **Blocks:** none

## Problem

FEATURE-13's `cashshop_buylist` covers the price/validate/pay/grant core, but three
small rAthena behaviours are not yet mirrored:

1. **Pet-egg grant.** rAthena's grant loop calls `pet_create_egg(sd, nameid)` first;
   if the bought item is a pet egg it **creates the pet directly** rather than adding
   the egg item to the bag. The C# path always grants the egg *item* (the player must
   hatch it separately).
2. **Trading-state gate.** rAthena rejects the buy with `ERROR_TYPE_EXCHANGE` when
   `sd->state.trading`. `PlayerEntity` exposes no trading-state flag, so the C# path
   has no such gate.
3. **Purchase log.** rAthena `log_cash(... LOG_TYPE_CASH ...)` records the cash spend.
   The C# path logs an info line but writes no audit/log row.

## Current state (C#)

- `Map.Server/Shop/Cash/CashShopService.cs:BuyList` — grant loop calls
  `_inventory.GiveItem(...)` for every item (no pet-egg branch); no trading gate; info-log only.
- `Map.Server/Pet/PetOps/PetOpsService.cs:CreateEgg` — the existing egg→pet-class resolver +
  `PetCreate` the pet-egg branch would call.

## rAthena reference (source of truth)

- `rathena/src/map/cashshop.cpp:cashshop_buylist` (~line 540 grant loop):
  - `if (!pet_create_egg(sd, nameid)) { … pc_additem … }` — pet egg → pet, else add item.
  - early `else if (sd->state.trading) { clif_cashshop_result(sd, 0, CASHSHOP_RESULT_ERROR_PC_STATE); return false; }`.
  - `pc_paycash(... LOG_TYPE_CASH)` → `log_cash`.

## Scope — every sub-system that must be touched

- [ ] Pet-egg branch: in the grant loop, if the item resolves to a pet egg
      (`IPetOpsService`/`PetOps` egg→class index), create the pet instead of granting the egg item.
      Mind the `CashShopService` (singleton) → `IPetOpsService` dependency direction.
- [ ] Trading-state gate: add a trading/busy flag to `PlayerEntity` (or read the existing trade
      service state) and reject with `CashShopResult.Exchange` when set — matching rAthena's pre-loop
      check order.
- [ ] Purchase log: record the cash spend (item ids, amount, points) via the project's audit/log path.

## Done criteria

- Buying a pet-egg cash item spawns/creates the pet (or stores the pet egg bound to a pet id),
  matching `pet_create_egg`, not a bare inventory egg.
- A buy attempted while the player is in a trade returns `CashShopResult.Exchange` with no mutation.
- A completed buy writes a cash-purchase log entry.

## Test plan

- `CashShopServiceTests`: pet-egg item → pet created (not an inventory egg); trading flag set → buy
  rejected with `Exchange`; successful buy emits a log/audit record.

## Notes / gotchas

- The pet-egg branch must not double-charge: price is summed in pass 1; the grant just chooses
  pet-vs-item.
- `CashShopService` is a singleton — inject `IPetOpsService` (also singleton) directly; do not create a
  DI cycle.
