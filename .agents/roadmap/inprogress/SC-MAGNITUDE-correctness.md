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

## Notes

- Element-endow SCs set the weapon element the combat resolver reads — not an all-stat buff
  (archive SC-02). Deferred (after gameplay).
