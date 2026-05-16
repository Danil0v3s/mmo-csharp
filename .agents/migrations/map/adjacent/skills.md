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

Nothing.

## Pending

1. **`SkillDbEntry`** with id, max level, cast time, cooldown, damage type, target type, range, sp cost, requirements (weapon, status).

2. **Per-skill implementation registry.** Map skill id → `ISkillHandler` (executes the skill). Skills are too varied to share one impl; each one is its own class.

3. **Skill cast lifecycle:**
   - `SkillUseRequest` validates: known skill, learned, in range, has sp, no cooldown.
   - Cast time tick: animate, may be interrupted by damage.
   - Resolve: damage/heal/status/unit applies.
   - Cooldown stored via `SaveSkillCooldownAsync` IPC.

4. **Skill units.** A new `EntityType.SKILL` placed at a cell; ticks every N ms applying its effect (damage in radius, status to step-on, etc.); removed when duration elapses.

5. **Mob skills.** Hook `mob_skill_db` into mob AI: on attack / on idle / on damaged / on death triggers can cast a skill from the mob's skill list. Defer per-mob skill assignment to later in MS3.

### Acceptance
- A swordsman can use Bash on a target; correct damage, sp consumed, cooldown saved.
- A wizard can drop Storm Gust; the ground unit damages mobs in its area for the duration.
- A priest can heal a party member.
- Skill cooldowns survive a `LeaveMap → EnterMap` cycle (IPC round-trip already verified in P6/P8).

## History
- **2026-05-16** — Plan stub.
