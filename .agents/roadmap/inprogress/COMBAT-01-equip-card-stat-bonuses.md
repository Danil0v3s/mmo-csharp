# COMBAT-01 — Equip / card flat-stat bonuses (bStr..bLuk + bMaxHP/bHit/bFlee/bCritical/bAspd)

> **Epic:** Combat parity · **Status:** 🚧 In progress · **Size:** L · **Player-visible:** yes
> **Depends on:** none · **Blocks:** COMBAT-06 (shares the consumer wiring)

## Problem

A `+10 STR` card, `+5 DEX` headgear, `+10% MaxHP` armor, `+30 HIT` weapon, etc. do
**nothing** in this server. Two independent gaps:

1. **Param bonuses (bStr/bAgi/bVit/bInt/bDex/bLuk + the trait stats) are dropped end
   to end.** `EquipBonusBundle` has no `Str/Agi/Vit/Int/Dex/Luk` fields,
   `BonusScriptExtractor.ApplyFlat` silently skips those keys
   (`BonusScriptExtractor.cs:121-123`: "Many other keys exist (bStr, bAgi, …): …
   Silently skip"), `EquipBonusAggregator` never sums them, and `EquipService.TryRecalcStats`
   feeds `CalcPc` only the character's base `stats.Str` etc. So the card never
   reaches the stat window or any derived formula.

2. **The bundle's existing flat fields have ZERO consumers in stat calc.**
   `FlatAtk/FlatHit/FlatCritical/FlatFlee/FlatAspd/FlatAspdRate/FlatMaxHp/MaxHpRate/
   FlatMaxSp/MaxSpRate` are populated by `BonusScriptExtractor.ApplyFlat`
   (`BonusScriptExtractor.cs:104-117`) but `StatusCalcService.CalcPc` resets
   `s.Hit=0; s.Flee=0; s.Cri=0; s.Batk=0;` and never adds them back
   (`StatusCalcService.cs:68-78`). `bHit/bFlee/bCritical/bAspd/bMaxHP/bAtk` are
   therefore no-ops too.

## Current state (C#)

- `Map.Server/Inventory/EquipBonusBundle.cs:38-94` — has `AddRace/SubRace/AddEle/…`
  arrays and `FlatAtk/FlatMatk/FlatCritical/FlatHit/FlatFlee/FlatAspd/FlatAspdRate/
  FlatMaxHp/FlatMaxSp/MaxHpRate/MaxSpRate/LongAtkRate/ShortAtkRate/CritAtkRate` —
  but **no `Str/Agi/Vit/Int/Dex/Luk/Pow/Sta/Wis/Spl/Con/Crt`** param fields.
- `Map.Server/Inventory/BonusScriptExtractor.cs:99-125` — `ApplyFlat` switch covers
  17 keys; no `str/agi/vit/int/dex/luk` cases.
- `Map.Server/Inventory/EquipBonusAggregator.cs:47-87` — `Aggregate` sums only
  `WeaponAtk/Def/Range/Element`; `BuildBundle` (`:121-157`) dispatches per-item
  hooks into the bundle but no aggregator step reads param bonuses into `PcBaseInputs`.
- `Map.Server/Inventory/EquipService.cs:244-256` — `TryRecalcStats` constructs
  `PcBaseInputs` from `stats.Str/Agi/…` (base values) only; the bundle is never
  consulted for stat deltas.
- `Map.Server/Status/StatusCalcService.cs:43-78` — `CalcPc` copies `inputs.Str` →
  `s.Str` etc., then **hard-zeroes** `Hit/Flee/Cri/Batk/Def2/Mdef2/Patk/…`. No path
  re-adds `bundle.FlatHit/FlatFlee/FlatCritical/FlatAtk/FlatMaxHp/MaxHpRate/FlatAspd`.
- `Map.Server/Status/IStatusCalcService.cs:68-94` — `PcBaseInputs` carries the
  primary stats but no slot for equip flat-stat / flat-derived bonuses.

## rAthena reference (source of truth)

Canonical source is the monolithic `status.cpp` / `pc.cpp` switch arms (the
`rathena-fork/src/map/skills/...` split paths in some docstrings do not exist here).

- `pc.cpp:3653-3661` `pc_bonus` `case SP_STR..SP_LUK`:
  `sd->indexed_bonus.param_bonus[type-SP_STR] += val;` (skipped only for arrow LR
  flag). Trait stats (`SP_POW..SP_CRT`) land in the same array region.
- `status.cpp:status_calc_pc_` (`status.cpp:4948`). At `:4044-4045` it snapshots
  `param_bonus` into `param_equip` then zeroes `param_bonus` for the card pass.
  Final stat is built at `:4244-4266`:
  `str = base_status->str + sd->status.str + param_bonus[PARAM_STR] + param_equip[PARAM_STR]`
  (and identically for agi/vit/int/dex/luk/pow/sta/wis/spl/con/crt). **These summed
  stats are what feed every downstream derive** (`status_calc_misc`, base_atk, matk…).
- Flat derived adds happen in the same function: `sd->battle_status.hit += sd->bonus.…`,
  `cri`, `flee`, `batk`, plus MaxHP `apply_rate(hp, sd->indexed_bonus.maxhp_rate)` /
  `+= sd->bonus.maxhp` (additive) and the equivalent SP path. ASPD bonus folds via
  `status_calc_aspd` (`status.cpp:8006`) and `aspd_rate` / `aspd_add`.

## Scope — every sub-system that must be touched

- [ ] **`EquipBonusBundle.cs`**: add `int Str/Agi/Vit/IntStat/Dex/Luk/Pow/Sta/Wis/Spl/Con/Crt`
      (mirror `indexed_bonus.param_bonus` PARAM_* order). Add them to `Reset()`.
- [ ] **`BonusScriptExtractor.cs` `ApplyFlat`**: add `case "str"/"agi"/"vit"/"int"/"dex"/
      "luk"/"pow"/"sta"/"wis"/"spl"/"con"/"crt"` writing the new bundle fields. (Keep
      additive `+=`.) These flow through `ScriptedBonusHost` automatically since it
      calls `ApplyFlatBonus`.
- [ ] **`IStatusCalcService.PcBaseInputs`**: add fields for the 12 param deltas **and**
      the flat-derived deltas the bundle already carries: `AddHit, AddFlee, AddCri,
      AddBatk(=FlatAtk), AddMaxHp, MaxHpRate, AddMaxSp, MaxSpRate, AddFlee(dup),
      AddAspd, AspdRate`. (Prefer one nested `EquipDerived` record to keep the ctor sane.)
- [ ] **`EquipService.TryRecalcStats`**: after `BuildBundle` + combos, read
      `player.EquipBonuses` and pass `Str: stats.Str + bundle.Str`, …, `Luk: stats.Luk +
      bundle.Luk` (and trait stats) **and** the flat-derived deltas into `PcBaseInputs`.
- [ ] **`StatusCalcService.CalcPc`**: the `inputs.Str` etc. already become `s.Str`; once
      EquipService adds the deltas there, the primary-stat path is correct. Then, after
      `CalcMisc`, add: `s.Hit += AddHit; s.Flee += AddFlee; s.Cri += AddCri*10` (note cri
      is stored ×10 — confirm card unit: a `bonus bCritical,N` is N display points = N×10
      internal, per the `FlatCritical` docstring "displayed in tenths"); `s.Batk += FlatAtk`;
      apply MaxHp: `maxHp = maxHp*(100+MaxHpRate)/100 + FlatMaxHp` then re-clamp HP/SP;
      `s.Amotion` reduced by `FlatAspd`/`AspdRate` (see COMBAT-09 for the full ASPD path —
      here just thread the fields; if COMBAT-09 not yet landed, apply
      `amotion = amotion*(100-AspdRate)/100 - FlatAspd*10` as the interim).
- [ ] **No DB migration** — all data comes from existing item scripts (regex/TS hooks).
- [ ] **No packet work** — `ZC_STATUS` / `ZC_PAR_CHANGE` already broadcast from the
      stat-window refresh path that runs after `CalcPc`; confirm `TryRecalcStats` callers
      trigger the stat push (they do for equip/unequip).

## Done criteria

- A card/headgear with `bonus bStr,10;` raises the displayed STR by 10 and raises
  `s.Batk` via the renewal `BaseAtk` formula (`StatusCalcService.cs:293-302`), measurably
  increasing auto-attack damage.
- `bonus bDex,10;` raises HIT by ~10 (renewal HIT includes `+ s.Dex`,
  `StatusCalcService.cs:247`) and ATK via the dex term.
- `bonus bHit,30;` adds exactly +30 to the HIT shown / used in `is_attack_hitting`.
- `bonus bMaxHPrate,10;` raises MaxHP by 10% of post-VIT base; `bonus bMaxHP,500;` adds 500.
- `bonus bCritical,20;` raises crit-rate roll target by 200 (×10 internal) → +20 display.
- `bonus bAspd,1;` / `bonus bAspdRate,5;` measurably lowers `s.Amotion`.
- No `// silently skip` comment remains for param keys in `BonusScriptExtractor`.

## Test plan

- `Map.Server.Tests` (or the equivalent combat test project): unit-test
  `BonusScriptExtractor.Apply("bonus bStr,10;", bundle)` asserts `bundle.Str==10`.
- Test `StatusCalcService.CalcPc` with a `PcBaseInputs` carrying `Str` base 1 + equip 10
  → `s.Str==11` and `s.Batk` increases vs base.
- Test the MaxHP rate+flat path: base, +10% rate, +500 flat → exact integer match.
- Test HIT/CRI/FLEE/ASPD deltas each in isolation against hand-computed renewal numbers.
- Regression: equip a STR card live, open stat window, confirm STR jumps and a mob hit
  deals more damage; unequip restores.

## Notes / gotchas

- rAthena keeps `param_bonus` (card) and `param_equip` (equip) **separate** only because
  some SCs read the card-only slot (`status.cpp:4751-4752` Concentration reads
  `param_bonus[1]/[4]`). Our bundle collapses both into one additive value, which is fine
  for stat derivation but means a future SC_CONCENTRATE port must read the equip-stat
  delta from somewhere — note it but don't split now.
- `FlatCritical` unit: the bundle docstring says stored ×1 but display is ×10. The renewal
  `s.Cri` internal is ×10 (`StatusCalcService.cs:284`). Decide one convention and apply
  consistently — recommend storing display value and multiplying by 10 at the CalcPc add.
- Trait stats (Pow/Sta/Wis/Spl/Con/Crt) feed `CalcMisc` Patk/Smatk/Res/Mres
  (`StatusCalcService.cs:259-264`); wiring them through is the same one-liner — do it now
  so 4th-job trait gear isn't a second pass.
