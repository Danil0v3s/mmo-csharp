# COMBAT-84 — SC speed-table tail: exotic SCs + freecast / hiding-walk early branches

> **Epic:** Combat parity · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
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

- [ ] Fold the slow-tail + fast-tail SCs into `ComputeScSpeed` (each `val = max(val, X)` with the
      rAthena value/val-field), guarding on SCs that exist in `StatusType`.
- [ ] Add the MarshOfAbyss `speed_rate > 150 → 150` clamp between the phases.
- [ ] Add the final-override tail (Paralyse +50%, Armor ≥200, WalkSpeed `speed*100/val1`).
- [ ] Implement the freecast / exceed-break early branch (coordinate with COMBAT-70).
- [ ] Implement the hiding / walk-mode early-return branch.

## Done criteria

- ➡️ from COMBAT-65: each listed SC changes move speed by the rAthena amount; the freecast and
  hiding-walk branches match; ChangeSpeed (SC_WALKSPEED) and Steel Body interplay is correct.

## Test plan

- Numeric tests for a representative slow-tail SC, fast-tail SC, the marsh clamp, the
  WalkSpeed override, and the freecast branch.

## Notes / gotchas

- Many tail SCs read a specific valN as the magnitude — match the rAthena field exactly.
- Skip SCs not yet in `StatusType`; note which were skipped so a later pass can add them.
