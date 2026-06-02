# COMBAT-13 — Asura Strike renewal ×2 when cast with >5 spirit spheres

> **Epic:** Combat parity · **Status:** ✅ Done (2026-06-02) · **Size:** S · **Player-visible:** yes
> **Depends on:** none · **Blocks:** none
> **Filed by:** COMBAT-02 on 2026-06-01 (ratio + constant landed; this bump did not).

## Problem

rAthena MO_EXTREMITYFIST (battle.cpp:4843) doubles the whole skill ratio when the
caster had **more than 5 spirit spheres** at cast time:

```c
skillratio += 700 + sstatus->sp * 10;
#ifdef RENEWAL
    if (wd->miscflag & 1)   // set upstream when >5 spirit balls were active
        skillratio *= 2;
#endif
skillratio = min(500000, skillratio);
```

COMBAT-02 ported the `+700 + sp*10` ratio and the `250 + 150*lv` constant, but the
`×2` is not applied — the C# cast pipeline never computes the `miscflag&1`
(>5-sphere) bit for Asura, and `AsuraStrike.CalculateSkillRatio` doesn't read the
sphere count.

## Current state (C#)

- `Map.Server/Skills/Behaviors/Acolyte/AsuraStrike.cs` — `CalculateSkillRatio`
  computes `+700 + sp*10` + cap, no ×2.
- `Map.Server/Entities/PlayerEntity.cs:SpiritBall` — the sphere counter exists.
- The skill-cast/requirement path consumes spirit spheres; **verify whether
  `SpiritBall` still holds the pre-cast count when `CastendDamageId` runs** (rAthena
  captures the count into `miscflag` at cast-start, before consumption).

## rAthena reference

- `battle.cpp:4843` MO_EXTREMITYFIST (above). The `miscflag&1` is set in
  `skill.cpp` Asura cast handling based on `sd->spiritball_old > 5` at cast time.

## Scope

- [x] **Capture the sphere count + thread it** — ✅ `AsuraStrike.CastendDamageId`
      reads `PlayerEntity.SpiritBall`, sets `miscflag = spheres > 5 ? 1 : 0`, and
      calls `base.CastendDamageId(…, ctx, miscflag)` so the bit flows through
      `WeaponSkillImpl.ComputeSkillDamage` → the miscflag-aware ratio overload.
- [x] **×2 before the cap** — ✅ added the miscflag-aware
      `CalculateSkillRatio(…, ctx, miscflag)` override: `ratio = 100 + 700 + sp*10`,
      then `if (miscflag & 1) ratio *= 2`, then `min(500000, ratio)` (battle.cpp:4843-4847).
      The no-ctx overload (funnel path) keeps the un-doubled ratio.
- [x] **Confirm consume-timing** — ✅ `SkillRequirementService.ConsumeRequirement`
      spends only HP/SP/AP; spirit balls are **never consumed** (`SpiritBallCost`
      has no callers), so the live `SpiritBall` IS the pre-cast count. The actual
      spirit-ball consumption (Asura `pc_delspiritball` + `SpiritBallCost` wiring)
      ➡️ **SKILL-19**.

## Done criteria

- ✅ Asura cast with 6+ spheres deals exactly 2× the ratio portion of a ≤5-sphere
  cast (constant `250+150*lv` added once on both; cap 500000 applied after the ×2)
  — `Combat13AsuraSphereTests.CastendDamageId_with_6_spheres_*`, `_with_exactly_5_*`,
  `Ratio_doubles_*`, `Ratio_caps_at_500000_*`.

## Test plan

- Two casts (5 vs 6 spheres), fixed swing, assert the ratio doubles for >5.

## Notes

- Small once the sphere-count-at-cast is threaded; the blocker is the plumbing of
  the pre-consume count, not the formula.

## History

- 2026-06-02 · Applied the renewal Asura ×2-when->5-spheres ratio bump. CastendDamageId
  reads PlayerEntity.SpiritBall (the pre-cast count — spirit balls aren't consumed yet,
  SKILL-19) and threads `spheres>5` as miscflag bit 1 through the weapon damage pipeline;
  the new miscflag-aware CalculateSkillRatio doubles `100+700+sp*10` before the 500000 cap.
  Combat13AsuraSphereTests (4); suite 3740 green. Filed SKILL-19 (spirit-ball requirement
  consumption + Asura delspiritball). Also reworded a stale TODO comment to cite SKILL-18.
