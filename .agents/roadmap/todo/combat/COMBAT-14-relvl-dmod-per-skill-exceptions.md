# COMBAT-14 — RE_LVL_DMOD per-skill exceptions (INF2_DISABLELVDMG data gate, 120/150 divisors, trap TMDMOD)

> **Epic:** Combat parity · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** COMBAT-03 · **Blocks:** none
> **Filed by:** COMBAT-03 on 2026-06-01 (the per-skill refinements it shipped the default for).

## Problem

COMBAT-03 shipped the renewal base-level damage modifier with the rAthena **default
divisor 100** applied to every weapon skill (via `WeaponSkillImpl.ReLvlDivisor`) and
unconditionally to the magic/misc `BattleCalculator` paths above level 99. That is
correct for the large majority of skills, but rAthena has three per-skill exceptions
that COMBAT-03 could not honor:

1. **`INF2_DISABLELVDMG` opt-out is not data-driven.** `SkillDefinition.Inf2` is
   never populated from skill_db (the loader doesn't parse Inf2 flags at all), so
   the `DisableLvDmg` bit (added in COMBAT-03) is always unset. Fixed-damage skills
   that should NOT scale are therefore over-scaled above level 99 on the magic/misc
   paths, and on the weapon path unless a plugin overrides `ReLvlDivisor => 0`.
2. **Per-arm divisors 120/150.** A handful of `battle_calc_attack_skill_ratio` arms
   use `RE_LVL_DMOD(120)` / `RE_LVL_DMOD(150)` instead of 100; those plugins must
   override `ReLvlDivisor` accordingly (none do yet).
3. **Ranger-trap `RE_LVL_TMDMOD`.** Traps use a different formula —
   `md.damage * 150/100 + md.damage * lv/100` — not the standard `× lv/100`. The
   misc path currently applies only the standard variant.

## Current state (C#)

- `Map.Server/Skills/SkillDefinition.cs` — `SkillInf2.DisableLvDmg` exists; **never
  set** (no `Inf2 =` in `SkillDbLoader.cs` / `SkillDb.cs`).
- `Map.Server/Skills/Behaviors/SkillImpl.cs` — `ReLvlDivisor` virtual (default 100,
  0 disables) + `ApplyReLvlDmod` helper; `WeaponSkillImpl` applies it.
- `Map.Server/Combat/BattleCalculator.cs` — `CalcMagicAttack`/`CalcMiscAttack` apply
  `× level/100` above 99 unconditionally (comments cite this ticket).

## rAthena reference

- `config/const.hpp:94-104` — `RE_LVL_DMOD` / `RE_LVL_MDMOD` / `RE_LVL_TMDMOD`.
- `battle.cpp:4590` arms — grep for `RE_LVL_DMOD(` to enumerate the non-100 divisors
  and which arms carry the macro vs omit it.
- `skill.hpp` `INF2_DISABLELVDMG` + the `db/re/skill_db.yml` `Flags:` block that sets it.

## Scope

- [ ] **Load Inf2 flags from skill_db** (at least `DisableLvDmg`) — extend the skill
      loader / `SkillDefinition` population so `Inf2` is real. (This also unblocks any
      other Inf2-gated behavior.)
- [ ] **Gate the magic/misc paths** on `DisableLvDmg`: thread the flag into
      `BattleCalculator.CalcMagicAttack`/`CalcMiscAttack` (inject `ISkillDb`, optional)
      and skip the level modifier when set.
- [ ] **Weapon path**: audit `battle.cpp` arms; for each plugin whose arm omits the
      macro, override `ReLvlDivisor => 0`; for 120/150 arms, override to that divisor.
- [ ] **Trap TMDMOD**: add the ranger-trap variant to the misc path (or a per-trap
      hook) — `damage * 150/100 + damage * lv/100` above 99.

## Done criteria

- A skill carrying `INF2_DISABLELVDMG` (loaded from skill_db) deals identical damage
  at level 99 and 175 on all three paths.
- The 120/150-divisor arms scale by the correct divisor.
- Ranger traps use the TMDMOD formula.

## Test plan

- Loader test: a known `DisableLvDmg` skill_db row sets `Inf2.DisableLvDmg`.
- Magic/misc: a disabled skill doesn't scale at 175; a normal one does.
- Trap: TMDMOD formula at level 150 matches rAthena.

## Notes

- The magic-bolt path (`MagicBoltHelper`) doesn't run through `CalcMagicAttack`, so
  bolts won't pick up RE_LVL_MDMOD until the magic pipeline is unified — **COMBAT-12**.
