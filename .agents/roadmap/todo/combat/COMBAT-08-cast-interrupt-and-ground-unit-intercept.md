# COMBAT-08 — Damage-driven cast interrupt + ground-unit damage intercept

> **Epic:** Combat parity · **Status:** ❌ Not started · **Size:** L · **Player-visible:** yes
> **Depends on:** COMBAT-06 (bNoCastCancel flag) · **Blocks:** none

## Problem

Two related gaps in the damage→world interaction:

1. **Taking damage never interrupts a cast.** In rAthena, when a casting entity is hit and the
   skill has `CastCancel` set, the cast is aborted. `DamageService.ApplyResolved` never calls
   `ISkillCastService.CancelCast`, never checks `skill_db.CastCancel`, and never checks for a
   `bNoCastCancel` / `SC_*` no-cancel state. So a wizard finishes Storm Gust no matter how hard
   it's being beaten. The client-facing cancel packet is a log stub: `SkillClientService.
   BroadcastSkillCastCancel` only `LogDebug`s (`SkillClientService.cs:88-95`).
2. **Defensive ground units don't intercept damage.** Safety Wall (block melee), Pneuma
   (block ranged), Land Protector (suppress hostile ground units) have **no** damage-path
   check. `DamageService` / `BattleCalculator` never query `ISkillUnitService` for units on the
   target's cell before applying a hit. `ISkillUnitService.GetUnitsInArea` exists
   (`ISkillUnitService.cs:84-95`) but no combat code calls it.
3. **`CastEndMap` (Teleport / Greed / warp) returns false** unconditionally
   (`SkillCastEndService.cs:71-80`) — the warp branch is deferred.

## Current state (C#)

- `Map.Server/Combat/DamageService.cs:147-257` `ApplyResolved` — applies SC reductions, HP
  delta, death; **no cast-cancel call**, **no `IsCasting`/`CastCancel` check**.
- `Map.Server/Skills/SkillCastService.cs:488-498` `CancelCast(EntityId)` — removes pending
  casts; returns true if any dropped. Comment notes it does NOT broadcast (caller should).
- `Map.Server/Skills/SkillCastService.cs:500-512` `IsCasting` / `GetCurrentCast` — exist and
  usable to find what's being cast.
- `Map.Server/Skills/SkillDb.cs:93-94, 358` — `GetCastCancel(skillId)` returns the skill's
  `CastCancel` flag (defaults true). Available; unused by the damage path.
- `Map.Server/Skills/SkillClientService.cs:88-95` `BroadcastSkillCastCancel` — log-only stub;
  notes the wire packet is `ZC_DISPEL (0x01b9)` (clif.cpp:5973).
- `Map.Server/Skills/ISkillUnitService.cs:84-95` — `GetUnitsInArea(map,cx,cy,radius[,skillId])`
  enumerates ground units on a cell. No combat consumer.
- `Map.Server/Skills/SkillCastEndService.cs:71-80` `CastEndMap` — returns false; warp deferred.
- `Map.Server/Entities/PlayerEntity.cs` — no casting-state field; casting state lives in
  `SkillCastService._pending` (queried via `IsCasting`).

## rAthena reference (source of truth)

Canonical: `status.cpp`, `unit.cpp`, `battle.cpp`, `skill.cpp` (not split files).

- `status.cpp:1550` (`status_fix_damage` / the damage commit path) — after applying HP loss
  to a target that survived: `unit_skillcastcancel(target, 2);` (also clears
  `pc_bonus_script BSF_REM_ON_DAMAGED`). On death (`:1704`): `unit_skillcastcancel(target,0)`.
  The `2` flag form is the "damage interrupt" variant.
- `unit.cpp:3107` `unit_skillcastcancel(bl, type)` — checks `skill_get_castcancel(skill_id)`;
  honors `sc->getSCE(SC_*)` no-cancel states (e.g. `SC_BASILICA`, `bNoCastCancel`); if the
  cast may be cancelled, stops the cast timer and calls `clif_skillcastcancel(*bl)`
  (`clif.cpp:5973`) to clear the client bar.
- Defensive units: `skill_unit_onplace` / the damage path consult cell units. Safety Wall
  (`MG_SAFETYWALL`) intercepts melee (`BF_SHORT`) hits and decrements its HP/hit pool; Pneuma
  (`AL_PNEUMA`) blocks ranged (`BF_LONG`); Land Protector (`SA_LANDPROTECTOR`) prevents hostile
  ground-unit placement/effect on its cells. These are checked in `battle_calc_damage` /
  `skill_attack` against `map_find_skill_unit_oncell` (the rAthena analogue of
  `GetUnitsInArea(..., skillId)`).

## Scope — every sub-system that must be touched

- [ ] **Cast-interrupt on damage.** In `DamageService.ApplyResolved`, after the surviving-hit
      branch (`:220` region, when `actual > 0` and target still alive): resolve
      `ISkillCastService` (via `_services` to avoid the DI cycle, like `MobAi` at
      `DamageService.cs:30-32`). If `castSvc.IsCasting(target.Id)`: get the current skill
      (`GetCurrentCast`), and if `skillDb.GetCastCancel(skillId)` is true **and** the target
      has no no-cancel state (`bundle` `NoCastCancel` flag from COMBAT-06, or
      `SC_*` like Basilica), call `castSvc.CancelCast(target.Id)` and
      `skillClient.BroadcastSkillCastCancel(target)`. On death (`HandleDeath`), also cancel
      unconditionally.
- [ ] **Implement `BroadcastSkillCastCancel`** (`SkillClientService.cs:88`): emit the real
      `ZC_DISPEL`/cast-cancel packet to the caster's AOI (replace the `LogDebug`). Add the
      packet definition under `Core.Server/Packets/Out/ZC` if absent (id `0x01b9`), and
      broadcast via the visibility service the way other ZC broadcasts do.
- [ ] **No-cancel gate.** Add the `NoCastCancel` bool to `EquipBonusBundle` (COMBAT-06 parses
      `bonus bNoCastCancel;`) and check it here; also check the relevant SCs (Basilica /
      Free Cast / Steel Body-style) before cancelling.
- [ ] **Ground-unit damage intercept.** In the melee/ranged apply path
      (`DamageService.PerformMeleeAttack` / `BattleCalculator.CalcWeaponAttack`, and the
      ranged/skill paths), before committing damage query `ISkillUnitService.GetUnitsInArea(
      target.MapId, target.X, target.Y, 0)`:
  - **Safety Wall** on the target's cell + the hit is melee (`BF_SHORT`) → block (damage 0,
    decrement the unit's HP/hit pool via `UnitOnDamaged`/group bookkeeping; delete when spent).
  - **Pneuma** on the cell + ranged (`BF_LONG`) → block.
  - **Land Protector**: hostile ground-unit skills shouldn't place/tick on its cells (this is
    primarily a `SkillUnitService.Place` gate, but note it here so the overlap check is
    consistent). Use the `skillId`-filtered overload (`ISkillUnitService.cs:94`).
  Thread the attack range/lane (`BF_SHORT`/`BF_LONG`) into the query — `BattleDamage` /
  `BattleAttackType` + `AttackRange` already distinguish melee vs ranged.
- [ ] **`CastEndMap` warp** (`SkillCastEndService.cs:71`): implement Teleport / Greed / warp via
      the player-warp service (`IPlayerWarpService` / `pc_setpos` analogue). Return true on
      success. (If `IPlayerWarpService` doesn't exist yet, this sub-item may split to its own
      ticket — but it must not stay `return false`.)
- [ ] **No DB migration.** Packet addition (`ZC_DISPEl`/cast-cancel) is the only wire change.

## Done criteria

- A casting wizard hit for >0 damage by a cancellable-on-hit skill has the cast aborted and the
  client cast bar cleared (real packet emitted, not a log line). A `bNoCastCancel` caster (or
  Basilica) is NOT interrupted.
- A target standing on Safety Wall takes 0 from a melee swing (and the wall's hit pool
  decrements; the wall vanishes when exhausted). Ranged hits pass through Safety Wall.
- A target on Pneuma takes 0 from a ranged hit; melee passes through.
- `CastEndMap` for Teleport relocates the caster (no longer `return false`).

## Test plan

- Unit-test the interrupt: target casting (seed `SkillCastService._pending`), apply damage,
  assert `IsCasting` becomes false and `BroadcastSkillCastCancel` was invoked; with
  `NoCastCancel` set, assert the cast survives.
- Unit-test on-death cancel.
- Unit-test ground intercept: place Safety Wall on a cell, melee a target there → 0 damage +
  pool decrement; ranged → full damage. Pneuma symmetric.
- Packet test: `BroadcastSkillCastCancel` produces a `ZC_DISPEl`/cast-cancel `OutgoingPacket`
  to the AOI.
- Manual: cast a long spell, get hit, watch the bar abort; stand on Safety Wall vs a melee mob.

## Notes / gotchas

- Resolve `ISkillCastService` / `ISkillClientService` / `ISkillUnitService` lazily through
  `_services` (`IServiceProvider`) in `DamageService` — these form registration cycles with
  the damage/attack pipeline (see the existing `MobAi` lazy resolve, `DamageService.cs:29-32`).
- `unit_skillcastcancel` flag `2` in rAthena is the damage-interrupt variant (vs `0` general,
  `1` walk). Our `CancelCast` takes no flag — that's fine; just ensure the
  `GetCastCancel`/no-cancel gate replicates the flag-2 semantics (some skills are
  uninterruptible-by-damage but cancellable by other means).
- Safety Wall / Pneuma pools: confirm `SkillUnitGroup` tracks remaining HP/hit count
  (`SkillUnitGroup.cs`). If it only tracks a tick lifetime, add the consumable pool so the wall
  breaks after N blocks (rAthena: Safety Wall blocks `val2` hits).
- Don't block PvE-friendly hits incorrectly — Land Protector affects ground-unit *skills*, not
  direct attacks; keep its scope to `SkillUnitService.Place`.
