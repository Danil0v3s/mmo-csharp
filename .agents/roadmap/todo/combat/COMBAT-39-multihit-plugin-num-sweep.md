# COMBAT-39 — Multi-hit plugin GetMultiHitCount sweep

> **Epic:** Combat parity · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** COMBAT-17 (GetMultiHitCount hook + wire)
> **Blocks:** none
> **Filed by:** COMBAT-17 — only AS_SONICBLOW was given its hit count; the rest still
> render a single hit.

## Problem

COMBAT-17 added `WeaponSkillImpl.GetMultiHitCount` (default 1) and overrode it for
Sonic Blow (8). Every other multi-hit weapon skill — Double Strafe (AC_DOUBLE),
Triple Attack (MO_TRIPLEATTACK), Counter Slash, Sonic Wave, Cross Impact, etc. — still
defaults to 1, so the client renders one fat hit instead of the correct N hits even
though the damage total is right.

## Current state (C#)

- `Map.Server/Skills/Behaviors/SkillImpl.cs:WeaponSkillImpl.GetMultiHitCount` — virtual,
  default 1. Only `Thief/SonicBlow.cs` overrides it.
- The rAthena `num` (hit count) lives in `db/re/skill_db.yml` `HitCount:` and is NOT
  loaded into `SkillDbEntity` / `SkillDefinition` (the loader drops it; `SkillDb.GetNum`
  therefore always returns 1 from the empty `HitCount[]`).

## rAthena reference (source of truth)

- `db/re/skill_db.yml` — each multi-hit skill's `HitCount:` (e.g. AC_DOUBLE 2,
  MO_TRIPLEATTACK 3, AS_SONICBLOW -8). Negative = "single damage shown as N hits"
  (magnitude on the wire); positive = per-hit damage ×N.
- `skill_get_num` (skill.cpp) returns the signed value; `DAMAGE_DIV_FIX` (battle.cpp:4365)
  applies the sign.

## Scope — every sub-system that must be touched

- [ ] EITHER: surface skill_db `num` as a real column (`SkillDbEntity.HitCount` +
      `SkillDbLoader` + seed importer) and have `WeaponSkillImpl.GetMultiHitCount` default
      to `abs(SkillDb.GetNum)` via the context (requires ISkillDb on `SkillBehaviorContext`),
      OR: override `GetMultiHitCount` per multi-hit plugin from the YAML values.
- [ ] Populate the hit count for every multi-hit weapon-skill plugin present in
      `Map.Server/Skills/Behaviors/**`.
- [ ] Decide and encode the positive/negative-div distinction per skill (see COMBAT-38).

## Done criteria

- Every multi-hit weapon-skill plugin renders its rAthena `num` magnitude on
  `ZC_NOTIFY_ACT3.div` (Double Strafe 2, Triple Attack 3, …), HP delta unchanged.
- No multi-hit plugin silently defaults to 1 when its skill_db row says otherwise.

## Test plan

- `Combat39HitCountTests`: table of skill → expected div, asserted against each plugin's
  `GetMultiHitCount` (or `abs(GetNum)` if the data-column route is taken).

## Notes / gotchas

- If the data-column route is chosen, `SkillBehaviorContext` needs `ISkillDb` threaded in
  (SkillCastService already has `_db`); the two `new SkillBehaviorContext(...)` sites pass it.
