# SC-07 — Audit the generator-default SCs for non-linear / bespoke magnitudes

> **Epic:** Status parity hardening · **Status:** ✅ Done (2026-06-01) — slice; remainder → SC-18/19/20 · **Size:** XL · **Player-visible:** yes
> **Depends on:** SC-02 (CalcStatField extensions land first) · **Blocks:** none

## Problem

`StatusCalcFlagDefaults` maps **356** SCs to one or more `CalcStatField`s, and
`RegisterDefaultsForMissingTypes` synthesizes a generic OnStart that adds **`Val1`** to each
listed field. For the subset whose rAthena magnitude is `+val1` to that stat (e.g. IncreaseAGI,
Blessing's three stats) this is exact. But ~159 of these SCs use a **non-linear or bespoke**
magnitude in rAthena (`a + b*val1`, `(sd?5:10)*val1`, a percentage, a bitmask, or a value that
isn't a stat at all). For those the generator moves the right *direction* but the wrong *amount*,
or mutates a field that should be a Val2-driven combat read.

This is a triage ticket: walk every generator-default SC, classify it **linear-exact** (leave),
**linear-wrong-magnitude** (give a `a + b*val1` body), or **bespoke / not-a-stat** (give an
explicit body or move to a combat-side Val read), and convert each non-exact one.

## Verified examples (rAthena status.cpp — generator is wrong)

| SC | C# generator (`+Val1`) | rAthena actual | Class |
|---|---|---|---|
| `Quagmire` (11281) | `+Val1 Agi/Dex/AspdRate` | `val2 = (sd?5:10)*val1` Agi/Dex **decrease** | linear-wrong + sign |
| `Adoramus` (10481) | `+Val1 Agi/AspdRate` | sub-effect: `sc_start(SC_BLIND, 1000, val1)` + Decrease AGI | bespoke (chains Blind) |
| `Fear` (9535) | `+Val1 Flee/Hit` | Flee/Hit **reduction**; `sc_def = int*20 + lv*20 + luk*10` | sign + bespoke def |
| `Jointbeat` (9511) | `+Val1 Batk/Def2/AspdRate` | `val2` is a **BREAK_* bitmask** (broken body part), not a stat; per-part penalties + Bleeding on BREAK_NECK | not-a-stat |
| `OdinsPower` | `+Val1 Batk/Mdef/Def` | verify init arm (likely fixed Atk/Mdef, not +Val1) | verify |
| `Stomachache` (11948) | `+Val1` six stats | `val2 = 8` (SP consume/tick), `val4 = tick/10000`; the stat penalty is fixed | bespoke (tick SP drain) |
| `Marshofabyss` | `+Val1 Agi/Dex/AspdRate` | speed/flee/agi/dex **reduction** % (verify init arm) | sign + % |
| `AngriffsModus` (12015) | `+Val1 Batk/Def/Flee/MaxHp` | `val2 = 50+20*val1` Atk, `val3 = 25+10*val1` Flee **reduction**, `val4 = tick/1000` HP/SP drain | linear-wrong + tick |
| `OveredBoost` (12030) | `+Val1 Flee/AspdRate/Def` | `val2 = 400+40*val1` flee, `val3 = 180+2*val1` aspd, `val4 = 50` def **reduc %** | linear-wrong |

## Current state (C#)

- `Map.Server/Status/StatusCalcFlagDefaults.cs:39-397` — the 356-entry `_table`.
- `Map.Server/Status/StatusEffectRegistry.cs:6238-6324` `RegisterDefaultsForMissingTypes()` —
  synthesizes the `+Val1` body for every CalcFlag SC not explicitly registered.
- `Map.Server/Status/StatusEffectRegistry.cs:6326-6427` `ApplyCalcFlagDelta` — the actual
  `field += Val1*sign` loop. Its own xmldoc (6335-6347) acknowledges the magnitude is approximate
  for non-linear SCs and says "those need an explicit Register()".
- The 159 non-linear count = (356 CalcFlag SCs) − (~167 bespoke OnStart already explicit) −
  (~30 presence-marker-despite-CalcFlag). The exact set is whatever `RegisterDefaultsForMissingTypes`
  still serves at runtime (a generator body, not an explicit one) AND whose rAthena formula isn't a
  plain `+val1`.

## rAthena reference (source of truth)

- Each SC's init arm in `rathena/src/map/status.cpp` `status_change_start` (~10400-12400) and the
  `sc_def`/`tick_def` arms (~9500-9650) for debuff-resist scaling. Search `case SC_<NAME>:`.
- The stat-application side in `status_calc_pc` / `status_calc_bl_main` reads `val2`/`val3` for the
  bespoke ones; `CalcFlags` in status.yml only says *which derived stats to recompute*, not the
  magnitude.

## Scope — every sub-system that must be touched

- [x] **Enumerate the runtime generator-default set**: ✅ the generator now records every SC it
      serves the synthesized `+Val1` body to → `StatusEffectRegistry.GeneratedStatModDefaultTypes`
      (the authoritative runtime worklist). `GeneratorDefaultAuditTests` reads + guards it.
- [x] **Sign-wrong debuffs**: ✅ **Fear** converted to a fixed 20% Hit/Flee *reduction*
      (status.cpp:7340/7448). Verified **Quagmire** (−5·Val1 Agi/Dex) and **Marshofabyss** (−3·Val1
      Agi/Dex) are ALREADY explicit + correct (audit was stale).
- [ ] **Classify each** remaining generator-default SC + triage table ➡️ **SC-20** (uses the new
      enumeration as the worklist).
- [ ] **Linear-wrong-magnitude** (AngriffsModus 50+20·Val1, OveredBoost 400+40·Val1, Gatlingfever…)
      ➡️ **SC-18**.
- [ ] **Bespoke / not-a-stat** (Jointbeat bitmask, Stomachache tick drain, Adoramus Blind-chain,
      Fear's SC_ANKLE chain) ➡️ **SC-19**.
- [ ] **Debuff-resist scaling** (`sc_def`/`tick_def2`) — the landing resist is already in
      `ScDefTable` (SKILL-01); the remaining arms ride **SC-08** / SKILL-15.
- [ ] **Generator override / re-run** — the enumeration makes converted SCs drop out automatically
      (explicit body wins); the per-SC override table cleanup rides **SC-20**.

## Done criteria

- ✅ The generator-default set is enumerated + guarded (`GeneratedStatModDefaultTypes` +
  `GeneratorDefaultAuditTests`) — nothing is silently un-reviewed (the guard fails if the set grows).
- ✅ **Fear** converted (20% Hit/Flee reduction); Quagmire/Marshofabyss verified already-correct.
- ➡️ The remaining ~150 generator-default SCs are catalogued in the enumeration and assigned by
  class to **SC-18** (linear-wrong), **SC-19** (bespoke), **SC-20** (bulk triage) — each citing the
  worklist + rAthena lines.
- ✅ `StatusEffectCompletenessTests` + `StatusEffectGeneratorTests` green.

## Test plan

- `GeneratorDefaultAuditTests`: the enumeration test that lists generator-default SCs (kept as a
  guard so new CalcFlag SCs are reviewed, not silently +Val1'd).
- Per-converted-SC formula tests (extend `Wave97Batch3FormulaTests` / new
  `BespokeMagnitudeTests`): pin Val2/Val3/Val4 and the applied stat delta + sign for the named nine
  plus every other converted SC.
- Regression: full `StatusEffectCompletenessTests`, `StatusEffectGeneratorTests`,
  `StatusCalcServiceTests`.

## Notes / gotchas

- This is the largest ticket; split the conversion into batches by class (sign-wrong debuffs first —
  they're the most player-visible), but the triage table must cover all ~159 in one pass so nothing
  is missed.
- Many "All-stat" CalcFlags overlap with SC-02's `CalcFlags: All` work — do SC-02 first so the
  RecalcOnly reclassification is available, then this ticket only handles the genuinely-non-linear
  remainder.
- `(sd?5:10)*val1` means the magnitude differs for players vs mobs — thread the entity type
  (`target is PlayerEntity`) into the body.
- Tick-driven drains (Stomachache, AngriffsModus, OveredBoost val4) need an OnPeriodic body, not
  just an OnStart — wire `PeriodMs` + the SP/HP charge.

## History

- 2026-06-01 · XL triage — shipped the enumeration infrastructure + first conversion. The generator
  now records every synthesized `+Val1` stat-mod SC into `StatusEffectRegistry.
  GeneratedStatModDefaultTypes` (the authoritative ~159 worklist); `GeneratorDefaultAuditTests` (3)
  guards it. Converted **Fear** to its rAthena fixed 20% Hit/Flee *reduction* (status.cpp:7340/7448)
  — verified Quagmire/Marshofabyss already correct (stale audit). 3721 green. Remaining set
  decomposed by class: SC-18 (linear-wrong a+b·Val1 + tick drains), SC-19 (bespoke/not-a-stat:
  Jointbeat bitmask, Stomachache drain, Adoramus/Fear SC-chains), SC-20 (bulk-remainder triage +
  tighten the audit bound). Fear's SC_ANKLE chain → SC-19.
