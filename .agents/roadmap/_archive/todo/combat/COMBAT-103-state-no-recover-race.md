# COMBAT-103 — bStateNoRecoverRace (on-hit no-HP/SP-recover by race)

> **Epic:** combat · **Status:** ❌ Not started · **Size:** S · **Player-visible:** yes
> **Depends on:** COMBAT-83 · **Blocks:** none · **Filed by:** COMBAT-83.

## Problem

`bonus3 bStateNoRecoverRace, r, rate, dur`: hitting race r has a `rate` chance to inflict
SC_NORECOVER_STATE on the target for `dur`, blocking its natural HP/SP regen. The live host skips it;
the SC + the on-hit proc aren't wired.

## Current state (C#)

- `Map.Server/Inventory/EquipBonusBundle.cs` — no state-no-recover list.
- `Map.Server/Status/StatusEffectRegistry.cs` — confirm SC_NORECOVER_STATE exists (regen gate).

## rAthena reference (source of truth)

- `pc.cpp` SP_STATE_NO_RECOVER_RACE; `status.cpp` SC_NORECOVER_STATE (regen block);
  `battle.cpp skill_additional_effect`/the on-hit proc.

## Scope

- [ ] Add the per-race no-recover list (race, rate, dur) to the bundle + parse the bonus3 form.
- [ ] On a PC hit vs the matching race, roll `rate` and apply SC_NORECOVER_STATE for `dur`.
- [ ] Ensure the regen service honors SC_NORECOVER_STATE.

## Done criteria

- A bStateNoRecoverRace card applies the no-recover SC on a matching-race hit; the target stops regen.

## Test plan

- Guaranteed-rate hit applies SC_NORECOVER_STATE; a regen tick is suppressed.
