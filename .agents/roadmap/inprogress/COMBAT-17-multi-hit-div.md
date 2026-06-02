# COMBAT-17 — Multi-hit div (battle_calc_multi_attack + ACT3 wire)

> **Epic:** Combat parity · **Status:** 🚧 In progress · **Size:** M · **Player-visible:** yes
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

- [ ] Port `battle_calc_multi_attack` into `CalcWeaponAttack` (set `result.Hits`):
      double-attack roll (needs `bonus bDoubleRate` — COMBAT-06), spear-on-Peco.
- [ ] Skill div: ensure skills like Sonic Blow set `Hits = 8` (skill-plugin side).
- [ ] Thread `BattleDamage` into `BroadcastAct`: `Div = Hits`, and make HP loss equal
      `per-hit × Hits` consistently (decide whether `Damage` is per-hit or total and
      keep `ApplyDamage` consistent — see COMBAT-04 notes).

## Done criteria

- Sonic Blow shows `Div = 8` in `ZC_NOTIFY_ACT3`; a double-attack auto-swing shows
  `Div = 2`; HP loss matches per-hit × div.

## Test plan

- Multi-hit: forced double-attack → `Hits==2`; Sonic Blow → `Hits==8`.
- Wire test: `BroadcastAct` emits `Div == BattleDamage.Hits`.
