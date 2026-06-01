# SC-13 — Magicrod magic-absorb + Poisonreact autocast-Envenom consumers

> **Epic:** Status parity hardening · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** none · **Blocks:** none · **Split from:** SC-04

## Problem

Two SC-04 starved SCs need an on-hit / magic-pipeline autocast consumer (not a DamageService
Val read):

1. **Magicrod** (`SC_MAGICROD`, SA) — when the bearer is hit by a single-target magic skill,
   the spell is nullified and the bearer gains `Val2` SP (`Val2 = Val1*20`). No magic-absorb
   reader exists.
2. **Poisonreact** (`SC_POISONREACT`, AM) — when the bearer is melee-hit, autocast Envenom on
   the attacker up to `Val2` times (`Val2 = Val1/2`), decrementing a counter. No on-hit reader.

## Current state (C#)

- `Map.Server/Status/StatusEffectRegistry.cs` — Magicrod (~1039) sets `Val2 = Val1*20`;
  Poisonreact (~1033) sets `Val2 = Val1/2`. Neither is read.
- The magic-cast resolution pipeline (`SkillCastService` / magic resolver / `SkillAttackService`)
  has no Magicrod absorb check; `DamageService` has no melee-hit Poisonreact autocast.

## rAthena reference (source of truth)

- Magicrod: `skill.cpp` magic-absorb path — when a single-target magic skill hits a
  `SC_MAGICROD` target, nullify the damage and `status_heal(bl, 0, val2, 0)` (SP gain).
- Poisonreact: `battle.cpp`/`skill.cpp` on melee-hit — autocast `TF_POISON` (Envenom) on the
  attacker, decrement the `val2` counter, end at 0.

## Scope — every sub-system that must be touched

- [ ] Magicrod: in the magic damage path, if the target has Magicrod, nullify the magic hit and
      grant `Val2` SP; consume/clear per rAthena (single-absorb or duration-based).
- [ ] Poisonreact: on a melee hit against the bearer, autocast Envenom on the attacker up to
      `Val2` times; decrement and end the SC at 0.

## Done criteria

- A single-target magic skill on a Magicrod target deals 0 and grants the bearer `Val2` SP.
- A melee hit on a Poisonreact bearer autocasts Envenom on the attacker; the counter decrements
  and the SC ends after `Val2` procs.

## Test plan

- `MagicrodTests`: magic hit nullified + SP granted.
- `PoisonreactTests`: melee hit autocasts Envenom; counter decrements to 0 then ends.

## Notes / gotchas

- Both are autocast/pipeline consumers, not stat reads — wire into the cast/damage flow, not
  the registry stat-mod path.
