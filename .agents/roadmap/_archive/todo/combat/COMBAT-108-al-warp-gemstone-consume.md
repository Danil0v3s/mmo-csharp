# COMBAT-108 — AL_WARP Blue Gemstone requirement (cast-begin check + selection consume)

> **Epic:** combat · **Status:** ❌ Not started · **Size:** S · **Player-visible:** yes
> **Depends on:** COMBAT-86, COMBAT-92 (skill_db Required-item column) · **Blocks:** none
> **Filed by:** COMBAT-86 — it deferred the SP consume to the destination pick; the Blue Gemstone
> item requirement can't be checked/consumed until the skill_db Required-item column is loaded.

## Problem

rAthena AL_WARP requires **1 Blue Gemstone** (`skill_db.yml` AL_WARP `Requirements.ItemCost`). It is
checked at cast-begin (`skill_check_condition_castbegin` — fail if absent) and consumed at the
destination pick (`skill_castend_map` → `skill_consume_requirement(…, 2)`), NOT on cancel. COMBAT-86
deferred the SP, but the gemstone is neither checked nor consumed because `ConsumeRequirement(type & 2)`
is a no-op (the skill_db Required-item column isn't loaded) and `SpCost`-only data is available.

## Current state (C#)

- `Map.Server/Skills/SkillRequirementService.cs:ConsumeRequirement` — `type & 2` (items/ammo) is a
  documented no-op (data-pending on the skill_db require column).
- `Map.Server/Skills/SkillCastService.cs:StartCast` — no item-requirement check for AL_WARP.
- `Map.Server/Skills/SkillCastEndService.cs:CastEndMap` AL_WARP — consumes SP (COMBAT-86), not items.

## rAthena reference (source of truth)

- `skill.cpp:15740` `skill_check_condition_castend` (re-check) + `15746 skill_consume_requirement(…,2)`.
- `db/re/skill_db.yml` AL_WARP `Requirements: { ItemCost: [{ Item: Blue_Gemstone, Amount: 1 }] }`.

## Scope

- [ ] Load the AL_WARP Required-item (Blue Gemstone ×1) — via the COMBAT-92 column loader, or a
      curated requirement until then; implement `ConsumeRequirement(type & 2)` to remove the items.
- [ ] Cast-begin: fail AL_WARP if the caster lacks the gemstone (no chooser).
- [ ] Selection: consume 1 Blue Gemstone on a successful pick (not on cancel).

## Done criteria

- Casting AL_WARP without a Blue Gemstone fails (no chooser); a successful pick consumes exactly one;
  cancel consumes none.

## Test plan

- No-gemstone cast → fail; pick → gemstone −1; cancel → gemstone unchanged.
