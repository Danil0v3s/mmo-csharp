# COMBAT-71 — Remaining status_calc_aspd SCs (debuff + misc terms)

> **Epic:** combat · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** COMBAT-28, COMBAT-50 (the ComputeScAspd seam) · **Blocks:** none
> **Filed by:** COMBAT-50 — the status_calc_aspd SCs beyond the subset COMBAT-28/50 ported.

## Problem

`ComputeScAspd` now covers the quicken family + Madness/Berserk/potions + Steel Body/Defender/
Gospel/DontForgetMe + Heat Barrel (COMBAT-28) and the SwingDance/IncAspdRate/GatlingFever rate
positives + Gust/Blast/WildStorm/FightingSpirit/SoulShadow/SincereFaith/OveredBoost fix_aspd
terms (COMBAT-50). rAthena `status_calc_aspd` (status.cpp:6180-6230) has more SCs not yet ported:
the **fixed-term debuffs** EnsembleFatigue, Longing, Gravitation, Joint Beat (wrist/knee),
Freezing, HallucinationWalk-postdelay, Paralyse, `SC__BODYPAINT`, `SC__INVISIBILITY` — and any
remaining rate-term entries. Players under these effects currently keep full ASPD.

## Current state (C#)

- `Map.Server/Status/StatusCalcService.cs:ComputeScAspd` — covers the COMBAT-28/50 subset; the SCs
  above are absent.
- Several need `StatusType` members verified/added (EnsembleFatigue, Longing, etc.).

## rAthena reference (source of truth)

- `status.cpp` `status_calc_aspd` fixed branch (~6180-6210): `SC_ENSEMBLEFATIGUE -val2/10`,
  `SC_LONGING -val2/10`, `SC_GRAVITATION -val2/10`, `SC_JOINTBEAT` (BREAK_WRIST −25, BREAK_KNEE −10),
  `SC_FREEZING −15`, `SC_HALLUCINATIONWALK_POSTDELAY −50`, `SC_PARALYSE` (val3==1) `−10`,
  `SC__BODYPAINT −5*val1`, `SC__INVISIBILITY -val2` (verify exact vals against the checkout).

## Scope — every sub-system that must be touched

- [ ] Add the remaining `status_calc_aspd` fixed/rate SC terms to `ComputeScAspd`, adding any
      missing `StatusType` members.
- [ ] Verify each against the `status.cpp` switch arm (vals + sign).

## Done criteria

- Each ported SC moves amotion by the rAthena-exact amount (verify ≥3 representative SCs).
- No `// TODO` / `data-pending` in the touched file.

## Test plan

- `Combat71AspdDebuffScTests`: Freezing / Joint Beat / Body Paint each slow attacks by the
  rAthena value via CalcPc.

## Notes / gotchas

- Keep the existing COMBAT-28/50 terms intact; this is purely additive to `ComputeScAspd`.
