# COMBAT-56 — Per-arm RE_LVL_DMOD audit: disable scaling on macro-omitting arms

> **Epic:** Combat parity · **Status:** ✅ Done (2026-06-02) · **Size:** L · **Player-visible:** yes
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

- [x] Audited `battle_calc_attack_skill_ratio` (weapon + magic) + `battle_calc_misc_attack`
      for the arms that OMIT the macro (scan of battle.cpp: 159 weapon/magic + 4 misc skills
      resolvable to C# `SkillIds`). Encoded as the data-driven `ReLvlDmodOmit` set (the
      ticket's "internal omit marker"); `SkillImpl.ComputeSkillDamage` forces divisor 0 for
      `OmitsRatioScaling(SkillId)`.
- [x] Replaced the unconditional magic/misc `×lv/100` with the per-arm gate:
      `CalcMagicAttack` skips it for `OmitsRatioScaling`, `CalcMiscAttack` for `OmitsMiscScaling`.
- [x] Dropped `SkillInf2.DisableLvDmg` (no rAthena data source; superseded by `ReLvlDmodOmit`).
- [x] Updated the COMBAT-03/14 blanket-scaling tests to per-arm expectations (Bash/SM_BASH
      omits → flat; RK_SONICWAVE macro-using → scales).

## Done criteria

- ➡️ from COMBAT-35: a weapon/magic/misc skill whose rAthena arm omits the macro
  deals identical damage at level 99 and 175. ✅ — Bash flat 99↔175; the omit-set membership
  + magic/misc gates verified; non-omit (SonicWave) still scales.

## History

- 2026-06-02 — Per-arm RE_LVL_DMOD: replaced the blanket weapon default-100 + unconditional
  magic/misc `×lv/100` with the data-driven `ReLvlDmodOmit` set (159 weapon/magic + 4 misc skills
  scanned from battle.cpp, resolved to `SkillIds`). Wired into `SkillImpl.ComputeSkillDamage`
  (divisor→0 for omit skills) + `BattleCalculator.CalcMagicAttack`/`CalcMiscAttack` (gate the
  >99 scaling). Dropped the dead `SkillInf2.DisableLvDmg`. Updated Combat03/Combat14 tests
  (Bash now correctly flat); new `Combat56ReLvlDmodOmitTests` (10). Full suite 4019 pass
  (1 fail = pre-existing INFRA-11 replay gate). No follow-ups (the 15 unresolvable rAthena
  labels have no C# port → never reach the scaling path).

## Test plan

- A macro-omitting skill (e.g. a fixed-damage misc skill) is flat across 99↔175.
- A macro-using skill still scales by its divisor.
