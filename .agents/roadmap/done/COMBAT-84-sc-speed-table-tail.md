# COMBAT-84 — SC speed-table tail: exotic SCs + freecast / hiding-walk early branches

> **Epic:** Combat parity · **Status:** ✅ Done (2026-06-03) · **Size:** M · **Player-visible:** yes
> **Depends on:** COMBAT-65 · **Blocks:** none
> **Filed by:** COMBAT-65 — it ported the `status_calc_speed` core (two-phase slow/fast
> accumulator + the common movement SCs + Steel Body / Defender overrides + caps). The long
> tail of exotic SCs and the two early-return branches were out of that "S" ticket's scope.

## Problem

`status_calc_speed` (status.cpp:7787) checks ~55 SCs across the slow + fast phases and has two
special early branches. COMBAT-65 wired the common ones (DecreaseAgi/Quagmire/Curse/SlowDown/
DontForgetMe slow; AgiUp/IncreaseAgi/WindWalk/CartBoost/Berserk/Run/FullThrottle fast; Steel
Body/Defender overrides). The remainder is unmodeled, so those SCs do not change move speed.

## Current state (C#)

- `Map.Server/Status/StatusCalcService.cs:ComputeScSpeed` — the core table; the SCs below are
  NOT read, and neither early branch is implemented.

## rAthena reference (source of truth)

- `status.cpp:7787` `status_calc_speed`. Missing pieces:
  - **Freecast / Exceed Break early branch** (`sd->ud.skilltimer != INVALID_TIMER && (SA_FREECAST || LG_EXEEDBREAK)` → `speed_rate = 175 - 5*freecast` / `160 - 10*lv`). Coordinate with COMBAT-70 (freecast cast-state).
  - **Hiding / walk-mode early-return branch** (SC_HIDING+RG_TUNNELDRIVE / SC_CHASEWALK / Ensemble / Longing / Dancing / the `speed += speed*val/100; return` path).
  - **Slow tail:** ChaseWalk, Wedding, JointBeat, Cloaking(hide), Gospel, GatlingFever, Suiton, Swoo, Freezing, MarshOfAbyss (+ the `speed_rate>150→150` clamp), Camouflage, StealthField, Laziness, RockCrusher, PowerOfGaia, MelonBomb, Rebound, B_Trap, CatnipPowder, SP_SHA, CreatingStar, ShieldChainRush, GroundGravity, ShadowClock.
  - **Fast tail:** TF_MISS, Cloaking(speedup), Avoid, Invincible, CloakingExceed, Paralyse, Hovering, GN_CartBoost, SwingDance, WindStepOption, ArclouseDash, DoramWalkSpeed, RushWindmill, EmergencyMove, JawaiiSerenade, WildWalk.
  - **Final overrides tail:** SC_PARALYSE(val3==1) +50%, SC_ARMOR ≥200, SC_WALKSPEED (`speed*100/val1`).

## Scope — every sub-system that must be touched

- [x] Folded the slow-tail + fast-tail SCs into `ComputeScSpeed` (each `val = max(val, X)` reading
      the rAthena val-field): slow — Ensemblefatigue, HallucinationWalk-postdelay/GloomyDay(50),
      ChaseWalk, Wedding, JointBeat(ankle/knee), Cloaking(hide), Gospel, GatlingFever, Suiton, Swoo,
      Freezing, MarshOfAbyss, Camouflage, StealthField, Laziness, RockCrusherAtk, PowerOfGaia,
      MelonBomb, Rebound, B_Trap, CatnipPowder, SP_SHA, CreatingStar, ShieldChainRush, GroundGravity,
      ShadowClock; fast — Cloaking(move), Avoid, Invincible, CloakingExceed, Paralyse(val3=0),
      Hovering, GN_CartBoost, SwingDance, WindStepOption, ArclouseDash, DoramWalkSpeed, RushWindmill,
      EmergencyMove, JawaiiSerenade, WildWalk.
- [x] Added the MarshOfAbyss `speed_rate > 150 → 150` clamp between the phases.
- [x] Added the final-override tail (Paralyse(val3=1) +50% pre-multiply, Armor ≥200, WalkSpeed
      `speed*100/val1`).
- [x] Implemented the hiding / walk-mode slow early-branch (SC_HIDING + RG_TUNNELDRIVE → `120-6*lv`;
      SC_CHASEWALK val3<0 → val3) — added `SkillIds.RG_TUNNELDRIVE`.
- [ ] ➡️ The **freecast / exceed-break** (and mado-gear) early-return branches need the live cast /
      mado state → **COMBAT-105** (coordinate with COMBAT-70). The **Dancing-lesson** song penalty +
      **TF_MISS** assassin speedup (skill/class-gated) → **COMBAT-106**.

## Done criteria

- ✅ each listed tail SC changes move speed by the rAthena amount (Combat65…SpeedTests +10); the
  hiding-walk slow branch matches; SC_WALKSPEED / Steel Body / Armor / Defender overrides are correct.
- ➡️ The freecast branch is **COMBAT-105**; the Dancing-lesson/TF_MISS skill-gated entries **COMBAT-106**.

## Test plan

- Numeric tests for a representative slow-tail SC, fast-tail SC, the marsh clamp, the
  WalkSpeed override, and the freecast branch.

## Notes / gotchas

- Many tail SCs read a specific valN as the magnitude — match the rAthena field exactly.
- Skip SCs not yet in `StatusType`; note which were skipped so a later pass can add them.

## History

- 2026-06-03 — Folded the full slow-tail + fast-tail SC set into `ComputeScSpeed` (each `max(val, X)`
  reading the exact rAthena val-field), added the MarshOfAbyss `>150→150` clamp, the final-override
  tail (Paralyse(val3=1) +50% pre-multiply, Armor ≥200, SC_WALKSPEED `*100/val1`), and the hiding/
  walk-mode slow early-branch (SC_HIDING+RG_TUNNELDRIVE → `120-6*lv`, SC_CHASEWALK val3<0 → val3;
  added `SkillIds.RG_TUNNELDRIVE`=213). Combat65…SpeedTests +10 (Wedding/Suiton/JointBeat-ankle/marsh-
  clamp/Hovering/Avoid/Hiding+TunnelDrive/Paralyse-override/WalkSpeed/Armor). Full suite 4168 pass
  (1 fail = pre-existing INFRA-11 replay gate). Filed COMBAT-105 (freecast/exeedbreak + mado-gear
  early-return branches — need live cast/mado state) and COMBAT-106 (Dancing-lesson song penalty +
  TF_MISS assassin speedup — skill/class-gated).
