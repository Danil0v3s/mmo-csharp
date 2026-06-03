# COMBAT-25 — Ground-unit damage intercept (Safety Wall / Pneuma / Land Protector)

> **Epic:** Combat parity · **Status:** ✅ Done (2026-06-02) · **Size:** L · **Player-visible:** yes
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

- [x] `DamageService.PerformMeleeAttack` queries `ISkillUnitService.GetUnitsInArea(target
      cell, radius 0)` (resolved lazily via `_services`, like the COMBAT-08 cast services)
      before committing the swing.
- [x] **Safety Wall** on the cell + melee (`IsShortRange`) → damage 0, decrement the
      group's `Val2` block pool, `DelUnitGroup` when exhausted. Ranged passes through.
- [x] **Pneuma** on the cell + ranged → damage 0; melee passes through.
- [x] `SkillUnitGroup.Val2` consumable pool added + initialized in `Place` (2 + 2·lv for
      Safety Wall).
- [x] Threaded melee/ranged via `BattleCalculator.IsShortRange(source)`.
- [ ] **Land Protector** place-gate ➡️ moved to **COMBAT-47** (needs the `UF_NOLP`
      unit-flag, not modeled on `SkillUnitFlag` yet). The skill-attack intercept (melee/
      ranged SKILLS, which need the lane threaded into the skill funnel) is also COMBAT-47.

## Done criteria

- A target on Safety Wall takes 0 from a melee swing ✅; the pool decrements and the wall
  vanishes when exhausted ✅; ranged hits pass through ✅.
- A target on Pneuma takes 0 from a ranged hit ✅; melee passes through ✅.
- ➡️ A hostile ground-unit skill cannot place/tick on a Land Protector cell — moved to **COMBAT-47**.

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

## History

- **2026-06-02** — inprogress→done. Defensive ground-unit intercept: `DamageService.
  TryGroundUnitBlock` (called from `PerformMeleeAttack`) blocks a landed melee swing on a
  Safety Wall cell (consuming the new `SkillUnitGroup.Val2` block pool, removing the wall
  when spent) and a ranged swing on a Pneuma cell; the opposite lane passes through. Pool
  initialized in `SkillUnitService.Place` (2+2·lv). `Combat25GroundUnitBlockTests` (3);
  unit suite 3830 (1 fail = pre-existing INFRA-11 replay gate). Filed COMBAT-47 (Land
  Protector place-gate via UF_NOLP + the skill-path intercept).
