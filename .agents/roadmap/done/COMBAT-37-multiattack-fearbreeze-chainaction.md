# COMBAT-37 — Auto-attack multi_attack: FearBreeze bow + Chain Action revolver

> **Epic:** Combat parity · **Status:** ✅ Done (2026-06-02) · **Size:** M · **Player-visible:** yes
> **Depends on:** COMBAT-17 (CalcMultiAttack + div wire), COMBAT-36 (ammo consumption — FearBreeze reads ammo count)
> **Blocks:** none
> **Filed by:** COMBAT-17 — the two auto-attack `battle_calc_multi_attack` branches it deferred.

## Problem

COMBAT-17 ported only the double-attack branch of `battle_calc_multi_attack`.
Two other auto-attack (skill_id == 0) multi-hit triggers are still missing, so a
Ranger with Fear Breeze or a Gunslinger with Chain Action never fires the extra
arrows/rounds.

## Current state (C#)

- `Map.Server/Combat/BattleCalculator.cs:CalcMultiAttack` — handles TF_DOUBLE /
  `bonus.double_rate` / SC_KAGEMUSYA only. No FearBreeze, no Chain Action.
- `EquipBonusBundle` has `DoubleRate` (COMBAT-17); no ammo-count read wired into the
  multi-attack path.

## rAthena reference (source of truth)

- `battle.cpp:4403-4437` `battle_calc_multi_attack` — SC_FEARBREEZE: bow (`W_BOW`) +
  equipped ammo amount > 1. Tiered roll on `rnd()%100`: val1 5 → <4% 5 hits, val1 4 →
  <7% 4 hits, val1 3 → <10% 3 hits, val1 1/2 → <13% 2 hits; `div_ = min(div_, ammo amount)`;
  stores `val4 = div_-1`; sets `DMG_MULTI_HIT` when div_ > 1.
- `battle.cpp:4459-4467` — Chain Action: `W_REVOLVER` + `pc_checkskill(GS_CHAINACTION)`,
  or `SC_E_CHAIN` (Eternal Chain) val1; `rnd()%100 < 5*skill_lv` → `div_ =
  skill_get_num(GS_CHAINACTION,lv)`, `DMG_MULTI_HIT`, and `sc_start(SC_QD_SHOT_READY,
  skill_get_time(RL_QD_SHOT,1))`.

## Scope — every sub-system that must be touched

- [x] `CalcMultiAttack`: FearBreeze branch — `TryFearBreeze` reads SC_FEARBREEZE val1,
      gates on bow + ammo > 1, single `rnd()%100` tier-ladder roll (val5 ≤4%→5, ≤7%→4,
      ≤10%→3, ≤13%→2, lower val1 starts further down), caps div by ammo, stores
      val4 = div-1, applies `Hits`/`Damage *= div` + DMG_MULTI_HIT.
- [x] `CalcMultiAttack`: Chain Action branch — `TryChainAction`: revolver +
      GS_CHAINACTION learned (or SC_E_CHAIN val1); `rnd()%100 < 5*lv` → 2 hits
      (skill_get_num) + starts SC_QD_SHOT_READY 1500 ms (val1 = target id). Branches run
      in rAthena order with `Hits == 1` guards (no stacking).
- [x] Wire the ammo-amount read — added `IAmmoService.GetEquippedAmmoAmount` (COMBAT-36)
      and injected `IAmmoService` (optional) into `BattleCalculator` (Program.cs).
- [x] SkillIds (GS_CHAINACTION 511, RL_QD_SHOT 2559) + StatusTypes (Fearbreeze, EChain,
      QdShotReady) all already present — no additions needed.

## Done criteria

- A bow PC with SC_FEARBREEZE val5 and ≥5 arrows rolls 2..5 hits at the rAthena
  probabilities (seedable rng test); div capped by ammo count. ✅
- A revolver PC with GS_CHAINACTION lv N fires div_ = skill_get_num at 5*N%. ✅
- No `// TODO` / log-only no-op in the touched files. ✅
- ➡️ **Production note:** FearBreeze + the SC_E_CHAIN path + the SC_QD_SHOT_READY start
  read `_sc`, which is null in the live `BattleCalculator` (the IStatusChangeService↔
  IDamageService↔IBattleCalculator cycle — same as COMBAT-17's Kagemusya). The branch
  logic is complete + tested; activating it (and the whole dormant SC-combat set) in
  production is COMBAT-59. ChainAction's revolver+learned-skill proc works live (no
  `_sc` needed for the proc itself).

## Test plan

- `Combat37MultiAttackTests`: FearBreeze tier table (forced rng), ammo cap, Chain
  Action div + SC_QD_SHOT_READY start, negative gates (wrong weapon type).

## Notes / gotchas

- These are auto-attack (skill_id == 0) branches only; the per-skill div_ switch arms
  are COMBAT-38. Keep `result.Hits != 1` early-out so branches don't stack.

## History

- 2026-06-02 · Refactored `BattleCalculator.CalcMultiAttack` into three sequential
  `Hits == 1`-gated branches (rAthena order): `TryFearBreeze` (bow + SC_FEARBREEZE +
  ammo>1, tier-ladder roll, ammo-capped div, val4=div-1), the existing `TryDoubleAttack`
  (COMBAT-17), and `TryChainAction` (revolver+GS_CHAINACTION or SC_E_CHAIN, 5*lv% → 2
  hits + SC_QD_SHOT_READY). Added `IAmmoService.GetEquippedAmmoAmount` for the live
  div cap and injected `IAmmoService` (optional) into `BattleCalculator`.
  Combat37MultiAttackTests (9: FearBreeze 5-hit/tier/ammo-cap/val4/gates, Chain Action
  learned+EChain+QD-start/wrong-weapon). Full Map.Server.Tests green except the
  pre-existing INFRA-11 replay gate. Filed COMBAT-59 (wire `_sc` into BattleCalculator
  so FearBreeze + the dormant SC-combat set activate in production).
