# COMBAT-101 — Drop-item bonus tables (bAddMonsterDropItem/bAddClassDropItem/...DropItemGroup)

> **Epic:** combat · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** COMBAT-83 · **Blocks:** none · **Filed by:** COMBAT-83 (drop tables are their own subsystem).

## Problem

`bonus3 bAddMonsterDropItem, iid, r, rate` / `bAddClassDropItem, iid, c, rate` / the `...DropItemGroup`
forms give the killer an extra chance to drop a specific item (or item-group) when killing a mob of a
given race/class. The live `ScriptedBonusHost.bonus3` silently skips them; there is no drop-bonus table
and the mob-death drop roll doesn't consult one. (7 stock items use bAddMonsterDropItem.)

## Current state (C#)

- `Map.Server/Inventory/EquipBonusBundle.cs` — no drop-bonus tables.
- `Map.Server/Items/ItemDropService.cs` (mob-death drop roll) — rolls only the mob_db drops.

## rAthena reference (source of truth)

- `pc.cpp` SP_ADD_MONSTER_DROP_ITEM / SP_ADD_CLASS_DROP_ITEM / *_GROUP arms; `mob.cpp mob_dead`
  the `sd->add_drop[]` roll appended to the mob's drop list.

## Scope

- [ ] Add an `add_drop` table (itemid/group, race/class key, rate) to the bundle + parse the bonus3
      forms in ScriptedBonusHost.
- [ ] On mob death, roll each matching add_drop entry and append the drop (killer/party as rAthena).

## Done criteria

- A bAddMonsterDropItem card adds the extra drop at its rate when the killer fells the matching race.

## Test plan

- A guaranteed-rate add_drop appends the bonus item on a matching-race kill.
