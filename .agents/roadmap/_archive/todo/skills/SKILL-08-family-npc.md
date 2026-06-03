# SKILL-08 — Family: NPC_* mob skills (45 shells of 154) — SC vals + AI-cast semantics

> **Epic:** Skills · **Status:** ❌ Not started · **Size:** XL · **Player-visible:** yes
> **Depends on:** SKILL-01 (SC apply-rate), SKILL-03 (splash allegiance), SKILL-04 (durations) · **Blocks:** none

## Problem

The `NPC_*` family (monster / boss / scripted skills) has **45 of 154 plugin files
that are bare shells**: a `StatusSkillImpl` or `WeaponSkillImpl` subclass with **no
override** — so the SC-apply skills apply *no SC* (their `TargetSc` is the default
`StatusType.None` and the `StatusSkillImpl` body short-circuits), and the
break-equip / debuff weapon skills deal a plain hit with **no break / no proc**. The
docstrings admit it inline: *"armor break TODO"*, *"Status start TODO"*, *"helm break
TODO"*, *"SC_COMA via SC start. Status start TODO."*

Two failure clusters:

1. **SC-apply shells with no SC.** `AgilityUp` (NPC_AGIUP self-buff), `AntiMagic`,
   `AttributeChange`, `DarkBlessing` (SC_COMA), `DeadlyCurse`/`DeadlyCurse2`
   (SC_DPOISON), `DecreaseAllStats` (NPC_ALL_STAT_DOWN), `DemonShockAttack`
   (SC_MAGICALATTACK), `DragonFear`, the element `*AttributeChange` family — all
   declare a `StatusSkillImpl` but never set `TargetSc` or apply the SC. Cast =
   nothing happens.

2. **Break-equip weapon shells with no break.** `BreakArmor` (NPC_ARMORBRAKE),
   `BreakHelm` (NPC_HELMBRAKE), `BreakShield` (NPC_SHIELDBRAKE), `BreakWeapon` — plain
   `WeaponSkillImpl` hits; the equip-break (`skill_break_equip`) is missing.

Because these are *mob* skills, the AI-cast semantics also matter: the rAthena
`NPC_*` arms read mob authority (`status_get_lv` of the mob, `md->skill_lv`), not a
PC's learned skill, and several (DeadlyCurse2) pack `src->id` into `val2` so the SC
knows its caster for later resolution.

## Current state (C#)

- `Map.Server/Skills/Behaviors/Npc/` — 154 files; **45 have no `override`** (verified by grep). Representative shells:
  - `AgilityUp.cs` — `class AgilityUp : StatusSkillImpl { ctor }` — no `TargetSc`, no apply. (NPC_AGIUP self-buff.)
  - `DarkBlessing.cs` — doc: *"(50 + 5*lv) % SC_COMA via SC start. Status start TODO."* — no body.
  - `DeadlyCurse.cs` / `DeadlyCurse2.cs` — SC_DPOISON; `DeadlyCurse2` doc: *"applies SC with src.Id as val2"* — neither applies.
  - `BreakArmor.cs` / `BreakHelm.cs` / `BreakShield.cs` — `WeaponSkillImpl`; doc: *"armor/helm/shield break TODO."*
  - `DecreaseAllStats`, `DemonShockAttack`, `DragonFear`, `AntiMagic`, `AttributeChange`, `EarthAttributeChange` (+ fire/water/wind variants) — SC shells.
- `Map.Server/Skills/Behaviors/SkillImpl.cs:174` — `StatusSkillImpl` with default `TargetSc => StatusType.None`; `CastendNoDamageId` calls `ApplyAdditionalEffects` which is empty by default → no-op. So a shell `StatusSkillImpl` literally does nothing.
- `ctx.SideEffect` (`ISkillSideEffectService`) — exposes `skill_break_equip` per the context doc; the break shells must call it but don't.
- `NPC_JACKFROST` (no plugin at all) is an AoE freeze — overlaps SKILL-06 (A); implement once here, register once.

## rAthena reference (source of truth)

- `rathena/src/map/skill.cpp:1866` `NPC_JACKFROST` — mob AoE; freeze proc on splash victims.
- `rathena/src/map/skill.cpp` NPC self-buff arms (`NPC_AGIUP`, `NPC_ANTIMAGIC`, `NPC_MAGICALATTACK`, the `NPC_CHANGE*` element arms) — `sc_start` on self with `skill_get_time`.
- `rathena/src/map/skill.cpp` NPC debuff arms (`NPC_DARKBLESSING` → SC_COMA at `(50 + 5*lv)%`, `NPC_DEADLYCURSE`/`2` → SC_DPOISON, `NPC_ALL_STAT_DOWN` → SC_STORMKICK-style all-stat-down) — `sc_start4` on target with the caster id in `val2` where rAthena packs it.
- `rathena/src/map/skill.cpp` break arms (`NPC_ARMORBRAKE`/`HELMBRAKE`/`SHIELDBRAKE`/`WEAPONBRAKE`) — weapon hit + `skill_break_equip(src, target, EQP_ARMOR/HELM/SHIELD/WEAPON, rate, BCT_ENEMY)`.
- `rathena/src/map/skill.cpp` `battle.cpp:4590` — ratio for the NPC weapon arms.
- Monolithic-switch caveat: canonical source is `skill.cpp` NPC arms + `mob.cpp` AI-cast (`mobskill_use`) for the cast authority; the C# `SkillCastService` already bypasses `pc_checkskill` for mob sources (`:224` PlayerEntity-only), so AI authority is mostly handled — verify per skill.

