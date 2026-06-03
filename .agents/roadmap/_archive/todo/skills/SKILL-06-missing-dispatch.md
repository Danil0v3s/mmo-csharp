# SKILL-06 — Missing dispatch: orphan skill ids + unverified `_ATK` sub-skill invocations

> **Epic:** Skills · **Status:** ❌ Not started · **Size:** XL · **Player-visible:** yes
> **Depends on:** SKILL-01 (SC apply-rate, for the buff/debuff arms) · **Blocks:** none

## Problem

Two distinct gaps:

**(A) Orphan ids with no `skill_castend_*` handler.** A set of skill ids have a
`SkillIds.*` constant but **no `SkillImpl` plugin** and no other handler — casting
them hits the registry, finds nothing, and falls through to the generic
`DamageKind` resolver (which for a `None`-kind buff is a no-op). These are
classic buffs / self-states (Berserk, Concentration, Explosion Spirits, Magic
Power, Meltdown, Deathbound, Akaitsuki) plus a damage skill (Magic Crasher, Jack
Frost) and a missing AoE (Magnus Exorcismus). They simply don't work.

**(B) Caveat: some "missing" ids are NOT castend cases.** Several ids in the
findings list are *passive `pc_checkskill` modifiers* or *YAML-data / produce
gates*, not `skill_castend_*` arms. They have no `case` in `skill.cpp`'s castend
switches because rAthena reads them inline elsewhere (damage formula, trap
duration, produce success-rate). For these the "dispatch" is a *read at the
consuming site*, not a plugin. This ticket must classify each id correctly so the
implementer doesn't write a no-op plugin for a passive.

