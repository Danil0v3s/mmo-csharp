# COMBAT-70 — FREECAST cast-state ASPD recompute trigger

> **Epic:** combat · **Status:** ✅ Done (2026-06-03) · **Size:** M · **Player-visible:** yes
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

- [x] Precompute the freecast-adjusted attack delay in `CalcPc` (`BattleStats.FreecastAdelay` =
      `RenewalPcAmotion(…, freecastLv = LearnedSkills[SA_FREECAST]) * 2`) so the `5*(lv+10)%`
      scale hits the **ASPD base** (not the final adelay — the conversion is non-linear);
      `AttackService.Tick` swaps `Adelay → FreecastAdelay` via `NextSwingDelay` when the attacker
      `IsCasting`. This is the "attack-schedule point" approach (no recalc-trigger / restore /
      double-application). OveredBoost overrides both delays.
- [x] Confirmed a Free-Cast caster can attack while casting: `StartCast` does **not** stop the
      caster's `AttackState`, so the auto-attack continues through the cast and now uses the
      freecast delay — the modifier is live, not dormant.

## Done criteria

- ➡️ from COMBAT-50: a Free-Cast caster mid-cast attacks at the reduced amotion end-to-end (not
  just via the `RenewalPcAmotion(freecastLv:)` unit path), and returns to normal when the cast
  ends. ✅ Combat70FreecastTests (5).
- Discovered adjacent gap: the C# auto-attack loop has no cast-lock, so non-FREECAST casters can
  also attack while casting (rAthena blocks them). ➡️ COMBAT-88.

## Test plan

- `Combat70FreecastTests`: begin a cast on a SA_FREECAST caster → amotion reflects the
  `5*(lv+10)%` scale; end the cast → amotion restored.

## Notes / gotchas

- COMBAT-50 supplies the formula; this ticket only supplies the cast-state trigger / attack-loop
  integration. Watch for double-application if both a recompute and an attack-time factor are added.

## History

- 2026-06-03 · Made the SA_FREECAST amotion modifier take effect live. CalcPc now precomputes
  `BattleStats.FreecastAdelay` via a second pass through `RenewalPcAmotion` with the learned
  freecast level (the `5*(lv+10)%` scale must hit the ASPD base before the non-linear
  ASPD→amotion conversion, so a flat factor on the final adelay would be wrong); `AttackService`
  gained an optional `ISkillCastService` and a `NextSwingDelay` that swaps to `FreecastAdelay`
  while `IsCasting`. Confirmed `StartCast` doesn't stop the caster's auto-attack, so the effect is
  live. Combat70FreecastTests (5); Combat+Status suite 831 green, full suite 4093 pass (1 fail =
  pre-existing INFRA-11 replay gate). Filed COMBAT-88 (cast-lock: block non-FREECAST attack/move
  while casting — an adjacent gap discovered here).
