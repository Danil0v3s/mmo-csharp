# COMBAT-05 — Defensive cardfix, element resolution, plant/GvG/BG reductions

> **Epic:** Combat parity · **Status:** 🚧 In progress · **Size:** XL · **Player-visible:** yes
> **Depends on:** COMBAT-01 (defender's bundle must be populated) · **Blocks:** none

## Problem

The cardfix / element / final-reduction stages only model the *attacker's* offensive
percent cards, and only when the attacker is a PC. Concretely:

1. **Mobs hitting a player ignore the player's resist cards.** `BattleCardService.CalcCardFix`
   returns `damage` unchanged when `src is not PlayerEntity`
   (`BattleCardService.cs:48`). A player wearing `bonus2 bSubRace,RC_Brute,30;` takes full
   damage from a brute mob.
2. **Target-side defensive cardfix is entirely missing.** `SubRace/SubEle/SubSize/SubClass`
   on the *defender's* bundle are never read (the `SubRace[]` etc. arrays exist on
   `EquipBonusBundle` but `CalcCardFix` only reads the attacker's `AddRace/AddEle/AddSize/
   AddClass`, `BattleCardService.cs:59-85`).
3. **Element resolution is approximate.** Magic/misc always use the caster's weapon element
   (`BattleCalculator.cs:328, 368`) instead of the skill's declared element;
   `battle_calc_element_damage` is not ported.
4. **No plant / GvG / BG stages.** `battle_calc_attack_plant` (1-damage to plant-type),
   `battle_calc_attack_gvg_bg` (WoE/BG % reductions) are absent.
5. Missing cardfix sub-stages: per-element debuff (`battle_calc_cardfix_debuff`), ignore-def
   cards, `bMagicAddRace`, `bCriticalAddRace`, sub-defele.

## Current state (C#)

- `Map.Server/Combat/BattleCardService.cs:41-89` — `CalcCardFix`: early-returns when src
  is not a PC; sums only attacker `AddRace/AddEle/AddSize/AddClass` + `Long/ShortAtkRate`
  into one `mult` and returns `damage * mult / 100`. No defender-side read, no ignore-def,
  no debuff, no critical/magic-add-race.
- `Map.Server/Combat/BattleCalculator.cs:89-91` — weapon element uses `s.WeaponElement`.
- `Map.Server/Combat/BattleCalculator.cs:326-329` — magic element: "for now we use the
  caster's weapon element (same as weapon path)".
- `Map.Server/Combat/BattleCalculator.cs:368-369` — misc element same.
- `Map.Server/Combat/BattleCalculator.cs:116-117` — def reduction; no plant 1-damage branch,
  no GvG/BG stage.
- `Map.Server/Combat/DamageService.cs` — applies SC reductions (`ApplyScDamageReduction`,
  `:268`) but no map-flag GvG/BG % reduction.

## rAthena reference (source of truth)

Canonical: `battle.cpp`.

- `battle.cpp:711` `battle_calc_cardfix(attack_type, src, target, nk, rh_ele, lh_ele, damage,
  left, flag)` — applies **both** the attacker's `addrace/addele/addsize/addclass/addrace2`
  AND the **defender's** `subrace/subele/subsize/subclass/subdefele/sub_race2`. Renewal uses
  the `APPLY_CARDFIX_RE` macro (`battle.cpp:781`). It also folds
  `battle_calc_cardfix_debuff(*tsc, rh_ele)` (`battle.cpp:667`) — per-element debuff from the
  target's SCs (e.g. element-vuln statuses). Magic path adds `magic_addrace`; critical adds
  `critical_addrace`; ignore-def cards zero the def subtract. Called for every lane
  (`BF_WEAPON/BF_MAGIC/BF_MISC`).
- `battle.cpp:3781` `battle_calc_element_damage(wd, src, target, skill_id, skill_lv)` —
  resolves the **attack element** from the skill's declared element (`skill_get_ele`), the
  caster's weapon element, and SC overrides (e.g. Pyroclastic, endow). This is what magic/misc
  must use instead of blindly taking `s.WeaponElement`.
- `battle.cpp:7074` `battle_calc_attack_plant(wd, src, target, skill_id, skill_lv)` — when the
  target is plant-type / has `NK_NODAMAGE`-ish plant immunity, damage is clamped to 1 (unless
  the skill ignores it).
- `battle.cpp:7225` `battle_calc_attack_gvg_bg(wd, src, target, skill_id, skill_lv)` — on GvG
  maps reduces skill/normal damage (e.g. skill `* battle_config.gvg_damage_rate / 100`,
  normal `gvg_long`/`gvg_weapon`/`gvg_magic`), and the BG equivalent on battleground maps.

## Scope — every sub-system that must be touched

- [ ] **`BattleCardService.CalcCardFix` — defender side.** Remove the `src is not PC`
      early-return. Split into two contributions: (a) attacker offensive (existing, only when
      `src` is PC and has a bundle); (b) **defender defensive** (when `target` is PC and has a
      bundle): subtract `bundle.SubRace[srcRace] + SubRace[All]`, `SubEle[srcAtkEle] +
      SubEle[All]`, `SubSize[srcSize]`, `SubClass[srcClass]` from `mult`. The attack element
      and attacker race/size/class must be passed in (extend the method signature or read from
      `src.Stats`).
- [ ] **Per-element debuff** (`battle_calc_cardfix_debuff`): port the target-SC element
      vulnerability fold; gate on `_sc` availability.
- [ ] **Ignore-def / magic-add-race / critical-add-race**: add the bundle fields (COMBAT-06
      ports the `bonus2` parse) and read them here; ignore-def zeroes the def subtract in
      `BattleCalculator` (thread a flag back, or compute def inside cardfix as rAthena does).
- [ ] **Element resolution** (`BattleCalculator.CalcMagicAttack`/`CalcMiscAttack`): take the
      skill's declared element from `ISkillDb` (add `GetElement(skillId, lvl)` if absent) and
      use it in `ElementTable.GetRate` instead of `s.WeaponElement`; honor SC element
      overrides (Pyroclastic already noted at `BattleCalculator.cs:177`). Weapon path: keep
      `s.WeaponElement` but allow endow SC override.
- [ ] **Plant 1-damage** (`BattleCalculator` def stage): when target is plant-type and the
      skill doesn't ignore it, clamp final damage to 1 (after element, before min-damage).
- [ ] **GvG / BG reduction**: add a stage (in `DamageService.ApplyResolved` or
      `BattleCalculator`) that, when the source map has `MapFlag.Gvg` / `MapFlag.Battleground`,
      multiplies by the configured rate. `DamageService` already resolves map flags
      (`DamageService.cs:136-143`).
- [ ] **No DB migration** (uses item scripts + map flags + skill_db element column). Add
      `ISkillDb.GetElement` loader if the column isn't surfaced.

## Done criteria

- A player wearing `bonus2 bSubRace,RC_Brute,30;` takes 30% less from a brute mob (mob→PC
  path now applies defender cards).
- `bonus2 bSubEle,Ele_Fire,50;` halves incoming fire damage regardless of attacker type.
- A Fire Bolt resolves as Fire element from the skill_db, not the caster's weapon element;
  hitting a Fire-armor mob deals reduced/absorbed damage per `ElementTable`.
- Hitting a plant-type mob with a non-ignoring skill deals exactly 1.
- On a GvG map, a skill deals `battle_config.gvg_damage_rate%` of its non-GvG value.

## Test plan

- Unit-test mob→PC cardfix: brute mob vs PC with `SubRace[Brute]=30` → 0.70× damage.
- Unit-test `SubEle`/`SubSize`/`SubClass` defender reductions in isolation.
- Unit-test element resolution: Fire Bolt vs Water/Fire/Neutral defenders → table rates.
- Unit-test plant clamp = 1; ignoring skill bypasses.
- Unit-test GvG rate multiply with the map flag set vs unset.
- Manual: tank a brute MVP with/without a Raydric-style resist card; confirm the delta.

## Notes / gotchas

- rAthena's renewal cardfix is **multiplicative per category** in places
  (`APPLY_CARDFIX_RE`), not a single additive `mult`. The current C# `mult += …; damage *
  mult/100` is a simplification. Match rAthena's category grouping (race fix applied, then
  element fix, etc.) to avoid off-by-percent errors when multiple categories stack — read
  `battle.cpp:781-1151` carefully and replicate the application order.
- The method signature must grow to carry attacker element + attacker race/size/class for the
  defender-side lookup. Today it only has `attackType, src, target, damage, leftHand`. Add the
  attacker stats from `src.Stats` inside the method rather than widening every call site.
- Critical hits ignore some defender cards in rAthena (`NK_` / crit-ignore-def). Thread the
  `isCritical` flag if you implement crit-ignore-def here.
