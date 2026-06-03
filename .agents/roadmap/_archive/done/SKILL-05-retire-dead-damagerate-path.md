# SKILL-05 — Retire the legacy `DamageRate` ratio path (two competing ratio sources)

> **Epic:** Skills · **Status:** ✅ Done (2026-06-01) · **Size:** M · **Player-visible:** yes (latent)
> **Depends on:** none · **Blocks:** none

## Problem

There are **two** independent skill-damage-ratio sources in the codebase, and which
one wins depends on whether a skill has a registered plugin:

1. **Per-plugin `CalculateSkillRatio`** — the *correct*, rAthena-faithful path
   (matches `battle_calc_attack_skill_ratio`'s hardcoded per-skill switch). Used by
   `WeaponSkillImpl.CastendDamageId` (the path a plugin-backed weapon skill takes).
2. **`SkillDefinition.DamageRate[level]`** — a per-level multiplier column on the
   skill catalog. Used by `WeaponSkillResolver.Resolve`, by `MagicSkillResolver`, and
   by `SkillAttackService.SkillAttack` / `CalcMagicDamage` / `CalcMiscDamage`.

A plugin-backed weapon skill computes ratio twice-divergently: its `CastendDamageId`
calls `CalculateSkillRatio(100, ...)` (correct), but if that same skill ever flows
through `SkillAttackService.SkillAttack` (e.g. a splash dispatch, or any caller that
hits the funnel) it instead gets `DamageRate[level]` — a *different* number for the
same skill. The two can disagree silently. For non-plugin skills the `DamageRate`
column is authoritative; for plugin skills it's dead weight that can still be
reached. This is a latent correctness trap: a future splash/AoE wiring that routes a
plugin skill through `SkillAttackService` will apply the wrong ratio with no error.

The project rule (per the findings + CLAUDE.md "parity first") is explicit:
**per-skill ratio is hardcoded per-plugin `CalculateSkillRatio` — this is the correct
granularity; rAthena's switch is hardcoded, NOT data-driven.** The `DamageRate`
column is the leftover of the pre-plugin design and must stop being a second ratio
authority for any skill that has a plugin.

## Current state (C#)

- `Map.Server/Skills/SkillImpl.cs:160` — `WeaponSkillImpl.CastendDamageId` computes `ratio = CalculateSkillRatio(100, src, target, skillLevel, ctx, miscflag)` then `dmg = swing.Total * ratio / 100`. **Correct path.**
- `Map.Server/Skills/Resolvers/WeaponSkillResolver.cs:27` — `rate = def.DamageRate.Length > lvl ? def.DamageRate[lvl] : 100; scaled = swing.Total * rate / 100`. **Second ratio source.**
- `Map.Server/Skills/SkillAttackService.cs:55-61` — `ratePerLevel = def.DamageRate[skillLevel] (else 100); damage = CalcWeaponAttack(...).Damage * ratePerLevel / 100` for `BattleAttackType.Weapon`. **Second ratio source on the funnel every offensive skill is meant to flow through.**
- `Map.Server/Skills/SkillAttackService.cs:163` / `:171` — `CalcMagicDamage` / `CalcMiscDamage` likewise pull `DamageRate[lvl]` and pass it to `CalcMagicAttack` / `CalcMiscAttack`.
- `Map.Server/Skills/Resolvers/MagicSkillResolver.cs:32`, `MiscSkillResolver.cs:21` — `DamageRate`-driven.
- `Map.Server/Skills/SkillDefinition.cs:127` — `public int[] DamageRate { get; init; }`. `SkillDbLoader.cs:33` parses it from the skill_db row.
- `Map.Server/Skills/SkillDb.cs:225` / `:281` / `:294` — fallback starter skills (Bash, Fire Bolt, Cold Bolt) set `DamageRate` literals. These are the *only* skills that legitimately use the column today, because they have no plugin ratio override (Bash *does* have a plugin — so even these overlap).

## rAthena reference (source of truth)

- `rathena/src/map/battle.cpp:4590` — `battle_calc_attack_skill_ratio(struct Damage* wd, block_list *src, block_list *target, uint16 skill_id, uint16 skill_lv)`. A single giant `switch (skill_id)` that hardcodes the per-skill ratio (`skillratio += ...`). There is **no** data-driven `DamageRate` column in rAthena — `db/re/skill_db.yml` carries Range/Splash/Knockback/Element/Hits/Duration, NOT a damage multiplier. The multiplier is *code*.
- So the C# `DamageRate` column is a non-parity invention. The faithful design is exactly the per-plugin `CalculateSkillRatio` override (which mirrors one `case SK_X:` arm).
- Monolithic-switch caveat: `battle_calc_attack_skill_ratio` is the canonical ratio source; do not reconstruct a data-driven ratio table to replace `DamageRate`.

## Scope — every sub-system that must be touched

- [x] **Make `SkillAttackService.SkillAttack` consult the plugin first** — ✅ injects `SkillBehaviorRegistry?`; the new `WeaponDamage(...)` helper uses `plugin.ComputeSkillDamage` when a `WeaponSkillImpl` plugin exists, else `DamageRate`. Magic/misc keep `DamageRate` (their plugins don't override the ratio today — "leave the magic fallback as-is"; future override → SKILL-17 funnel-ctx work surfaces it).
- [x] **Single ratio entry point** — ✅ `WeaponSkillImpl.ComputeSkillDamage(swing, src, target, level, ctx?, miscflag)` is the one formula (ratio → `RE_LVL_DMOD` → constant); both `CastendDamageId` and `SkillAttackService`/`WeaponSkillResolver` call it. *(ctx-aware ratio via the ctx-less funnel ➡️ **SKILL-17**.)*
- [x] **Resolvers** — ✅ `WeaponSkillResolver` now defers to the plugin's `ComputeSkillDamage` (and logs) if a plugin skill leaks to it; `MagicSkillResolver` logs the leak (keeps `DamageRate` per "leave magic as-is"); `MiscSkillResolver` uses `DamageRate` as an *amount* (not a ratio) — left alone per the gotcha.
- [x] **Audit the fallback starter set** — ✅ annotated Bash / Fire Bolt / Cold Bolt `DamageRate` literals as "fallback-only — the plugin owns the live ratio."
- [x] **Document the column** — ✅ rewrote `SkillDefinition.DamageRate` doc + the `WeaponSkillImpl` class-doc "combine both" framing → "per-skill ratio via `CalculateSkillRatio`; `DamageRate` is the no-plugin fallback only."
- [x] **No new packets / IPC / DB.** ✅ `DamageRate` column retained for the no-plugin tail.

## Done criteria

- ✅ A plugin-backed weapon skill yields the *same* damage via `CastendDamageId` (the shared `ComputeSkillDamage`) and via `SkillAttackService.SkillAttack` — pinned by `SkillRatioConsistencyTests.PluginSkillSameRatioBothPaths`.
- ✅ `SkillAttackService.SkillAttack` consults the plugin ratio for any skill with a plugin; `DamageRate` is read only when no plugin exists (`NoPluginUsesDamageRate`).
- ✅ The `WeaponSkillImpl` / `SkillDefinition.DamageRate` docs no longer describe "combine".
- ✅ No skill has two live ratio sources (weapon funnel + resolver both route to the plugin; `Resolver_DefersToPlugin_WhenDispatchLeaks`). *(ctx-aware ratio parity on the funnel ➡️ **SKILL-17**.)*

## Test plan

- `SkillRatioConsistencyTests.PluginSkillSameRatioBothPaths` — register a plugin with `CalculateSkillRatio` returning a distinctive value; resolve the skill via `ResolveSkill` and via `SkillAttackService.SkillAttack`; assert identical damage.
- `SkillRatioConsistencyTests.NoPluginUsesDamageRate` — a skill with no plugin still scales by `DamageRate[level]` (the fallback path is intact).
- `SkillAttackServiceTests.ResolverNeverHandlesPluginSkill` — assert the generic resolver is not invoked for a skill that has a plugin (dispatch doesn't leak).

## The divergence, concretely

For a plugin-backed weapon skill (e.g. Bash, which has both a plugin *and* a
`DamageRate` fallback row at `SkillDb.cs:225`):
- `ResolveSkill` → `WeaponSkillImpl.CastendDamageId` → `CalculateSkillRatio(100, …)` →
  the plugin's `baseRatio + 30*lv` (correct, mirrors the `battle.cpp:4590` arm).
- `SkillAttackService.SkillAttack(Weapon, …)` → `DamageRate[lvl]` = `130/160/.../400`
  (the fallback literal) → a *different* multiplier on the same swing.

Today the only thing keeping these from being observed-divergent is that Bash never
routes through `SkillAttackService.SkillAttack` — but any future splash/AoE wiring
that funnels a plugin skill through that service would silently apply the fallback
column. The fix removes that landmine by making the funnel consult the plugin first.

## Notes / gotchas

- Don't rip out `DamageRate` — the bulk of vanilla skills that never got a plugin still ride the resolver + column. The fix is precedence (plugin wins), not deletion.
- The fallback starter rows (Bash/FireBolt/ColdBolt) double as the seed for those skills *and* shadow their plugins. Pick one resolution (drop the literal or annotate "fallback-only") and apply it to all three so the file isn't self-contradictory.
- Heal (`SkillDb.cs:230`) and Misc skills repurpose `DamageRate`/`EffectAmount` as a per-level *amount*, not a ratio — leave those alone; this ticket is about the weapon/magic *ratio* duplication only.
- `SkillAttackService` currently has no `SkillBehaviorRegistry` dependency; adding it is the crux. Confirm no DI cycle (registry depends on the plugins, plugins depend on services, services don't depend back on the attack service — should be acyclic).
- Magic/misc skills mostly lack a `CalculateSkillRatio` override today (they ride `CalcMagicAttack(..., ratePerLevel)`); for those, the plugin precedence is a no-op until they get ratio overrides — fine, leave the magic fallback as-is but route it through the same single entry point so the future override is honored automatically.

## History

- 2026-06-01 · Retired the second ratio authority. Added the single entry point
  `WeaponSkillImpl.ComputeSkillDamage` (ratio→RE_LVL_DMOD→constant) shared by
  `CastendDamageId` + the funnel; added a ctx-free `CalculateSkillConstantAddition` overload
  (AsuraStrike re-pointed). `SkillAttackService` injects `SkillBehaviorRegistry?` and uses the
  plugin ratio (`.Total` basis) when a plugin exists, else `DamageRate`. `WeaponSkillResolver`
  defers to the plugin (and logs) on a dispatch leak; `MagicSkillResolver` logs the leak.
  Annotated the Bash/FireBolt/ColdBolt fallback rows + rewrote the `DamageRate` /
  `WeaponSkillImpl` docs. `SkillRatioConsistencyTests` (3, deterministic FixedRandom swing).
  3680/3680 green. Follow-ups: SKILL-17 (ctx-aware ratio through the funnel), SKILL-18
  (Asura/MovePos dash slide ZC_HIGHJUMP broadcast — pre-existing TODO surfaced here).
  (Note: lane "start" commit was skipped; card moved todo→done directly in the finish commit.)
