# COMBAT-36 — Ammo consumption + no-ammo gate on ranged attacks

> **Epic:** Combat parity · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** COMBAT-16 (arrow_atk + weapon-type resolution) · **Blocks:** none
> **Filed by:** COMBAT-16 on 2026-06-02 (the attack-loop half of the bow slice).

## Problem

COMBAT-16 folds the equipped ammo's ATK into the bow/gun swing (`arrow_atk`) and
resolves the weapon type, but the **attack loop never consumes ammo** and there is
**no "out of ammo → cannot attack" gate**. A bow user fires forever with a single
arrow, and can fire with none. rAthena consumes one round per shot
(`battle_consume_ammo`) and refuses the attack when the ammo pool is empty / the
equipped ammo type doesn't match the weapon.

## Current state (C#)

- `Map.Server/Inventory/EquipBonusAggregator.cs` — folds ammo ATK for
  `WeaponTypeCodes.UsesAmmo` weapons (COMBAT-16); the ammo item sits in the
  `EquipAmmo` (0x8000) slot.
- The auto-attack entry (`unit_attack` equivalent → `DamageService` / the basic-attack
  game-loop path) does **not** decrement the equipped ammo `InventoryItem.Amount`
  nor block on zero.
- No ammo-type↔weapon-type validity check (Arrow↔Bow, Bullet↔Revolver, …).

## rAthena reference

- `battle.cpp battle_consume_ammo` — spends `sd->state.arrow_atk`/`req.ammo` count
  per attack; `skill_check_condition_castend` / `pc_checkequip` ammo gate.
- Ammo type ↔ weapon: `clif.hpp`/`pc.cpp` ammo matching (AMMO_ARROW for bows,
  AMMO_BULLET/SHELL/GRENADE for guns).

## Scope

- [ ] Consume one equipped-ammo unit per ranged auto-attack (and per ammo-using
      skill); decrement `InventoryItem.Amount`, unequip + clear when it hits 0,
      emit the inventory delete/update packet.
- [ ] Gate the attack: a `UsesAmmo` weapon with no valid equipped ammo cannot
      auto-attack (rAthena shows the "you are out of ammunition" message).
- [ ] Validate ammo type vs weapon type (Arrow→Bow, Bullet/Shell/Grenade→guns);
      a mismatch is treated as no ammo.

## Done criteria

- A bow auto-attack decrements the arrow stack by 1; at 0 arrows the attack is
  refused.
- A gun requires matching ammo; an Arrow in a gun's ammo slot does not fire it.

## Test plan

- Bow attack decrements ammo Amount; reaching 0 blocks the next attack.
- Ammo-type mismatch blocks firing.

## Notes

- COMBAT-16 already surfaces `WeaponTypeCodes.UsesAmmo` and the `EquipAmmo` slot —
  this ticket is purely the consumption + gating in the attack loop, not the
  damage math.
