# SKILL-05 — Retire the legacy `DamageRate` ratio path (two competing ratio sources)

> **Epic:** Skills · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes (latent)
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

- [ ] **Make `SkillAttackService.SkillAttack` consult the plugin first** — when a `SkillBehaviorRegistry` plugin exists for `skillId`, route weapon damage through the plugin's `CalculateSkillRatio` (matching `WeaponSkillImpl.CastendDamageId`) instead of `DamageRate[level]`. Inject `SkillBehaviorRegistry` into `SkillAttackService`. For magic/misc, route through the plugin's ratio override likewise. Only fall back to `DamageRate` for skills with no plugin AND no ratio override.
- [ ] **Single ratio entry point** — extract the "ratio for (skill, level, src, target, ctx, miscflag)" decision into one helper used by both `WeaponSkillImpl.CastendDamageId` and `SkillAttackService.SkillAttack`, so a plugin skill cannot get two different ratios depending on entry path.
- [ ] **`WeaponSkillResolver` / `MagicSkillResolver` / `MiscSkillResolver`** — these run only for skills with *no* plugin (the generic `DamageKind` fallback). Keep them as the no-plugin fallback but document that `DamageRate` is the *fallback-only* ratio source, never consulted for a skill that has a plugin. Add an assertion/log if a resolver is asked to resolve a skill that *does* have a plugin (that would mean dispatch leaked).
- [ ] **Audit the fallback starter set** — Bash/Fire Bolt/Cold Bolt in `SkillDb.LoadFallback` have plugins; their `DamageRate` literals are now dead for those skills. Either drop the literal (let the plugin own ratio) or annotate it as "fallback-only, superseded by plugin." Pick one and make it consistent so a reader isn't misled.
- [ ] **Document the column** — update `SkillDefinition.DamageRate` doc + the `WeaponSkillImpl` class doc (which currently says "skill_db DamageRate + per-skill ratio bump" — that "combine both" framing is the bug; reword to "per-skill ratio via `CalculateSkillRatio`; `DamageRate` is the no-plugin fallback only").
- [ ] **No new packets / IPC / DB.** Do NOT delete the `DamageRate` column outright (the no-plugin long tail still uses it) — just stop it being a *second* authority for plugin skills.

## Done criteria

- A plugin-backed weapon skill yields the *same* damage whether resolved via `ResolveSkill` (→ `WeaponSkillImpl.CastendDamageId`) or via `SkillAttackService.SkillAttack` — one ratio, not two (test pins equality).
- `SkillAttackService.SkillAttack` consults the plugin's `CalculateSkillRatio` for any skill with a registered plugin; `DamageRate` is only read when no plugin exists.
- The `WeaponSkillImpl` / `SkillDefinition.DamageRate` docs no longer describe "combine skill_db DamageRate + plugin bump."
- No skill has two live ratio sources.

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
