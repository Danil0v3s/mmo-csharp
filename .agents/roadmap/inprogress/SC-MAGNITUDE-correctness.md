# SC-MAGNITUDE — SC magnitudes correct (CalcFlags mis-map + generator-default review)

> **Epic:** status · **Status:** 🚧 In progress · **Size:** L · **Player-visible:** yes
> **Depends on:** none · **Unlocks:** SC-FAMILIES

## The deliverable

> Status changes apply their rAthena-exact magnitudes — no phantom "+Val1 to all six stats"
> mis-mappings, no linear-wrong generator defaults, complete element-endow SCs.

## What this absorbs (archive)

- `_archive/todo/status/SC-10` — triage remaining `CalcFlags: All` all-six-stat mis-mappings (~35 SCs).
- `_archive/todo/status/SC-11` — complete element-endow SCs (Aspersio/Shadow/Ghost/Enchantarms + magic).
- `_archive/todo/status/SC-18` — convert linear-wrong-magnitude generator-default SCs (a+b·Val1).
- `_archive/todo/status/SC-19` — bespoke/not-a-stat generator-default SCs (Jointbeat bitmask, tick drains, SC chains).
- `_archive/todo/status/SC-20` — bulk-triage the remaining generator-default SCs.

## rAthena reference

- `rathena/src/map/status.cpp` — `status_calc_*` per-SC arms (the real Val2/Val3 magnitudes);
  the `SCB_*` calc-flag mapping. The archived SC-07 built the `GeneratedStatModDefaultTypes`
  worklist enumeration + the `GeneratorDefaultAuditTests` guard.

## Scope

- [x] `CalcFlags: All` → 6-base-stat mis-mappings — already handled (SC-02/SC-10, guarded by
      `SC02CalcFlagAllTests`); verified at HEAD (weapon-endow + MATK%/resist/random-ring apply real effect).
- [x] **Worklist accounting fix (turn 1)**: the generator-default worklist *over-reported*. The generator
      (`RegisterDefaultsForMissingTypes`, ctor line 1061) lists an SC; the post-generator override waves
      (Wave 32/60/61, lines 1071-1086) replace its handler with the real magnitude **but never removed the
      worklist entry** — so ~91 already-converted SCs (Fortune/Whistle/Humming/Dontforgetme/Truesight/…)
      were still listed as "default". Added `_synthesizedHandlers` capture + `PruneOverriddenGeneratedTypes()`
      (end of ctor): the worklist drops any SC whose handler an override replaced. **236 → 145** = the
      genuinely-still-default review remainder.
- [~] Convert the remaining generator-default SCs to their real magnitudes. The clean well-known-formula
      SCs are already done; the **145 genuine remainder** is the hard/bespoke tail (Gospel/Basilica/Jointbeat/
      Dancing, Taekwon stances Ske/Swoo/Ska/Fusion, the SC_INC* script family, merc bonuses, …).
      **→ future turns** (per-SC rAthena-magnitude conversion).

## Done criteria

- Each converted SC applies the rAthena magnitude; the `GeneratorDefaultAuditTests` worklist shrinks to
  the genuinely-default set (turn 1: 236 → 145, now accurate); no SC silently buffs all six stats that
  shouldn't (✅ SC-02). **The 145 genuine remainder still needs per-SC conversion across future turns.**

## Test plan

- Extend the archived SC-10/11/18/19/20 per-SC formula tests; the completeness/audit guards stay green.

## Progress log (multi-turn)

- **2026-06-04 (turn 1)** — Fixed the worklist accounting: the generator-default worklist was
  over-reporting by ~91 SCs (the post-generator Wave 32/60/61 overrides fixed behaviour but left the
  worklist entry). New `_synthesizedHandlers` capture + `PruneOverriddenGeneratedTypes()` make
  `GeneratedStatModDefaultTypes` reflect only the SCs still served by the generic `+Val1` synthesis
  (236 → 145). Confirmed the simple/well-known-formula SCs (Fortune/Whistle/Humming/Dontforgetme/Truesight/
  Fleet/Spearquicken/…) are already converted; the 145 remainder is the hard/bespoke tail. 6 prune-guard
  tests (`SC07GeneratorAuditTests.OverriddenSc_IsPrunedFromGeneratorDefaultWorklist`); full suite 4561 pass
  (1 = standing replay-fixture). The loop resumes this card to convert the 145 remainder per-SC.
