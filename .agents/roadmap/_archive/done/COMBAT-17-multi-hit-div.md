# COMBAT-17 — Multi-hit div (battle_calc_multi_attack + ACT3 wire)

> **Epic:** Combat parity · **Status:** ✅ Done (2026-06-02) · **Size:** M · **Player-visible:** yes
> **Depends on:** COMBAT-04 · **Blocks:** none
> **Filed by:** COMBAT-04 (axis 3).

## Problem

Multi-hit attacks always render/deal a single hit. `BattleDamage.Hits` exists but is
never set > 1, and `DamageService.BroadcastAct` hardcodes `Div = 1` in
`ZC_NOTIFY_ACT3`. rAthena `battle_calc_multi_attack` (battle.cpp:4394) sets `div_`:
auto-attack double-attack (weapon `bonus bDoubleRate` / DA skills), Sonic Blow
`div_ = 8`, spear-while-riding-Peco `div_ = 2` vs medium/large, etc.

## Current state (C#)

- `Map.Server/Combat/BattleCalculator.cs:CalcWeaponAttack` — never sets `result.Hits`.
- `Map.Server/Combat/DamageService.cs:BroadcastAct` — `Div = 1` hardcoded; the full
  `BattleDamage` is available on the `PerformMeleeAttack` path.
- Double-attack rate comes from `bonus bDoubleRate` — depends on COMBAT-06 bonus
  coverage for the auto-attack case.

## rAthena reference

- `battle.cpp:4394` `battle_calc_multi_attack` — the `div_` rules.
- rAthena stores **per-hit** damage in `wd.damage` and the count in `div_`; the client
  multiplies, and HP loss is `per-hit × div_`.

## Scope

- [x] Port `battle_calc_multi_attack` into `CalcWeaponAttack` (set `result.Hits`):
      double-attack roll. Implemented the auto-attack double-attack branch (TF_DOUBLE+dagger
      / `bonus bDoubleRate` / SC_KAGEMUSYA, renewal `max(7*lv, double_rate)` rate, positive-div
      ×2 damage). ➡️ FearBreeze bow + Chain Action revolver branches moved to **COMBAT-37**;
      per-skill div_ switch arms moved to **COMBAT-38**. The ticket's "spear-on-Peco" premise
      is **fictional** in this rAthena — `battle_calc_multi_attack` has no `pc_isridingpeco`
      div_ rule (verified), so nothing to port (cf. COMBAT-14's INF2_DISABLELVDMG).
- [x] Skill div: ensure skills like Sonic Blow set `Hits = 8` (skill-plugin side). Added
      `WeaponSkillImpl.GetMultiHitCount` (default 1); Sonic Blow → 8 (rAthena `num` -8 magnitude).
      ➡️ Sweep of the remaining multi-hit plugins moved to **COMBAT-39**.
- [x] Thread `BattleDamage` into `BroadcastAct`: `Div = Hits`. `ApplyResolved`/`BroadcastAct`/
      `ApplyDamage`(+`IDamageService`) now carry `hits`; `PerformMeleeAttack` passes
      `damage.Hits`. `Damage`/`Total` is the resolved total either way (double-attack ×2 in
      `CalcWeaponAttack`; skills already total), so HP loss = total and the wire div renders N.

## Done criteria

- Sonic Blow shows `Div = 8` in `ZC_NOTIFY_ACT3` ✅; a double-attack auto-swing shows
  `Div = 2` ✅; HP loss matches per-hit × div ✅ (double-attack damage ×2; skill total).

## Test plan

- Multi-hit: forced double-attack → `Hits==2`; Sonic Blow → `Hits==8`.
- Wire test: `BroadcastAct` emits `Div == BattleDamage.Hits`.

## History

- **2026-06-02** — inprogress→done. Auto-attack double-attack (TF_DOUBLE+dagger /
  `bonus bDoubleRate` (new `EquipBonusBundle.DoubleRate` + extractor max-merge) /
  SC_KAGEMUSYA) sets `Hits=2` + doubles per-hit damage in `BattleCalculator.CalcMultiAttack`;
  `WeaponSkillImpl.GetMultiHitCount` (Sonic Blow → 8) feeds the skill div; the `hits` count
  threads `PerformMeleeAttack`/skill funnel → `ApplyResolved` → `BroadcastAct` → `ZC_NOTIFY_ACT3.Div`.
  `Combat17MultiHitTests` (10) green; Map.Server unit suite 3771/3772 (the 1 fail is the
  pre-existing INFRA-11 replay E2E readiness gate, unrelated). Filed COMBAT-37 (FearBreeze/
  ChainAction), COMBAT-38 (per-skill div_ arms), COMBAT-39 (multi-hit plugin sweep);
  "spear-on-Peco" found fictional in this rAthena.
