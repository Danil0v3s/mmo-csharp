# SCR-BULK — Bulk NPC conversion + real town NPCs (capstone)

> **Epic:** scripting · **Status:** ❌ Not started · **Size:** XL · **Player-visible:** yes
> **Depends on:** SCR-DIALOG, SCR-PLAYER, SCR-EVENTS, SCR-CONTROL, SCR-VARS, SCR-DOMAIN · **Unlocks:** GP-WOE (castle scripts)

## The deliverable

> The rAthena `.txt` NPC corpus transpiles + loads, and the core town NPCs work in-game: a
> kafra (warp/save/storage), tool dealer, healer, job-changers, and the WoE `agit_controller` +
> castle scripts.

## What this absorbs (archive)

- `_archive/todo/scripting/SCRIPT-10` — bulk NPC script conversion from rAthena `.txt` (transpiler + duplicate).

## rAthena reference

- `npc/` corpus (`.txt` scripts); `rathena/src/map/script.cpp` `buildin_*` coverage; the
  `duplicate(...)` / `registerDuplicate` mechanism.

## Scope

- [ ] The `.txt`→V8 transpiler covering the builtins landed in SCR-DIALOG/PLAYER/EVENTS/CONTROL/
      VARS/DOMAIN + `duplicate()`.
- [ ] Convert + place the core town NPCs (kafra, tool dealer, healer, job-changers) and the WoE
      `agit_controller` + castle scripts.

## Done criteria

- A player in prontera can use a converted kafra (warp/save/storage), buy from a tool dealer, get
  healed, change job; the rAthena town corpus transpiles + places; the WoE castle scripts drive
  GP-WOE's engine hooks.

## Test plan

- Transpiler tests on a sample of `.txt` NPCs + a live town walkthrough.

## Notes

- Truly last, and the capstone — hard-blocked on every other scripting ticket (it composes them).
  This is what makes the world feel populated.
