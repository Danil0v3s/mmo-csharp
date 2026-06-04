# GP-VEND-OVERWEIGHT — a buyer over the weight limit is refused

> **Epic:** gameplay · **Status:** ❌ Not started · **Size:** S · **Player-visible:** yes
> **Depends on:** none · **Unlocks:** none

## The deliverable

> A vending purchase that would push the buyer over their max weight is refused with the overweight
> result (no transfer) — matching rAthena `vending_purchasereq`'s `PURCHASEMC_OVERWEIGHT` gate.

## Player story / why it matters

GP-VEND (turn 2) implemented the full browse→buy flow with the zeny / stock / store-incorrect /
inventory-full gates and the result emits. rAthena `vending_purchasereq` (vending.cpp:183) has one
more gate: it sums the bought items' weight and, if `w + sd->weight > sd->max_weight`, emits
`clif_buyvending(idx, amount, PURCHASEMC_OVERWEIGHT)` and aborts. The C# `PurchaseReq` validates
inventory-slot count but not weight.

**Why it's split out:** the weight gate needs the buyer's current weight (sum of inventory item
weights) + max weight, exposed as values. The pieces exist inside `PlayerWeightStatusService`
(`IItemCatalog.Get(id).Weight` + `RenewalFormulas.MaxWeight`) but only as a private percent
calculation — there's no public current/max-weight accessor for `PurchaseReq` to call, and no
"weight of the items I'm about to buy" helper. That's a small, self-contained addition.

## Current state — per layer

| Layer | Exists? | Where / what's missing |
|---|---|---|
| Purchase gates | ✅ (partial) | `Map.Server/Shop/Vending/VendingService.cs` `PurchaseReq` — zeny/stock/slot/store-incorrect done |
| Weight values | partial | `PlayerWeightStatusService.GetPercentWeight` (private) has the inputs; no public current/max accessor |
| Overweight gate | ☐ | sum to-buy weight + `buyer weight > max` → `VendPurchaseResult.Overweight`, no transfer |

## rAthena reference

- `rathena/src/map/vending.cpp` `vending_purchasereq` (183-187) — `w += itemdb_weight(nameid) * amount;
  if (w + sd->weight > sd->max_weight) { clif_buyvending(..., PURCHASEMC_OVERWEIGHT); return; }`.

## Scope — every layer

- [ ] Expose current weight + max weight from the weight service (or compute inline in `PurchaseReq`
      via `IItemCatalog` weights + `RenewalFormulas.MaxWeight`).
- [ ] In `PurchaseReq`, add the overweight gate (sum the plan's item weights × qty) before the transfer;
      on failure emit `VendPurchaseResult.Overweight` and abort.

## Done criteria

- A buyer whose bag would exceed max weight after the purchase is refused with the overweight result;
  no zeny/item moves.

## Test plan

- Service test: a near-cap buyer buying a heavy stack → `Overweight` result, no transfer.

## Notes

- Filed by GP-VEND (turn 2). The other purchase gates are done; this is the weight gate.
