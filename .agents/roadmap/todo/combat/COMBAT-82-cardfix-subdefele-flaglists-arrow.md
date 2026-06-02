# COMBAT-82 — Cardfix remainder: SubDefEle + magic_subsize + flag-matched subele2/subrace3 + arrow arrays

> **Epic:** Combat parity · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** COMBAT-63 · **Blocks:** none
> **Filed by:** COMBAT-63 — the defensive/flag-matched/arrow cardfix arrays, each a distinct
> new bonus surface beyond the element-debuff + distinct-magic-array work that landed there.

## Problem

`battle_calc_cardfix` reads several more arrays the C# port does not model:

1. **`subele_script` / `magic_subdefele`** — script-sourced element resist distinct from the
   card `subele`, and a magic-only defense-element resist (`SubDefEle`).
2. **`magic_subsize`** — magic-only per-size defensive reduction (weapon uses `subsize`).
3. **Flag-matched `subele2` / `subrace3` lists** — `bonus3 bSubEle`/`bSubRace` forms that gate
   on a battle flag (BF_SHORT/BF_LONG/BF_WEAPON/etc.) before applying.
4. **Arrow-specific `arrow_addrace` / `arrow_addele`** — bonuses that apply only on ranged
   (arrow) attacks.

## Current state (C#)

- `Map.Server/Combat/BattleCardService.cs:CalcCardFix` — defensive branch folds
  `SubEle`/`SubSize`/`SubRace`/`SubClass` only; no `subele_script`, no `SubDefEle`, no
  `magic_subsize`, no flag-matched lists, no arrow arrays.
- `Map.Server/Inventory/EquipBonusBundle.cs` — none of these arrays/lists exist.
- `Map.Server/Inventory/BonusScriptExtractor.cs` — no `bSubDefEle`/`bMagicSubSize`/the
  `bonus3` flag-matched forms / arrow keys.

## rAthena reference (source of truth)

- `battle.cpp:711-1151` `battle_calc_cardfix` — `subele_script`, `magic_subdefele`,
  `magic_subsize`, the `subele2`/`subrace3` `for`-loops (flag match), `arrow_addele`/
  `arrow_addrace`.
- `pc.cpp` SP_SUBDEFELE / SP_MAGIC_SUBSIZE / SP_SUBELE / SP_SUBRACE (bonus3) / SP_ADD_DAMAGE_BY_*
  parse arms for the script-side keys.

## Scope — every sub-system that must be touched

- [ ] Add `SubEleScript`/`SubDefEle`/`MagicSubSize` arrays + the flag-matched `subele2`/
      `subrace3` lists (each entry: index + flag mask + value) + arrow `AddRace`/`AddEle` to
      `EquipBonusBundle` + reset.
- [ ] Parse the corresponding `bonus2`/`bonus3` forms in `BonusScriptExtractor`.
- [ ] Fold each into `CalcCardFix` at the correct spot (defensive ele section reads
      subele_script; magic defense reads SubDefEle + magic_subsize; the flag-matched lists
      gate on the damage flag; arrow arrays gate on a ranged/arrow swing).

## Done criteria

- ➡️ from COMBAT-63: a `bSubDefEle` card reduces magic of that element; `magic_subsize`
  reduces magic by size; a flag-matched `bonus3 bSubEle, Ele, BF_LONG, n` reduces only long
  attacks; `arrow_addele` applies only on an arrow swing.

## Test plan

- SubDefEle (magic), magic_subsize, flag-matched subele2 (BF_LONG only), arrow_addele numeric
  tests.

## Notes / gotchas

- The flag-matched lists need the incoming damage's BF_* flags threaded into `CalcCardFix`
  (currently it only knows the lane); coordinate the signature change with the auto-attack +
  skill funnels.
