# SKILL-12 — Family depth-polish: Mage / Archer / Thief / Swordman / Merchant / Acolyte

> **Epic:** Skills · **Status:** ❌ Not started · **Size:** XL · **Player-visible:** yes
> **Depends on:** SKILL-01, SKILL-02, SKILL-04, COMBAT-03 (RE_LVL_DMOD) · **Blocks:** none

## Problem

The six actively-worked families (the wave-98 set) are *bodied* — almost no bare
shells — but carry **cross-cutting residual depth gaps** that affect damage and
timing parity. These are not "skill does nothing" bugs (those are the other family
tickets); they're "skill does roughly the right thing but the number is off" bugs,
applied broadly across many already-implemented plugins. Four cross-cutting classes
(call them X1–X4) plus per-family residual gap types:

- **X1 — multi-hit `Num` flattening.** Many plugins hardcode `div_`/hits to 1 or a
  literal instead of reading `skill_get_num` per level. The `SkillDb.GetNum` accessor
  exists and defaults to 1; plugins that should fan out N hits collapse to one. (The
  starter `MG_FIREBOLT` comment even says *"first slice flattens hits=1."*)
- **X2 — weapon-type branches.** Skills whose ratio/behavior branches on the equipped
  weapon (bow vs not, dagger vs sword, two-hand vs one-hand) skip the branch and
  apply one ratio for all weapons.
