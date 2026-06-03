# COMBAT-94 — Immediate inventory amount-update packet on consume (ammo + items)

> **Epic:** combat · **Status:** ✅ Done (2026-06-03) · **Size:** S · **Player-visible:** yes
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

- [x] Added `ZC_DELETE_ITEM_FROM_BODY` (0x07fa, rAthena `clif_delitem`) to
      `Core.Server/Packets/Out/ZC/` — 8 bytes `deleteType.W + index.W + amount.W` + the
      `PacketHeader.ZC_DELETE_ITEM_FROM_BODY` entry.
- [x] Wired the **shared** delitem helper once on `MapSessionData.NotifyItemConsumed(serverIndex,
      count, reason)` (client index = server slot + 2; no-op for count 0) and emit it on a partial
      decrement (amount > 0) from both consume paths: `AmmoService.ConsumeAmmoFrom` (reason 1 =
      "used for a skill", matching rAthena `battle_consume_ammo`) and `ItemUseService.UseItem`
      (reason 0 = Normal, matching `pc_useitem`). A full-slot removal still rides the existing
      `RemovedInventoryIds` sync.

## Done criteria

- ✅ Spending ammo / using a potion updates the client's shown stack count immediately (the SELF
  `ZC_DELETE_ITEM_FROM_BODY` frame), without waiting for the periodic state sync; a stack hitting 0
  still removes the slot via `RemovedInventoryIds` as today.

## Test plan

- ✅ `Combat94InventoryDelItemTests` (6): a partial ammo consume emits the frame with the consumed
  count + client index (reason 1); a full ammo consume emits nothing + tracks `RemovedInventoryIds`;
  a partial item use emits reason 0; item-use-to-zero emits nothing; the helper is a no-op for count 0.

## Notes / gotchas

- Wired once in the shared `MapSessionData.NotifyItemConsumed` helper rather than per call site.
- The skill-requirement item consume (`SkillRequirementService.ConsumeRequirement` type & 2) is a
  pre-existing **no-op** (data-pending on the skill_db Required-item column consume — distinct from
  COMBAT-92's loader); when that path wires real inventory mutation it should call the same
  `NotifyItemConsumed` helper. Not in this ticket's "ammo + item" scope, so no new ticket.

## History

- 2026-06-03 — Added `ZC_DELETE_ITEM_FROM_BODY` (0x07fa, rAthena clif_delitem) + the shared
  `MapSessionData.NotifyItemConsumed` helper, emitted on a partial decrement from the ammo
  (`AmmoService`, reason 1) and item-use (`ItemUseService`, reason 0) consume paths so the client's
  shown stack updates immediately. Full-slot removal keeps the `RemovedInventoryIds` sync.
  `Combat94InventoryDelItemTests` (6); Core.Server.Tests 111 + Map.Server.Tests 4241 (1 fail =
  pre-existing INFRA-11 replay gate) + Char.Server.Tests 167 green; solution builds. No follow-ups.
