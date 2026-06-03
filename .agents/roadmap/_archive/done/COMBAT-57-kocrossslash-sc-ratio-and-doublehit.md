# COMBAT-57 — KO_JYUMONJIKIRI SC_JYUMONJIKIRI ratio bonus + position-shift/double-hit

> **Epic:** Combat parity · **Status:** ✅ Done (2026-06-02) · **Size:** S · **Player-visible:** yes
> **Depends on:** COMBAT-35
> **Blocks:** none
> **Filed by:** COMBAT-35 — KoCrossSlash's pre-existing unfinished behavior (cleared its
> docstring TODO while adding the RE_LVL_DMOD divisor).

## Problem

`KoCrossSlash` (KO_JYUMONJIKIRI) implements the base ratio `+(-100 + 200*lv)` and the
SC_JYUMONJIKIRI debuff apply, but two rAthena behaviors are missing:

1. **The conditional ratio bonus** `+lv*srcBaseLevel` when the target already carries
   `SC_JYUMONJIKIRI` (rAthena `kocrossslash.cpp`).
2. **Position-shift + double-hit** behavior.

## Current state (C#)

- `Map.Server/Skills/Behaviors/Ninja/KoCrossSlash.cs` — `CalculateSkillRatio` returns
  the base ratio only (no SC_JYUMONJIKIRI branch); no double-hit / position shift.
  COMBAT-35 added `ReLvlDivisor => 120`.

## rAthena reference

- `rathena/src/map/battle.cpp` KO_JYUMONJIKIRI arm (~5641): `+lv*src_base_level` when
  the target has SC_JYUMONJIKIRI; the skill hits twice with a position shift.

## Scope

- [x] Add the `+lv*srcBaseLevel` ratio bonus when the target has SC_JYUMONJIKIRI. Added via a
      new `SkillImpl.CalculateSkillRatioPostDmod` hook (applied AFTER RE_LVL_DMOD, matching
      rAthena where the bonus is added below the macro — so it is NOT scaled). KoCrossSlash
      reads the target SC through the behavior context.
- [x] Port the position-shift + double-hit behavior. Position-shift: `CastendDamageId` now
      slides the caster to a cell offset from the target (rAthena `map_calc_dir` convention)
      via `ctx.UnitOps.MovePos` (slide-broadcast), then strikes; blocked move → no strike.
      Double-hit was already present (`SkillHitCounts` `KO_JYUMONJIKIRI → -2`, COMBAT-39).

## Done criteria

- A target already carrying SC_JYUMONJIKIRI takes the boosted ratio. ✅ (re-hit: cast 1 applies
  the SC, cast 2 reads it → +`lv*srcLv`% verified).
- The skill renders/deals its two hits with the rAthena position shift. ✅ (`GetMultiHitCount`=2;
  the move-pos slide is broadcast before the strike).

## History

- 2026-06-02 — Added `SkillImpl.CalculateSkillRatioPostDmod` (post-RE_LVL_DMOD unscaled ratio
  hook) + wired it into the shared `ComputeSkillDamage`; KoCrossSlash uses it for the
  `+lv*srcBaseLv` SC_JYUMONJIKIRI bonus, and overrides `CastendDamageId` for the caster
  position-shift (`ctx.UnitOps.MovePos` slide, rAthena dir offset) before the (already
  2-hit) strike. `Combat57KoCrossSlashTests` (3, green); full suite 4022 pass (1 fail =
  pre-existing INFRA-11 replay gate). Filed COMBAT-75 (the SC_KAGEMUSYA ratio bonus, a
  systemic 11-arm gap visible in the same arm but out of scope).

## Test plan

- Ratio with vs without SC_JYUMONJIKIRI on the target.
- Hit-count = 2.
