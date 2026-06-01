# COMBAT-03 — Renewal base-level damage modifier (RE_LVL_DMOD)

> **Epic:** Combat parity · **Status:** 🚧 In progress · **Size:** M · **Player-visible:** yes
> **Depends on:** COMBAT-02 (the ratio stage this modifier post-multiplies) · **Blocks:** none

## Problem

In renewal, almost every 2nd-class-and-up skill scales its damage by the caster's base
level above 99 via the `RE_LVL_DMOD` macro. This is implemented **nowhere** in the C#
port — `grep` for `RE_LVL`, `LvDmg`, base-level-damage scaling returns nothing in
`BattleCalculator.cs` / `SkillAttackService.cs` / `SkillImpl.cs`. Hundreds of skill
plugins carry a docstring comment claiming the level modifier is "applied at calc time",
but no calc-time stage exists. Net result: every renewal skill is mis-scaled — a 3rd-class
skill does the same damage at base level 175 as at 99, when rAthena would scale it ~1.77×.

## Current state (C#)

- `Map.Server/Skills/Behaviors/SkillImpl.cs:152-164` — `WeaponSkillImpl.CastendDamageId`
  computes `swing.Total * ratio / 100` and applies it. **No level-modifier stage.**
- `Map.Server/Combat/BattleCalculator.cs` — no reference to caster base level in the damage
  multiply (level is only used in `CalcMisc` stat derivation).
- `Map.Server/Skills/SkillDefinition.cs:31-50` — `SkillInf2` enum is missing the rAthena
  `INF2_DISABLELVDMG` flag entirely, so there is no per-skill opt-out bit to gate on.
- The many `RE_LVL_DMOD(100)` comments in `Map.Server/Skills/Behaviors/**` are aspirational;
  no code reads them.

## rAthena reference (source of truth)

Canonical: `battle.cpp` switch arms + the macro in `config/const.hpp`. Confirmed by reading:

- `src/config/const.hpp:94-104` — the exact macro (guarded by `RENEWAL_LVDMG`, which our
  renewal build defines):
  ```c
  #define RE_LVL_DMOD(val) \
      if( status_get_lv(src) > 99 && val > 0 ) \
          skillratio = skillratio * status_get_lv(src) / val;
  ```
  Variants: `RE_LVL_MDMOD(val)` (magic, on `md.damage`), `RE_LVL_TMDMOD()` (ranger trap
  special: `md.damage * 150/100 + md.damage * lv/100`).
- `battle.cpp:4590` `battle_calc_attack_skill_ratio` — most arms end with `RE_LVL_DMOD(100)`
  (e.g. `CH_TIGERFIST` at `battle.cpp:4884`, `CH_CHAINCRUSH` at `:4892`); some use other
  divisors (`RE_LVL_DMOD(150)`, `RE_LVL_DMOD(120)`). The modifier runs **inside** the ratio
  switch, i.e. it multiplies `skillratio` *before* `ATK_RATE` applies it. Effectively:
  `effective_ratio = skillratio * baseLv / val` when `baseLv > 99`.
- The opt-out is `INF2_DISABLELVDMG` (skill_db `Flags: DisableLazyHazard`-style yaml key
  `DisableLvDmg`/`Lvdamage`): skills that carry it skip the modifier (e.g. fixed-damage
  skills). Arms that should NOT scale simply omit the macro; the INF2 flag is the data-driven
  equivalent the engine checks for the generic path.

## Scope — every sub-system that must be touched

- [ ] **`SkillInf2` enum (`SkillDefinition.cs:31`)**: add `DisableLvDmg = 1UL << 16` (or the
      next free bit) mapped to rAthena `INF2_DISABLELVDMG`. Wire the skill_db loader
      (`SkillDb.cs`) to set it from the yaml `Flags` block.
- [ ] **Add a level-modifier helper** on `SkillImpl` (or a shared static in
      `Map.Server/Combat`): `protected static int ApplyReLvlDmod(int ratio, Entity src,
      int divisor = 100)` returning `src.Level > 99 ? ratio * src.Level / divisor : ratio`.
- [ ] **Wire it into `WeaponSkillImpl.CastendDamageId` (`SkillImpl.cs:160`)**: after
      `CalculateSkillRatio`, before applying — but **only** when the skill does NOT carry
      `DisableLvDmg`. Most plugins use the default divisor 100; plugins needing 150/120 must
      override a new virtual `protected virtual int ReLvlDivisor => 100;` (return 0 to
      disable, mirroring `val > 0` guard).
- [ ] **Magic path (`BattleCalculator.CalcMagicAttack`, `:298`)**: the analogous
      `RE_LVL_MDMOD` multiplies the magic damage by `baseLv/100` above 99. Add the same gated
      multiply there, keyed off `source.Level` and the skill's `DisableLvDmg` flag (thread the
      flag in via the existing `skillId` param + `ISkillDb.GetInf2`).
- [ ] **Misc/trap path (`CalcMiscAttack`, `:359`)**: ranger traps use `RE_LVL_TMDMOD`
      (`md.damage*150/100 + md.damage*lv/100`). Add a misc variant; the trap plugins can opt
      into it.
- [ ] **No DB migration, no packets, no IPC** — formula + one enum bit + loader.

## Done criteria

- A 3rd-class weapon skill at base level 175 deals `ratio175 = ratio99 × 175 / 100` (i.e.
  1.75×) more pre-card/def damage than the same skill at level 99; at level 99 and below the
  modifier is a no-op (exact equality with current behavior).
- A skill flagged `DisableLvDmg` deals identical damage at level 99 and 175.
- Magic skills scale with `RE_LVL_MDMOD` (e.g. Storm Gust at 175 vs 99).
- The aspirational `RE_LVL_DMOD(100)` comments now correspond to real applied behavior.

## Test plan

- Unit test `ApplyReLvlDmod`: level 99 → unchanged; level 100 → `ratio*100/100`; level 175,
  divisor 100 → `ratio*175/100`; divisor 0 → unchanged; `DisableLvDmg` skill → unchanged.
- Integration: pick one 3rd-class weapon skill plugin, run `CastendDamageId` at level 99 and
  175 with a fixed swing, assert the 1.75× ratio on the pre-def damage.
- Magic: same for one bolt skill via `CalcMagicAttack`.

## Notes / gotchas

- The modifier multiplies the **ratio**, not the post-def damage, in rAthena (it's inside the
  ratio switch). Applying it to the post-ratio damage value is mathematically equivalent
  **only if** done before the constant-addition stage (COMBAT-02) and before def reduction.
  Apply it to `ratio` (cleanest) so order can't drift.
- Divisor is per-arm in rAthena (mostly 100, occasionally 120/150). Don't hardcode 100
  globally — expose `ReLvlDivisor` so the handful of non-100 skills are correct.
- `status_get_lv(src)` is **base** level, not job level. Use `src.Level` (the entity's base
  level), not `JobLevel`.
- Mobs/NPCs: `status_get_lv` works for them too, but mob skills rarely carry the macro; the
  gate `src.Level > 99` naturally excludes most mobs. No special-casing needed.
