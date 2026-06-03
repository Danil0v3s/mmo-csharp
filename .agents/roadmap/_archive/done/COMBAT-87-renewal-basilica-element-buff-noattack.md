# COMBAT-87 — Renewal SC_BASILICA effects: offensive element buff + NoAttack caster state

> **Epic:** Combat parity · **Status:** ✅ Done (2026-06-03) · **Size:** M · **Player-visible:** yes
> **Depends on:** COMBAT-68 · **Blocks:** none
> **Filed by:** COMBAT-68 — it corrected the premise (renewal Basilica is NOT a cell-immunity
> sanctuary) and applies the `SC_BASILICA` self-buff, but the SC's two real renewal effects
> need infrastructure that does not exist yet.

## Problem

Renewal `SC_BASILICA` (status.yml: `CalcFlags: All`, `States: NoAttack`) has two effects the C#
does not implement — the C# `StatusType.Basilica` SC is currently an inert marker:

1. **Offensive element buff** (status.cpp:4768, `#ifdef RENEWAL`): while SC_BASILICA is active,
   `right_weapon.addele[ELE_DARK] += val1*5`, `right_weapon.addele[ELE_UNDEAD] += val1*5` (and
   left weapon unless `left_cardfix_to_right`), and `magic_atk_ele[ELE_HOLY] += val1*3`.
2. **NoAttack caster state**: the Basilica caster cannot auto-attack while the SC is up.

## Current state (C#)

- `Map.Server/Status/StatusEffectRegistry.cs` — no `Register(StatusType.Basilica, …)` handler;
  the SC is applied (COMBAT-68 `Acolyte/Basilica.cs`) but does nothing.
- `Map.Server/Inventory/EquipBonusBundle.cs` — has `AddEle`/`MagicAddEle` (COMBAT-63), but they
  are populated from equipment only; there is **no SC→element-fold seam**. An `OnRecalc` that
  added to `EquipBonuses.AddEle` would *accumulate/leak* because `CalcPc` consumes the bundle
  without resetting it (only `EquipBonusAggregator.BuildBundle`, on equip change, resets it).
- `Map.Server/Status/EntityActionGates.cs` — has `CanAct`/`CanCastSkill` (coarse OPT1 blocks),
  but **no fine-grained `CanAttack`** for the `NoAttack` state (Basilica must still allow casting
  to re-toggle, so it can't go into `CanAct`).

## rAthena reference (source of truth)

- `status.cpp:4768` (RENEWAL SC_BASILICA addele block) + `db/re/status.yml` Basilica
  (`CalcFlags: All`, `States: NoAttack`).
- The `States: NoAttack` enforcement in the attack path (rAthena `battle_check_target` /
  `unit_attack` consult the SC state).

## Scope — every sub-system that must be touched

- [x] SC→element seam: chose the **leak-free combat-time read** (the ticket's sanctioned alternative
      "a separate SC-element … summed in `BattleCardService.CalcCardFix`") over an OnRecalc that would
      leak into the un-reset equip bundle. The weapon Dark/Undead buff is read in `CalcCardFix`; the
      Holy magic buff in `CalcMagicAttack` — both straight off the live `SC_BASILICA.Val1`, so they
      vanish the instant the SC ends (no accumulation).
- [x] No registry `OnRecalc` needed — the SC stays a marker (applied by COMBAT-68's `Basilica.cs`);
      its effects are consumed at combat/attack time (val1·5 weapon Dark/Undead in CalcCardFix, val1·3
      Holy magic in CalcMagicAttack). ➡️ The general equip `bMagicAtkEle` seam (not Basilica-specific)
      is **COMBAT-109**.
- [x] Added `EntityActionGates.CanAttack` (= `CanAct` && no `SC_BASILICA`); wired it into the
      auto-attack entry + per-tick swing guard in `AttackService` (casting still uses `CanCastSkill`).

## Done criteria

- ✅ A Basilica caster's weapon does +`val1*5`% vs Dark/Undead targets (lv5 → +25%) and +`val1*3`%
  Holy magic (lv5 → +15%); both clear on SC end with no leak.
- ✅ The Basilica caster cannot auto-attack while the SC is up but can still cast (re-cast cancels it).

## Test plan

- Cardfix numeric test: SC_BASILICA adds the Dark/Undead weapon % + Holy magic % (and clears on
  SC end without leaking across recalcs).
- Attack-gate test: a Basilica caster's auto-attack is refused; casting still works.

## Notes / gotchas

- The SC→element-fold seam is the crux (no existing pattern) — it likely benefits other SCs
  (endow/weapon-property), so design it generally. Coordinate with the SC-magnitude tickets.
- Pre-renewal Basilica (the PVP-block sanctuary + SC_BASILICA_CELL) is explicitly NOT in scope.

## History

- 2026-06-03 — Implemented the renewal SC_BASILICA effects via leak-free combat-time SC reads (not
  an OnRecalc, which the ticket itself flagged would leak into the un-reset equip bundle): the weapon
  `addele[Dark/Undead] += val1*5` buff folds into the offensive ele term in `BattleCardService.CalcCardFix`,
  the `magic_atk_ele[Holy] += val1*3` buff into `BattleCalculator.CalcMagicAttack` (keyed on the resolved
  Holy skill element) — both read straight off the live SC so they clear instantly on end. Added
  `EntityActionGates.CanAttack` (CanAct && no SC_BASILICA) and wired it into the auto-attack entry +
  per-tick swing guard in `AttackService` (casting still uses CanCastSkill, so the caster can re-cast to
  cancel). Combat87BasilicaTests (4: weapon Dark/Undead +25%, no-leak on end, Holy magic +15%, NoAttack
  gate). Full suite 4180 pass (1 fail = pre-existing INFRA-11 replay gate). Filed COMBAT-109 (the general
  equip `bMagicAtkEle` seam, adjacent to the targeted Basilica Holy read).
