# COMBAT-39 — Multi-hit plugin GetMultiHitCount sweep

> **Epic:** Combat parity · **Status:** ✅ Done (2026-06-02) · **Size:** M · **Player-visible:** yes
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

- [x] Took the **per-plugin / centralized-table** route (the DB-column route needs a
      migration + importer + reseed): added `SkillHitCounts` — the signed `skill_db`
      `HitCount` for all 60 multi-hit `WeaponSkillImpl` skills, transcribed (script-
      generated) from `db/re/skill_db.yml`, incl. the 5 per-level ones (CH_CHAINCRUSH,
      DK_STORMSLASH, HFLI_MOON, NPC_COMBOATTACK, CR_ACIDDEMONSTRATION).
- [x] `WeaponSkillImpl.GetMultiHitCount` defaults to `abs(SkillHitCounts.Get(SkillId,
      lv))`, so every multi-hit WeaponSkillImpl plugin renders its num magnitude (removed
      SonicBlow's now-redundant explicit override).
- [x] Positive/negative-div distinction encoded as the **sign** in `SkillHitCounts`
      (magnitude drives the wire; the sign feeds COMBAT-60's per-hit multiply).

## Done criteria

- Every multi-hit weapon-skill plugin renders its rAthena `num` magnitude on
  `ZC_NOTIFY_ACT3.div` (Triple Attack 3, Sonic Blow 8, Vulcan Arrow 9, …), HP delta
  unchanged. ✅ (all 60 WeaponSkillImpl plugins; Double Strafe/AC_DOUBLE is a plain
  SkillImpl that already renders 2 via its own per-hit loop)
- No multi-hit plugin silently defaults to 1 when its skill_db row says otherwise. ✅
  for WeaponSkillImpl; ➡️ the SkillImpl/splash multi-hit base counts not already looped
  ride COMBAT-60 (splash/SkillImpl div path).

## Test plan

- `Combat39HitCountTests`: table of skill → expected div, asserted against each plugin's
  `GetMultiHitCount` (or `abs(GetNum)` if the data-column route is taken).

## Notes / gotchas

- If the data-column route is chosen, `SkillBehaviorContext` needs `ISkillDb` threaded in
  (SkillCastService already has `_db`); the two `new SkillBehaviorContext(...)` sites pass it.

## History

- 2026-06-02 · Added `SkillHitCounts` (signed skill_db `HitCount` for all 60 multi-hit
  WeaponSkillImpl skills, script-generated from db/re/skill_db.yml incl. 5 per-level
  tables) and routed `WeaponSkillImpl.GetMultiHitCount` through it
  (`abs(SkillHitCounts.Get(SkillId, lv))`); removed SonicBlow's redundant override.
  Triple Attack now renders 3, Vulcan Arrow 9, Sonic Blow 8, etc. (display only — HP
  delta unchanged). Combat39HitCountTests (11) + updated Combat17/Combat38 expectations
  (TF_DOUBLE→2, KN_BOWLINGBASH base→2); full Map.Server.Tests green except the
  pre-existing INFRA-11 replay gate. The SkillImpl/splash base-count remainder rides
  COMBAT-60.