- **X3 — RE_LVL_DMOD missing.** The renewal base-level damage modifier
  (`(base_lv/100)^2`-style scaling) is absent everywhere — but that fix belongs to
  **COMBAT-03**; this ticket only *references* it and ensures the family ratio
  overrides compose with it once it lands (don't double-apply).
- **X4 — hardcoded durations / Val (SKILL-04).** Bodied plugins hardcode SC
  durations and side-effect windows. E.g. `SevereRainstorm` sets `pc.CanEquipTick =
  TickCount64 + skillLevel * 4000` instead of `skill_get_time`. SKILL-04 plumbs
  `ctx.SkillDb`; this ticket migrates the family call sites.

Per-family residual gap types: ground-unit cadence (Mage trap/pillar intervals),
trap/falcon branches (Archer), combo/coin/poison stacks (Thief), shield/spear weapon
branches + reflect (Swordman), cart/cannon ammo (Merchant), heal-formula + party
broadcast nuance (Acolyte).

## Current state (C#)

- `Map.Server/Skills/Behaviors/{Mage,Archer,Thief,Swordman,Merchant,Acolyte}/` — the actively-worked families; mostly bodied (the working-tree `git status` shows ~30 of these under active edit). Residual examples:
  - `Archer/SevereRainstorm.cs` — `pc.CanEquipTick = Environment.TickCount64 + skillLevel * 4000` (X4: should be `GetTime`).
  - `Mage/MeteorStorm.cs` — same-tick meteor drop (X1/SKILL-02) + `durationMs: 3000` stun literal (X4) + raw `_rng.Next(100) < 3*lv` proc (SKILL-01).
  - `Mage/PsychicWave.cs`, `Mage/FirePillar.cs` — multi-hit / ground-cadence notes (X1).
  - `Thief/BackStab.cs`, `EternalSlash.cs`, `FrenzyShot.cs`, `FatalMenace.cs`, `ImpactCrater.cs` — `div_`/hits notes (X1) + weapon-type (X2).
- `Map.Server/Skills/SkillDb.cs:341` — `GetNum` exists (defaults 1); `GetTime`/`GetTime2`/`GetTime3` exist (SKILL-04 plumbs them into `ctx.SkillDb`).
- `Map.Server/Skills/SkillImpl.cs:71-94` — `CalculateSkillRatio` overloads (the per-skill ratio hook, the correct path). `ModifyHitRate` for hit-rate branches.
- COMBAT-03 (`.agents/roadmap/combat/COMBAT-03-renewal-level-damage-modifier.md`) owns RE_LVL_DMOD.

## rAthena reference (source of truth)

- `rathena/src/map/battle.cpp:4590` `battle_calc_attack_skill_ratio` — per-skill ratio arms for all six families; the weapon-type branches (`sd->weapontype1 == W_BOW`, dagger/sword masks) live here.
- `rathena/src/map/skill.cpp` `skill_get_num` — the per-level hit count each multi-hit arm reads into `wd->div_`.
- `rathena/src/map/skill.cpp` per-family castend arms — ground-unit interval (`skill_get_unit_interval`), trap/falcon gates (`AC_*`/`HT_*`/`WM_*`), Thief poison/coin stacks, Swordman shield/spear branches, Merchant cart/cannon, Acolyte heal formula (`skill_calc_heal`) + party broadcast.
- RE_LVL_DMOD: `rathena/src/map/battle.cpp` `RE_LVL_DMOD(n)` macro — applied AFTER the skill ratio. (COMBAT-03.)
- Monolithic-switch caveat: canonical source is `battle.cpp:4590` (ratio + weapon branch) and `skill.cpp` (hits, durations, ground cadence); the split-file `rathena-fork/src/map/skills/<family>/*.cpp` paths in the docstrings DO NOT exist here — map to the `case SK_X:` arms.

## Scope — every sub-system that must be touched

Cross-cutting (apply across all six families):
- [ ] **X1 — multi-hit `Num`** — audit every family plugin for a hardcoded hit count; replace with `ctx.SkillDb.GetNum(SkillId, skillLevel)` feeding the damage div_. Multi-hit damage = per-hit ratio × hits, matching rAthena `wd->div_`. Remove "flattens hits=1" notes.
- [ ] **X2 — weapon-type branches** — for each ratio arm that branches on weapon type in `battle.cpp:4590`, add the branch in the plugin's `CalculateSkillRatio` via `ctx.Equip`/weapon-type read. (Bow gates for Archer, dagger/sword for Thief/Swordman.)
- [ ] **X3 — RE_LVL_DMOD compose** — ensure family `CalculateSkillRatio` overrides return the *pre-RE_LVL_DMOD* ratio so COMBAT-03 applies the modifier once. Add a regression that the modifier isn't double-applied. (Implementation of RE_LVL_DMOD itself is COMBAT-03.)
- [ ] **X4 — durations / Val from skill_db** — migrate every hardcoded `durationMs`/`CanEquipTick`/`val` literal in the six families to `ctx.SkillDb.GetTime/GetTime2/GetTime3` (per SKILL-04). `SevereRainstorm.CanEquipTick` → `GetTime`. SC procs → SKILL-01 rate path + `GetTime` duration.

Per-family residual:
- [ ] **Mage** — ground-unit interval/cadence via `GetUnitInterval`; meteor/comet trains via SKILL-02; magic multi-hit (bolts) via `GetNum`.
- [ ] **Archer** — falcon/warg gates (`ctx.Options`), trap deploy + `WH_ADVANCED_TRAP` duration (SKILL-06 B), Severe Rainstorm canequip (X4), Arrow Shower/Double Strafe hits (X1).
- [ ] **Thief** — poison/EDP stacks, coin/steal gates, combo continuation, dagger weapon branch (X2), multi-hit (Sonic/Eternal Slash) via `GetNum`.
- [ ] **Swordman** — shield/spear weapon branches (X2), Crush Strike / Brandish reflect + knockback (`GetBlewCount`), Vitality/Millennium Shield charge stacks.
- [ ] **Merchant** — cart/cannon ammo + weight branches, Crazy Uproar, Mammonite zeny cost.
- [ ] **Acolyte** — `skill_calc_heal` formula + party-broadcast heals (`ctx.PartyMap`), Lauda/Clearance/Adoramus proc → SKILL-01 rate path.
- [ ] **DI** — all stay registered; no orphan, no duplicate.
- [ ] **No new packets** beyond existing broadcasts.

## Done criteria

- No family plugin hardcodes a hit count where `skill_get_num` differs from 1 (X1); multi-hit skills deal per-hit × `GetNum` damage (test on a 2-hit and a 5-hit skill).
- Weapon-type-branching skills yield different ratios per weapon matching `battle.cpp:4590` (X2; test 2 weapons on one skill).
- RE_LVL_DMOD is applied exactly once for a family skill (X3; test no double-apply once COMBAT-03 lands).
- No hardcoded `durationMs`/`CanEquipTick`/`val` literal as the *primary* source in the six families (X4); `SevereRainstorm` reads `GetTime`.
- Per-family residual gaps closed per the checklist (one acceptance test per family).
- No "flattens hits=1" / "TODO" / "first slice" comments remain in the six families on skills now fully implemented.

## Test plan

- `MultiHitNumTests` — a 2-hit and a 5-hit skill deal per-hit × `GetNum` (seeded SkillDb).
- `WeaponBranchRatioTests` — one Archer (bow vs not) + one Thief/Swordman (dagger/sword vs other) skill; assert distinct ratios.
- `LevelDmodComposeTests` — family ratio × RE_LVL_DMOD applied once (gated on COMBAT-03).
- `SevereRainstormTests.CanEquipFromDb` — canequip window == `GetTime`, not `lv*4000`.
- Per-family: one acceptance test each (Mage bolt hits; Archer trap duration; Thief EDP stack; Swordman shield branch; Merchant cannon ammo; Acolyte party heal).
- DI audit green.

## Notes / gotchas

- This is breadth, not depth-on-one-skill: the same four edits (X1–X4) repeat across dozens of plugins. Batch by edit class, not by plugin, to stay consistent.
- X3 is a *composition* contract with COMBAT-03, not an implementation here — the risk is double-applying RE_LVL_DMOD (once in the family ratio, once in COMBAT-03). Keep family ratios RE_LVL_DMOD-free.
- SKILL-01 (rate), SKILL-02 (staggered timers for Mage trains), SKILL-04 (durations) are hard prerequisites — migrate proc/duration call sites only after those land, or you rewrite them.
- Don't regress the families currently under active edit (working-tree `git status` shows ~30 modified): coordinate so this ticket's batch edits land on top of, not under, the in-flight wave-98 work.
