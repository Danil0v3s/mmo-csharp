# INF-GAMELOG — Game-log SQL tables written

> **Epic:** infra · **Status:** ❌ Not started · **Size:** S · **Player-visible:** no
> **Depends on:** none · **Unlocks:** none

## The deliverable

> The game-log SQL tables (pick/zeny/mvp/chat/branch/feeding/npc) are written by the relevant
> systems, matching rAthena's `log_*` config gates — for audit/GM tooling.

## What this absorbs (archive)

- `_archive/todo/infra/INFRA-08` — game-log SQL tables (pick/zeny/mvp/chat/branch/feeding/npc).

## rAthena reference

- `rathena/src/map/log.cpp` — `log_pick_pc`/`log_zeny`/`log_mvpdrop`/`log_chat`/`log_branch`/
  `log_feeding`/`log_npc`, gated on `log_config`.

## Scope

- [ ] **Entities + migrations** for the log tables.
- [ ] **Write sites**: hook the item-pick/zeny/mvp/chat/branch/feeding/npc events to write a log row
      (gated on a `log_config` toggle).

## Done criteria

- The configured log types write a row on the matching event; the cash-shop purchase log
  (archive FEATURE-39) folds into this.

## Test plan

- Write-site tests (event → row) + the config gate.

## Notes

- Parallel, no client surface. Cross-cuts many systems — small per hook.
