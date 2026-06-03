# COMBAT-92 — Real skill_db Requirements column loader (fold curated ammo/Inf2 overlays)

> **Epic:** combat · **Status:** 🚧 In progress · **Size:** M · **Player-visible:** no (infra)
> **Depends on:** COMBAT-76 (ammo overlay), COMBAT-62 (Inf2 overlay) · **Blocks:** none
> **Filed by:** COMBAT-76 — it loaded the per-skill ammo mask/qty via a curated overlay
> (the COMBAT-62 pattern) rather than a real skill_db column, since the SQL `skill_db`
> table + `SkillDbLoader` surface only ~12 columns and have no Requirements block.

## Problem

`SkillDefinition` carries many rAthena `Requirements`/`skill_get_*` fields (`AmmoTypeMask`,
`AmmoQuantity`, `WeaponTypeMask`, `RequiredState`, `Inf2`, `UnitFlags`, …) but the SQL
`skill_db` table (`SkillDbEntity`, ~12 columns) and `SkillDbLoader.FromEntity` populate none
of them. Two areas are filled by **curated overlays** in `SkillDb.LoadingFinished` instead:

- COMBAT-62/66 — `CuratedInf2` (IgnoreGvg/Bg/LandProtector flags).
- COMBAT-76 — `CuratedAmmo` (61-skill `Requirements.Ammo` mask + `AmmoAmount`).

These are correct + sourced from `db/re/skill_db.yml`, but they're hand-maintained tables, not
a data-driven load. A real loader would import the YAML `Requirements` block once.

## Current state (C#)

- `Core.Database/Entities/SkillDbEntity.cs` — no Ammo/AmmoAmount/Weapon/State/Inf2 columns.
- `Map.Server/Skills/SkillDbLoader.cs:FromEntity` — maps only the ~12 base columns.
- `Map.Server/Skills/SkillDb.cs` — `CuratedInf2` + `CuratedAmmo` overlays folded in `LoadingFinished`.

## rAthena reference (source of truth)

- `db/re/skill_db.yml` `Requirements: { Ammo:, AmmoAmount:, Weapon:, State:, … }` + the `Flags` block.
- `skill_read_db` / `skill_get_ammotype` / `skill_get_ammo_qty` / `skill_get_weapontype`.

## Scope — every sub-system that must be touched

- [ ] Add the Requirements columns (Ammo mask, AmmoAmount per-level, Weapon mask, State, Flags/Inf2)
      to `SkillDbEntity` + an EF migration; extend `Tools.RathenaImporter` to emit them from the YAML.
- [ ] Populate them in `SkillDbLoader.FromEntity`; retire `CuratedAmmo` (and fold `CuratedInf2`)
      once the column load covers them — keep behavior identical (assert via the existing tests).
- [ ] Include the `Unit.Flag` block too (COMBAT-85 currently loads it via a `CuratedUnitFlags` overlay) — fold that overlay into the column load.
- [ ] Re-seed `seed_skill_db.sql`.

## Done criteria

- `GetAmmoType`/`GetAmmoQty`/`GetInf2` return the same values they do today, now sourced from the
  DB column load; the curated overlays are removed; Combat76/Combat62 tests stay green.

## Test plan

- Reuse `Combat76SkillAmmoDataTests` + the Inf2 tests against the DB-loaded path (no overlay).

## Notes / gotchas

- COMBAT-85 is the sibling for the `UnitFlags` column; this ticket can share its loader plumbing.
