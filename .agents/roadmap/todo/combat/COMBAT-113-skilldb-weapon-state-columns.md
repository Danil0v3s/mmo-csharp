# COMBAT-113 — skill_db Requirements: Weapon-type mask + RequiredState columns (+ e_require_state map)

> **Epic:** combat · **Status:** ❌ Not started · **Size:** S · **Player-visible:** no (infra)
> **Depends on:** COMBAT-92 (the Requirements-column loader + importer plumbing) · **Blocks:** none
> **Filed by:** COMBAT-92 — its scope listed "Weapon mask, State" among the Requirements columns,
> but those two have no runtime consumer today and `RequiredState` has no defined enum mapping, so
> loading them now would be unverifiable dead data. Split out until a consumer needs them.

## Problem

`SkillDefinition` exposes `WeaponTypeMask` (int) + `RequiredState` (int) and `SkillDb` exposes
`GetWeaponType` / `GetState`, but:
- **No consumer** reads either today (grep: zero call sites outside SkillDb).
- `RequiredState` is a bare `int` with no `e_require_state` enum mapping, so the importer has no
  defined value to emit (the YAML `Requires.State` is a string like `Hiding` / `Moveable` / `Cart`).

COMBAT-92 loaded the consumed Requirements columns (Ammo / AmmoAmount / Inf2 / UnitFlags) and
deliberately deferred these two to avoid emitting unverifiable data.

## Current state (C#)

- `Core.Database/Entities/SkillDbEntity.cs` — has `ammo`/`ammo_amount`/`inf2`/`unit_flags` (COMBAT-92),
  no `weapon`/`state`.
- `Map.Server/Skills/SkillDbLoader.cs:FromEntity` — parses the COMBAT-92 columns; not weapon/state.
- `Map.Server/Skills/SkillDefinition.cs` — `WeaponTypeMask` / `RequiredState` exist, default 0.
- `Tools.RathenaImporter/Converters/SkillDbConverter.cs` — emits the COMBAT-92 columns.

## rAthena reference (source of truth)

- `db/re/skill_db.yml` `Requires: { Weapon:, State:, … }`; `skill_get_weapontype` / `skill_get_state`
  (skill.cpp); `enum e_require_state` (skill.hpp).

## Scope

- [ ] Define an `e_require_state` enum (or string→int map) mirroring rAthena's `Requires.State`.
- [ ] Add `weapon` (mask, pipe-delimited weapon-type names) + `state` columns to `SkillDbEntity`
      + EF migration; emit them from the importer; parse them in `FromEntity`.
- [ ] Re-seed `seed_skill_db.sql`.
- [ ] Wire `GetWeaponType` / `GetState` into a consumer (skill cast-condition gate) — OR, if no
      consumer is ready, keep this ticket open; do NOT load dead data without a reader.

## Done criteria

- `GetWeaponType` / `GetState` return the rAthena `Requires.Weapon` / `Requires.State` for a sample
  set, consumed by a real cast-condition gate.

## Test plan

- A loader test: a skill with `Requires.Weapon: {Bow}` / `Requires.State: Moveable` resolves the
  mask / state; the gate honors it.
