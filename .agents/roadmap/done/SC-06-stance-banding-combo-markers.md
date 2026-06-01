# SC-06 — Star Emperor stances, Royal Guard Banding/Inspiration, and combo markers: real formulas

> **Epic:** Status parity hardening · **Status:** ✅ Done (2026-06-01) · **Size:** M · **Player-visible:** yes
> **Depends on:** SC-01 (de-shadow) · **Blocks:** none

## Problem

The Star Emperor stance buffs and several Royal Guard / combo SCs have generator-style `+Val1`
OnStart bodies that mutate the wrong field by the wrong amount. rAthena computes a fixed `Val2`/
`Val3` per stance (ATK%, MaxHP%, ASPD, all-stat) and the combat/stat path reads those Vals — but
the C# bodies add `+Val1` directly to a single stat instead.

Examples (live runtime bodies):
- **Sunstance** (`StatusEffectRegistry.cs:2980`): `Stats.Batk += Val1`. rAthena: `val2 = 2 + val1`
  (ATK% increase), `tick = INFINITE_TICK`.
- **Starstance** (2916): `+Val1 AspdRate`. rAthena: `val2 = 4 + 2*val1` (ASPD increase).
- **Inspiration** (2590): `+Val1` to Batk + six base stats. rAthena: `val2 = 40*val1` (ATK/MATK),
  `val3 = 6*val1` (all-stat bonus), `val4 = tick/5000`, and `status_change_clear_buffs(SCCB_DEBUFFS)`
  on start (removes debuffs).
- **Banding** (2342): `+Val1 Def`. rAthena: `val2 = skill_banding_count(sd)` (number of banded RG
  party members), 5 s tick; the Def/Atk aggregate scales with the count, not Val1.

By contrast, `Lunarstance` (7113) and `Universestance` (7170) live in
`RegisterWave61BespokeGeneratorOverrides` and may already be correct — verify against rAthena and
align all four stances to the same pattern.

## Verified rAthena formulas (status.cpp init arms ~11798-12350)

| SC | rAthena | Note |
|---|---|---|
| `SC_SUNSTANCE` (12333) | `val2 = 2 + val1` | ATK% increase, INFINITE_TICK |
| `SC_LUNARSTANCE` (12337) | `val2 = 2 + val1` | MaxHP% increase, INFINITE_TICK |
| `SC_STARSTANCE` (12341) | `val2 = 4 + 2*val1` | ASPD increase, INFINITE_TICK |
| `SC_UNIVERSESTANCE` (12350) | `val2 = 2 + val1` | All-stat increase, INFINITE_TICK |
| `SC_BANDING` (11798) | `val2 = skill_banding_count(sd)` | banded member count; 5 s tick |
| `SC_INSPIRATION` (11806) | `val2 = 40*val1; val3 = 6*val1; val4 = tick/5000` | ATK/MATK + all-stat; clears debuffs |
| `SC_NEN` | (read arm) | STR/INT marker; verify |

## Current state (C#)

- `Map.Server/Status/StatusEffectRegistry.cs:2980` Sunstance, `2916` Starstance, `2590` Inspiration,
  `2342` Banding — all in `RegisterWave32Val2Val3Formulas` (ctor line 1002 — these win over the
  wave 5b PresenceMarkers at 4650/4651/4707/4692; see SC-01).
- `7113` Lunarstance, `7170` Universestance — in `RegisterWave61BespokeGeneratorOverrides`
  (ctor line 1017, wins).
- `2694` Nen (Wave32) wins over `4810` Nen (PresenceMarker).
- `StatusCalcFlagDefaults`: `Sunstance`→Batk, `Starstance`→AspdRate, `Inspiration`→Batk+6 stats+Hit+
  MaxHp, `Banding`→Def, `Lunarstance`→MaxHp, `Universestance`→6 stats, `Nen`→Str+IntStat — these
  drive the generator default but are overridden by the explicit bodies; the bodies themselves are
  the bug.
- No consumer reads `Banding.Val2` as a member count or `Inspiration.Val2/Val3` as ATK/all-stat —
  the bodies apply the wrong thing directly.

## rAthena reference (source of truth)

- `rathena/src/map/status.cpp` cases above. ATK%/all-stat application is in `status_calc_pc` /
  `status_calc_*` reading `val2`/`val3` (the stance Vals feed % multipliers, not flat adds).
- `skill_banding_count()` (skill.cpp) — counts RG party members within Banding range; Banding's Def/
  Atk bonus scales with this count (the SC stores the count in val2 and the aggregate read applies
  the per-member bonus).
- Inspiration clears debuffs on start (`status_change_clear_buffs(bl, SCCB_DEBUFFS)`).

