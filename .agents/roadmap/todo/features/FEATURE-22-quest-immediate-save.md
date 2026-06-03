# FEATURE-22 — Immediate quest persistence on mutation (rAthena chrif_save parity)

> **Epic:** Feature unlock · **Status:** ❌ Not started · **Size:** S · **Player-visible:** no
> **Depends on:** FEATURE-03 (quest mutations) · **Blocks:** none

## Problem

rAthena saves the quest log to the char server **immediately** on each mutation
(`quest_add`/`quest_delete`/`quest_update_status` call `chrif_save(sd, CSAVE_NORMAL)`
when `save_settings&CHARSAVE_QUEST`). FEATURE-03's `QuestService` mutates the in-memory
`PlayerEntity.QuestLog` but does **not** trigger a save — persistence rides the
existing FEATURE-02 fan-out (the periodic autosave + logout final-save). So a server
crash within the autosave window (default 30 s) loses quest accepts / abandons / kill
progress that rAthena would have already persisted.

## Current state (C#)

- `Map.Server/Quest/QuestService.cs Add/Delete/Change/UpdateStatus` — mutate
  `pc.QuestLog`; no save call (correct round-trip via autosave, but not immediate).
- `Map.Server/Services/Intif/IntifService.cs QuestSave(pc)` — the real save call, today
  driven by `MapServerImpl` autosave + `PlayerStateService` final-save (FEATURE-02).
- Note: `QuestService` cannot inject `IIntifService` directly — `IntifService` already
  depends on `IQuestService` (DI cycle). A callback/event seam is needed.

## rAthena reference (source of truth)

- `rathena/src/map/quest.cpp` — `quest_add` (:621), `quest_delete` (:711),
  `quest_update_status` (:877): `if (save_settings&CHARSAVE_QUEST) chrif_save(...)`.

## Scope

- [ ] Add a save seam to `QuestService` that does not create the `IntifService` DI cycle
      — e.g. an `Action<PlayerEntity>? OnQuestDirty` callback wired at startup to
      `IntifService.QuestSave`, or route mutations through a thin mediator. Invoke it
      from `Add`/`Delete`/`Change`/`UpdateStatus` (and optionally on objective complete).
- [ ] Gate on a config flag mirroring `CHARSAVE_QUEST` if one exists; otherwise always.

## Done criteria

- Accepting / abandoning / completing a quest triggers an immediate `QuestSave` (the
  char-side row is written without waiting for the autosave tick), with no DI cycle.

## Test plan

- `QuestServiceTests` — a recording save seam is invoked exactly once per Add/Delete/
  Change/complete.

## Notes / gotchas

- Don't double-save: the autosave + final-save fan-out stays; this only adds the
  immediate write. Keep it cheap (fire-and-forget, same as the autosave path).