**(C) The 22 `_ATK` / element-split sub-skills.** Skills like AG_ALL_BLOOM,
WL_TETRAVORTEX, EM_ELEMENTAL_BUSTER fan their damage out through child ids
(`*_ATK`, `*_FIRE/_WATER/_WIND/_GROUND`). The child id often has no ctor (expected —
it's not cast directly), but the **invocation** matters: does the *parent* plugin
actually fire the child id with the right element/ratio? The bug to verify per pair
is the missing invocation, not the missing ctor.

## Current state (C#)

- `Map.Server/Skills/Behaviors/SkillBehaviorRegistry.cs:32` — `Get(id)` returns null for unregistered ids; `SkillCastService.ResolveSkill` then falls to the generic resolver, which no-ops for `DamageKind.None`. So an orphan buff silently does nothing.
- **Orphan ids (no plugin, verified via `grep -rl "SkillIds.<ID>" Behaviors/`):**
  - `LK_BERSERK` — `skill.cpp:8248` (`skill_castend_nodamage_id` SC_BERSERK self-buff).
  - `LK_CONCENTRATION` — `skill.cpp:8265` (SC_CONCENTRATION self-buff).
  - `MO_EXPLOSIONSPIRITS` — `skill.cpp:8259` (SC_EXPLOSIONSPIRITS, max spirit spheres).
  - `PR_LEXAETERNA` — `skill.cpp:8243` (SC_AETERNA on target).
  - `HW_MAGICCRASHER` — `skill.cpp:5319` (weapon-based magic damage, BF_WEAPON w/ magic ele).
  - `HW_MAGICPOWER` — `skill.cpp:8274` (SC_MAGICPOWER self-buff, +MATK next magic hit).
  - `RK_DEATHBOUND` — `skill.cpp:8297` (SC_DEATHBOUND self-state, reflect-on-next-hit).
  - `WS_MELTDOWN` — `skill.cpp:8271` (SC_MELTDOWN self-buff, breaks target equip on hit).
  - `OB_AKAITSUKI` — `skill.cpp:12753` (SC_AKAITSUKI, heal→damage inversion state).
  - `NPC_JACKFROST` — `skill.cpp:1866` (mob AoE, freeze proc — also a missing-invocation candidate, see family-NPC ticket).
  - `PR_MAGNUSEXORCISMUS` — **no castend `case`** in this checkout: it's a *ground unit* skill (`UNT_MAGNUS`), dispatched via `skill_unitsetting` + per-tick `skill_unit_onplace_timer` (damage to demon/undead). Implement as a ground-unit skill, NOT a `castend` plugin.
- **Passive / data-gate ids (NOT castend — do NOT write a no-op plugin):**
  - `HP_MEDITATIO` — `skill.cpp:551/569/592` — passive heal-bonus read via `pc_checkskill` inside the heal formula. Belongs in the heal calc, not a plugin.
  - `GC_RESEARCHNEWPOISON` — `skill.cpp:22947/23124` — produce-list gate + create-poison success-rate (`skill_produce_db` path). Belongs in the production service.
  - `WH_ADVANCED_TRAP` — `skill.cpp:14342/16286` — trap-duration `pc_checkskill` modifier. Belongs in the trap-unit duration calc.
  - `WH_NATUREFRIENDLY` — `skill.cpp:1372` — `pc_checkskill` rate modifier in a damage/effect formula. Inline read.
  - `ABC_MAGIC_SWORD_M`, `IG_SPEAR_SWORD_M` — mastery `_M` ids: no castend case; they're `pc_checkskill` masteries that buff the parent skill (`ABC_MAGIC_SWORD` / `IG_SPEAR_SWORD`) ratio. Inline read in the parent plugin's `CalculateSkillRatio`.
  - `SH_COMMUNE_WITH_*`, `SH_MYSTICAL_CREATURE_MASTERY`, `HN_SELFSTUDY_*` — referenced inside existing multi-skill plugins (`ShamanFormulas.cs`, `HyperNoviceFormulas.cs`) as `pc_checkskill` reads; confirm the read is wired, not that a new plugin is needed.
- **`_ATK` / element-split sub-skills (verify invocation, not ctor):** `AG_ALL_BLOOM_ATK`/`AG_ALL_BLOOM_ATK2` (`skill.cpp:433/434`), `WL_TETRAVORTEX_FIRE/WATER/WIND/GROUND` (`skill.cpp:382…`, child of `WL_TETRAVORTEX`), `EM_ELEMENTAL_BUSTER_FIRE/…` (`skill.cpp:458`, child of `EM_ELEMENTAL_BUSTER`), `AG_DESTRUCTIVE_HURRICANE_CLIMAX` (`skill.cpp:429`), plus the `EL_*_ATK` elemental-assist hits. Parent plugins exist for most (e.g. `Mage/ElementalBuster.cs`); the open question is whether the parent *fires* each child id with the correct per-element ratio/element.

## rAthena reference (source of truth)

- `rathena/src/map/skill.cpp` — the castend arms cited per-id above (line numbers verified in this checkout). Self-buff arms live in `skill_castend_nodamage_id`; damage arms in `skill_castend_damage_id`; ground skills in `skill_unitsetting`.
- `rathena/src/map/skill.cpp:382/433/458` — the `battle_calc_attack_skill_ratio` (and damage-element) arms for the `_ATK`/element children; each child has its own element + `skillratio +=` so the parent must fire the correct child id per wave/element.
- `rathena/src/map/skill.cpp:551` (HP_MEDITATIO), `:22947` (GC_RESEARCHNEWPOISON), `:14342`/`:16286` (WH_ADVANCED_TRAP), `:1372` (WH_NATUREFRIENDLY) — the *inline `pc_checkskill` read* sites, proving these are passives, not castend dispatch.
- Monolithic-switch caveat: the canonical source is `skill.cpp` (`skill_castend_damage_id` / `_nodamage_id` / `skill_unitsetting`) and `battle.cpp:4590` for the ratio. Map each id to its real `case`/read site (done above); do NOT invent a plugin for a passive.

## Scope — every sub-system that must be touched

For **(A) orphan castend ids** — one `SkillImpl` plugin each, DI-registered in `Program.cs` (~579+), under the correct family folder:
- [ ] `LK_BERSERK`, `LK_CONCENTRATION`, `MO_EXPLOSIONSPIRITS`, `HW_MAGICPOWER`, `RK_DEATHBOUND`, `WS_MELTDOWN`, `OB_AKAITSUKI` → `StatusSkillImpl` self-buff plugins. Each applies its SC via `ctx.Sc.Start(... rate from SKILL-01, duration from SKILL-04 GetTime)`. Cite the `skill.cpp` case + SC name in the docstring.
- [ ] `PR_LEXAETERNA` → `StatusSkillImpl` target-debuff (SC_AETERNA, doubles next physical hit).
- [ ] `HW_MAGICCRASHER` → `WeaponSkillImpl` (weapon attack with magic element; ratio per `battle.cpp:4590` arm).
- [ ] `NPC_JACKFROST` → mob AoE damage + freeze proc (coordinate with SKILL-08 family-NPC; one plugin, registered once).
- [ ] `PR_MAGNUSEXORCISMUS` → **ground-unit** skill (`ISkillUnitService.Place` + per-tick `skill_unit_onplace_timer` damage to undead/demon). NOT a castend plugin; wires through the unit-tick path.

For **(B) passive / data-gate ids** — NO plugin; wire the inline read at the consuming site:
- [ ] `HP_MEDITATIO` — add the `pc_checkskill(HP_MEDITATIO)` heal-bonus into the heal calc (Heal plugin / heal formula service).
- [ ] `GC_RESEARCHNEWPOISON` — add the produce-list gate + success-rate into `ISkillProductionService`.
- [ ] `WH_ADVANCED_TRAP` — add the trap-duration modifier into the trap-unit duration calc.
- [ ] `WH_NATUREFRIENDLY` — add the rate modifier at its damage/effect read site.
- [ ] `ABC_MAGIC_SWORD_M`, `IG_SPEAR_SWORD_M` — fold the mastery into the parent plugin's `CalculateSkillRatio`.
- [ ] `SH_COMMUNE_WITH_*`, `SH_MYSTICAL_CREATURE_MASTERY`, `HN_SELFSTUDY_*` — verify the existing `ShamanFormulas`/`HyperNoviceFormulas` reads are correct; fix if stubbed.

For **(C) `_ATK` / element-split invocations** — verify and fix the *parent fires the child*:
- [ ] For each pair (`AG_ALL_BLOOM`→`_ATK`/`_ATK2`, `WL_TETRAVORTEX`→`_FIRE/_WATER/_WIND/_GROUND`, `EM_ELEMENTAL_BUSTER`→`_FIRE/…`, `AG_DESTRUCTIVE_HURRICANE`→`_CLIMAX`, `EL_*`→`_ATK`): read the parent plugin and confirm it dispatches each child id with the correct element + ratio (per the `skill.cpp:382/433/458` arms). Where the parent does NOT fire the child, add the dispatch (via `ISkillAttackService` / a child-id `ResolveSkill`). Where the child needs its own ratio override, add a minimal `WeaponSkillImpl`/magic plugin for the child id.
- [ ] DI-register any new child plugins in `Program.cs`.

## Done criteria

- Every orphan castend id (A) has a registered plugin and produces its rAthena effect (SC applied / damage dealt) when cast — verified per-id.
- `PR_MAGNUSEXORCISMUS` places a ground unit that damages undead/demon on its tick.
- Every passive id (B) is read at its consuming site; no no-op plugin was created for a passive.
- For every `_ATK`/element child (C), casting the parent fires each child with the correct element + ratio (test asserts each element's hit lands with its element).
- `Program.cs` registers all new plugins; the DI-audit test (SkillImplDiAuditTests) passes.

## Test plan

- `MissingDispatchTests` — per orphan id: cast it, assert the SC is present (buffs) or damage > 0 (Magic Crasher / Jack Frost).
- `MagnusExorcismusTests` — place on a cell with an undead + a normal mob; assert only undead/demon take periodic damage.
- `PassiveReadTests` — `HP_MEDITATIO` raises heal output; `WH_ADVANCED_TRAP` lengthens trap duration; `GC_RESEARCHNEWPOISON` raises poison-create success-rate — each at the consuming site, no plugin.
- `ElementSplitTests` — cast `WL_TETRAVORTEX` / `EM_ELEMENTAL_BUSTER` / `AG_ALL_BLOOM`; assert each element child fires once with its element + ratio.
- DI audit: `SkillImplDiAuditTests` green (no duplicate ids, no id 0).

## Notes / gotchas

- The biggest trap is writing a no-op `StatusSkillImpl` for a (B) passive — that *looks* like dispatch but does nothing and hides the real read site. The classification above is load-bearing; honor it.
- `NPC_JACKFROST` overlaps SKILL-08 (family-NPC). Implement once; reference it from both tickets, register once.
- For (C), a child id with no ctor is *correct* if the parent fires it through a ratio/element override path that doesn't need a plugin — the test is behavioral (does the element land?), not "does a plugin exist."
- SKILL-01 must land first for the (A) buff/debuff arms so their SC applications go through the apply-rate path (most are self-buffs at rate 10000, but the debuffs like LEX_AETERNA need the resist path).
