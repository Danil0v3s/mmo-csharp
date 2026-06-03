# SKILL-18 — Dash/knockback slide broadcast (ZC_HIGHJUMP) on UnitOps.MovePos

> **Epic:** Skills · **Status:** ❌ Not started · **Size:** S · **Player-visible:** yes
> **Depends on:** none · **Blocks:** none

## Problem

When a skill slides the caster (Asura Strike's 3-cell forward dash, and the
knockback/`clif_blown` family), `UnitOpsService.MovePos` relocates the entity but does NOT
broadcast the slide animation packet, so to other clients the caster teleports instead of
sliding. `AsuraStrike.cs:62-67` notes it: *"our UnitOpsService.MovePos doesn't yet emit
ZC_HIGHJUMP — that's TODO for the unit-ops layer."* (Surfaced while doing SKILL-05.)

## Current state (C#)

- `Map.Server/Skills/Behaviors/Acolyte/AsuraStrike.cs:59-67` — calls `_unitOps.MovePos(...)`
  then a comment notes the missing `ZC_HIGHJUMP` broadcast.
- `Map.Server/.../UnitOpsService` `MovePos` — relocates the entity (collision-checked) but
  emits no slide packet.
- `Core.Server/Packets/.../ZC_HIGHJUMP` (id `0x01ff`) — the slide-animation packet exists
  (used by `clif_slide` knockback visuals) but `MovePos` doesn't send it.

## rAthena reference (source of truth)

- `clif.cpp clif_slide` / `clif_blown` — the slide/knockback broadcast (`ZC_HIGHJUMP`,
  `0x01ff`: `<id>.L <x>.W <y>.W`). rAthena's `unit_movepos(..., checkpath)` + `clif_slide`
  emit it so onlookers see the entity glide to the new cell.
- `skill.cpp MO_EXTREMITYFIST` runs the forward dash via `unit_movepos` + the slide clif.

## Scope — every sub-system that must be touched

- [ ] In `UnitOpsService.MovePos` (or a dedicated `Slide` helper), after a successful move,
      broadcast `ZC_HIGHJUMP` to the entity's AOI with the destination cell — mirroring
      `clif_slide`. Gate it so a normal (non-slide) reposition isn't animated as a jump if
      that would be wrong; match rAthena's `clif_slide` usage.
- [ ] Remove the `AsuraStrike.cs` TODO comment once the broadcast lands (Asura's dash then
      animates correctly with no plugin change needed).
- [ ] Audit other `MovePos` callers (knockback, Charge, Back Slide) to confirm they get the
      slide visual too.

## Done criteria

- Asura Strike's forward dash animates as a slide on onlooker clients (ZC_HIGHJUMP emitted),
  not an instant teleport.
- The `AsuraStrike.cs` ZC_HIGHJUMP TODO is gone.

## Test plan

- Unit/integration: call `MovePos` for a sliding skill; assert a `ZC_HIGHJUMP` packet to the
  AOI with the destination coordinates.

## Notes / gotchas

- Confirm `ZC_HIGHJUMP`'s exact wire shape against the existing packet class before wiring.
- Don't double-broadcast: if `MovePos` already sends a normal movement packet, ensure the
  slide replaces/augments it the way rAthena's `clif_slide` does.
