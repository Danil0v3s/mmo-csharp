# COMBAT-85 — Generic skill_db UnitFlags column loader (+ SkillUnitFlag bit-order fix)

> **Epic:** Combat parity · **Status:** ✅ Done (2026-06-03) · **Size:** M · **Player-visible:** no (infra)
> **Depends on:** COMBAT-66 · **Blocks:** none
> **Filed by:** COMBAT-66 — it corrected the Land Protector exemption to use
> `INF2_IGNORELANDPROTECTOR` (the real rAthena flag), so the generic `Unit.Flag` loader is no
> longer needed for the LP gate and has **no current consumer**. It is filed here for when a
> future feature needs the real UF_* flags.

## Problem

`SkillDefinition.UnitFlags` is never populated from skill_db (`SkillDbLoader.FromEntity` does not
read a unit-flag column, and `SkillDbEntity` has no such column). Today the **only** caller of
`GetUnitFlag` was the COMBAT-47 Land Protector gate, which COMBAT-66 migrated to
`GetInf2(IgnoreLandProtector)` — so `GetUnitFlag` now has **zero** live consumers. Loading the
real `Unit.Flag` bitmask (UF_NOENEMY/UF_NOOVERLAP/UF_PATHCHECK/…) is dormant infra until a
ground-unit feature needs it (overlap/path/reiteration placement rules).

Additionally, the existing `SkillUnitFlag` enum is **bit-misaligned** with rAthena's
`e_skill_unit_flag`: it omits `UF_KNOCKBACKGROUP` (rAthena value 17), so `HiddenTrap`/the higher
bits sit one position off from `rAthena UF_value - 1`. A loader that decodes the rAthena bitmask
must fix this mapping first.

## Current state (C#)

- `Map.Server/Skills/SkillDefinition.cs:SkillUnitFlag` — `None`..`HiddenTrap` + the now-unused
  `NoLandProtector` (COMBAT-47 placeholder; superseded by `SkillInf2.IgnoreLandProtector`). Bit
  order diverges from rAthena at `KnockbackGroup`.
- `Map.Server/Skills/SkillDbLoader.cs:FromEntity` — no UnitFlags decode.
- `Core.Database/Entities/SkillDbEntity.cs` — no unit-flag column.
- `Map.Server/Skills/SkillDb.cs:GetUnitFlag` — works, but every skill returns `None`.

## rAthena reference (source of truth)

- `skill.hpp` `enum e_skill_unit_flag` (UF_NONE..UF_HIDDENTRAP, incl. UF_KNOCKBACKGROUP=17).
- `db/re/skill_db.yml` `Unit: { Flag: { NoEnemy: true, ... } }` per skill.

## Scope — every sub-system that must be touched

- [x] Re-aligned `SkillUnitFlag` to the rAthena `e_skill_unit_flag` order: added `KnockbackGroup`
      (1<<16), shifted `HiddenTrap` to 1<<17, renamed `Removed`→`RemovedByFireRain`, and **removed**
      `NoLandProtector` (superseded by `SkillInf2.IgnoreLandProtector`, no consumers).
- [x] Loaded the real `Unit.Flag` set via a curated `CuratedUnitFlags` overlay in
      `SkillDb.LoadingFinished` (the COMBAT-62/76 pattern), sourced from `db/re/skill_db.yml` for the
      8 C#-handled ground skills. ➡️ The DB column + EF migration + importer path is **COMBAT-92**
      (its scope now includes the Unit block).
- [x] Wired a real consumer: the **UF_NOREITERATION** place-gate in `SkillUnitService.Place` (rAthena
      skill.cpp:488 — a non-reiterable ground skill can't be placed where a same-skill unit already
      exists in range). ➡️ The other placement flags (NoOverlap/PathCheck/NoFootSet) are loaded but
      their placement rules are **COMBAT-107**.

## Done criteria

- ✅ `GetUnitFlag(skill, UF_*)` returns the correct value for ≥2 known skills (SafetyWall→NoReiteration,
  Sanctuary→NoOverlap+PathCheck), and a placement rule (UF_NOREITERATION) consumes a loaded flag.

## Test plan

- Loader: a skill_db row with a known flag set decodes to the right `SkillUnitFlag`; the enum
  realignment is covered by a bit-value test against the rAthena order.

## Notes / gotchas

- Do not ship the loader without a consumer (HARD RULE: no dead data). If no placement rule is
  ready, this ticket waits — the LP gate (the original motivation) is already handled via INF2.

## History

- 2026-06-03 — Re-aligned `SkillUnitFlag` to rAthena `e_skill_unit_flag` (added `KnockbackGroup`,
  moved `HiddenTrap` to 1<<17, `Removed`→`RemovedByFireRain`, removed the `NoLandProtector` placeholder).
  Added a curated `CuratedUnitFlags` overlay in `SkillDb.LoadingFinished` (COMBAT-62 pattern, from
  db/re/skill_db.yml, 8 handled ground skills) populating `SkillDefinition.UnitFlags`. Wired the live
  consumer — the **UF_NOREITERATION** place-gate in `SkillUnitService.Place` (refuse a same-skill unit
  overlapping an existing one, skill.cpp:488). Combat85UnitFlagsTests (4: enum bit-alignment, overlay
  GetUnitFlag, NoReiteration blocks an overlapping SafetyWall, no-flag stacks). Full suite 4172 pass
  (1 fail = pre-existing INFRA-11 replay gate). Filed COMBAT-107 (the NoOverlap/PathCheck/NoFootSet
  placement rules); the DB-column loader folds into COMBAT-92 (scope extended to the Unit block).
