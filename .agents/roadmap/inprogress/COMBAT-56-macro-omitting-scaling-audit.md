# COMBAT-56 — Per-arm RE_LVL_DMOD audit: disable scaling on macro-omitting arms

> **Epic:** Combat parity · **Status:** 🚧 In progress · **Size:** L · **Player-visible:** yes
> **Depends on:** COMBAT-35, COMBAT-12 (magic-pipeline unification)
> **Blocks:** none
> **Filed by:** COMBAT-35 — the blanket weapon default-100 + unconditional magic/misc
> `×lv/100` over-scale every arm that omits the macro.

## Problem

rAthena controls renewal level-scaling **per arm**: each
`battle_calc_attack_skill_ratio` / `battle_calc_misc_attack` case either invokes
`RE_LVL_DMOD(val)` / `RE_LVL_MDMOD(100)` or **omits it entirely** (fixed-damage and a
number of weapon/magic/misc skills do not scale at all). The C# port instead applies:

- a **blanket** `ReLvlDivisor => 100` to every weapon skill, and
- an **unconditional** `× lv/100` above level 99 in `CalcMagicAttack` /
  `CalcMiscAttack`.

So every arm that omits the macro is over-scaled above level 99.

`INF2_DISABLELVDMG` does **not** exist in this rAthena checkout (the speculative
`SkillInf2.DisableLvDmg` enum value added by COMBAT-03 has no YAML data source) — the
disable must be encoded per-arm (override `ReLvlDivisor => 0` on the weapon plugins
that omit it, and an analogous per-skill opt-out on the magic/misc paths), not loaded
from a flag.

## Current state (C#)

- `Map.Server/Skills/Behaviors/SkillImpl.cs:ReLvlDivisor` — default 100; `=> 0`
  already disables (the `divisor > 0` guard in `ApplyReLvlDmod`).
- `Map.Server/Combat/BattleCalculator.cs:CalcMagicAttack` (~line 551) and
  `CalcMiscAttack` (~line 621) — unconditional `damage = damage * source.Level / 100`
  above 99.
- `Map.Server/Skills/SkillDefinition.cs:SkillInf2.DisableLvDmg` — defined, never read.

## Scope

- [ ] Audit `battle_calc_attack_skill_ratio` (weapon + magic) + `battle_calc_misc_attack`
      for the arms that OMIT `RE_LVL_DMOD`/`RE_LVL_MDMOD`; for each, disable scaling
      (`ReLvlDivisor => 0` on the weapon plugin; a per-skill opt-out the magic/misc
      paths honor).
- [ ] Replace the unconditional magic/misc `×lv/100` with per-arm application (couple
      with COMBAT-12's magic-pipeline unification so magic plugins carry per-arm
      `RE_LVL_DMOD`).
- [ ] Repurpose or drop `SkillInf2.DisableLvDmg` (it has no rAthena data source — use
      it as the internal "this arm omits RE_LVL_DMOD" marker, or remove it).
- [ ] Update the COMBAT-03 blanket-scaling tests to the per-arm expectations.

## Done criteria

- ➡️ from COMBAT-35: a weapon/magic/misc skill whose rAthena arm omits the macro
  deals identical damage at level 99 and 175.

## Test plan

- A macro-omitting skill (e.g. a fixed-damage misc skill) is flat across 99↔175.
- A macro-using skill still scales by its divisor.
