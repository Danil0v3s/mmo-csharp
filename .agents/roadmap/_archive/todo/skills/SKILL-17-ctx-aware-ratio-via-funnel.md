# SKILL-17 — Thread SkillBehaviorContext through the SkillAttackService funnel for ctx-aware ratios

> **Epic:** Skills · **Status:** ❌ Not started · **Size:** S · **Player-visible:** yes (narrow)
> **Depends on:** SKILL-05 (ComputeSkillDamage single entry point) · **Blocks:** none

## Problem

SKILL-05 made `SkillAttackService.SkillAttack` (the splash / secondary-dispatch funnel) and
`WeaponSkillResolver` use the plugin's `WeaponSkillImpl.ComputeSkillDamage` as the single
ratio authority — but those call sites have **no `SkillBehaviorContext`**, so they pass
`ctx: null`. `ComputeSkillDamage` then uses the ctx-free 4-arg `CalculateSkillRatio`. Plugins
that override the **ctx-aware** ratio (the 6-arg `CalculateSkillRatio(..., ctx, miscflag)` —
e.g. DK_DRAGONIC_BREATH, LG_CANNONSPEAR's SC_SPEAR_SCAR, SS_REIKETSUHOU's
SC_WATER_CHARM_POWER) get their *base* (ctx-free) ratio when resolved through the funnel,
not the SC-modified one. On the normal cast path (`SkillCastService` → `CastendDamageId`,
which has a real ctx) the SC-aware ratio IS honored — so this only affects skills that flow
through the splash/funnel path AND read ctx for their ratio.

## Current state (C#)

- `Map.Server/Skills/Behaviors/SkillImpl.cs` `WeaponSkillImpl.ComputeSkillDamage(swing, src,
  target, level, ctx?, miscflag)` — when `ctx == null` it calls the 4-arg `CalculateSkillRatio`
  and the ctx-free `CalculateSkillConstantAddition`.
- `Map.Server/Skills/SkillAttackService.cs` `WeaponDamage(...)` — calls `ComputeSkillDamage(..., ctx: null, ...)`.
- `Map.Server/Skills/Resolvers/WeaponSkillResolver.cs` — same (`ctx: null`).
- `ISkillAttackService.SkillAttack` / `ISkillResolver.Resolve` signatures carry no ctx.

## rAthena reference (source of truth)

- `battle.cpp:battle_calc_attack_skill_ratio` reads SC state (`sc->getSCE(...)`) for several
  skills' ratios — the ctx-aware overrides mirror those arms. The splash path
  (`skill_attack` → `skill_area_sub`) carries the same `src`/`sc` context, so rAthena's ratio
  is SC-aware on every path.

## Scope — every sub-system that must be touched

- [ ] Thread an optional `SkillBehaviorContext?` into `ISkillAttackService.SkillAttack` (and
      `SkillAttackArea`) and `ISkillResolver.Resolve`, populated by the splash/secondary
      dispatchers that currently call them, so `ComputeSkillDamage` receives a real ctx.
- [ ] Pass that ctx through to `ComputeSkillDamage` so the 6-arg ctx-aware ratio +
      constant overrides are honored on the funnel path too.
- [ ] Where a caller genuinely has no ctx (e.g. a mob auto-attack funnel), keep the ctx-free
      path (the 4-arg ratio) — that's correct for ctx-agnostic skills.

## Done criteria

- A plugin that overrides the ctx-aware `CalculateSkillRatio` (reads an SC) yields the
  SC-modified ratio whether resolved via `CastendDamageId` or via the `SkillAttackService`
  splash funnel (test with a fake SC-reading plugin, asserting equal damage both paths).

## Test plan

- Register a plugin whose 6-arg `CalculateSkillRatio` returns a different value when a marker
  SC is present; resolve via both paths with the SC active; assert identical damage.

## Notes / gotchas

- Keep `ctx` optional so existing callers/tests compile; default null = ctx-free (today's behavior).
- This is narrow — only ctx-reading ratio plugins routed through the splash funnel are affected;
  the common single-target cast path already passes a real ctx.
