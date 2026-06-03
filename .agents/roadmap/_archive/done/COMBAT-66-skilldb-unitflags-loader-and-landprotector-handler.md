# COMBAT-66 — skill_db UnitFlags loader + production Land Protector unit handler

> **Epic:** combat · **Status:** ✅ Done (2026-06-03) · **Size:** M · **Player-visible:** yes
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

## ⚠️ Premise correction (discovered during implementation)

The ticket assumed "ignore Land Protector" is a **unit flag** (`UF_NOLP`) loadable from the
skill_db `Unit.Flag` column. **It is not.** rAthena has no `UF_NOLP`; the exemption is
`INF2_IGNORELANDPROTECTOR` (an **INF2** flag — skill.cpp:22267 `!skill->inf2[INF2_IGNORELANDPROTECTOR]`).
COMBAT-47's `SkillUnitFlag.NoLandProtector` was a placeholder. Since COMBAT-62 made INF2 loadable
(the curated overlay + `GetInf2`), the faithful fix is to migrate the gate to INF2 and seed the
real `IgnoreLandProtector` skills — which also makes the gate fire live (the whole point). The
generic `Unit.Flag` loader is therefore **not needed for the LP gate** (it had no other consumer)
and is split to COMBAT-85.

## Scope — every sub-system that must be touched

- [x] Migrated the LP gate to the correct mechanism: added `SkillInf2.IgnoreLandProtector`, seeded
      the 11 `IgnoreLandProtector` skills present in `SkillIds` via the curated overlay
      (`SkillDb.LoadingFinished`), and changed `SkillUnitService.Place` to gate on
      `GetInf2(IgnoreLandProtector)`.
- [x] New `LandProtectorUnit : ISkillUnitTickHandler` (`SA_LANDPROTECTOR`, Duration
      `120000+45000*lv`, radius 1/1/2/2/3 = 3×3/5×5/7×7, no-damage/no-SC) + DI registration, so LP
      is placeable in production and `CellHasLandProtector` returns true on a live server.
- [x] Confirmed the LP cast caller (`MagneticEarth.CastendPos2`) does **not** refund on `Place`
      null — faithful to rAthena (the cast pipeline consumes SP before placement; a ground skill
      blocked by LP fizzles without refund).
- [ ] Generic `Unit.Flag` column loader (`SkillDbEntity` column + importer + migration +
      `FromEntity` decode + `SkillUnitFlag` bit-order fix). ➡️ Moved to COMBAT-85 — no live consumer
      now that the LP gate uses INF2; would be dead data.

## Done criteria

- Loading skill_db yields `GetInf2(skill, IgnoreLandProtector) == true` for the seeded skills,
  `false` otherwise ✅ (AC_SHOWER/SG_SUN_WARM vs WZ_STORMGUST).
- A cast of `SA_LANDPROTECTOR` places a ground-unit group via the real handler; WZ_STORMGUST on a
  covered cell is refused (incl. the 7×7 edge), while an `IgnoreLandProtector` skill places ✅.
- No `// TODO` / `data-pending` / log-only no-op in the touched files ✅.

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

## History

- 2026-06-03 · Made the COMBAT-47 Land Protector gate fire in production — but via the
  *correct* mechanism: discovered "ignore LP" is `INF2_IGNORELANDPROTECTOR`, not the fictional
  `UF_NOLP`. Added `SkillInf2.IgnoreLandProtector`, seeded the 11 flagged skills via the curated
  overlay, migrated `SkillUnitService.Place` to `GetInf2`, and shipped the placeable
  `LandProtectorUnit` (Duration 120000+45000*lv, radius 1/1/2/2/3) + DI registration. Updated the
  COMBAT-47 exemption test to INF2. Combat66LandProtectorLoaderTests (4) + Combat47 (5); skills+
  combat suite 3107 green, full suite 4075 pass (1 fail = pre-existing INFRA-11 replay gate).
  Filed COMBAT-85 (generic Unit.Flag column loader + SkillUnitFlag bit-order fix — dormant infra
  with no live consumer now that the LP gate uses INF2).
