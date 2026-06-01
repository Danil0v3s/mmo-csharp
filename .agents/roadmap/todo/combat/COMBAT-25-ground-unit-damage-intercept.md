# COMBAT-25 — Ground-unit damage intercept (Safety Wall / Pneuma / Land Protector)

> **Epic:** Combat parity · **Status:** ❌ Not started · **Size:** L · **Player-visible:** yes
> **Depends on:** none · **Blocks:** none

## Problem

Defensive ground skill-units do not intercept incoming attacks. A player standing on
**Safety Wall** still takes full melee damage; a player on **Pneuma** still takes full
ranged damage; **Land Protector** does not suppress hostile ground-unit effects on its
cells. The combat path (`DamageService` / `BattleCalculator`) never consults
`ISkillUnitService` for units on the target's cell before committing a hit, so these
three classic defensive tools are inert. This was Scope axis 2 of COMBAT-08, split out
here because it requires `SkillUnitGroup` hit-pool bookkeeping that is orthogonal to the
cast-interrupt work COMBAT-08 shipped.

## Current state (C#)

- `Map.Server/Combat/DamageService.cs:ApplyResolved` — applies SC reductions, HP delta,
  death; **no ground-unit query** before damage commit.
- `Map.Server/Combat/BattleCalculator.cs` `CalcWeaponAttack` / the ranged/skill paths —
  never call `ISkillUnitService.GetUnitsInArea`.
- `Map.Server/Skills/ISkillUnitService.cs:84-95` — `GetUnitsInArea(map,cx,cy,radius[,skillId])`
  enumerates ground units on a cell. **No combat consumer.**
- `Map.Server/Skills/SkillUnitGroup.cs` — confirm whether it tracks a consumable HP / hit
  count (Safety Wall blocks `val2` hits) or only a tick lifetime. If only lifetime, add the
  consumable pool so the wall breaks after N blocks.
- `BattleDamage` / `BattleAttackType` + `AttackRange` already distinguish melee (`BF_SHORT`)
  vs ranged (`BF_LONG`) — thread that lane into the query.

## rAthena reference (source of truth)

Canonical: `battle.cpp`, `skill.cpp` (monolithic switch arms).

- `MG_SAFETYWALL` (`skill.cpp` `skill_unit_onplace` / `battle_calc_damage`) — intercepts melee
  (`BF_SHORT`) hits on its cell, sets damage 0, decrements the unit's HP/hit pool (`group->val2`
  remaining blocks in renewal; some versions use a damage-absorb pool), deletes the unit when
  spent. Ranged passes through.
- `AL_PNEUMA` — blocks ranged (`BF_LONG`) hits on its cell; melee passes through.
- `SA_LANDPROTECTOR` — prevents hostile ground-unit *skills* from placing/ticking on its cells
  (this is primarily a `SkillUnitService.Place` gate, NOT a direct-attack block — keep its
  scope there). Use the `skillId`-filtered overload (`ISkillUnitService.cs:94`).

## Scope — every sub-system that must be touched

- [ ] In the melee/ranged apply path (`DamageService` pre-HP-commit and/or
      `BattleCalculator.CalcWeaponAttack`), before committing damage, query
      `ISkillUnitService.GetUnitsInArea(target.MapId, target.X, target.Y, 0)`.
- [ ] **Safety Wall** on the target's cell + hit is melee (`BF_SHORT`) → damage 0, decrement
      the unit's hit pool via the group bookkeeping; delete the unit/group when exhausted.
- [ ] **Pneuma** on the cell + hit is ranged (`BF_LONG`) → damage 0.
- [ ] **Land Protector** — enforce the hostile-ground-unit suppression in
      `SkillUnitService.Place` (skillId-filtered), and note the overlap here so the cell check
      is consistent. (Do not block direct attacks.)
- [ ] `SkillUnitGroup` consumable hit/HP pool if not already present.
- [ ] Thread `BF_SHORT`/`BF_LONG` from `BattleDamage`/`AttackRange` into the cell query.

## Done criteria

- A target on Safety Wall takes 0 from a melee swing; the wall's hit pool decrements and the
  wall vanishes when exhausted. Ranged hits pass through Safety Wall.
- A target on Pneuma takes 0 from a ranged hit; melee passes through.
- A hostile ground-unit skill cannot place/tick on a Land Protector cell.

## Test plan

- Place Safety Wall on a cell, melee a target there → 0 damage + pool decrement; after N
  blocks the unit is gone. Ranged → full damage.
- Pneuma symmetric (ranged blocked, melee through).
- Land Protector: attempt to place a hostile ground unit on its cell → refused.

## Notes / gotchas

- Resolve `ISkillUnitService` lazily through `_services` in `DamageService` (DI cycle), same as
  the COMBAT-08 cast services.
- Confirm the renewal Safety Wall semantics (hit-count vs damage-absorb pool) against the
  loaded `skill_db` `val2` before picking the pool model.
