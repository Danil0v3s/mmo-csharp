# SKILL-09 — Family: Ninja / Kagerou-Oboro (7 shells of 63)

> **Epic:** Skills · **Status:** ❌ Not started · **Size:** L · **Player-visible:** yes
> **Depends on:** SKILL-01 (SC apply-rate), SKILL-04 (durations) · **Blocks:** none

## Problem

The Ninja / Kagerou / Oboro family has **7 of 63 plugin files that are bare shells**:
splash skills that use the inherited 2-cell default radius (wrong size) and default
100 % ratio (wrong damage), plus SC buffs that animate but apply no SC. The
splash shells in particular trip the `RecursiveDamageSplashSkillImpl` default
(`GetSplashSearchSize => 2`, `SplashDamage => 0`) — so `KunaiSplash` and `RapidThrow`
either hit the wrong radius or deal **zero** damage (the base `SplashDamage` returns
0). Their own docstrings describe the correct behavior but don't implement it.

The SC buffs (`DistortedCrescent`, `EmptyShadow`, `OminousMoonlight`, `ShadowWarrior`,
`ShadowHiding`) are `StatusSkillImpl` with no `TargetSc` and no apply — `ShadowHiding`
even admits *"Animation only here; the dedicated SC enum is not yet exposed."*

## Current state (C#)

- `Map.Server/Skills/Behaviors/Ninja/` — 63 files; 7 have no `override`:
  - `KunaiSplash.cs` (KO_HAPPOKUNAI, `skill.cpp:5779`) — `RecursiveDamageSplashSkillImpl` with no `GetSplashSearchSize` / `SplashDamage` override → **2-cell default radius + 0 damage** (base `SplashDamage` returns 0).
  - `RapidThrow.cs` (KO_MUCHANAGE, `skill.cpp:3863`) — same; docstring even states the intended hit-rate gate `(100 - 1000/(dex+luk)*5) * (lv/2 + 5) / 10` but it's unimplemented; no `SplashDamage`.
  - `DistortedCrescent.cs` (OB_ZANGETSU, `skill.cpp:12762`) — `StatusSkillImpl`, no SC.
  - `EmptyShadow.cs` (KG_KYOMU, `skill.cpp:12763`) — no SC.
  - `OminousMoonlight.cs` (KO_IZAYOI, `skill.cpp:12761`) — no SC.
  - `ShadowWarrior.cs` (KG_KAGEMUSYA, `skill.cpp:12764`) — doc says SC_KAGEMUSYA; no apply.
  - `ShadowHiding.cs` (KO_YAMIKUMO, `skill.cpp:9376`) — doc: *"Toggles SC_YAMIKUMO … Animation only here; the dedicated SC enum is not yet exposed."*
- `Map.Server/Skills/Behaviors/SkillImpl.cs:227/235` — `RecursiveDamageSplashSkillImpl` defaults: `GetSplashSearchSize => 2`, `SplashDamage => 0`. A shell splash deals nothing.
- `StatusType` enum — confirm SC_YAMIKUMO / SC_KAGEMUSYA / SC_ZANGETSU / SC_KYOMU / SC_IZAYOI exist; `ShadowHiding` says one is missing. Add the missing enum members.

## rAthena reference (source of truth)

