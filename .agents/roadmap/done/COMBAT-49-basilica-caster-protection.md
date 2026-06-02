# COMBAT-49 — Basilica caster protection (SC_BASILICA cell invulnerability)

> **Epic:** Combat parity · **Status:** ✅ Done (2026-06-02) · **Size:** S · **Player-visible:** yes
> **Depends on:** none
> **Blocks:** none
> **Filed by:** COMBAT-27 — the Basilica done-criterion it found to be a different mechanism.

## Problem

COMBAT-27 implemented the no-cast-cancel gate (no_castcancel2 + the GvG-gated
no_castcancel / SC_UNLIMITEDHUMMINGVOICE). The ticket's premise that **SC_BASILICA**
is a cast-cancel exemption is fictional in this rAthena (`unit_skillcastcancel` exempts
SC_UNLIMITEDHUMMINGVOICE, not SC_BASILICA). In rAthena a Basilica caster is uninterrupted
because units inside Basilica take **no damage** (the cell is a sanctuary), so the
damage-interrupt path is never reached. That damage-immunity is not implemented here, so
a caster standing in Basilica can still be hit (and interrupted).

## Current state (C#)

- `Map.Server/Combat/DamageService.cs` — no SC_BASILICA damage-immunity check;
  `InterruptCastOnDamage` correctly does NOT special-case Basilica (parity with rAthena).
- `Map.Server/Status/StatusType.cs` — `Basilica` (134) + `BasilicaCell` (710) exist.

## rAthena reference (source of truth)

- `status.cpp` / `battle.cpp` — SC_BASILICA / the Basilica ground-unit make targets on its
  cells immune to damage (sanctuary); `skill.cpp` AL_BASILICA unit placement.

## Scope — every sub-system that must be touched

- [x] Apply SC_BASILICA_CELL damage immunity in `DamageService` (renewal
      `battle_calc_damage`): `DamageService.IsBasilicaImmune` blocks a hit on a target with
      `StatusType.BasilicaCell` unless the attacker has `MD_STATUSIMMUNE`; wired into the
      auto-attack (`PerformMeleeAttack`) and the skill funnel (`SkillAttackService`, with the
      `SP_SOULEXPLOSION` exemption). 0 damage → the cast-interrupt gate (`actual > 0`) is never
      reached, so the cast survives.
- [x] Confirm the AL_BASILICA ground unit applies the protective SC to entities on its cells.
      ➡️ The renewal port has **no Basilica ground unit / cell-basilica system** (the cast
      applies only the `StatusType.Basilica` self-buff), so `SC_BASILICA_CELL` is never applied
      in prod — moved to **COMBAT-68** (the immunity is implemented + tested here, but dormant
      live until the cell-apply path lands).

## Done criteria

- ➡️ from COMBAT-27: a caster standing in Basilica takes no damage from a hostile attack
  (and therefore its cast is not interrupted). ✅ damage-path logic verified via tests (target
  carrying `SC_BASILICA_CELL`); end-to-end prod reachability (the cell that applies the SC)
  ➡️ COMBAT-68.

## History

- 2026-06-02 — Implemented the renewal Basilica damage-immunity: `IDamageService.IsBasilicaImmune`
  + `DamageService` impl (target `SC_BASILICA_CELL`, attacker not `MD_STATUSIMMUNE`), wired into
  `PerformMeleeAttack` (zeroes the swing) and the `SkillAttackService` funnel (returns 0; exempts
  `SP_SOULEXPLOSION`) — matches `battle_calc_damage` RENEWAL. Cast-survival follows from the
  existing `actual > 0` interrupt gate. Tests: `Combat49BasilicaTests` (6, green); suite green
  (combat+skills 2936). Filed COMBAT-68 (renewal Basilica ground-unit + `pc_cell_basilica`
  SC_BASILICA_CELL application so the immunity is reachable in prod).

## Test plan

- `Combat49BasilicaTests`: a target under SC_BASILICA takes 0 from an attack; a mid-cast
  Basilica caster's cast survives a swing.

## Notes / gotchas

- This is damage-prevention, NOT a cast-cancel exemption — keep it out of
  `InterruptCastOnDamage` (COMBAT-27) and put it on the damage-apply path.
