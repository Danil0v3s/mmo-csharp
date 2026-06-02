# COMBAT-52 — die_counter persistence + death-increment wiring

> **Epic:** Combat parity · **Status:** ❌ Not started · **Size:** S · **Player-visible:** yes
> **Depends on:** COMBAT-32 (the `PlayerEntity.DieCounter` field + Super Novice gate)
> **Blocks:** none
> **Filed by:** COMBAT-32 — the Super Novice +10 gate reads a `DieCounter` that nothing populates.

## Problem

COMBAT-32 added `PlayerEntity.DieCounter` and gated the Super Novice all-stat +10
on `DieCounter == 0` (rAthena status.cpp:4222). The multiplier logic is correct and
tested, but the field is **never written**: it defaults 0 and is not loaded from the
character record nor incremented when the player dies. So in live play every Super
Novice is treated as "never died" forever — a Super Novice keeps the +10 after dying
until the field is wired.

rAthena loads `sd->die_counter` from the char DB and increments it in `pc_dead`
(and persists it back), so the bonus is correctly lost after the first death.

## Current state (C#)

- `Map.Server/Entities/PlayerEntity.cs:DieCounter` — `int`, defaults 0, no writer.
- `Map.Server/Status/StatusCalcService.cs:ApplyPassiveBaseStatAddends` — reads it.
- The PC death path (`IPcDeathService` / `PcDeathService`) does not touch it.
- `Core.Database/Entities/CharEntity.cs` has no `die_counter` column.

## rAthena reference (source of truth)

- `pc.cpp pc_dead` — `sd->die_counter++` (and a `pc_setglobalreg`/save), then triggers
  the `OnPCDieEvent` + stat recalc.
- char-load fills `sd->die_counter` from the saved character data.

## Scope — every sub-system that must be touched

- [ ] Add the `die_counter` column to `CharEntity` (+ EF migration) and load it into
      `PlayerEntity.DieCounter` on map enter (the char→map prepare-player handoff).
- [ ] Increment `DieCounter` in the PC death path and persist it; trigger a stat
      recalc so the Super Novice +10 drops on the first death.
- [ ] Verify the recalc actually re-evaluates the gate (it rides the COMBAT-10
      delta-fold, so a recalc after the increment must shift the six base stats −10).

## Done criteria

- A Super Novice that dies once permanently loses the +10 (after recalc), and the
  loss survives relog (die_counter persisted + reloaded).

## Test plan

- Unit: increment `DieCounter` then `CalcPc` → the six base stats drop by 10.
- Integration: char-load round-trips a non-zero `die_counter`.

## Notes / gotchas

- rAthena resets die_counter on some events (e.g. certain rebirths) — out of scope
  here; this ticket only covers load + on-death increment + persistence.
