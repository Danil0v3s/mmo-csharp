# COMBAT-66 — skill_db UnitFlags loader + production Land Protector unit handler

> **Epic:** combat · **Status:** 🚧 In progress · **Size:** M · **Player-visible:** yes
> **Depends on:** COMBAT-47 (Land Protector place-gate + UF_NOLP enum) · **Blocks:** none
> **Filed by:** COMBAT-47 — the LP place-gate is wired but dormant in production: the
> `UF_NOLP` exemption never fires (UnitFlags aren't loaded from skill_db) and Land Protector
> itself can't be placed (no unit handler), so the gate never sees an LP cell live.

## Problem

COMBAT-47 implemented the Land Protector place-gate in `SkillUnitService.Place` and the
skill-funnel ground-unit intercept. Two data/wiring gaps make the LP gate dormant in
production (it is fully exercised in tests via stubs, but never triggers on a live server):

1. **`SkillDefinition.UnitFlags` is never populated from skill_db.** `SkillDbLoader.FromEntity`
   does not read the unit-flag bitmask, so `GetUnitFlag(skill, UF_NOLP)` returns `false` for
   *every* real skill. Skills that should ignore Land Protector (Storm Gust et al. carrying
   `UF_NOLP`) are therefore wrongly gated, and the exemption branch is unreachable live.
2. **No production `SA_LANDPROTECTOR` unit handler exists.** `Map.Server/Skills/Units/Handlers/`
   has no Land Protector handler, so `SkillUnitService.Place(SA_LANDPROTECTOR, …)` returns null
   (no handler) — Land Protector can't be laid down, and `CellHasLandProtector` never returns
   true on a real server. The gate is correct but never sees an LP cell.

## Current state (C#)

- `Map.Server/Skills/SkillDbLoader.cs:FromEntity` — populates the skill columns but **does not
  set `UnitFlags`**; the property stays `SkillUnitFlag.None` for every loaded skill.
- `Map.Server/Skills/SkillDefinition.cs:SkillUnitFlag` — has `NoLandProtector = 1u << 17` (added
  by COMBAT-47) but no other UF_* members modeled.
- `Map.Server/Skills/SkillUnitService.cs:Place` — gate reads
  `_db?.GetUnitFlag(skillId, SkillUnitFlag.NoLandProtector)` and `CellHasLandProtector(...)`;
  both correct, both dormant without (1) and (2).
- `Map.Server/Skills/Units/Handlers/` — has StormGust/Magnus/Pneuma/SafetyWall/Sanctuary; **no
  LandProtector handler**.

## rAthena reference (source of truth)

- `skill.cpp` `skill_unit_flag` YAML / `skill_db.yml` `Unit: { ... Flag: ... }` — the unit-flag
  bitmask (UF_NOLP, UF_NOPC, UF_NOMOB, UF_PATHCHECK, UF_RANGEDSINGLEUNIT, …). The C# port loads
  skill_db from the SQL table populated by `Tools.RathenaImporter`; the unit-flag column must be
  imported and decoded into `SkillDefinition.UnitFlags`.
- `skill.cpp` `skill_unitsetting` SA_LANDPROTECTOR arm — Land Protector lays an immobile ground
  unit that suppresses other ground skills on its cells for `skill_get_time` (it does no damage;
  it only blocks placement/ticks). Canonical source is the `skill.cpp` monolithic switch (the
  `rathena-fork/src/map/skills/...` split paths do not exist in this checkout).

## Scope — every sub-system that must be touched

- [ ] Model the remaining `UF_*` members on `SkillUnitFlag` that the importer/skill_db carry
      (at minimum the ones present in the seeded `skill_db` rows).
- [ ] `Tools.RathenaImporter` + `Core.Database/Seeds` — import the skill_db unit-flag column so
      it lands in the SQL `skill_db` table (add the column to the entity/migration if missing).
- [ ] `SkillDbLoader.FromEntity` — decode the unit-flag bitmask into `SkillDefinition.UnitFlags`.
- [ ] New `LandProtectorUnit : ISkillUnitTickHandler` in `Map.Server/Skills/Units/Handlers/`
      (`SkillId => SA_LANDPROTECTOR`, `DurationMs = skill_get_time`, radius from
      `skill_get_unit_range`, no-damage OnTick) so LP is placeable in production. Register it in
      the DI handler set alongside the existing ground-unit handlers.
- [ ] Confirm the LP cast caller refunds SP/items when `Place` returns null (gate refusal).

## Done criteria

- Loading skill_db yields `GetUnitFlag(skill, UF_NOLP) == true` for skills flagged UF_NOLP in the
  rAthena data, `false` otherwise (verify against ≥2 known skills).
- A live cast of `SA_LANDPROTECTOR` places a ground-unit group; a subsequent hostile ground skill
  (e.g. WZ_STORMGUST) on a covered cell is refused, while a UF_NOLP skill places.
- No `// TODO` / `data-pending` / log-only no-op in the touched files.

## Test plan

- `Combat66UnitFlagsLoaderTests`: load a skill_db row with the UF_NOLP bit set → `GetUnitFlag`
  returns true; a row without it → false.
- `Combat66LandProtectorHandlerTests`: place SA_LANDPROTECTOR via the real handler set, then
  assert a hostile ground skill is refused on a covered cell and a UF_NOLP skill is allowed
  (end-to-end version of the COMBAT-47 stub test, now with the production handler + loader).

## Notes / gotchas

- COMBAT-47 already proved the gate + exemption logic with stub handler/db; this ticket only
  supplies the live data (UnitFlags) and the placeable LP unit. Keep the `1u << 17` bit value for
  `NoLandProtector` so the COMBAT-47 enum stays stable.
