# COMBAT-52 — die_counter persistence + death-increment wiring

> **Epic:** Combat parity · **Status:** ✅ Done (2026-06-02) · **Size:** S · **Player-visible:** yes
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

- [x] Persist + load `die_counter`. **rAthena stores it as the per-character permanent
      register `PC_DIE_COUNTER` (`PCDIECOUNTER_VAR`, `pc_readglobalreg`/`pc_setglobalreg`),
      NOT a char-table column** — so this uses the existing perm var-reg pipeline
      (`PlayerStateService` ↔ `char_reg_num`) via the new `DieCounterReg` helper. No EF
      migration needed (more faithful than the audit's column assumption). Loaded onto
      `PlayerEntity.DieCounter` in `NotifyActorInitHandler` before the login `CalcPc`.
- [x] Increment `DieCounter` in `PcDeathService.OnPcDead` (rAthena `pc_dead` →
      `pc_setparam(SP_PCDIECOUNTER, +1)`) and run `status_calc_pc` (via the shared
      `PcRecalcInputs.FromCurrent`) so the Super Novice +10 drops on the first death;
      persisted at the next `SaveAsync`.
- [x] Verified the recalc re-evaluates the gate (rides the COMBAT-10 delta-fold): the
      `OnPcDead` test asserts the six base stats shift −10 after the increment.

## Done criteria

- A Super Novice that dies once permanently loses the +10 (after recalc), and the
  loss survives relog (die_counter persisted + reloaded). ✅ — `OnPcDead` drops the +10
  (recalc), and `DieCounterReg` round-trips through the perm scope (→ `char_reg_num`).

## History

- 2026-06-02 — Wired `die_counter` end-to-end via the rAthena `PC_DIE_COUNTER` char
  register (new `DieCounterReg` helper over the perm var-reg pipeline — no schema column):
  loaded onto `PlayerEntity.DieCounter` pre-CalcPc in `NotifyActorInitHandler`, incremented +
  recalced in `PcDeathService.OnPcDead`, persisted in `PlayerStateService.SaveAsync`. Extracted
  the recalc-input builder to shared `PcRecalcInputs.FromCurrent` (ExpService delegates).
  Tests: `Combat52DieCounterTests` (5, green); Status+Combat+Session suite 721 green; full suite
  3995 pass (1 fail = pre-existing INFRA-11 replay gate). No follow-ups (rebirth-reset is
  out-of-scope per the ticket Notes).

## Test plan

- Unit: increment `DieCounter` then `CalcPc` → the six base stats drop by 10.
- Integration: char-load round-trips a non-zero `die_counter`.

## Notes / gotchas

- rAthena resets die_counter on some events (e.g. certain rebirths) — out of scope
  here; this ticket only covers load + on-death increment + persistence.
