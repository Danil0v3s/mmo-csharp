# COMBAT-85 — Generic skill_db UnitFlags column loader (+ SkillUnitFlag bit-order fix)

> **Epic:** Combat parity · **Status:** 🚧 In progress · **Size:** M · **Player-visible:** no (infra)
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

- [ ] Re-align `SkillUnitFlag` to the rAthena `e_skill_unit_flag` order (add `KnockbackGroup`,
      shift `HiddenTrap`), and either remove `NoLandProtector` or keep it clearly deprecated.
- [ ] Add a `UnitFlags` column to `SkillDbEntity` (+ EF migration).
- [ ] `Tools.RathenaImporter` + `Core.Database/Seeds` — decode the YAML `Unit.Flag` block into the
      column.
- [ ] `SkillDbLoader.FromEntity` — decode the bitmask into `SkillDefinition.UnitFlags`.
- [ ] Wire a real consumer (e.g. UF_NOOVERLAP / UF_PATHCHECK placement rules in
      `SkillUnitService.Place`) so the loaded flags are not dead.

## Done criteria

- `GetUnitFlag(skill, UF_NOOVERLAP)` etc. return the correct value for ≥2 known skills, and at
  least one placement rule consumes a loaded flag.

## Test plan

- Loader: a skill_db row with a known flag set decodes to the right `SkillUnitFlag`; the enum
  realignment is covered by a bit-value test against the rAthena order.

## Notes / gotchas

- Do not ship the loader without a consumer (HARD RULE: no dead data). If no placement rule is
  ready, this ticket waits — the LP gate (the original motivation) is already handled via INF2.
