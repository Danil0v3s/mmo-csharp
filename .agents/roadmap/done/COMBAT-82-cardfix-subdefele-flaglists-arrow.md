# COMBAT-82 — Cardfix remainder: SubDefEle + magic_subsize + flag-matched subele2/subrace3 + arrow arrays

> **Epic:** Combat parity · **Status:** ✅ Done (2026-06-03) · **Size:** M · **Player-visible:** yes
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

- [x] Add `MagicSubDefEle`/`MagicSubSize`/`ArrowAddRace`/`ArrowAddEle` arrays + the flag-matched
      `SubEle2`/`SubRace3` lists (ele/race + BF_* flag + rate) to `EquipBonusBundle` + reset.
      (`subele_script` needs NO new array — the C# already routes `bonus2 bSubEle` → `SubEle`, which
      rAthena sums with `subele`, battle.cpp:818.)
- [x] Parse the `bonus2` forms (`bMagicSubDefEle`/`bMagicSubSize`/`bArrowAddEle`/`bArrowAddRace`) +
      the `bonus3 bSubEle/bSubRace, …, bf` flag-matched forms (via `ScriptedBonusHost.bonus3` →
      `ApplyFlagMatchedBonus3`, with the rAthena `pc_bonus_subele` flag defaulting in `BattleFlags`).
- [x] Fold each into `CalcCardFix`: magic defense adds `MagicSubDefEle` (attacker def-ele) +
      `MagicSubSize`; the flag-matched lists gate on the derived attack BF_* flag; arrow arrays gate
      on a ranged swing.

## Done criteria

- ✅ a `bMagicSubDefEle` card reduces magic from an attacker of that element (1000→800); `bMagicSubSize`
  reduces magic by size (→800); a flag-matched `bonus3 bSubEle, Ele, BF_LONG, n` reduces only long
  attacks (→800 long / 1000 short); `bArrowAddEle` applies only on a ranged swing (→1200 / 1000).
- ➡️ The skill/normal flag discriminator + a skill's range type are **moved to COMBAT-99** (the BF
  flag is derived from lane + attacker range, skillmask = both — exact for auto-attacks; the
  skill-flag threading is the precision tail).

## Test plan

- SubDefEle (magic), magic_subsize, flag-matched subele2 (BF_LONG only), arrow_addele numeric
  tests.

## Notes / gotchas

- The flag-matched lists need the incoming damage's BF_* flags threaded into `CalcCardFix`
  (currently it only knows the lane); coordinate the signature change with the auto-attack +
  skill funnels.

## History

- 2026-06-03 — Added `EquipBonusBundle.MagicSubDefEle/MagicSubSize/ArrowAddRace/ArrowAddEle` + the
  flag-matched `SubEle2`/`SubRace3` lists (+ Reset); the `bonus2 bMagicSubDefEle/bMagicSubSize/
  bArrowAddEle/bArrowAddRace` parse + the `bonus3 bSubEle/bSubRace, …, bf` flag-matched parse
  (`ScriptedBonusHost.bonus3` → `BonusScriptExtractor.ApplyFlagMatchedBonus3`); a shared `BattleFlags`
  (BF_* consts + the `pc_bonus_subele` defaulting + the cardfix triple-mask `Matches`). Folded into
  `CalcCardFix`: ranged-only arrow arrays (offensive), `MagicSubDefEle` (attacker def-ele) + `MagicSubSize`
  on the magic defensive path, and the flag-matched ele/race lists gated on the derived attack flag.
  `subele_script` needed no work (already covered by the C# `SubEle`). Combat82CardfixRemainderTests
  (4); full suite 4157 pass (1 fail = pre-existing INFRA-11 replay gate). Filed COMBAT-99 (thread the
  real BF skill/normal + skill-range flag into CalcCardFix — the precision tail beyond auto-attacks).
