# COMBAT-57 — KO_JYUMONJIKIRI SC_JYUMONJIKIRI ratio bonus + position-shift/double-hit

> **Epic:** Combat parity · **Status:** 🚧 In progress · **Size:** S · **Player-visible:** yes
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

- [ ] Add the `+lv*srcBaseLevel` ratio bonus when the target has SC_JYUMONJIKIRI (use
      the ctx-aware ratio overload so the target SC is readable).
- [ ] Port the position-shift + double-hit behavior.

## Done criteria

- A target already carrying SC_JYUMONJIKIRI takes the boosted ratio.
- The skill renders/deals its two hits with the rAthena position shift.

## Test plan

- Ratio with vs without SC_JYUMONJIKIRI on the target.
- Hit-count = 2.
