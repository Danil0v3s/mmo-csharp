# COMBAT-04 — Base damage (DEX-derived atkmin), size-fix, multi-hit div, dual-wield

> **Epic:** Combat parity · **Status:** ✅ Done (2026-06-01) · **Size:** XL · **Player-visible:** yes
> **Depends on:** COMBAT-01 (weapon-type / equip data on the bundle) · **Blocks:** none
>
> **XL split on implementation:** axis 1 (PC DEX-derived atkmin) shipped here — the
> dominant, self-contained, high-impact axis. The other three axes each need separate
> data/infra and were filed as follow-ups: **size-fix table + bow** → COMBAT-16 (note:
> renewal size penalties are tiny — only Knuckle/Whip vs Large), **multi-hit div** →
> COMBAT-17, **dual-wield left-hand** → COMBAT-18.

## Problem

The base weapon-damage chain is a stub on four axes:

1. **atkmin is wrong for PCs.** `CalcBaseDamage` rolls between `WatkMin..WatkMax`
   (`BattleCalculator.cs:258-269`) but for a PC, rAthena derives `atkmin` from DEX and weapon
   level, not a stored min. With `EquipBonusAggregator` flattening `WeaponAtkMin =
   WeaponAtkMax` (`EquipBonusAggregator.cs:80-82`), every PC swing is the flat max — no
   variance, wrong floor.
2. **Size-fix is a no-op.** `SizeMod` returns 100 for all sizes
   (`BattleCalculator.cs:284-290`). rAthena reads the per-weapon `atkmods[size]` table.
3. **Multi-hit is never modeled.** `Div` is hardcoded `1` in `ZC_NOTIFY_ACT3`
   (`DamageService.cs:458`); `BattleDamage.Hits` defaults 1; `battle_calc_multi_attack`
   (double-attack, Sonic Blow ×8, spear-on-Peco ×2) is not ported.
4. **Dual-wield / left-hand is absent.** `Damage2` is always 0 (`DamageService.cs:460`);
   `battle_calc_attack_left_right_hands` is not ported; `EquipService` resolves a left-hand
   slot (`EquipBits.HandL`) but the aggregator only reads the right hand
   (`EquipBonusAggregator.cs:66-69`).

## Current state (C#)

- `Map.Server/Combat/BattleCalculator.cs:258-276` — `CalcBaseDamage`:
  `atkMin = s.WatkMin; atkMax = s.WatkMax;` roll, `+= s.Batk`, crit `×14/10`.
- `Map.Server/Combat/BattleCalculator.cs:84-87` — PC size-mod multiply uses `SizeMod(t.Size)`.
- `Map.Server/Combat/BattleCalculator.cs:284-290` — `SizeMod` returns 100 always.
- `Map.Server/Combat/BattleCalculator.cs:37-213` — `CalcWeaponAttack` returns a single-hit
  `BattleDamage`; never sets `Hits` > 1 or `Damage2`.
- `Map.Server/Inventory/EquipBonusAggregator.cs:51-87` — `Aggregate` sums only the right-hand
  weapon ATK and flattens min=max. No left-hand weapon, no weapon-level, no per-weapon
  size mods, no `atkmin` DEX derivation inputs.
- `Map.Server/Combat/DamageService.cs:448-461` — `BroadcastAct` builds `ZC_NOTIFY_ACT3` with
  `Div = 1, Damage2 = 0` hardcoded.
- `Map.Server/Combat/BattleDamage.cs` — has `Hits`, `Damage`, `Total`; confirm whether
  `Damage2` field exists (add if missing).

## rAthena reference (source of truth)

Canonical: `battle.cpp`.

- `battle.cpp:2453` `battle_calc_base_damage(src, status, wa, sc, t_size, flag)`:
  - Non-PC / mob: `atkmin = wa->atk; atkmax = wa->atk2;` (then `if (atkmin>atkmax) atkmin=atkmax`).
  - PC path: `atkmin = status->dex;` then if the equipped weapon in `type` slot is a weapon,
    `atkmin = atkmin * (80 + weapon_lv*20) / 100;` (verified at `battle.cpp:2483-2486` region),
    capped so `atkmin <= atkmax (= wa->atk)`. Bows take `atkmin/atkmax` from arrow + weapon,
    and `arrow_atk` is added. `t_size` indexes the weapon's `atkmods[]` (size penalty table)
    — this is where the small/medium/large multiplier comes from (per-weapon, from item_db
    `subtype`/weapon table, not a flat 100/75/50).
- `battle.cpp:4394` `battle_calc_multi_attack(wd, src, target, skill_id, skill_lv)`:
  sets `wd->div_` — double-attack chance (`sd->weapon_atk_rate`/`DA` skills), Sonic Blow
  `div_ = 8`, spear while riding Peco `div_ = 2` vs medium/large, etc.
- `battle.cpp:7150` `battle_calc_attack_left_right_hands(wd, src, target, skill_id, skill_lv)`:
  splits damage across hands. For dual-wield, `wd->damage2` is computed from the left weapon;
  `damage` and `damage2` get the `100/(100+masteryAtk)`-style split and the renewal left-hand
  ATK reduction. Single-weapon → `damage2 = 0`.
- Call order: `battle_calc_multi_attack` (`battle.cpp:7676`) → base damage (right+left at
  `:7708-7743` region via `battle_calc_base_damage` for `&sstatus->rhw` and `&sstatus->lhw`)
  → ratio/cardfix → `battle_calc_attack_left_right_hands` (`:7807`+).

## Scope — every sub-system that must be touched

