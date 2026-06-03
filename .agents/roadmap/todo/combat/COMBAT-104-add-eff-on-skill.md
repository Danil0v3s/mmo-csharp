# COMBAT-104 — bAddEffOnSkill (on-skill status proc)

> **Epic:** combat · **Status:** ❌ Not started · **Size:** S · **Player-visible:** yes
> **Depends on:** COMBAT-83 · **Blocks:** none · **Filed by:** COMBAT-83 (3 stock items use it).

## Problem

`bonus3 bAddEffOnSkill, sk, eff, rate` (+ the bonus4/5 target-flag forms): when the player uses skill
`sk`, a `rate` chance to inflict status `eff` on the target — distinct from the on-HIT AddEff family
(COMBAT-64) which fires on any weapon hit. The live host skips it.

## Current state (C#)

- `Map.Server/Inventory/EquipBonusBundle.cs` — has `AddEffOnAttack`/`AddEffWhenHit` (on-hit) but no
  per-skill `AddEffOnSkill` map.
- `Map.Server/Skills/...` — the skill cast/hit path applies no on-skill equip proc.

## rAthena reference (source of truth)

- `pc.cpp` SP_ADDEFF_ONSKILL (`pc_bonus_addeff_onskill`); `skill.cpp skill_additional_effect`
  the `sd->addeff_onskill` loop gated on `skill_id`.

## Scope

- [ ] Add an `AddEffOnSkill` map (skillId → list of (sc, rate, [target flag])) to the bundle + parse
      the bonus3/4/5 forms in ScriptedBonusHost.
- [ ] In the skill post-hit path, apply the matching `AddEffOnSkill` entries for the cast skill id.

## Done criteria

- A bAddEffOnSkill, SK_X, Eff_Y, 10000 card inflicts Eff_Y when the player casts SK_X (not on a plain hit).

## Test plan

- Casting the matching skill applies the SC; a normal attack does not.
