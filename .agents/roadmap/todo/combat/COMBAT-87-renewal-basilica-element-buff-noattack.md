# COMBAT-87 — Renewal SC_BASILICA effects: offensive element buff + NoAttack caster state

> **Epic:** Combat parity · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
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

- [ ] An SC→element-fold seam so `SC_BASILICA` can contribute `addele[Dark/Undead]` +
      `magic_addele[Holy]` per recalc without leaking (fold during `BuildBundle`, or a separate
      SC-element array summed in `BattleCardService.CalcCardFix`).
- [ ] Register `StatusType.Basilica` with the element-buff `OnRecalc`/fold (val1·5 weapon
      Dark/Undead, val1·3 magic Holy).
- [ ] A `CanAttack` gate (or a `NoAttack` state set) consulted by the auto-attack path; add
      `SC_BASILICA` to it (still allow casting to re-toggle Basilica).

## Done criteria

- A Basilica caster's weapon does +`val1*5`% vs Dark/Undead targets and +`val1*3`% Holy magic.
- The Basilica caster cannot auto-attack while the SC is up (but can re-cast to cancel it).

## Test plan

- Cardfix numeric test: SC_BASILICA adds the Dark/Undead weapon % + Holy magic % (and clears on
  SC end without leaking across recalcs).
- Attack-gate test: a Basilica caster's auto-attack is refused; casting still works.

## Notes / gotchas

- The SC→element-fold seam is the crux (no existing pattern) — it likely benefits other SCs
  (endow/weapon-property), so design it generally. Coordinate with the SC-magnitude tickets.
- Pre-renewal Basilica (the PVP-block sanctuary + SC_BASILICA_CELL) is explicitly NOT in scope.
