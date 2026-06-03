# SKILL-16 — Route DamageService.CanDamage through BattleTargetResolver (+ attack vs mechanic-damage split, BG teams)

> **Epic:** Skills · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** SKILL-03 (BattleTargetResolver) · **Blocks:** none

## Problem

SKILL-03 made the splash victim filter (`MapForeachInRangeService`) use the shared
`BattleTargetResolver` (slave-master substitution + PvP/GvG/BG mapflags), so an offensive
AoE now correctly excludes a player's own slave, the master's party, and unaffiliated
players on a peaceful map. But `DamageService.CanDamage` was **left on its legacy logic**
(it allows PC↔PC damage on a field map unless `NoPvp`, and does not do slave substitution),
because routing it naively through the resolver broke two real behaviors:

1. **Mechanic-damage is not an attack.** `ApplyDamage` is also the path for the Akaitsuki
   heal-flip (`Heal.cs:162` applies `-heal` to a friendly/self target), reflect, and DoT.
   These must apply to non-enemies, so they cannot be allegiance-gated. A faithful CanDamage
   unification first needs to distinguish *attack* damage (gated) from *mechanic* damage
   (ungated) — e.g. an `isAttack`/flag parameter or a separate apply path.
2. **Direct-attack PvP on field maps.** With the legacy gate, a player can still melee an
   unaffiliated player on a non-PvP field map (rAthena forbids this). Fixing it requires the
   resolver but only on the *attack* path (so heal-flip still works).

The result: the splash filter and the direct-attack damage gate use different allegiance
logic. They agree for the offensive splash set (the splash side never feeds a friendly/
neutral victim into the gate), but they are not the single source the SKILL-03 Done criterion
envisioned, and direct melee PvP on field maps is still wrong.

## Current state (C#)

- `Map.Server/Combat/DamageService.cs` `CanDamage` — legacy PC↔PC logic: allow unless same
  party/guild or `NoPvp`; PvE (mob involved) → allow; no slave substitution; no `Pvp`/`Gvg`/
  `Battleground` flag read. A comment marks the SKILL-16 boundary.
- `Map.Server/Skills/Splash/BattleTargetResolver.cs` — the shared resolver (built by SKILL-03):
  `Classify(src, target, entities, mapFlags, world)` → `BattleCheckTarget`.
- `Map.Server/Skills/Behaviors/Acolyte/Heal.cs:162` — `ctx.Damage?.ApplyDamage(target, -heal, src)`
  (heal-flip; must remain ungated).
- BG team allegiance: `BattleTargetResolver` treats a `Battleground` map as a hostile zone but
  has no BG-team concept, so two players on the same BG team are not recognized as allies.

## rAthena reference (source of truth)

- `battle.cpp:battle_check_target` — the single allegiance authority for BOTH attack
  validation and splash. The damage path validates via `battle_check_target(src, bl, BCT_ENEMY)`.
- `battle.cpp:11366` BG team: `sbg_id == tbg_id` keeps BG teammates out of `BCT_ENEMY`.
- Mechanic-damage (heal-flip, reflect, status DoT) calls `status_fix_damage`/`status_damage`
  directly — it does NOT pass through `battle_check_target`.

## Scope — every sub-system that must be touched

- [ ] Add an attack-vs-mechanic distinction to the damage entry points (e.g. an `isAttack`
      flag on `ApplyDamage`/`PerformMeleeAttack`, or a separate `ApplyMechanicDamage`). Heal-flip
      / reflect / DoT use the ungated path.
- [ ] Route the *attack* `CanDamage` through `BattleTargetResolver` (allow iff `Enemy`), so
      field-map direct PvP is refused, slave substitution applies, and PvP/GvG/BG + friendly-fire
      flags match the splash filter exactly.
- [ ] BG team allegiance in `BattleTargetResolver` (same BG team → ally) once BG teams are
      modeled (coordinate with FEATURE-15 / the battleground subsystem).
- [ ] Agreement test: for a matrix of (src, target, map) the splash `Classify` and the attack
      `CanDamage` return the same hostile/non-hostile verdict.

## Done criteria

- `Classify` and the attack-path `CanDamage` agree for every (src, target, map) — no victim
  passes the splash filter then gets dropped by the damage gate, and direct melee obeys the
  same PvP/field rules as splash.
- Akaitsuki heal-flip / reflect / DoT still apply to friendly/self targets (mechanic-damage
  ungated).
- A player cannot melee an unaffiliated player on a non-PvP field map.

## Test plan

- `SplashDamageParity` — victim set from `ForEachInSplash` == victim set the attack `CanDamage`
  accepts for the same cast/map.
- Mechanic-damage regression: Akaitsuki heal-flip damages a non-enemy target (no allegiance drop).
- Field-map direct melee between unaffiliated players is refused; PvP-map melee is allowed.

## Notes / gotchas

- This is the other half of SKILL-03's "unify the damage gate" scope item, split out because
  the attack-vs-mechanic-damage distinction is real design work and a naive merge regresses
  heal-flip (`Heal.cs:162`) and friendly DoT.
