# COMBAT-21 — Advanced cardfix (debuff, ignore-def, magic/critical-add-race, per-category RE)

> **Epic:** Combat parity · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
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

- [ ] Add the bundle fields (after COMBAT-06's bonus2 parse): IgnoreDefRate,
      MagicAddRace, CriticalAddRace, SubDefEle, AddRace2/SubRace2.
- [ ] Fold per-element debuff from target SCs (gate on `_sc`).
- [ ] Apply ignore-def (zero the def subtract), magic-add-race (magic lane),
      critical-add-race (on crits; thread `isCritical`).
- [ ] Convert `CalcCardFix` to the per-category multiplicative `APPLY_CARDFIX_RE`
      grouping so stacked categories match rAthena.

## Done criteria

- A card stack with multiple categories matches rAthena's per-category multiplicative
  result (not the additive approximation).
- Ignore-def / magic-add-race / critical-add-race / element-debuff each verified.

## Test plan

- Per-category stack vs rAthena reference numbers.
- Each new sub-stage in isolation.
