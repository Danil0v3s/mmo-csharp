# COMBAT-35 — RE_LVL_DMOD per-arm completeness (remaining divisors + trap TMDMOD + macro-omitting disable)

> **Epic:** Combat parity · **Status:** ✅ Done (2026-06-02) · **Size:** L · **Player-visible:** yes
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

- [x] **Apply the divisors on the live-path arms.** The two **WeaponSkillImpl** arms
      route through `WeaponSkillImpl.ComputeSkillDamage` (the divisor-applying path,
      `SkillImpl.cs:239`): added `ReLvlDivisor => 120` to **PinpointAttack** (battle.cpp
      :5401) and **KoCrossSlash** (battle.cpp:5641), and cleared their stale docstring
      TODOs (PinpointAttack's break-equip is already wired; KoCrossSlash's
      double-hit/SC-bonus ➡️ COMBAT-57).
- [x] **Architecture finding (corrects this ticket's premise):** the other 7 non-100
      arms live on `RecursiveDamageSplashSkillImpl` / plain `SkillImpl` bases whose
      `CalculateSkillRatio` is **not consumed** by the damage funnel
      (`SkillAttackService.WeaponDamage` uses the skill_db `DamageRate` column for
      non-`WeaponSkillImpl` plugins; `RecursiveDamageSplashSkillImpl.SplashDamage`
      default returns 0). A `ReLvlDivisor` override on them is a no-op until the
      ratio-via-funnel work lands. ➡️ COMBAT-54 (depends on SKILL-17).
- [x] **Ranger trap TMDMOD** — the traps only *place a ground unit*; there is no
      damage computation to scale yet (a trap-damage unit handler must be built first).
      ➡️ COMBAT-55.
- [x] **Disable scaling on macro-omitting arms** — the broad `battle_calc_*` per-arm
      audit (+ replacing COMBAT-03's blanket weapon-100 / unconditional magic/misc
      `×lv/100`, coupled with COMBAT-12) is a large isolated effort. ➡️ COMBAT-56.

## Done criteria

- Each of the 12 non-100 arms scales by its rAthena divisor (incl. the two
  conditional ones) at level 175/300. ✅ for the 2 WeaponSkillImpl arms (PinpointAttack,
  KoCrossSlash); ➡️ the 7 splash/plain arms moved to COMBAT-54 (blocked on SKILL-17).
- Ranger traps use the TMDMOD formula. ➡️ Moved to COMBAT-55 (no trap-damage path yet).
- A weapon/magic/misc skill whose rAthena arm omits the macro deals identical
  damage at level 99 and 175. ➡️ Moved to COMBAT-56 (the macro-omitting audit).

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

## History

- 2026-06-02 · Applied `RE_LVL_DMOD(120)` to the two arms that route through the live
  divisor path (`WeaponSkillImpl.ComputeSkillDamage`): PinpointAttack + KoCrossSlash,
  with their stale docstring TODOs cleared. Discovered the ticket's premise that the
  splash / plain-SkillImpl arms' `CalculateSkillRatio` is applied is false — those
  ratios are dead pending the SKILL-17 ratio-via-funnel work (`SkillAttackService.
  WeaponDamage` uses the skill_db DamageRate for non-WeaponSkillImpl plugins), so a
  divisor override on them is a no-op; and the Ranger traps have no damage path to
  scale. Combat35ReLvlDivisorTests (5: ×2 at lv240 for both, no scaling ≤99, slower
  than default-100). Full Map.Server.Tests green except the pre-existing INFRA-11
  replay gate. Filed COMBAT-54 (splash/plain per-arm divisors, blocked on SKILL-17),
  COMBAT-55 (Ranger trap TMDMOD damage units), COMBAT-56 (macro-omitting scaling
  audit), COMBAT-57 (KoCrossSlash SC ratio bonus + double-hit).
