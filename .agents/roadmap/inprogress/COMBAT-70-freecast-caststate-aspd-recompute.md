# COMBAT-70 — FREECAST cast-state ASPD recompute trigger

> **Epic:** combat · **Status:** 🚧 In progress · **Size:** M · **Player-visible:** yes
> **Depends on:** COMBAT-50 (the FREECAST amotion formula) · **Blocks:** none
> **Filed by:** COMBAT-50 — the cast-state trigger that makes the FREECAST formula take effect live.

## Problem

COMBAT-50 implemented the SA_FREECAST amotion formula in `RenewalPcAmotion` (the `freecastLv`
parameter: while casting, ASPD scales to `5*(lv+10)%`). But `StatusCalcService.CalcPc` always
passes `freecastLv: 0` because ASPD is only recomputed on stat changes, not on cast start/end —
so the FREECAST speed-up is **dormant on a live server**. To make it take effect, the caster's
amotion must be recomputed (with `freecastLv` = learned SA_FREECAST level) when a cast begins,
and restored when it ends — i.e. attacking-while-casting must use the FREECAST-adjusted amotion.

## Current state (C#)

- `Map.Server/Status/StatusCalcService.cs:RenewalPcAmotion` — accepts `freecastLv` and applies
  `aspd = aspd * 5 * (freecastLv + 10) / 100` (COMBAT-50, tested), but `CalcPc` passes 0.
- No cast-start/cast-end hook recomputes amotion; `Map.Server/Combat/AttackService.cs` schedules
  the next swing off `Stats.Adelay` with no cast-state awareness.

## rAthena reference (source of truth)

- `status.cpp:6156` — `if (sd->ud.skilltimer != INVALID_TIMER && (skill_lv = pc_checkskill(sd, SA_FREECAST)) > 0)` then (RENEWAL_ASPD) `amotion = amotion * 5 * (skill_lv + 10) / 100;`. The
  modifier is part of `status_calc_bl`'s amotion path, evaluated while the cast timer is active.

## Scope — every sub-system that must be touched

- [ ] On cast start (the `ud.skilltimer` equivalent), recompute the caster's amotion with
      `freecastLv = LearnedSkills[SA_FREECAST]` (and restore on cast end), OR apply the FREECAST
      factor at the attack-schedule point in `AttackService` when the attacker is mid-cast + has
      SA_FREECAST.
- [ ] Confirm a Free-Cast caster can actually attack/move while casting (the precondition for the
      modifier to matter).

## Done criteria

- ➡️ from COMBAT-50: a Free-Cast caster mid-cast attacks at the reduced amotion end-to-end (not
  just via the `RenewalPcAmotion(freecastLv:)` unit path), and returns to normal when the cast ends.

## Test plan

- `Combat70FreecastTests`: begin a cast on a SA_FREECAST caster → amotion reflects the
  `5*(lv+10)%` scale; end the cast → amotion restored.

## Notes / gotchas

- COMBAT-50 supplies the formula; this ticket only supplies the cast-state trigger / attack-loop
  integration. Watch for double-application if both a recompute and an attack-time factor are added.
