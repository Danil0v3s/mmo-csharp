# COMBAT-14 — RE_LVL_DMOD per-skill exceptions (INF2_DISABLELVDMG data gate, 120/150 divisors, trap TMDMOD)

> **Epic:** Combat parity · **Status:** ✅ Done (2026-06-02) · **Size:** M · **Player-visible:** yes
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

> **Premise correction (2026-06-02):** `INF2_DISABLELVDMG` does **not exist** in this
> rAthena checkout — the INF2 enum has no such flag and it appears nowhere in src.
> rAthena controls level-scaling purely **per-arm** (each `battle_calc_attack_skill_ratio`
> case invokes `RE_LVL_DMOD(val)` or omits it). So scope items 1–2 (load + gate the
> fictional flag) are void; the real work is per-arm divisors. This ticket ships the
> clean exact subset; the heterogeneous remainder is **COMBAT-35**.

- [x] ~~Load Inf2 `DisableLvDmg` from skill_db~~ — ✅ **void**: the flag doesn't exist.
      The disable is per-arm (omit the macro). ➡️ **COMBAT-35** (macro-omitting audit).
- [x] **Per-arm 120/150 divisors (clean subset)** — ✅ overrode `ReLvlDivisor` on the
      three weapon plugins that route through `ComputeSkillDamage`: PhantomThrust(150),
      FallenEmpire(150), FeintBomb(120). `ApplyReLvlDmod` scales the ratio by
      `casterBaseLv / divisor` above 99.
- [ ] **Remaining 120/150 divisors** (Recursive-splash / plain-SkillImpl / conditional
      bases + the two TODO-carrying plugins) ➡️ **COMBAT-35**.
- [ ] **Trap TMDMOD** — Ranger traps compute their base formula in their own plugins,
      not `CalcMiscAttack`'s generic `level+int`, so TMDMOD belongs there ➡️ **COMBAT-35**.
- [ ] **Disable scaling on macro-omitting arms** (replace COMBAT-03's blanket weapon
      default-100 + unconditional magic/misc `×lv/100`) ➡️ **COMBAT-35**.

## Done criteria

- ✅ ~~INF2_DISABLELVDMG skill flat at 99↔175~~ — void (flag fictional). ➡️ COMBAT-35.
- ◑ The 120/150-divisor arms scale by the correct divisor — ✅ for the 3
  ComputeSkillDamage plugins (`Combat14ReLvlDivisorTests`: ×2 for divisor-150, ×2.5 for
  divisor-120 at lv300 vs ×3 default-100); the other 9 ➡️ **COMBAT-35**.
- ➡️ Ranger traps use the TMDMOD formula ➡️ **COMBAT-35**.

## Test plan

- Loader test: a known `DisableLvDmg` skill_db row sets `Inf2.DisableLvDmg`.
- Magic/misc: a disabled skill doesn't scale at 175; a normal one does.
- Trap: TMDMOD formula at level 150 matches rAthena.

## Notes

- The magic-bolt path (`MagicBoltHelper`) doesn't run through `CalcMagicAttack`, so
  bolts won't pick up RE_LVL_MDMOD until the magic pipeline is unified — **COMBAT-12**.

## History

- 2026-06-02 · Discovered the ticket's INF2_DISABLELVDMG premise is fictional in this
  rAthena (no such flag in the INF2 enum / src) — corrected the scope to the real
  per-arm reality. Shipped the clean exact subset: ReLvlDivisor overrides for the three
  RE_LVL_DMOD 120/150 weapon plugins that route through ComputeSkillDamage —
  PhantomThrust(150), FallenEmpire(150), FeintBomb(120). Combat14ReLvlDivisorTests (4):
  divisor-150 ×2 / divisor-120 ×2.5 at lv300 vs default-100 ×3. Suite 3744 green. Filed
  COMBAT-35 for the heterogeneous remainder (9 more divisor plugins incl. 2 conditional,
  Ranger trap TMDMOD, and the disable-scaling-on-macro-omitting-arms audit across
  weapon/magic/misc).
