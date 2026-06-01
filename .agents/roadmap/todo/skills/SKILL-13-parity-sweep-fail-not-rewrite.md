# SKILL-13 — Parity-sweep snapshots must fail, not silently rewrite

> **Epic:** Skills · **Status:** ❌ Not started · **Size:** S · **Player-visible:** no
> **Depends on:** none · **Blocks:** trustworthy green builds for every SKILL-* ticket
> **Filed by:** COMBAT-01 run on 2026-06-01 (was an unticketed "honest caveat").

## Problem

`FamilyParitySweepBase` snapshot-diffs each skill's recorded trace against an
on-disk baseline `.json`. The harness **auto-records the baseline on mismatch**
(it writes the new trace instead of failing), so a skill-behavior change can make
the baseline silently drift and the suite still reports green. During the COMBAT-01
run, a full-suite pass regenerated ~85 baselines that had drifted from
already-committed skill code — with zero test failures. That means "3553/3553
green" does not actually guarantee skill traces match their committed baselines,
which undermines every SKILL-* ticket's Done-criteria ("baseline matches").

## Current state (C#)

- `Map.Server.Tests/Skills/Parity/FamilyParitySweep.cs` — `RunOne` →
  `ex.AssertMatchesBaseline(typeName, lv)`.
- `Map.Server.Tests/Skills/Parity/SkillExerciser.cs` (or wherever
  `AssertMatchesBaseline` lives) — verify the record-on-mismatch behavior: it
  appears to write `<Type>_<lv>.json` when the trace differs rather than throwing.
- `AssertMatchesRathena` already throws on over-emission (good); the *snapshot*
  side is the lenient one.

## rAthena reference

N/A — this is test-harness integrity, not a parity port. The acceptance bar is
"a committed baseline that no longer matches current code is a RED test," matching
how rAthena ports are supposed to be pinned.

## Scope — every sub-system that must be touched

- [ ] Confirm exactly where the snapshot is written and gate it behind an explicit
      opt-in env var (e.g. `RECORD_SKILL_BASELINES=1`), default OFF.
- [ ] With the flag OFF (CI/local default): a missing baseline → fail with a clear
      "run with RECORD_SKILL_BASELINES=1 to create" message; a **mismatched**
      baseline → fail with a trace diff. Never write.
- [ ] With the flag ON: (re)write the baseline (the explicit regeneration path).
- [ ] Add a guard test asserting the default path does not write to
      `Skills/Baselines/` (e.g. a known-divergent fixture fails rather than rewrites).
- [ ] Document the regeneration workflow in the skills parity README / TEMPLATE.

## Done criteria

- Running the suite with no env var on a deliberately-changed skill **fails** with a
  diff (does not rewrite the `.json`).
- `RECORD_SKILL_BASELINES=1 dotnet test ...` regenerates baselines on purpose.
- A regression test pins "default = fail, not rewrite".
- After this lands, re-run the full suite once with the record flag to capture the
  current (already-committed) behavior as the trusted baseline, then commit those.

## Test plan

- Unit/harness test: feed the exerciser a trace that differs from an existing
  baseline with the flag off → expect `XunitException`, and assert the file on disk
  is byte-unchanged.
- With the flag on → assert the file is rewritten.

## Notes / gotchas

- This is why COMBAT-01's own acceptance used explicit `Assert.Equal` tests
  (`Combat01EquipBonusTests`) rather than trusting the sweep.
- The ~85 baselines regenerated during the COMBAT-01 run were committed in their own
  commit (`test: regenerate drifted skill baselines …`); after SKILL-13 lands they
  become the trusted, fail-locked baseline.
