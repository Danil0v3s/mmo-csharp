# COMBAT-13 — Asura Strike renewal ×2 when cast with >5 spirit spheres

> **Epic:** Combat parity · **Status:** ❌ Not started · **Size:** S · **Player-visible:** yes
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

- [ ] Capture the caster's spirit-sphere count at Asura cast-start (before the
      requirement consumes them) and thread it to `CastendDamageId` (via the
      miscflag-aware ratio overload, or a per-cast field).
- [ ] In `AsuraStrike` (the miscflag-aware `CalculateSkillRatio` overload), if the
      captured count > 5, `ratio *= 2` **before** the 500000 cap.
- [ ] Confirm the consume-timing so the count read is the pre-consume value.

## Done criteria

- Asura cast with 6+ spheres deals ~2× the ratio of an otherwise identical cast
  with ≤5 spheres (both still capped at 500000% and plus the `250+150*lv` constant).

## Test plan

- Two casts (5 vs 6 spheres), fixed swing, assert the ratio doubles for >5.

## Notes

- Small once the sphere-count-at-cast is threaded; the blocker is the plumbing of
  the pre-consume count, not the formula.
