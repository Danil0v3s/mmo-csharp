# COMBAT-71 — Remaining status_calc_aspd SCs (debuff + misc terms)

> **Epic:** combat · **Status:** ✅ Done (2026-06-03) · **Size:** M · **Player-visible:** yes
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

- [x] Added the remaining renewal `status_calc_aspd(false)` (rate-term) SCs to `ComputeScAspd`:
      the debuffs **EnsembleFatigue** (−val2/10), **JointBeat** (BREAK_WRIST 0x02 −25 / BREAK_KNEE
      0x04 −10), **Freezing** (−30), **HallucinationWalk-postdelay** (−50), **Paralyse** (val3==1
      −10), **BodyPaint** (−5·val1), **Invisibility** (−val2), **Groomy** (−val2); plus the
      remaining rate entries DanceWithWug/GtChange/GoldeneFerse/StarComfort/WindInsignia/
      IncreaseAgi/Nibelungen(RINGNBL_ASPDRATE +20)/StarStance (+) and GloomyDay/MelonBomb (−). All
      `StatusType` members already existed.
- [x] Verified each against `status.cpp:8006`. **Corrections vs the ticket draft:** FREEZING is
      **−30** (the draft said −15); **LONGING** and **GRAVITATION** are `#else`/`#ifndef RENEWAL`
      (pre-renewal) and correctly **skipped** in renewal; **GROOMY** (−val2) was missing from the
      draft and is now included.

## Done criteria

- Each ported SC moves amotion by the rAthena-exact amount ✅ (Combat71AspdDebuffScTests asserts
  the exact `rateSc` for Freezing −30, JointBeat wrist/knee/both, EnsembleFatigue, BodyPaint,
  Paralyse, HallucinationWalk/Invisibility/Groomy, and the positives + an end-to-end slowdown).
- No `// TODO` / `data-pending` in the touched file ✅.

## Test plan

- `Combat71AspdDebuffScTests`: Freezing / Joint Beat / Body Paint each slow attacks by the
  rAthena value via CalcPc.

## Notes / gotchas

- Keep the existing COMBAT-28/50 terms intact; this is purely additive to `ComputeScAspd`.

## History

- 2026-06-03 · Added the remaining renewal `status_calc_aspd(false)` rate-term SCs to
  `ComputeScAspd` (EnsembleFatigue / JointBeat wrist+knee / Freezing / HallucinationWalk-postdelay
  / Paralyse / BodyPaint / Invisibility / Groomy debuffs + DanceWithWug/GtChange/GoldeneFerse/
  StarComfort/WindInsignia/IncreaseAgi/Nibelungen/StarStance positives + GloomyDay/MelonBomb).
  Verified each against status.cpp:8006 — corrected the draft (FREEZING −30 not −15; LONGING/
  GRAVITATION are pre-renewal → skipped; GROOMY added). Made `ComputeScAspd` internal for exact
  testing. Combat71AspdDebuffScTests (8, exact rateSc values + end-to-end slowdown); Status+Combat
  suite 813 green, full suite 4101 pass (1 fail = pre-existing INFRA-11 replay gate). No follow-ups.
