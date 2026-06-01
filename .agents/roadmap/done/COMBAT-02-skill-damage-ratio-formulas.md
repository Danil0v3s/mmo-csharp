# COMBAT-02 — Skill damage ratio formulas + constant-addition stage

> **Epic:** Combat parity · **Status:** ✅ Done (2026-06-01) · **Size:** L · **Player-visible:** yes
> **Depends on:** none · **Blocks:** COMBAT-03 (RE_LVL_DMOD plugs onto the ratio stage)
>
> **Scope notes:** the weapon ratio pipeline + constant-addition stage shipped here,
> validated on the 5 named representatives. Two named items moved out: **Soul Strike**
> is a *magic* skill whose plugin ratio is ignored by the magic path → **COMBAT-12**
> (magic ratio pipeline). Asura's renewal **×2 when >5 spirit spheres** → **COMBAT-13**.
> The per-skill ratio audit of the other ~1000 skills + their constants stays with
> the wave-98 / **SKILL-12** stream. Retiring the dead `DamageRate` path = **SKILL-05**.

## Problem

Skill damage in rAthena = `base_weapon_damage × skillratio% (+ constant additions)`.
The C# port has **two competing ratio paths**, and the legacy one silently produces
wrong magnitudes:

- The **plugin path** (correct granularity): each skill plugin overrides
  `CalculateSkillRatio` (e.g. `Bash.cs:30`: `baseRatio + 30 * skillLevel`) and
  `WeaponSkillImpl.CastendDamageId` (`SkillImpl.cs:152-164`) applies it:
  `swing.Total * ratio / 100`. This is the rAthena `battle_calc_attack_skill_ratio`
  per-arm port.
- The **legacy fallback** in `SkillAttackService.SkillAttack`
  (`SkillAttackService.cs:55-61`) multiplies the swing by the flat `skill_db.DamageRate[lvl]`
  column. For a skill whose ratio is also encoded in its plugin, this double-counts or
  diverges; for skills with neither, it returns a flat per-level % that ignores all the
  `battle_calc_attack_skill_ratio` SC/level conditionals.

Additionally there is **no `battle_calc_skill_constant_addition` stage** — flat additive
terms (e.g. Asura's `(sp+1)*…`, AC_SHOWER bonus, GS skills' weapon-weight adds) are not
applied anywhere.

## Current state (C#)

- `Map.Server/Skills/Behaviors/SkillImpl.cs:71-94` — `CalculateSkillRatio` overloads
  (base, ctx-aware, miscflag-aware). Default returns `baseRatio` (pass-through).
- `Map.Server/Skills/Behaviors/SkillImpl.cs:152-164` — `WeaponSkillImpl.CastendDamageId`:
  `ratio = CalculateSkillRatio(100, …)`; `dmg = swing.Total * ratio / 100`. **No constant
  addition between ratio and apply.**
- `Map.Server/Skills/SkillAttackService.cs:55-65` — legacy `DamageRate[lvl]` multiply for
  `BattleAttackType.Weapon`. This is the path used by any caller that funnels through
  `SkillAttack` rather than a plugin's `CastendDamageId`.
- `Map.Server/Skills/Behaviors/Swordman/Bash.cs:30` — `baseRatio + 30 * skillLevel` (correct).
- `Map.Server/Combat/BattleCalculator.cs:37-213` — `CalcWeaponAttack` returns the base
  swing (no skill ratio). Magic/misc ratio comes in via the `ratePerLevel` arg to
  `CalcMagicAttack` / `CalcMiscAttack` (`:298,359`).

## rAthena reference (source of truth)

Canonical: the ~2000-line switch in `battle.cpp`; not split files.

- `battle.cpp:4590` `battle_calc_attack_skill_ratio` — `int32 skillratio = 100;` then a
  giant `switch(skill_id)` adding per-skill, e.g.:
  - `SM_BASH`: `skillratio += 30 * skill_lv;` (matches `Bash.cs`).
  - `MG_SOULSTRIKE`, `AC_DOUBLE` (Double Strafe): `skillratio += skill_lv * 100`-ish arms.
  - `MO_EXTREMITYFIST` (Asura): `skillratio += 8 + skill_lv * 100; skillratio += sd->spiritball_old * 50;`
    plus an `(sp+1)` style term in the constant-addition stage.
  - `AS_SONICBLOW`: `skillratio += 300 + 40 * skill_lv;` and `wd->div_ = 8` (multi-hit — see COMBAT-04).
  Many arms wrap a trailing `RE_LVL_DMOD(100)` — that level modifier is **COMBAT-03**.
- `battle.cpp:6606` `battle_calc_skill_constant_addition` — separate switch returning a
  **flat additive** (not %). Applied at `battle.cpp:7711`:
  `ATK_ADD(wd.damage, wd.damage2, battle_calc_skill_constant_addition(...))`, **after**
  `ATK_RATE(... battle_calc_attack_skill_ratio ...)` at `:7708`.
- Call order (PC weapon path) is: multi-attack → base damage → **ATK_RATE(ratio)** →
  **ATK_ADD(constant)** → cardfix → def → … (`battle.cpp:7676-7760`).

