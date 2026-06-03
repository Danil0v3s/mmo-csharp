# SC-19 — Bespoke / not-a-stat generator-default SCs (Jointbeat bitmask, tick drains, SC chains)

> **Epic:** Status parity hardening · **Status:** ❌ Not started · **Size:** L · **Player-visible:** yes
> **Depends on:** SC-07 (enumeration) · **Split from:** SC-07

## Problem

A subset of the generator-default SCs aren't stat mods at all — their rAthena `val2`/`val3` is a
bitmask, a tick SP/HP drain, or a chained SC start. The `+Val1` generator body is meaningless for
them.

## Verified examples

| SC | rAthena | Real mechanic |
|---|---|---|
| `Jointbeat` (status.cpp:9511) | `val2` = BREAK_* bitmask (broken body part) | per-part penalties + Bleeding on BREAK_NECK |
| `Stomachache` (11948) | `val2 = 8` SP consume/tick, `val4 = tick/10000` | periodic SP drain + fixed stat penalty |
| `Adoramus` (10481) | chains `sc_start(SC_BLIND, 1000, val1)` + Decrease AGI | chained SC start |
| `Fear` on-start | `sc_start(... SC_ANKLE ...)` | chained ankle-snare (SC-07 left this) |

## Current state (C#)

- `StatusEffectRegistry.GeneratedStatModDefaultTypes` worklist; these need real bodies, not stat mods.
- Chained-SC / debuff-clear starts need an `IStatusChangeService` callback the `OnStart` lambda
  lacks (same constraint as SC-17) — route through the engine apply path.

## Scope

- [ ] Jointbeat: store the BREAK_* bitmask in val2; apply the per-part penalty; Bleeding on BREAK_NECK.
- [ ] Stomachache + other tick drains: `OnPeriodic` SP/HP drain + the fixed stat penalty.
- [ ] Adoramus / Fear: chain the secondary SC start (Blind / Ankle) via the engine apply path.

## Done criteria

- Jointbeat stores a bitmask (not +Val1 stats); Stomachache drains SP on tick; Adoramus chains
  Blind; Fear chains Ankle.

## Test plan

- Per-SC mechanic tests (bitmask storage, tick drain, chained-SC presence).
