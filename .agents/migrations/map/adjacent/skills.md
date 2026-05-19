# MS3 · Skills

**Phase:** MS3 (adjacent)
**Depends on:** [combat.md](combat.md), [status.md](status.md), [items.md](items.md)
**Blocks:** anything class-specific (most quests, many mobs)

Skills are the largest single gameplay system. rAthena's [skill.cpp](/Volumes/1TB/Projetos/rathena/src/map/skill.cpp) is **26,438 lines** — almost every Ragnarok ability is hand-written there. This doc plans the skeleton; per-skill implementation is iterative.

## Source of truth

- [rathena/src/map/skill.cpp](/Volumes/1TB/Projetos/rathena/src/map/skill.cpp) — skill cast / unit (e.g. magnus, ice wall) / damage / heal / status
- [rathena/src/map/skill.hpp](/Volumes/1TB/Projetos/rathena/src/map/skill.hpp) — `enum e_skill_id`, `skill_db` schema
- [rathena/db/re/skill_db.yml](/Volumes/1TB/Projetos/rathena/db/re/skill_db.yml) — static catalog (1500+ skills) — **note:** like mob_db / item_db, we'll load from the equivalent SQL table (rAthena's `use_sql_db` path) once the seed/repository lands rather than parsing this YAML.
- [rathena/db/re/skill_tree.yml](/Volumes/1TB/Projetos/rathena/db/re/skill_tree.yml) — class skill tree
- [rathena/db/re/mob_skill_db.yml](/Volumes/1TB/Projetos/rathena/db/re/mob_skill_db.yml) — mob skill assignment + use conditions

## Scope (MS3 first pass)

**In scope:**
- `ISkillDb` catalog hydrated from a `skill_db` repository (DB-backed; mirrors the mob_db / item_db pattern). When the schema and seed land in `Core.Database`, the catalog reads from there; until then this subsystem is blocked on that DB work.
- Skill tree parsing → which skills which classes can learn.
- Skill use flow: `CZ_USE_SKILL (0x0113)` / `CZ_USE_SKILL_TOID (0x0438)` → cast time → damage / status / unit emission → cooldown set.
- Skill cooldown persistence via existing IPC (`SaveSkillCooldown` / `LoadSkillCooldown`) — already wired in P6.
- Skill units (ground effects like Magnus Exorcismus, Storm Gust, Ice Wall): server-tracked entity that ticks effects on overlapping entities.
- A starter set of 30-50 skills covering basics (Heal, Bash, Magnum Break, Bowling Bash, Magnus, Fire Bolt, Cold Bolt) — enough to test combat+status+units interaction.

**Out of scope (long-tail):**
- Full 1500-skill coverage. Treat as evergreen.
- Ranger trap mechanics, Sage's Endure on hit, Star Gladiator day/night, Soul Linker — exotic systems with their own quirks; each gets a focused PR.

## Done

