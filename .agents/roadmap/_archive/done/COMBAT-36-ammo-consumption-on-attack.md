# COMBAT-36 — Ammo consumption + no-ammo gate on ranged attacks

> **Epic:** Combat parity · **Status:** ✅ Done (2026-06-02) · **Size:** M · **Player-visible:** yes
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

- [x] Consume one equipped-ammo unit per ranged **auto-attack** — new
      `AmmoService.ConsumeAmmo` decrements `InventoryItem.Amount`, clears the equip bit
      + drops the stack at 0 (rAthena `pc_delitem`), via the same `RemovedInventoryIds`
      client-sync path as `ItemUseService`. Wired into `AttackService.Tick` per swing.
      ➡️ The "per ammo-using skill" half + an explicit inventory amount packet →
      COMBAT-58.
- [x] Gate the attack: `AmmoService.HasUsableAmmo` refuses the swing for a `UsesAmmo`
      weapon with no valid equipped ammo (rAthena `ATK_NONE`); `AttackService.Tick`
      reschedules instead of swinging (the out-of-ammo *message* ➡️ COMBAT-58).
- [x] Validate ammo type vs weapon type — `RequiredAmmoSubtype`: Bow→Arrow, all guns
      (Revolver..Grenade)→Bullet (renewal, battle.cpp:10401-10426); a mismatch is
      treated as no ammo.

## Done criteria

- A bow auto-attack decrements the arrow stack by 1; at 0 arrows the attack is
  refused. ✅
- A gun requires matching ammo; an Arrow in a gun's ammo slot does not fire it. ✅

## Test plan

- Bow attack decrements ammo Amount; reaching 0 blocks the next attack.
- Ammo-type mismatch blocks firing.

## Notes

- COMBAT-16 already surfaces `WeaponTypeCodes.UsesAmmo` and the `EquipAmmo` slot —
  this ticket is purely the consumption + gating in the attack loop, not the
  damage math.

## History

- 2026-06-02 · Added `IAmmoService`/`AmmoService` (Map.Server/Inventory): `HasUsableAmmo`
  (gate) + `ConsumeAmmo` (one round/swing, drop stack + clear equip bit at 0 via the
  `RemovedInventoryIds` sync path), with `RequiredAmmoSubtype` validating Bow→Arrow /
  guns→Bullet (renewal, battle.cpp:10401). Injected (optional) into `AttackService` and
  consulted per auto-swing: no/wrong ammo → reschedule without swinging (ATK_NONE);
  otherwise swing then consume (battle_consume_ammo). Registered in Program.cs.
  Combat36AmmoConsumptionTests (5: melee no-op, bow consume-to-zero-then-refused,
  no-ammo gate, arrow-in-gun rejected, gun+bullet fires); full Map.Server.Tests green
  except the pre-existing INFRA-11 replay gate. Filed COMBAT-58 (ammo consumption on
  ammo-using skills + the out-of-ammo client fail/amount packet).
