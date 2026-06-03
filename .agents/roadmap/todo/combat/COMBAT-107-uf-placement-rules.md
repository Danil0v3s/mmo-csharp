# COMBAT-107 — Remaining UF_* placement rules (NoOverlap / PathCheck / NoFootSet)

> **Epic:** combat · **Status:** ❌ Not started · **Size:** S · **Player-visible:** yes
> **Depends on:** COMBAT-85 · **Blocks:** none
> **Filed by:** COMBAT-85 — it loaded the real Unit.Flag set + wired the UF_NOREITERATION place-gate;
> the other placement-relevant flags are loaded but not yet consumed.

## Problem

`SkillUnitService.Place` now reads `UnitFlags` (COMBAT-85) and consumes UF_NOREITERATION. Three more
placement flags are loaded but unconsumed:
- **UF_NOFOOTSET** (skill.cpp:493): cannot place where a unit of this skill is on the caster's/target's
  foot cell (`skill_check_unit_range2`).
- **UF_NOOVERLAP** (skill.cpp:22223): the unit's effect cells don't overlap an existing same-skill unit
  (placement layout trims overlapping cells).
- **UF_PATHCHECK**: only cells with a shootable path from the center are placed (LoS per cell).

## Current state (C#)

- `Map.Server/Skills/SkillUnitService.cs:Place` — consumes UF_NOREITERATION only; the layout loop
  (`BuildSquareFallback` / `ISkillLayoutService`) adds every cell with no overlap/path trim.

## rAthena reference (source of truth)

- `skill.cpp:493` (NoFootSet), `skill.cpp:22223` (NoOverlap), the per-cell `path_search_long` for
  PathCheck in `skill_unitsetting`.

## Scope

- [ ] UF_NOFOOTSET place-gate (refuse if a same-skill unit sits on the foot cell).
- [ ] UF_NOOVERLAP: when placing, skip/clear cells that overlap an existing same-skill unit.
- [ ] UF_PATHCHECK: drop layout cells with no shootable path from the center.

## Done criteria

- Each flag changes placement per rAthena for a skill that carries it (Sanctuary PathCheck, Warp
  NoOverlap/NoFootSet, …).

## Test plan

- Per-flag placement test using the COMBAT-85 overlay data.
