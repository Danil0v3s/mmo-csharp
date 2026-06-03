# SCR-PLAYER — Player-state mutation builtins

> **Epic:** scripting · **Status:** ❌ Not started · **Size:** L · **Player-visible:** yes
> **Depends on:** SCR-DIALOG · **Unlocks:** SCR-BULK, GP-ACHIEVE (reward scripts)

## The deliverable

> The big player-state builtins work: `warp`, `heal`/`percentheal`, `getitem`/`delitem`/`countitem`,
> `jobchange`, `sc_start`/`sc_end`, `skill`/`addtoskill`, `set` of player vars, `checkweight`, etc.

## What this absorbs (archive)

- `_archive/todo/scripting/SCRIPT-02` — player state-mutation builtins (warp/heal/item/job/sc/skill).

## rAthena reference

- `rathena/src/map/script.cpp` — `buildin_warp`, `buildin_heal`/`percentheal`, `buildin_getitem`/
  `delitem`/`countitem`, `buildin_jobchange`, `buildin_sc_start`/`sc_end`, `buildin_skill`/`addtoskill`,
  `buildin_checkweight`, the `@`/player-var readers/writers.

## Scope

- [ ] Implement each builtin in the V8 host, calling the real map services (warp→setpos, item→
      InventoryService, sc→StatusChangeService, skill→skill grant, job→jobchange).

## Done criteria

- A test NPC can warp/heal/give-item/change-job/start-SC/grant-skill on the player and it takes
  effect + persists; no `ScriptStub` left for these.

## Test plan

- Builtin tests (each → its service) + a live NPC that mutates player state.

## Notes

- Truly last. This is the biggest scripting ticket (the "do something to the player" surface).
  Reuses the gameplay services already built (inventory/sc/skill/warp).
