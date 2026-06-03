# COMBAT-106 — status_calc_speed: Dancing/Longing-lesson song penalty + TF_MISS assassin speedup

> **Epic:** combat · **Status:** ❌ Not started · **Size:** S · **Player-visible:** yes
> **Depends on:** COMBAT-84 · **Blocks:** none
> **Filed by:** COMBAT-84 — the speed tail landed Ensemble Fatigue (renewal) but not the
> skill/class-gated song-dance penalty nor the assassin TF_MISS bonus.

## Problem

Two `status_calc_speed` entries are gated on a learned skill / class the C# tail skipped:
1. **Song/dance penalty** (slow chain): `else if (SC_DANCING) val = max(val, 500 - (40 + 10*(SC_SPIRIT
   && val2==SL_BARDDANCER)) * pc_checkskill(sd, sex ? BA_MUSICALLESSON : DC_DANCINGLESSON))`. Needs the
   lesson skill (DC_DANCINGLESSON isn't in `SkillIds`), the player's sex, and the SC_SPIRIT
   SL_BARDDANCER check. (Pre-renewal SC_LONGING is the `#ifndef RENEWAL` sibling.)
2. **TF_MISS** (fast chain): `if (sd && class&MAPID_UPPERMASK == MAPID_ASSASSIN && pc_checkskill(TF_MISS))
   val = max(val, pc_checkskill(TF_MISS))`. Needs the assassin class-mask + the TF_MISS skill.

## Current state (C#)

- `Map.Server/Status/StatusCalcService.cs:ComputeScSpeed` — the `else` slow chain has Ensemble Fatigue
  but no Dancing-lesson penalty; the fast chain has no TF_MISS.
- `Map.Server/Skills/SkillIds.cs` — DC_DANCINGLESSON / TF_MISS may be missing.

## rAthena reference (source of truth)

- `status.cpp:7842` (dancing) / `status.cpp:7935` (TF_MISS).

## Scope

- [ ] Add the Dancing-lesson penalty (lesson skill by sex + SC_SPIRIT SL_BARDDANCER) to the slow chain.
- [ ] Add the TF_MISS assassin speedup to the fast chain (class-mask gated).

## Done criteria

- A dancing Bard/Dancer's move penalty matches the lesson formula; an assassin with TF_MISS speeds up.

## Test plan

- Dancing with a lesson level; an assassin with TF_MISS.