- [ ] **Weapon data plumbing.** Surface on the equip path: per-equipped-weapon `WeaponLevel`,
      `WeaponType` (already on `PcBaseInputs.WeaponType`), and the per-weapon size-penalty
      table (`atkmods[3]`). Add columns/loader reads in `EquipBonusAggregator.Aggregate`
      (`EquipBonusAggregator.cs:47`) — read `row.WeaponLevel` (verify column name on
      `ItemEntity`) and the weapon's size mods; return them in `EquipSummary`.
- [ ] **`PcBaseInputs`** (`IStatusCalcService.cs:68`): add `WeaponLevel`, `SizeMods` (3 ints),
      and left-hand weapon atk + level for dual-wield.
- [ ] **`StatusCalcService.CalcPc`**: store weapon level + size mods on `BattleStats` (add
      fields `WeaponLevel`, `SizeMod[3]`, `WatkMinL/WatkMaxL`).
- [ ] **`BattleCalculator.CalcBaseDamage`**: implement the PC `atkmin = dex * (80 +
      weaponLv*20)/100` floor, capped at `atkmax`; keep the mob path (`atk`/`atk2`).
- [ ] **`BattleCalculator.SizeMod`**: read `BattleStats.SizeMod[(int)targetSize]` (the
      equipped weapon's atkmods), defaulting 100 when bare-handed.
- [ ] **Multi-hit:** port `battle_calc_multi_attack` into `CalcWeaponAttack` — set
      `result.Hits` (div) from double-attack chance, Sonic Blow (skill path), spear-on-Peco.
      For auto-attack this requires the double-attack roll (DA skill / weapon `bonus
      bDoubleRate`). Surface `Hits` so the per-hit damage = `damage` and total = `damage*Hits`
      (matching rAthena which stores per-hit damage and `div_`).
- [ ] **Dual-wield / left hand:** port `battle_calc_attack_left_right_hands` — compute
      `result.Damage2` from the left weapon when both hand slots hold weapons. Add `Damage2`
      to `BattleDamage` if absent.
- [ ] **`DamageService.BroadcastAct` (`:448`)**: set `Div = damage.Hits` and `Damage2 =
      damage.Damage2` from the resolved `BattleDamage` instead of hardcoded `1`/`0`. (Add a
      `BattleDamage`-taking overload to `ApplyResolved`/`BroadcastAct` so the data threads
      through — currently `ApplyDamage` only carries a scalar `int damage`.)
- [ ] **No DB migration if `ItemEntity` already has weapon-level / weapon-type columns**
      (verify); otherwise add them.

## Done criteria

- A PC with DEX 50 and a level-4 weapon (atk 100): `atkmin = 50*(80+80)/100 = 80`, swings
  roll in `[80,100]` (not flat 100). Bare-handed PC: `atkmin = dex*(80+0)/100`.
- ➡️ **COMBAT-16** — size-fix (renewal: only Knuckle/Whip × Large = 75%; the current
  all-100 stub is correct for every other weapon).
- ➡️ **COMBAT-17** — Sonic Blow `Div = 8`, double-attack `Div = 2` (multi-hit).
- ➡️ **COMBAT-18** — dual-wield `Damage2 > 0`.

## Test plan

- Unit-test `CalcBaseDamage` PC path for the DEX/weaponLv floor across weapon levels 1-4 and
  bare hand; assert exact `atkmin`.
- Unit-test `SizeMod` reads the per-weapon table (small/medium/large) for a dagger vs a
  two-hand sword vs bare hand.
- Unit-test multi-hit: Sonic Blow → `Hits==8`; forced double-attack → `Hits==2`.
- Unit-test dual-wield split: two weapons → `Damage + Damage2`, left ≈ rAthena reduction.
- Wire-shape test: `BroadcastAct` emits `Div`/`Damage2` matching the `BattleDamage`.

## Notes / gotchas

- rAthena stores **per-hit** damage in `wd.damage` and the hit count in `div_`; the client
  multiplies. Decide whether `BattleDamage.Damage` is per-hit or total and keep
  `ApplyDamage` consistent (HP loss must be `per-hit × div`). Today `DamageService` applies a
  scalar total — when threading `Hits`, ensure HP delta = total, not per-hit.
- Arrow/bow `arrow_atk` and ammo consumption are a related but separable slice; the DEX-floor
  + size-fix + div + dual-wield are the player-visible core. Note bow handling as a follow-up
  if `EquipSummary` doesn't yet distinguish bows.
- `ApplyDamage(Entity, int, Entity?)` is the narrow API (`DamageService.cs:69`); `Div`/
  `Damage2` only flow through `PerformMeleeAttack` (`:81`) which has the full `BattleDamage`.
  Skills that call `ApplyDamage` directly will still emit `Div=1` — that's acceptable for
  single-hit skills but Sonic Blow must go through the `BattleDamage`-carrying path.

## History

- **2026-06-01** — Done (axis 1 — PC DEX-derived base-damage atkmin). Added
  `BattleCalculator.ComputePcAtkMin` (rAthena battle.cpp:2453: `atkmin = DEX`, ×(80 +
  weaponLv*20)/100 when a weapon is equipped, capped at atkmax; bare-handed keeps DEX)
  and wired it into `CalcBaseDamage` (PC path; mobs keep rhw.atk). Plumbed the
  right-hand weapon level end-to-end: `ItemEntity.WeaponLevel` → `EquipSummary` →
  `PcBaseInputs.WeaponLevel` → `BattleStats.WeaponLevel` (via EquipService recalc + the
  enter path). Tests: `Combat04BaseDamageTests` (11). Full Map.Server suite 3613/3613
  green. Follow-ups (the XL ticket's other 3 axes): **COMBAT-16** (size-fix + bow),
  **COMBAT-17** (multi-hit div), **COMBAT-18** (dual-wield). Commits: start `ab96009`,
  finish `<this>`.
