# COMBAT-58 — Ammo consumption on ammo-using skills + out-of-ammo client feedback

> **Epic:** Combat parity · **Status:** 🚧 In progress · **Size:** M · **Player-visible:** yes
> **Depends on:** COMBAT-36 (the `IAmmoService` gate/consume seam)
> **Blocks:** none
> **Filed by:** COMBAT-36 — it wired ammo gate/consume into the auto-attack loop only.

## Problem

COMBAT-36 added `IAmmoService` (gate + one-round consume) and wired it into the
**auto-attack** loop (`AttackService.Tick`). Two pieces remain:

1. **Ammo-using skills don't consume / gate ammo.** Ranged skills (Double Strafe,
   Arrow Shower, Arrow Storm, the Gunslinger skills, …) fire without spending a round
   and without the no-ammo gate. rAthena calls `battle_consume_ammo(sd, skill, lv)`
   with `skill_get_ammo_qty(skill, lv)` (some skills spend more than one) in the
   skill-castend path, and `skill_check_condition_castend` blocks the cast with no ammo.
2. **No client feedback on the gate / decrement.** The auto-attack gate currently
   refuses the swing silently; rAthena sends `clif_arrow_fail` / `USESKILL_FAIL_
   NEED_MORE_BULLET`. And ammo decrement rides the same client-prediction + save-sync
   path as `ItemUseService` (no immediate amount packet) — fine for the common case,
   but an explicit amount/fail packet would match rAthena's feedback.

## Current state (C#)

- `Map.Server/Inventory/AmmoService.cs` — `HasUsableAmmo` / `ConsumeAmmo`; consume is
  one round (no `skill_get_ammo_qty`).
- `Map.Server/Combat/AttackService.cs` — calls the gate/consume per auto-swing only.
- Skill dispatch (`SkillAttackService` / resolvers / `SkillCastEndService`) does not
  consult `IAmmoService`.

## rAthena reference

- `battle.cpp battle_consume_ammo(sd, skill, lv)` + `skill_get_ammo_qty`.
- `skill.cpp skill_check_condition_castend` ammo gate; `clif_arrow_fail` /
  `clif_skill_fail(USESKILL_FAIL_NEED_MORE_BULLET)`.

## Scope

- [ ] Consult `IAmmoService` in the ammo-using skill cast path: gate the cast on no/
      wrong ammo, and consume `skill_get_ammo_qty(skill, lv)` rounds on success
      (extend `ConsumeAmmo` to take a quantity).
- [ ] Send the rAthena out-of-ammo feedback (`clif_arrow_fail` / need-more-bullet) on
      the gate, and emit an inventory amount/delete packet on consume.

## Done criteria

- ➡️ from COMBAT-36: an ammo-using skill spends its `skill_get_ammo_qty` rounds and is
  blocked when out of ammo.
- The client receives the out-of-ammo message and the updated ammo count.

## Test plan

- A ranged skill decrements ammo by its qty; blocked at 0.
- Gate emits the fail packet; consume emits the amount packet.
