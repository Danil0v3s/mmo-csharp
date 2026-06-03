# COMBAT-109 — General bMagicAtkEle equip bonus (magic_atk_ele[skill element])

> **Epic:** combat · **Status:** ❌ Not started · **Size:** S · **Player-visible:** yes
> **Depends on:** COMBAT-87 · **Blocks:** none
> **Filed by:** COMBAT-87 — it added SC_BASILICA's Holy magic buff via a targeted SC read in
> CalcMagicAttack; the general equip-sourced `bMagicAtkEle` bonus has no array/consumer yet.

## Problem

rAthena `bonus2 bMagicAtkEle, e, n` (SP_MAGIC_ATK_ELE) → `indexed_bonus.magic_atk_ele[e] += n`, a
+n% bonus to magic damage of element `e` (the SKILL's element). Various cards/gear grant it (e.g.
+% Fire magic). The C# `EquipBonusBundle` has no `MagicAtkEle` array and `CalcMagicAttack` applies no
such bonus — only COMBAT-87's targeted SC_BASILICA[Holy] read exists.

## Current state (C#)

- `Map.Server/Inventory/EquipBonusBundle.cs` — no `MagicAtkEle` array.
- `Map.Server/Combat/BattleCalculator.cs:CalcMagicAttack` — applies SC_BASILICA[Holy] (COMBAT-87)
  but no equip `magic_atk_ele[atkEle]`.
- `Map.Server/Inventory/BonusScriptExtractor.cs` — no `bMagicAtkEle` parse.

## rAthena reference (source of truth)

- `pc.cpp` SP_MAGIC_ATK_ELE; `battle.cpp battle_calc_magic_attack` the `magic_atk_ele[ele]` rate.

## Scope

- [ ] Add `int[] MagicAtkEle` (element-sized) to `EquipBonusBundle` + Reset; parse `bonus2 bMagicAtkEle`.
- [ ] In `CalcMagicAttack`, add `magic_atk_ele[atkEle] + [ELE_ALL]` to the magic damage (keyed on the
      resolved skill element), alongside the COMBAT-87 SC read.

## Done criteria

- A `bMagicAtkEle, Ele_Fire, 20` card adds +20% to Fire-element magic; non-Fire magic is unaffected.

## Test plan

- Fire-element magic with the bonus → +20%; a Holy skill → unaffected.
