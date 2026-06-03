# COMBAT-92 — Real skill_db Requirements column loader (fold curated ammo/Inf2 overlays)

> **Epic:** combat · **Status:** ✅ Done (2026-06-03) · **Size:** M · **Player-visible:** no (infra)
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

- [x] Added the Requirements/Flags/Unit columns `ammo` / `ammo_amount` / `inf2` / `unit_flags` to
      `SkillDbEntity` + EF config (`SkillDbEntityConfiguration`) + migration
      (`20260603082556_SkillDbRequirementsColumns`). Stored as pipe-delimited name tokens (decoupling
      the importer from the runtime enum bit-values, matching the existing string-column convention).
      Extended `SkillDbConverter` to emit them from the YAML (`Requires.Ammo`/`AmmoAmount`, `Flags`,
      `Unit.Flag` via the new `YamlHelpers.TrueKeys`). ➡️ The `Weapon` mask + `State` columns are
      **COMBAT-113** (no runtime consumer today; `RequiredState` has no `e_require_state` map — loading
      them now would be unverifiable dead data).
- [x] Populated them in `SkillDbLoader.FromEntity` (ammo names → `1<<AMMO_x` mask, qty broadcast
      across levels, `Enum.TryParse`-filtered Inf2 / UnitFlags); **retired** `CuratedAmmo` +
      `CuratedInf2` + `CuratedUnitFlags` and their `LoadingFinished` folds. Behavior identical
      (the seed reproduces every curated value; verified via the existing tests + spot-checks).
- [x] Included the `Unit.Flag` block (COMBAT-85's `CuratedUnitFlags`) in the column load.
- [x] Re-seeded `seed_skill_db.sql` (1614 rows, now with `ammo`/`ammo_amount`/`inf2`/`unit_flags`)
      by running `Tools.RathenaImporter`.

## Done criteria

- ✅ `GetAmmoType`/`GetAmmoQty`/`GetInf2`/`GetUnitFlag` return the same values they did via the
  overlays, now sourced from the `skill_db` column load (`FromEntity`); the curated overlays are
  removed; Combat76/Combat62/Combat66/Combat85 tests stay green (rewired through the column path) +
  a new `Combat92SkillDbColumnsTests` (8) covers the loader. ➡️ `GetWeaponType`/`GetState` columns
  are COMBAT-113 (no consumer yet — out of scope here).

## Test plan

- ✅ Reused `Combat76SkillAmmoDataTests` + the Inf2 (Combat62/66) + UnitFlags (Combat85) tests
  against the DB-loaded path (their `Def`/`Db` helpers now build via `SkillDbLoader.FromEntity` from
  a `SkillDbEntity` with the columns — no overlay). Added `Combat92SkillDbColumnsTests` (ammo-name→bit,
  multi-token OR, unknown-flag skip, qty broadcast, empty→none).

## Notes / gotchas

- COMBAT-85 is the sibling for the `UnitFlags` column; this ticket shared its loader plumbing.
- The retired overlays previously also covered the DB-empty **fallback** path; that path no longer
  carries ammo/inf2/unitflags (production always loads the 1614-row seed → SQL path). Tests that need
  the data now construct entities with the columns (the production loader path), which is the point.
- The importer emits the FULL YAML Flags/Unit data (more skills than the curated subset); `FromEntity`
  keeps only known enum members, and the only *consumed* flags are IgnoreGvg/Bg/LandProtector +
  NoReiteration — so the extra tokens are inert. The ammo TYPE is emitted only when `AmmoAmount > 0`,
  exactly reproducing the prior curated subset.

## History

- 2026-06-03 — Replaced the three hand-maintained curated overlays (CuratedAmmo / CuratedInf2 /
  CuratedUnitFlags) with a real data-driven load: added `ammo`/`ammo_amount`/`inf2`/`unit_flags`
  columns to `skill_db` (entity + EF config + migration), taught `Tools.RathenaImporter` to emit
  them from `db/re/skill_db.yml` (Requires.Ammo/AmmoAmount, Flags, Unit.Flag), regenerated the
  1614-row `seed_skill_db.sql`, and parse them in `SkillDbLoader.FromEntity`. Retired the overlays +
  their `LoadingFinished` folds. Rewired Combat76/62/66/85 tests through the column path + added
  `Combat92SkillDbColumnsTests` (8). Full Map.Server.Tests 4235 pass (1 fail = pre-existing INFRA-11
  replay gate); Char.Server.Tests 167 + Core.Server.Tests 87 green; whole solution builds. Filed
  COMBAT-113 for the deferred Weapon-mask / RequiredState columns (no consumer / needs an
  e_require_state enum).
