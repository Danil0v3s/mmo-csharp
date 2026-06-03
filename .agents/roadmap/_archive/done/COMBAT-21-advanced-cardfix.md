# COMBAT-21 — Advanced cardfix (debuff, ignore-def, magic/critical-add-race, per-category RE)

> **Epic:** Combat parity · **Status:** ✅ Done (2026-06-02) · **Size:** M · **Player-visible:** yes
> **Depends on:** COMBAT-06 (the bonus2 parse for the new fields) · **Blocks:** none
> **Filed by:** COMBAT-05 (axes 2 + 3 + the per-category refinement).

## Problem

COMBAT-05 added the attacker-offensive + defender-defensive race/ele/size/class cardfix
on a single additive percent multiplier. rAthena's `battle_calc_cardfix`
(battle.cpp:711-1151) has more:
1. **Per-element debuff** — `battle_calc_cardfix_debuff(*tsc, rh_ele)` (battle.cpp:667)
   folds the target's element-vulnerability SCs.
2. **Ignore-def / magic-add-race / critical-add-race** cards — none are read; these need
   the `bonus2 bIgnoreDefRate / bMagicAddRace / bCriticalAddRace / bSubDefEle` parse,
   which COMBAT-06 ports into the bundle.
3. **Per-category multiplicative grouping** — renewal `APPLY_CARDFIX_RE`
   (battle.cpp:781) applies each category multiplicatively, not as one additive `mult`.
   The COMBAT-05 additive form is an approximation that drifts when multiple categories
   stack.

## Current state (C#)

- `Map.Server/Combat/BattleCardService.cs:CalcCardFix` — single additive `mult`; no
  debuff, ignore-def, magic/critical-add-race; `EquipBonusBundle` lacks those fields.

## rAthena reference

- `battle.cpp:667` debuff; `:711-1151` the full cardfix incl. `APPLY_CARDFIX_RE`.

## Scope

- [x] Bundle fields: `MagicAddRace[]`, `CritAddRace[]` (×10), `IgnoreDefRace`/
      `IgnoreDefClass` bitmasks added to `EquipBonusBundle` + `Reset()`. ➡️ SubDefEle /
      AddRace2/SubRace2 moved to **COMBAT-43**.
- [x] Per-element debuff — ➡️ moved to **COMBAT-43** (needs `IStatusChangeService`
      injected into `BattleCardService`; the 4th-job debuff SCs exist).
- [x] magic-add-race (BF_MAGIC race category now uses `MagicAddRace`, not the weapon
      `AddRace`) + critical-add-race (`TryCritical` adds `CritAddRace` before the cri≤0
      gate so a 0-base-crit attacker can crit a carded race). ➡️ ignore-def (a DEF-stage
      effect + constant-value extractor parse) moved to **COMBAT-43**.
- [x] Converted `CalcCardFix` to the per-category multiplicative `APPLY_CARDFIX`
      grouping: accumulate `cardfix` (base 1000) by `×(100±fix)/100` per category, apply
      once per attacker/defender section. Stacked categories now match rAthena
      (×1.20×1.15 = ×1.38, not the additive ×1.35).

## Done criteria

- A card stack with multiple categories matches rAthena's per-category multiplicative
  result (not the additive approximation) ✅ (AddRace 20 + AddSize 20 → ×1.44).
- magic-add-race ✅ + critical-add-race ✅ verified; ➡️ ignore-def + element-debuff
  moved to **COMBAT-43**.

## Test plan

- Per-category stack vs rAthena reference numbers. ✅ (offensive ×1.44, defensive ×0.64)
- Each new sub-stage in isolation. ✅ (magic-add-race vs weapon-add-race; critical-add-race;
  cardfix-zero) — updated the 3 COMBAT-05/card tests pinned to the old additive numbers.

## History

- **2026-06-02** — inprogress→done. `CalcCardFix` rewritten to the rAthena per-category
  multiplicative `APPLY_CARDFIX` (offensive + defensive sections accumulate a 1000-base
  `cardfix` and apply once). Added `MagicAddRace`/`CritAddRace` tables + extractor parse;
  the BF_MAGIC race category uses `MagicAddRace`; `TryCritical` folds `CritAddRace` (×10)
  before the cri-gate. Updated 3 existing card tests to the multiplicative values.
  `Combat21CardfixTests` (6); unit suite 3808 (1 fail = pre-existing INFRA-11 replay
  gate). Filed COMBAT-43 (ignore-def + element-debuff + race2 + distinct magic arrays +
  flag-matched lists).