- **`SkillDefinition`** ([SkillDefinition.cs](../../../../Map.Server/Skills/SkillDefinition.cs)) — id / name / max level / target mode / damage kind / range / per-level sp cost / cast time / cooldown / damage rate / effect amount / element / applied SC. Mirror of `s_skill_db`.
- **`SkillIds`** ([SkillIds.cs](../../../../Map.Server/Skills/SkillIds.cs)) — constants for the ported skills, values pinned to rAthena `e_skill_id` so client packets line up.
- **`SkillDb`** ([SkillDb.cs](../../../../Map.Server/Skills/SkillDb.cs)) — loads from the new SQL repo when seeded, falls back to the 6-skill hand-built starter catalog otherwise. Reload() supports `/reloadskilldb`.
- **`SkillDbLoader`** ([SkillDbLoader.cs](../../../../Map.Server/Skills/SkillDbLoader.cs)) — `SkillDbEntity` → `SkillDefinition` parser, handles the colon-delimited per-level packed columns.
- **`SkillDbEntity` + `ISkillDbRepository`** ([Core.Database/Entities/SkillDbEntity.cs](../../../../Core.Database/Entities/SkillDbEntity.cs), [Api/ISkillDbRepository.cs](../../../../Core.Database/Repositories/Api/ISkillDbRepository.cs)) — `skill_db` SQL table mirror, same pattern as item_db / mob_db.
- **Strategy-pattern resolution** ([Skills/Resolvers/](../../../../Map.Server/Skills/Resolvers/)) — `ISkillResolver` with one impl per `SkillDamageKind`: `WeaponSkillResolver` / `MagicSkillResolver` / `HealSkillResolver` / `StatusSkillResolver` / `MiscSkillResolver`. New damage kinds ship as a class; no switch case to edit.
- **`SkillCastService`** ([SkillCastService.cs](../../../../Map.Server/Skills/SkillCastService.cs)) — `StartCast` validates id / level / target / range / sp / cooldown, `pc_checkskill` gate (player must have learned the skill at requested level), and either resolves immediately (instant cast) or schedules via `Tick` (cast-time deferral). Resolution dispatches to the strategy registry.
- **`SkillUnitService`** ([SkillUnitService.cs](../../../../Map.Server/Skills/SkillUnitService.cs)) + `SkillUnitGroup` / `SkillUnit` — ground-placed periodic effects (Magnus Exorcismus / Storm Gust seeded). Mirrors `skill_unitsetting` + `skill_unit_onplace_timer`.
- **`CZ_USE_SKILL_TOID` (0x0438)** + **`UseSkillToIdHandler`** wired to client.
- **Skill point allocation** — `CZ_UPGRADE_SKILLLEVEL` (0x0112) + `UpgradeSkillHandler`. `PlayerEntity.LearnedSkills` (Dictionary<ushort, byte>) mirrors `mmo_charstatus.skill[]`.
- **Mob skills** — `MobSkillEntry` on `MobDbEntry`; `MobAiService.TryUseMobSkill` evaluates per-mob skills against the strategy-dispatched `MobSkillConditionRegistry` (Always / MyHpLessThanRate ported). Engaged mobs cast before falling back to the basic swing.
- **Starter catalog**: SM_BASH, AL_HEAL, AL_INCAGI, AL_BLESSING, MG_FIREBOLT, MG_COLDBOLT, PR_MAGNUSEXORCISMUS, WZ_STORMGUST. Damage/heal numbers cross-checked against rAthena db/re/skill_db.yml.
- **17 tests** across `SkillCastServiceTests`, `SkillUnitServiceTests`, `MobSkillUseTests`, `SkillDbLoaderTests`.

## Pending

1. **Skill cooldown persistence** — `SaveSkillCooldownAsync` / `LoadSkillCooldownAsync` IPC is wired (P6) but not yet called from `SkillCastService`. Lands as a small wiring slice.
2. **Skill tree gate** — `skill_tree.yml` SQL table + per-class prereq check inside `UpgradeSkillHandler`. Today we cap by `SkillDefinition.MaxLevel` only.
3. **Long-tail skills** — 1500 entries in rAthena's `skill_db.yml`. Each ships as either a `SkillDbEntity` SQL row (data-driven) or a hand-built entry when the formula has quirks beyond `SkillDefinition`'s shape. Bulk YAML→SQL converter is a separate data-migration job.
4. **Mob skill conditions tail** — `MASTERATTACKED`, `SLAVELT`, `GROUNDATTACKED`, `AFTERSKILL`, etc. Each ships as a new `IMobSkillConditionEvaluator` class.

### Acceptance
- ✅ Bash → weapon damage scaled by per-level rate; sp consumed; range gated.
- ✅ Heal → restores HP using the renewal formula.
- ✅ Fire Bolt → MATK roll + element table + RE-Mdef formula.
- ✅ Storm Gust → 11x11 ground unit ticks damage to enemies in its cells.
- ✅ Mob with assigned skill casts on engagement.
- ⚠️ Cooldowns survive LeaveMap → EnterMap (wiring pending).

## History
- **2026-05-16** — Plan stub.
- **2026-05-19** — Slice shipped end-to-end. CZ_USE_SKILL_TOID + handler + ISkillResolver strategy dispatch + ground-unit ticker + per-class skill_db SQL infrastructure + mob skill use + skill point allocation packet. 17 tests green. The catalog data side (1500 rAthena skills) is the remaining piece.
