# SC-17 — Inspiration debuff-clear + drain tick; Banding real party-count + Def/Atk aggregate

> **Epic:** Status parity hardening · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** SC-06 (Inspiration/Banding Val2/Val3 set) · **Blocks:** none · **Split from:** SC-06

## Problem

SC-06 set the correct Inspiration (Val2=40·Val1 ATK/MATK, Val3=6·Val1 all-stat) and Banding
(Val2 = best-effort count) magnitudes, but two infrastructure-dependent effects were deferred:

1. **Inspiration on-start `status_change_clear_buffs(SCCB_DEBUFFS)`** — rAthena removes all debuffs
   when Inspiration starts. The C# `OnStart(target, sc, source)` hook has no `IStatusChangeService`
   reference, so it can't call `ClearBuffs`. Also the **5 s drain tick** (`val4 = tick/5000`, an
   HP/SP cost per interval) is not wired.
2. **Banding real party-count** — rAthena `skill_banding_count(sd)` counts RG party members within
   Banding range; `Val2` = that count, and the Def/Atk aggregate scales with it on a 5 s tick.
   SC-06 stores a best-effort count of 1 and applies no Def/Atk (no faked +Val1). The real count +
   aggregate need the RG banding party system.

## Current state (C#)

- `Map.Server/Status/StatusEffectRegistry.cs` — Inspiration OnStart sets Val2/Val3 + applies the
  ATK/MATK/all-stat/MaxHp stat deltas; comment marks the debuff-clear + drain tick `→ SC-17`.
  Banding OnStart sets `Val2 = 1` (best-effort); comment marks the real count + aggregate `→ SC-17`.
- The `OnStart`/`OnEnd` handler signatures don't receive the SC service; `OnPeriodic` exists for
  tick effects (used by Poison DoT etc.).

## rAthena reference (source of truth)

- `status.cpp:11806` SC_INSPIRATION: `status_change_clear_buffs(bl, SCCB_DEBUFFS)` on start;
  `val4 = tick/5000`, `tick_time = 5000` (periodic HP/SP drain).
- `skill.cpp skill_banding_count` — counts RG party members in range; `status.cpp:11798` SC_BANDING
  `val2 = skill_banding_count(sd)`, 5 s tick re-evaluates; the Def/Atk aggregate read scales by val2.

## Scope — every sub-system that must be touched

- [ ] Inspiration debuff-clear: clear `SCCB_DEBUFFS` on start (via a service hook — extend the
      apply path so the SC engine performs the clear after OnStart, or give Inspiration a bespoke
      apply that calls `IStatusChangeService.ClearBuffs(SccbFlag.Debuffs)`).
- [ ] Inspiration 5 s drain tick: wire `OnPeriodic` (HP/SP cost per `val4` interval).
- [ ] Banding real count: compute `skill_banding_count` (RG party members in range) into `Val2` on
      the 5 s tick; apply the Def/Atk aggregate scaled by `Val2`.

## Done criteria

- Casting Inspiration removes the caster's active debuffs on start.
- Inspiration drains HP/SP on its 5 s tick.
- Banding's `Val2` reflects the real banded-member count; Def/Atk scales with it.

## Test plan

- `InspirationDebuffClearTests`: apply a debuff, start Inspiration, assert the debuff is gone.
- `BandingCountTests`: stub a party with N banded RG members in range → Val2 == N + scaled Def/Atk.

## Notes / gotchas

- The OnStart hook lacks the SC service — route the debuff-clear through the engine's apply path
  (which has it) rather than the lambda.
- Banding depends on the RG party-banding system; if absent, this ticket also ports the minimal
  count helper.