- `rathena/src/map/skill.cpp:5779` `KO_HAPPOKUNAI` — splash; damage `battle.cpp:4590` arm scales by kunai count + skill_lv; splash radius from `skill_get_splash`.
- `rathena/src/map/skill.cpp:3863` `KO_MUCHANAGE` — throw-coins splash; hit-rate gated by `(100 - 1000/(dex+luk)*5) * (skill_lv/2 + 5) / 10` (the exact formula the C# docstring quotes); requires Misty Frost / zeny throw cost.
- `rathena/src/map/skill.cpp:12761-12764` — `KO_IZAYOI` / `OB_ZANGETSU` / `KG_KYOMU` / `KG_KAGEMUSYA`: `sc_start` self/target buffs with `skill_get_time`. `:9376` `KO_YAMIKUMO` (SC_YAMIKUMO hiding-like state).
- `battle.cpp:4590` — per-skill ratio arms for the two splash skills.
- Monolithic-switch caveat: canonical source is `skill.cpp` Ninja/KG/OB/KO arms + `battle.cpp:4590` ratio; the split-file `rathena-fork/src/map/skills/ninja/*.cpp` paths in the docstrings DO NOT exist here — map to the `case KO_*:` arms.

## Scope — every sub-system that must be touched

- [ ] **`KunaiSplash`** — override `GetSplashSearchSize` to read `ctx`/`GetSplash(SkillId, lv)` (NOT the hardcoded 2), and `SplashDamage` to compute the per-victim damage via the `battle.cpp:4590` `KO_HAPPOKUNAI` ratio (kunai-count × lv). Element/hits from skill_db.
- [ ] **`RapidThrow`** — override `GetSplashSearchSize` from skill_db, `SplashDamage` per the ratio arm, AND implement the hit-rate gate `(100 - 1000/(dex+luk)*5) * (lv/2 + 5) / 10` (its own docstring's formula) in `ModifyHitRate` or the splash roll. Wire the zeny/Misty-Frost cost if required.
- [ ] **SC buffs** — `DistortedCrescent`/`EmptyShadow`/`OminousMoonlight`/`ShadowWarrior`/`ShadowHiding`: set `TargetSc` + apply via `ctx.Sc.Start(rate, GetTime)`. Self-buffs at guaranteed rate; debuffs through the SKILL-01 resist path.
- [ ] **`StatusType` additions** — add any missing SC enum members (SC_YAMIKUMO etc.) so `ShadowHiding` can apply its real SC. Wire a minimal `StatusEffectHandler` if the SC has an active effect (hiding-like for Yamikumo).
- [ ] **DI** — all stay registered; no orphan.
- [ ] **No new packets** beyond the `clif_skill_*` broadcasts already emitted.

## Done criteria

- `KunaiSplash` deals non-zero splash damage at the skill_db radius with the `battle.cpp:4590` ratio (test: damage > 0, radius matches `GetSplash`).
- `RapidThrow` deals splash damage and its hit-rate gate matches the formula for a worked (dex, luk, lv) example (test).
- Each Ninja SC buff applies its real SC at rate + duration (test per skill); `ShadowHiding` applies SC_YAMIKUMO, no longer "animation only."
- The missing `StatusType` members exist and are handled.
- No `TODO` / "animation only" / "SC enum not yet exposed" comment remains.
- No no-override `RecursiveDamageSplashSkillImpl` (zero-damage) shell remains in `Ninja/`.

## Test plan

- `NinjaSplashTests.KunaiSplash_DealsDamageAtDbRadius` — damage > 0, radius == `GetSplash`.
- `NinjaSplashTests.RapidThrow_HitRateFormula` — assert the gate value for (dex, luk, lv) inputs.
- `NinjaScTests` — each buff applies its SC at the right duration; `ShadowHiding` → SC_YAMIKUMO.
- DI audit green.

## Full Ninja-family shell inventory (the 7)

| Plugin | Skill id | rAthena | Gap | Fix kind |
|---|---|---|---|---|
| `KunaiSplash` | KO_HAPPOKUNAI | skill.cpp:5779 | 2-cell radius + 0 dmg | override `GetSplashSearchSize` + `SplashDamage` |
| `RapidThrow` | KO_MUCHANAGE | skill.cpp:3863 | 0 dmg + no hit-rate gate | splash dmg + `ModifyHitRate` formula |
| `DistortedCrescent` | OB_ZANGETSU | skill.cpp:12762 | no SC | `TargetSc` + apply |
| `EmptyShadow` | KG_KYOMU | skill.cpp:12763 | no SC | `TargetSc` + apply |
| `OminousMoonlight` | KO_IZAYOI | skill.cpp:12761 | no SC | `TargetSc` + apply |
| `ShadowWarrior` | KG_KAGEMUSYA | skill.cpp:12764 | no SC (SC_KAGEMUSYA) | `TargetSc` + apply |
| `ShadowHiding` | KO_YAMIKUMO | skill.cpp:9376 | no SC; enum missing | add SC_YAMIKUMO + apply |

## Notes / gotchas

- The `RecursiveDamageSplashSkillImpl` base returns `SplashDamage => 0` by default — a splash shell that forgets to override deals literally nothing while still animating. This is the single most common Ninja-family trap; verify damage > 0 in tests, not just "no crash."
- `GetSplashSearchSize` defaults to 2; many Ninja splashes are larger or smaller. Read `GetSplash` from skill_db (SKILL-04 plumbs `ctx.SkillDb`), don't keep the 2.
- KO_MUCHANAGE's hit-rate formula is exact — copy it from the `skill.cpp:3863` arm, don't approximate; the C# docstring already has it.
