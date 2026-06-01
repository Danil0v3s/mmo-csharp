# SC-20 — Bulk-triage the remaining generator-default SCs (classify + convert non-exact)

> **Epic:** Status parity hardening · **Status:** ❌ Not started · **Size:** XL · **Player-visible:** yes
> **Depends on:** SC-07 (enumeration), SC-18, SC-19 · **Split from:** SC-07

## Problem

SC-07 built the generator-default enumeration and converted Fear; SC-18/SC-19 handle the
linear-wrong and bespoke classes. This ticket walks the FULL remaining
`StatusEffectRegistry.GeneratedStatModDefaultTypes` set and classifies every entry not already
converted as **linear-exact** (rAthena `+val1` — leave, document) or **needs-conversion** (hand off
to SC-18/SC-19 patterns), so no SC silently applies the wrong magnitude.

## Scope

- [ ] Produce the authoritative triage table covering EVERY remaining `GeneratedStatModDefaultTypes`
      entry, citing each SC's `status.cpp:line` and its class (linear-exact / linear-wrong / bespoke
      / not-a-stat / sign-wrong).
- [ ] Convert every non-exact one (using the SC-18 linear / SC-19 bespoke patterns).
- [ ] Document the confirmed linear-exact ones (they stay on the generator body legitimately).
- [ ] Tighten the `GeneratorDefaultAuditTests` bound to the post-conversion count so future CalcFlag
      SCs are reviewed.

## Done criteria

- Every generator-default SC is classified + (if non-exact) converted; the audit guard reflects the
  reduced worklist; `StatusEffectCompletenessTests` green.

## Test plan

- The triage table (in-PR) + per-converted-SC formula tests; the audit guard as the standing check.

## Notes

- This is the long-tail completion of SC-07's XL triage; batch by class. The enumeration
  (`GeneratedStatModDefaultTypes`) is the canonical worklist — nothing is missed because the audit
  test fails if the set grows unreviewed.
