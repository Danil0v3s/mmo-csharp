# COMBAT-94 — Immediate inventory amount-update packet on consume (ammo + items)

> **Epic:** combat · **Status:** ❌ Not started · **Size:** S · **Player-visible:** yes
> **Depends on:** none · **Blocks:** none
> **Filed by:** COMBAT-76 — its scope mentioned "the explicit inventory amount packet on consume";
> the codebase has no immediate amount-update packet, so ammo/item consume only syncs the FULL
> removal (RemovedInventoryIds) and relies on the periodic PlayerStateService sync.

## Problem

When a stack is **partially** consumed (ammo round spent, potion used, item requirement paid),
the client is not told the new amount immediately. `AmmoService.ConsumeAmmo` and
`ItemUseService` only add to `MapSessionData.RemovedInventoryIds` when the stack hits **0**; a
partial decrement (amount still > 0) pushes nothing, so the client shows a stale count until the
next full state sync. rAthena sends an amount-update frame per consume.

## Current state (C#)

- `Map.Server/Inventory/AmmoService.cs:ConsumeAmmoFrom` — decrements `ammo.Amount`; only syncs
  (RemovedInventoryIds) when it reaches 0.
- `Map.Server/Inventory/ItemUseService.cs` — same pattern (decrement; RemovedInventoryIds on empty).
- No `ZC_ITEM_AMOUNT`-style packet exists in `Core.Server/Packets/Out/ZC/`.

## rAthena reference (source of truth)

- `clif_delitem` / the inventory amount-update frame rAthena emits from `pc_delitem`
  (the client decrements the shown stack each consume).

## Scope — every sub-system that must be touched

- [ ] Add the amount-update ZC packet (rAthena `pc_delitem` notify; PACKET_ZC_ITEM_DELITEM /
      the amount-decrement frame for the modern packetver) to `Core.Server/Packets/Out/ZC/`.
- [ ] Emit it from the shared consume path (ammo + item) on a partial decrement (amount > 0),
      to the owning client (SELF).

## Done criteria

- Spending ammo / using a potion updates the client's shown stack count immediately, without
  waiting for the periodic state sync; a stack hitting 0 still removes the slot as today.

## Test plan

- A consume that leaves amount > 0 emits the amount-update packet to the owner.

## Notes / gotchas

- This is codebase-wide (every consume path), not ammo-specific — wire it once in the shared
  delitem helper rather than per call site.
