# COMBAT-37 — Auto-attack multi_attack: FearBreeze bow + Chain Action revolver

> **Epic:** Combat parity · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
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

- [ ] `CalcMultiAttack`: FearBreeze branch — read SC_FEARBREEZE val1, bow weapon-type,
      equipped-ammo amount (thread ammo count onto the swinger or read `EquipBonuses`);
      tiered roll → set `result.Hits` (2..5) and `result.Damage *= Hits`; store val4.
- [ ] `CalcMultiAttack`: Chain Action branch — revolver + GS_CHAINACTION learned or
      SC_E_CHAIN; roll → Hits = skill_get_num(GS_CHAINACTION); start SC_QD_SHOT_READY.
- [ ] Wire the ammo-amount read (depends on COMBAT-36's ammo plumbing).
- [ ] SkillIds for GS_CHAINACTION / RL_QD_SHOT + StatusType for FearBreeze / E_Chain /
      QD_Shot_Ready if not present.

## Done criteria

- A bow PC with SC_FEARBREEZE val5 and ≥5 arrows rolls 2..5 hits at the rAthena
  probabilities (seedable rng test); div capped by ammo count.
- A revolver PC with GS_CHAINACTION lv N fires div_ = skill_get_num at 5*N%.
- No `// TODO` / log-only no-op in the touched files.

## Test plan

- `Combat37MultiAttackTests`: FearBreeze tier table (forced rng), ammo cap, Chain
  Action div + SC_QD_SHOT_READY start, negative gates (wrong weapon type).

## Notes / gotchas

- These are auto-attack (skill_id == 0) branches only; the per-skill div_ switch arms
  are COMBAT-38. Keep `result.Hits != 1` early-out so branches don't stack.