## Scope — every sub-system that must be touched

- [x] **Stances (Sun/Lunar/Star/Universe)**: ✅ Sunstance fixed to `Val2 = 2+Val1` applied as ATK%
      (Batk + WatkMin/Max, recompute-revert), Starstance fixed to `Val2 = 4+2*Val1` → AspdRate.
      Lunarstance (MaxHP%) + Universestance (flat all-stat +Val2) were already correct (Wave61);
      verified against rAthena consumers (status.cpp:7089 / 8304 / 3192 / 6576).
- [x] **Banding**: ✅ stores `Val2 = best-effort count (1)`; no faked +Val1 Def. The real
      `skill_banding_count` + Def/Atk aggregate (needs the RG party-banding system) ➡️ **SC-17**.
- [x] **Inspiration**: ✅ `Val2 = 40*Val1` (Batk + Matk), `Val3 = 6*Val1` (flat all-stat),
      `Val4` reserved; applied ATK/MATK + all-stat + MaxHp (+4*Val1, status.cpp:3170). The on-start
      `ClearBuffs(SCCB_DEBUFFS)` + the 5 s drain tick ➡️ **SC-17** (need the SC-service callback the
      OnStart hook lacks).
- [x] **Nen**: ✅ verified — rAthena SC_NEN consumer is `str += val1` / `int_ += val1`
      (status.cpp:6540/6749); the live `+Val1 STR/INT` body is correct. Auto-revive gate
      (DamageService) untouched.
- [x] Duplicate registrations: the wave5b PresenceMarker dups were already collapsed by SC-01; no
      live Nen/stance duplicate remains.
- [ ] Stance % via the stat-pipeline re-fold: the imperative OnStart applies the % at apply-time;
      the proper `status_calc_pc` re-fold reading `Val2` belongs to **COMBAT-10** (SC stat re-fold).

## Done criteria

- ✅ Sunstance Val1=5 → Val2=7 (ATK% on Batk+Watk); Starstance → Val2=14 (ASPD); Lunarstance →
  Val2=7 (MaxHP%); Universestance → Val2=7 (flat all-stat). Pinned in `SC06StanceFormulaTests`.
- ✅ Inspiration Val1=5 → Val2=200, Val3=30; ATK/MATK +200, all-stat +30, MaxHp +20. *(Debuff-clear
  on start ➡️ **SC-17**.)*
- ✅ Banding Val2 = best-effort banded count (no +Val1 Def). *(Real count + Def/Atk aggregate ➡️ **SC-17**.)*
- ✅ DamageService Nen auto-revive gate still works (untouched).
- ✅ `StatusEffectCompletenessTests` + generator-count green; no stance produces a raw `+Val1` add.

## Test plan

- `StanceFormulaTests`: parametrized Val1 for all four stances; assert Val2 and the applied %.
- `InspirationTests`: Val2/Val3/Val4, ATK/MATK + all-stat, debuff-clear-on-start (apply a debuff
  first, assert it's gone).
- `BandingTests`: stub a party with N banded members; assert Val2 == N and the scaled Def/Atk.
- Regression: `DamageServiceTests` (Nen gate), `StatusEffectCompletenessTests`.

## Notes / gotchas

- Stance Vals are **percentages**, not flat stat points — applying `Val2` as a flat add to Batk
  (the current Sunstance bug) is wrong even after fixing the Val2 value. Apply as a % of the base.
- INFINITE_TICK stances persist until the player toggles stance — ensure the SC isn't swept by
  RemoveOnLogout incorrectly (check `ScfFlag`).
- `skill_banding_count` depends on party + range; if the C# RG banding system isn't ported, store a
  best-effort count and file the exact-count wiring as a follow-up rather than faking +Val1.

## History

- 2026-06-01 · Fixed the stance/RG magnitudes from generator +Val1 to rAthena formulas. Sunstance
  → Val2=2+Val1 ATK% (Batk+Watk, recompute-revert); Starstance → Val2=4+2*Val1 ASPD; Inspiration →
  Val2=40*Val1 (ATK/MATK), Val3=6*Val1 (all-stat), MaxHp +4*Val1; Banding → best-effort count
  Val2=1 (no faked +Val1 Def, removed from StatusCalcFlagDefaults). Verified Lunarstance/
  Universestance already correct (Wave61) and Nen (+Val1 STR/INT, status.cpp:6540). SC06Stance
  FormulaTests (7); completeness + generator + full suite 3718 green. Filed SC-17 (Inspiration
  debuff-clear + drain tick, Banding real party-count + Def/Atk aggregate). Stance %-via-CalcPc
  re-fold noted as COMBAT-10.