## Scope — every sub-system that must be touched

- [ ] **SC self-buff shells** — for each (`AgilityUp`, `AntiMagic`, `DemonShockAttack`, `AttributeChange`, the element `*AttributeChange` family): set `TargetSc` + apply via `CastendNoDamageId` → `ctx.Sc.Start(self, sc, rate, val…, GetTime, src)`. Self-buffs apply at guaranteed rate (the SKILL-01 no-resist wrapper).
- [ ] **SC debuff shells** — `DarkBlessing` (SC_COMA at `(50+5*lv)%` → rate `(50+5*lv)*100` through SKILL-01), `DeadlyCurse`/`DeadlyCurse2` (SC_DPOISON; `DeadlyCurse2` packs `val2 = src.Id`), `DecreaseAllStats`, `DragonFear`. Each applies via the apply-rate path with the rAthena rate + `GetTime` duration.
- [ ] **Break-equip shells** — `BreakArmor`/`BreakHelm`/`BreakShield`/`BreakWeapon`: after the weapon hit, call `ctx.SideEffect.BreakEquip(src, target, slot, rate)` with the rAthena slot + rate. Confirm `ISkillSideEffectService` exposes `skill_break_equip`; add if missing.
- [ ] **`NPC_JACKFROST`** — new plugin: AoE damage (splash via `RecursiveDamageSplashSkillImpl`, radius from `GetSplash`) + freeze proc on each victim (apply-rate path). Uses SKILL-03 allegiance so a slave mob's Jack Frost doesn't freeze its master. Register in `Program.cs`.
- [ ] **AI-cast authority** — verify mob-source casts of these skills bypass `pc_checkskill` (they do via `SkillCastService` PlayerEntity gate) and use the mob's level for rate/duration scaling (`status_get_lv(src)`). Where a shell needs the caster level, read it from the `MobEntity` status.
- [ ] **`val2 = src.Id` packing** — `DeadlyCurse2` and any arm that stores the caster id passes it through `Start(..., val2: src.Id, ...)` so later resolution (DoT attribution, reflect) knows the source.
- [ ] **DI** — all stay registered; new Jack Frost registered.

## Done criteria

- Every NPC SC shell applies its SC at the rAthena rate + duration when a mob casts it (test per skill: AgilityUp self-buff present; DarkBlessing SC_COMA lands at `(50+5*lv)%`; DeadlyCurse SC_DPOISON present with `val2 = caster id`).
- Break-equip skills break the named slot at the rAthena rate (test: BreakArmor breaks armor, not helm).
- `NPC_JACKFROST` deals splash damage + freezes victims, respecting slave-mob allegiance (test).
- No `TODO` / "Status start TODO" / "break TODO" comment remains on a skill that now has an effect.
- Zero no-override `StatusSkillImpl`/`WeaponSkillImpl` shells remain in `Npc/` except genuinely effect-less skills documented with their rAthena arm.

## Test plan

- `NpcScTests` — per SC skill, mob casts → assert SC present (self or target) at rate + duration; `DeadlyCurse2` carries `val2 = caster`.
- `NpcBreakEquipTests` — BreakArmor/Helm/Shield/Weapon each break only their slot at the rAthena rate (seeded rng).
- `NpcJackFrostTests` — splash + freeze; slave-mob owner not hit (depends SKILL-03).
- `NpcAiCastTests` — mob-source cast bypasses learned-skill gate and scales rate/duration by mob level.
- DI audit green.

## Notes / gotchas

- A shell `StatusSkillImpl` with default `TargetSc => None` is a silent no-op — easy to mistake for "done." The fix is per-skill: set `TargetSc` AND give it the SC vals + rate + duration.
- Some NPC arms are *boss-only* or have mob-level-scaled rates; read the mob's level, don't hardcode a PC-level assumption.
- SKILL-01/03/04 are hard prerequisites: rate path, allegiance, durations. Don't migrate these call sites before those land or you'll rewrite them.
- `NPC_JACKFROST` is double-listed with SKILL-06 (A). Build it here; reference from SKILL-06; register once.
