# SCR-EVENTS — Event-hook dispatch

> **Epic:** scripting · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** none · **Unlocks:** SCR-BULK

## The deliverable

> NPC event labels fire on the right triggers: `OnInit`, `OnTouch`/`OnTouch_`, `OnTimer<ms>`,
> `OnClock<HHMM>`/`OnDay`, `OnPCLoginEvent`/`OnPCDieEvent`/`OnPCKillEvent`/etc.

## What this absorbs (archive)

- `_archive/todo/scripting/SCRIPT-03` — event-hook dispatch (onInit/onTouch/onTimer/onClock/onPC*).

## rAthena reference

- `rathena/src/map/npc.cpp` — `npc_event_do`/`npc_event_runall`, `npc_touch_areanpc`,
  `npc_timerevent`, the `OnClock`/`OnDay` time labels, the `OnPC*` player-event hooks.

## Scope

- [ ] Wire each event source: boot (`OnInit`), cell-touch (`OnTouch`), per-NPC timers (`OnTimer`),
      wall-clock (`OnClock`/`OnDay`), and the player-lifecycle events (`OnPCLogin/Die/Kill/...`).

## Done criteria

- A test NPC's `OnInit`/`OnTouch`/`OnTimer`/`OnClock`/`OnPCLoginEvent` labels each fire at the
  right time; the WoE scheduler's `OnAgitStart` already proves the event-do path.

## Test plan

- Per-trigger tests + a live touch/timer NPC.

## Notes

- Truly last. The `EventDoAll` path already exists (used by the WoE scheduler) — extend it to the
  full trigger set.
