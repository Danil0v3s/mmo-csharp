# COMBAT-98 — race2 cardfix: melee per-group multiply + pet race2

> **Epic:** combat · **Status:** ❌ Not started · **Size:** S · **Player-visible:** yes
> **Depends on:** COMBAT-81 (the race2 axis + cardfix folds) · **Blocks:** none
> **Filed by:** COMBAT-81 — it summed `AddRace2` across a mob's groups (the rAthena ranged/magic
> semantics) and handled mob targets only; two faithful edges remain.

## Problem

1. **Melee multiplies per group.** rAthena's weapon cardfix is split by range: ranged sums the race2
   values then applies one multiply (`cardfix *= (100 + Σ addrace2)/100`, battle.cpp:910); **melee**
   multiplies cardfix **once per group** (`for each r2: cardfix *= (100 + addrace2[r2])/100`,
   battle.cpp:936). COMBAT-81 sums in both cases (the C# `CalcCardFix` doesn't split melee/ranged for
   any weapon fold). For a mob in a SINGLE race2 group this is exact; for a mob in **2+** groups under
   a **melee** hit the result differs slightly (`Σ` vs `∏`).
2. **Pet race2.** rAthena `status_get_race2` also returns a PET's race2 (`pet_data->db->race2`); the
   C# `BattleCardService.SumRace2` returns 0 for non-mob entities, so a pet attacker/target gets no
   race2 fold.

## Current state (C#)

- `Map.Server/Combat/BattleCardService.cs:SumRace2` — mob-only; sums across groups, applied as one
  multiply (offensive weapon + magic + defensive).
- `Map.Server/Combat/BattleCardService.cs:CalcCardFix` — no melee/ranged split for the weapon folds
  (a pre-existing COMBAT-21 simplification that affects race/ele/size/class too, not just race2).

## rAthena reference (source of truth)

- `battle.cpp:906-910` (ranged: sum) vs `battle.cpp:935-936` (melee: per-group multiply).
- `status.cpp:9040 status_get_race2` (BL_MOB + BL_PET).

## Scope — every sub-system that must be touched

- [ ] When `CalcCardFix` gains the melee/ranged weapon-fold split (broader than race2), apply the
      race2 fold per-group-multiply for melee and summed for ranged/magic, matching rAthena.
- [ ] Extend the race2 set lookup to pets (a pet's db race2) if/when pets become combat participants.

## Done criteria

- A melee hit on a mob in 2+ race2 groups matches rAthena's per-group `∏(100+addrace2)/100`; ranged/
  magic keep the summed form; a pet's race2 folds like a mob's.

## Test plan

- A 2-group mob under melee vs ranged, asserting the `∏` vs `Σ` difference.

## Notes / gotchas

- This is an edge: most mobs carry 0–1 race2 groups (so COMBAT-81's sum is already exact for them).
  Bundle this with the general COMBAT-21 melee/ranged weapon-fold split rather than doing it race2-only.