## Scope — every sub-system that must be touched

- [ ] **Make the plugin ratio path authoritative.** Audit `SkillAttackService.SkillAttack`
      (`:41-91`): for `BattleAttackType.Weapon`, when a plugin exists for `skillId`, it must
      NOT also multiply by `DamageRate[lvl]`. Decide: either route weapon skills exclusively
      through plugin `CastendDamageId`, or have `SkillAttack` look up the plugin's
      `CalculateSkillRatio(100, …)` and drop the `DamageRate` multiply. The skill_db
      `DamageRate` column should be treated as the *plugin's* `baseRatio` source only when a
      plugin chooses to read it (most hardcode), not applied twice.
- [ ] **Add a constant-addition hook** to `SkillImpl`: `public virtual long
      CalculateSkillConstantAddition(Entity src, Entity target, ushort skillLevel,
      SkillBehaviorContext ctx) => 0;` and apply it in `WeaponSkillImpl.CastendDamageId`
      between ratio and `ApplyDamage`:
      `dmg = swing.Total * ratio / 100; dmg += constant;`.
- [ ] **Port the representative arms** named below into their plugins (verify each against
      `battle.cpp:4590` arm): Bash (already ✓), Magnum Break, Double Strafe, Soul Strike,
      Asura Strike (ratio + the `(sp)` constant term). Confirm Asura's plugin
      (`Map.Server/Skills/Behaviors/Acolyte/AsuraStrike.cs`) overrides the constant hook.
- [ ] **No DB / packet / IPC changes** — pure formula wiring.

## Done criteria

- Bash lv10 on a target: damage ≈ `baseSwing × 400 / 100` (300 + 100 base = 400%), matching
  `Bash.cs` already; no double-application via `DamageRate`.
- Magnum Break lv10 (inner 100+20·lv / outer 100+10·lv) and Double Strafe lv10
  (100+10·(lv-1)) match the rAthena arms. ➡️ **Soul Strike → COMBAT-12** (magic path).
- Asura Strike: ratio includes the SP term (`sp*10`) and the constant-addition term
  (`250+150·lv`) contributes. ➡️ the renewal **×2 (>5 spheres) → COMBAT-13**.
- Sonic Blow ratio applied once; div=8 handled by COMBAT-04 (cross-link, not duplicated here).
- The legacy `DamageRate[lvl]` weapon multiply in `SkillAttackService.SkillAttack` no longer
  double-counts for skills that have a plugin.

## Test plan

- Add `Map.Server.Tests` cases per representative skill: construct a known attacker/target,
  set RNG to a fixed swing, call the plugin `CastendDamageId`, assert the dealt damage equals
  `swing × ratio / 100 + constant` with the rAthena-derived numbers.
- Add a guard test: a skill with a plugin must not have its damage multiplied by both the
  plugin ratio AND `DamageRate` (assert single application).
- Manual: cast Bash/Magnum/Double Strafe on a Poring, compare to a reference rAthena log line
  for identical stats.

## Notes / gotchas

- The miscflag-aware overload (`SkillImpl.cs:93`, `SKILL_ALTDMG_FLAG`) is already threaded by
  splash dispatchers — the constant-addition hook should also take/forward `ctx` so SC-gated
  constants (rare) can read status. Keep the default `=> 0`.
- `ATK_RATE` in rAthena operates on `wd.damage` AND `wd.damage2` (left-hand). Left-hand is
  COMBAT-04; here only `damage` (right hand) is in scope. Don't try to model `damage2` yet.
- Renewal applies an *ATK-percent* modifier (`battle_get_atkpercent`) **before** the
  skillratio switch (`battle.cpp:4604`). That is SC/equip `bonus bAtkRate` territory — leave
  it to COMBAT-06; this ticket only owns the per-skill switch + constant stage.

## History

- **2026-06-01** — Done (weapon ratio pipeline + constant stage). Added
  `SkillImpl.CalculateSkillConstantAddition` (rAthena `battle_calc_skill_constant_addition`,
  battle.cpp:6606) and applied it in `WeaponSkillImpl.CastendDamageId` after the
  ratio (`dmg = swing*ratio/100 + constant`), matching rAthena's
  ATK_RATE-then-ATK_ADD order (battle.cpp:7708-7711). `AsuraStrike` now adds its
  `250+150*lv` constant (was missing — Asura was short by exactly that). Fixed
  `MagnumBreak` ratio: was a flat `120+20*lv`; now the rAthena inner-3×3 `100+20*lv`
  / outer-5×5 `100+10*lv` split by Chebyshev distance from the caster. Verified
  Bash (`+30*lv`) and Double Strafe (`+10*(lv-1)`) already match rAthena. Confirmed
  the weapon ratio path (`CastendDamageId`) and the legacy `DamageRate`
  `SkillAttack` path are mutually exclusive (no double-count) and locked it with a
  guard test. Tests: new `Combat02RatioTests` (8). Full Map.Server suite
  **3594/3594** green. Follow-ups: **COMBAT-12** (magic ratio pipeline / Soul
  Strike), **COMBAT-13** (Asura >5-sphere ×2). Commits: start `9d1ca8b`, finish `<this>`.
