# COMBAT-60 — Per-skill div_ remainder: splash/SkillImpl arms + miscflag/ctx hook + positive-div multiply

> **Epic:** Combat parity · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** COMBAT-38, SKILL-17 (ratio-via-funnel for splash plugins)
> **Blocks:** none
> **Filed by:** COMBAT-38 — the arms it could not reach through the WeaponSkillImpl path.

## Problem

COMBAT-38 wired the dead `ModifyDamageData` hook into the **WeaponSkillImpl** damage
path, activating the per-skill div for KN_PIERCE (size+1), KN_BOWLINGBASH, SC_FATAL
MENACE (dagger+1), RA_WUGSTRIKE, RagingQuadrupleBlow, ThrowSpiritSphere, FrenzyShot.
Three slices remain:

1. **Splash + plain-SkillImpl arms.** RK_WINDCUTTER (2HSword→2), MT_AXE_STOMP
   (2HAxe→3), IG_OVERSLASH (miscflag→5/7) are `RecursiveDamageSplashSkillImpl`; RG_BACKSTAP
   (dagger→2), KiExplosion, PsychicWave are plain `SkillImpl` with their own
   `CastendDamageId`. Their `ModifyDamageData` overrides exist but aren't invoked (the
   splash ratio path is dead per COMBAT-35/SKILL-17; the SkillImpl ones bypass the
   WeaponSkillImpl wire-point).
2. **miscflag / ctx-dependent arms.** `ModifyDamageData(ref dmg, src, target, lv)` has
   no `miscflag` or `SkillBehaviorContext`, so the miscflag tiers (KN_BOWLINGBASH 3/4,
   IG_OVERSLASH 5/7, SHC_SAVAGE_IMPACT, IQ_THIRD_FLAME_BOMB, HN_DOUBLEBOWLINGBASH) and
   the SC-gated arms (NC_BOOSTKNUCKLE/ARMSCANNON SC_ABR_DUAL_CANNON, SHC_ETERNAL_SLASH
   SC_E_SLASH_COUNT, MT_MIGHTY_SMASH SC_AXE_STOMP, BO_* SC_RESEARCHREPORT) can't be
   resolved. The hook needs `miscflag` + `ctx` (several plugin docstrings already note
   "reroute when the hook gains a context").
3. **Positive-div per-hit damage multiply.** COMBAT-38 wired the div as display-only.
   rAthena's positive-div skills (KN_PIERCE per-hit-full → total = ratio × (size+1))
   should multiply the damage by the div; negative-div skills (Sonic Blow) keep the
   "ratio carries the total" convention. The plugins already encode the sign in
   `dmg.Hits` (`> 0 ? size : -size`) — the apply path must honor it.

## Current state (C#)

- `Map.Server/Skills/Behaviors/SkillImpl.cs:WeaponSkillImpl.CastendDamageId` +
  `Map.Server/Skills/SkillAttackService.cs` — invoke `ModifyDamageData` (display div).
- `ModifyDamageData(ref BattleDamage, Entity, Entity, ushort)` — no miscflag/ctx.
- Splash/SkillImpl plugins override `ModifyDamageData` but it isn't called for them.

## rAthena reference

- `battle.cpp:4470-4523` (multi_attack switch) + `battle.cpp:7422-7558` (weapon_attack
  switch). `DAMAGE_DIV_FIX` (battle.cpp:4365): positive div multiplies per-hit damage.

## Scope

- [ ] Base hit count for SkillImpl/splash multi-hit skills (COMBAT-39 covered the
      WeaponSkillImpl plugins via `SkillHitCounts` + `GetMultiHitCount`; SkillImpl/splash
      plugins that don't already render N via their own per-hit loop still default to
      div 1). Route their base div through `SkillHitCounts` in the funnel/their path.
- [ ] Extend the div hook with `miscflag` + `ctx` (or add `ResolveDiv(src,target,lv,
      miscflag,ctx)`), and route the miscflag/SC-gated arms through it.
- [ ] Invoke the hook in the splash + plain-SkillImpl damage paths (coordinate with
      SKILL-17's ratio funnel) so RK_WINDCUTTER / RG_BACKSTAP / AXE_STOMP / OVERSLASH
      arms activate.
- [ ] Apply the positive-div per-hit multiply (honor the `dmg.Hits` sign) so a
      positive-div skill's damage scales by its div; negative-div stays total.

## Done criteria

- ➡️ from COMBAT-38: RK_WINDCUTTER 2HSword → div 2; RG_BACKSTAP dagger → div 2; the
  miscflag tiers (BowlingBash 3/4, OverSlash 5/7) resolve.
- A positive-div skill (KN_PIERCE) deals per-hit × (size+1) total.

## Test plan

- Per-plugin div for the splash/SkillImpl arms; miscflag tiers; the positive-div
  damage multiply (Pierce total vs single-hit).
