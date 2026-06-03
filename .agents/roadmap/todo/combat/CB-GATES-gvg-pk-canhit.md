# CB-GATES — GvG/BG/PK/can-hit/Emperium combat gates

> **Epic:** combat · **Status:** ❌ Not started · **Size:** S · **Player-visible:** yes
> **Depends on:** none (the Emperium half co-develops with GP-WOE) · **Unlocks:** GP-WOE

## The deliverable

> `battle_check_target` allows/denies hits correctly for guardians, the Emperium, immune
> mobs, and the GvG/BG can-hit cases. **Combat last** (but the Emperium branch is needed by GP-WOE).

## What this absorbs (archive)

- `_archive/todo/combat/COMBAT-80` — can-hit GvG/BG gate (guardian/Emperium/immune) + Emperium GvG branch.

## rAthena reference

- `rathena/src/map/battle.cpp` — `battle_check_target` (the BCT_* allegiance + the
  guardian/Emperium/`MD_*` immune branches), `battle_calc_pk_damage` (already landed in
  archive COMBAT-62).

## Scope

- [ ] Can-hit gate for guardians + the Emperium + `MD_STATUSIMMUNE`/`MD_DETECTOR` cases.
- [ ] The Emperium GvG can-be-hit branch (only damageable during WoE by an attacking guild).

## Done criteria

- A non-WoE player can't damage the Emperium; during WoE an attacking-guild member can; guardians
  follow the castle-owner allegiance; immune mobs are gated per rAthena.

## Test plan

- Extend the archived COMBAT-80 / Combat62 gate tests with the guardian/Emperium cases.

## Notes

- This is the engine gate GP-WOE's Emperium-break flow rides — co-develop the Emperium half there.
