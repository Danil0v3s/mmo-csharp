# GP-PET-RENAME-NAMEPKT — pet over-head name refreshes live on rename

> **Epic:** gameplay · **Status:** ❌ Not started · **Size:** S · **Player-visible:** yes (cosmetic)
> **Depends on:** none · **Unlocks:** none

## The deliverable

> After a player renames their pet, the floating name shown above the pet sprite (to everyone in
> view) updates immediately — matching rAthena's `clif_name`/`clif_blname_ack` push.

## Player story / why it matters

GP-PET (turn 4) implemented pet rename: `pet_change_name` validates + applies the new name + re-emits
the **status panel** (`ZC_PROPERTY_PET`), so the pet window shows the new name and the rename can't be
repeated. rAthena ALSO pushes the unit's over-head name to everyone in view (`clif_name` for the
`BL_PET`, the 0x0095 short form: `<GID>.L <name>.24`). The C# side has no `BL_PET` name packet — the
existing `ZC_ACK_REQNAMEALL` (0x0a30, 106 bytes) is the `BL_PC` form (with party/guild/title) and is
wrong for a pet. So after a live rename the floating name stays stale until the client re-requests it.

The pet-window name (the gameplay-relevant part) already updates; this is the cosmetic over-head label.

## Current state — per layer

| Layer | Exists? | Where / what's missing |
|---|---|---|
| Rename service | ✅ | `Map.Server/Pet/PetOps/PetOpsService.cs` `ChangeName` → status panel emit |
| BL_PET name packet | ☐ | no `clif_name`/0x0095 short-form packet; only the BL_PC 0x0a30 form exists |
| Over-head refresh on rename | ☐ | `ApplyPetName` should broadcast the unit name to view |

## rAthena reference

- `rathena/src/map/clif.cpp` `clif_name` (BL_PET branch) — the short `<GID>.L <name>.24` name packet
  (0x0095), broadcast `AREA` after a rename.
- `pet_change_name_ack` calls `clif_send_petstatus` + the name refresh.

## Scope — every layer

- [ ] Add the short BL-unit name packet (0x0095: `<GID>.L <name>.24`).
- [ ] In `PetOpsService.ApplyPetName`, broadcast it to the pet's view (via `IVisibilityService.SendToArea`).

## Done criteria

- Renaming a pet updates the floating name above the pet for the owner and nearby players without a
  relog or name re-request.

## Test plan

- Service test: `ChangeName` broadcasts the unit-name packet to area with the new name.

## Notes

- Filed by GP-PET (turn 4). Cosmetic — the pet window name already updates; this is the over-head label.
