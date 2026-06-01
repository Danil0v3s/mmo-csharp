# SC-06 — Star Emperor stances, Royal Guard Banding/Inspiration, and combo markers: real formulas

> **Epic:** Status parity hardening · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
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

- [ ] **Stances (Sun/Lunar/Star/Universe)**: replace the `+Val1` bodies with `Val2 = 2+Val1`
      (Sun/Lunar/Universe) / `Val2 = 4+2*Val1` (Star), apply as the correct derived effect — ATK%
      (Sun), MaxHP% (Lunar), ASPD (Star), all-stat% (Universe). Use INFINITE_TICK semantics
      (permanent until toggled). Align all four to one helper.
- [ ] **Banding**: store `Val2 = banded member count` (thread `skill_banding_count` — if the C# RG
      banding aggregator exists, call it; else compute from party members in range). Apply the
      Def/Atk aggregate scaled by count, on the 5 s tick. Confirm the RG party-share consumer reads
      Val2.
- [ ] **Inspiration**: set `Val2 = 40*Val1` (ATK/MATK), `Val3 = 6*Val1` (all-stat), `Val4 = tick/5000`;
      apply ATK/MATK + all-stat bonus; call the debuff-clear (`ClearBuffs(SCCB_DEBUFFS)` equivalent)
      on start. Wire the 5 s tick HP/SP drain if rAthena has one.
- [ ] **Nen**: verify the live body (2694) matches the rAthena STR/INT marker + auto-revive gate
      (DamageService reads `Nen` presence at `DamageService.cs:227`); fix magnitude if wrong.
- [ ] Remove the now-dead wave 5b PresenceMarker re-registrations for these (covered by SC-01) and
      delete the duplicate Nen registration (4810).
- [ ] If a stance's % effect needs a derived-stat read that the C# stat pipeline lacks, add it to
      `StatusCalcService` (read `Val2` and apply the % to the right base).

## Done criteria

- Sunstance Val1=5 → Val2=7 (ATK% +7); Starstance Val1=5 → Val2=14 (ASPD); Lunarstance Val1=5 →
  Val2=7 (MaxHP% +7); Universestance Val1=5 → Val2=7 (all-stat% +7).
- Inspiration Val1=5 → Val2=200, Val3=30; ATK/MATK +200, all-stat +30; debuffs cleared on start.
- Banding Val2 = number of banded RG members; Def/Atk aggregate scales with count, not Val1.
- DamageService Nen auto-revive gate still works.
- `StatusEffectCompletenessTests` green; no stance produces a raw `+Val1` single-stat add.

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
