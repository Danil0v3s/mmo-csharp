# COMBAT-35 — RE_LVL_DMOD per-arm completeness (remaining divisors + trap TMDMOD + macro-omitting disable)

> **Epic:** Combat parity · **Status:** ❌ Not started · **Size:** L · **Player-visible:** yes
> **Depends on:** COMBAT-03 · **Blocks:** none
> **Filed by:** COMBAT-14 on 2026-06-02 (the parts beyond the clean ComputeSkillDamage subset).

## Problem

COMBAT-14 shipped the per-arm 120/150 `RE_LVL_DMOD` divisor for the three weapon
plugins that route through `WeaponSkillImpl.ComputeSkillDamage` (PhantomThrust,
FallenEmpire, FeintBomb) by overriding `ReLvlDivisor`. Three larger pieces remain,
all rooted in the same fact: **`INF2_DISABLELVDMG` does NOT exist in this rAthena
checkout** (the INF2 enum has no such flag, and it appears nowhere in src) — so the
"data-driven disable" the original COMBAT-14 premise assumed is fictional. rAthena
controls level-scaling purely **per-arm**: each `battle_calc_attack_skill_ratio`
case either invokes `RE_LVL_DMOD(val)` or omits it; magic/misc use the single
`RE_LVL_MDMOD(100)` (Blitz-beat) and `RE_LVL_TMDMOD()` (Ranger traps) spots.

COMBAT-03 instead applies a **blanket** divisor 100 to every weapon skill (via the
`ReLvlDivisor` default) and an **unconditional** `× lv/100` to the entire
magic/misc paths above level 99. That over-scales every arm that omits the macro.

## Current state (C#)

- The remaining non-100 divisor plugins don't use `ComputeSkillDamage`, so an
  `ReLvlDivisor` override alone won't apply (their base computes damage itself):
  - **RecursiveDamageSplashSkillImpl** (override `SplashDamage`): `GC_COUNTERSLASH`
    (120, Thief/CounterSlash), `NC_COLDSLOWER` (150, Merchant/ColdSlower),
    `KO_BAKURETSU` (120, Ninja/KunaiExplosion), `SR_RAMPAGEBLASTER`
    (**conditional** 120 if target SC_EARTHSHAKER else 150, Acolyte/RampageBlaster).
  - **plain SkillImpl**: `NC_FLAMELAUNCHER` (150, Merchant/FlameLauncher),
    `SR_KNUCKLEARROW` (**conditional** 150 if `miscflag&4` else 100, Acolyte/KnuckleArrow),
    `EL_ROCK_CRUSHER` (120, ElementalNpc/RockLauncher).
  - **WeaponSkillImpl but carry pre-existing TODOs** (rule-1 blocked in COMBAT-14):
    `LG_PINPOINTATTACK` (120, Swordman/PinpointAttack — break-equip TODO),
    `KO_JYUMONJIKIRI` (120, Ninja/KoCrossSlash — double-hit/position-shift TODO).
- `Map.Server/Combat/BattleCalculator.cs` `CalcMiscAttack` — generic `level+int`
  base + unconditional `× lv/100` above 99. Ranger traps (`RA_CLUSTERBOMB`,
  `RA_FIRINGTRAP`, `RA_ICEBOUNDTRAP`) have their OWN base formula
  (`skill_lv*dex + int*5`) in their plugins, not this generic path, and use
  `RE_LVL_TMDMOD()` = `damage*150/100 + damage*lv/100`.

## rAthena reference

- `battle.cpp` non-100 arms (12): 5217 RK_PHANTOMTHRUST(150, done), 5227
  GC_COUNTERSLASH(120), 5312 NC_FLAMELAUNCHER/NC_COLDSLOWER(150), 5358
  SC_FEINTBOMB(120, done), 5401 LG_PINPOINTATTACK(120), 5459 SR_FALLENEMPIRE(150,
  done), 5486/5489 SR_RAMPAGEBLASTER(120/150 cond), 5499 SR_KNUCKLEARROW(150 cond),
  5641 EL_ROCK_CRUSHER/KO_JYUMONJIKIRI(120), 5665 KO_BAKURETSU(120), 8477
  NPC_JACKFROST(100/150 cond).
- `battle.cpp:9766` `RE_LVL_TMDMOD()` for RA_CLUSTERBOMB/FIRINGTRAP/ICEBOUNDTRAP.
- `config/const.hpp:95-104` macro defs.

## Scope

- [ ] **Apply the remaining 120/150 divisors.** Give `RecursiveDamageSplashSkillImpl`
      (and the plain-SkillImpl damage paths) a `ResolveReLvlDivisor(src,target,miscflag)`
      hook applied to the ratio, then override it on CounterSlash, ColdSlower,
      KunaiExplosion, FlameLauncher, RockLauncher, PinpointAttack, KoCrossSlash.
      Conditional: RampageBlaster (target SC_EARTHSHAKER → 120 else 150),
      KnuckleArrow (`miscflag&4` → 150 else 100). (Reword the PinpointAttack /
      KoCrossSlash pre-existing TODOs while there.)
- [ ] **Ranger trap TMDMOD.** In the trap plugins (or the misc path keyed on the trap
      skill ids), apply `damage = damage*150/100 + damage*lv/100` above level 99 +
      the RA_RESEARCHTRAP multiplier.
- [ ] **Disable scaling on macro-omitting arms.** Audit `battle_calc_attack_skill_ratio`
      (weapon + magic) + `battle_calc_misc_attack`: arms that omit `RE_LVL_DMOD`
      must NOT scale. Replace COMBAT-03's blanket weapon default-100 and the
      unconditional magic/misc `×lv/100` with per-arm application (most misc skills
      and a set of weapon/magic skills do not scale at all). Couple with COMBAT-12's
      magic-pipeline unification so magic plugins get per-arm `RE_LVL_DMOD`.

## Done criteria

- Each of the 12 non-100 arms scales by its rAthena divisor (incl. the two
  conditional ones) at level 175/300.
- Ranger traps use the TMDMOD formula.
- A weapon/magic/misc skill whose rAthena arm omits the macro deals identical
  damage at level 99 and 175.

## Test plan

- Per-plugin divisor tests (lv99 vs lv300 multiplier) for the remaining 9 + the 2
  conditional branches.
- Trap TMDMOD at lv150.
- A macro-omitting skill (e.g. a fixed-damage misc skill) is flat across 99↔175.

## Notes

- `INF2_DISABLELVDMG` is fictional in this rAthena — do not try to load it. The
  speculative `SkillInf2.DisableLvDmg` enum value (added by COMBAT-03) has no data
  source; either repurpose it as an internal "this plugin omits RE_LVL_DMOD" marker
  or drop it.
