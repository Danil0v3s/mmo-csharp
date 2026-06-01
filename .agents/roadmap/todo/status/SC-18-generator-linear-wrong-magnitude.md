# SC-18 — Convert linear-wrong-magnitude generator-default SCs to a+b*val1 bodies

> **Epic:** Status parity hardening · **Status:** ❌ Not started · **Size:** L · **Player-visible:** yes
> **Depends on:** SC-07 (enumeration + audit guard) · **Split from:** SC-07

## Problem

SC-07 built the generator-default enumeration (`StatusEffectRegistry.GeneratedStatModDefaultTypes`)
and converted the first sign-wrong debuff (Fear). Many SCs still on the `+Val1` generator default
use a **linear-but-wrong** rAthena magnitude (`a + b*val1`) and/or a tick drain. This ticket
converts that class.

## Verified examples (rAthena status.cpp — generator is wrong)

| SC | rAthena | Note |
|---|---|---|
| `AngriffsModus` (status.cpp:12015) | `val2 = 50+20*val1` Atk, `val3 = 25+10*val1` Flee **reduction**, `val4 = tick/1000` HP/SP drain | linear + tick |
| `OveredBoost` (12030) | `val2 = 400+40*val1` flee, `val3 = 180+2*val1` aspd, `val4 = 50` def **reduc %** | linear |
| `Gatlingfever` | `20*val1` (verify field) | linear |
| (others surfaced by the audit set) | per their init arms | linear |

## Current state (C#)

- `StatusEffectRegistry.GeneratedStatModDefaultTypes` — the runtime worklist (read it; filter to
  the linear-wrong subset by checking each SC's rAthena init arm).
- `RegisterDefaultsForMissingTypes` / `ApplyCalcFlagDelta` synthesize the `+Val1` body these need
  to override.

## Scope

- [ ] For each linear-wrong SC in the worklist, add an explicit `Register` body with the
      `a + b*val1` formula and correct sign, citing `status.cpp:line`.
- [ ] Tick-driven drains (AngriffsModus/OveredBoost val4) need an `OnPeriodic` body — wire
      `PeriodMs` + the HP/SP charge.
- [ ] Each converted SC leaves `GeneratedStatModDefaultTypes` (assert in the audit guard).

## Done criteria

- AngriffsModus Val1=5 → Val2=150 Atk, Val3=75 Flee reduction; OveredBoost Val1=5 → Val2=600 flee,
  Val3=190 aspd, Val4=50 def reduc. No linear-wrong SC silently applies +Val1.

## Test plan

- Per-SC formula tests (Val2/Val3/Val4 + applied delta + sign); audit-guard count drops accordingly.