- **2026-06-04 (turn 2)** — Per-SC conversion + an audit of the 145 remainder. Converted **SC_GUARD_STANCE**
  (IG_GUARD_STANCE): Val2 = 50+50·Val1 (DEF↑), Val3 = 50·Val1 (Watk↓) — a real-stat handler replacing the
  generator's wrong +Val1-to-both default (status.cpp:12445; status.yml CalcFlags Watk+Def). Worklist
  145 → 144. 2 tests (`SC02CalcFlagAllTests.GuardStance_*`); full suite 4563 pass (1 = standing replay-fixture).
  **Audit of the remaining 144:** only ~29 have a `status_change_start` val2 arm at all; of those, most are
  (a) tick/state machines (Gospel/Basilica/Run/Dancing/Insignia/Rebound), (b) blocked on a missing C#
  field — there is **no real MoveSpeed% stat** (the codebase uses an AspdRate proxy), so the speed SCs
  (GnCartboost/Catnippowder/Arclousedash/…) can't be faithfully converted, or (c) use 4th-job stats
  (P.ATK/S.MATK) not yet modeled. The rest of the 144 are **correctly** served by the +Val1 default (their
  skill sets Val1 directly). So the genuinely-clean-convertible subset is small and largely exhausted;
  the remaining tail needs deliberate per-SC work + some infrastructure (a MoveSpeed% field) — a long,
  human-paced grind rather than a fast bulk sweep.
- **2026-06-04 (turn 3)** — Converted **SC_HISS** (SU_HISS): flat +50 Perfect Dodge (Flee2), replacing
  the wrong +Val1 default (status.cpp:12301; status.yml CalcFlag Flee2). Worklist 144 → 143. 2 tests
  (`SC02CalcFlagAllTests.Hiss_*`); full suite 4565 pass (1 = standing replay-fixture). **Clean-convertible
  subset now exhausted:** Hiss was the last simple single-real-stat SC in the val2-arm subset. The
  movement-speed cluster (GnCartboost/Catnippowder/Arclousedash/Walkspeed/DoramWalkspeed) is blocked on a
  missing MoveSpeed% field ➡️ filed **SC-MOVESPEED-FIELD**; the rest of the 143 are correctly-default,
  tick/state machines, or use unmodeled 4th-job trait stats (P.ATK/S.MATK/Spl). **Honest status:** the
  structural work + every clean conversion are done; closing the worklist further requires the MoveSpeed%
  infra + 4th-job-stat modeling (separate tickets) — SC-MAGNITUDE has reached the limit of what an
  autonomous sweep can faithfully convert.
- **2026-06-04 (turn 4)** — Corrected the turn-3 MoveSpeed premise + fixed SC_GN_CARTBOOST. The movement
  speed system is **not** missing — `StatusCalcService.ComputeScSpeed` (status_calc_speed) already
  implements the full SC speed_rate accumulator and CalcPc overwrites `BattleStats.Speed` with it; it
  reads each speed SC's real magnitude directly (GnCartboost.Val2@718, Catnippowder.Val3, Arclousedash.Val3,
  DoramWalkspeed.Val1@723, Walkspeed.Val1@739). So the `Speed`-CalcFlag generator stat-mod is redundant
  (immediately overwritten), not blocked-on-infra → **deleted the false-premise SC-MOVESPEED-FIELD ticket.**
  Fixed **SC_GN_CARTBOOST**: its OnStart now sets Val2 = 50/75/100 by level so ComputeScSpeed has a value
  (the +Val1 generator default never set Val2, so the speed-up read 0 — the bonus was broken). Worklist
  143 → 142. (DoramWalkspeed/Walkspeed stay generator-default: their +Val1-to-Speed is harmlessly
  overwritten, and the Register no-downgrade-to-NoOp guard blocks converting them to presence-only.)
  3 tests + a completeness-allowlist entry; full suite 4567 pass (1 = standing replay-fixture).

## Notes

- Element-endow SCs set the weapon element the combat resolver reads — not an all-stat buff
  (archive SC-02). Deferred (after gameplay).
