# COMBAT-49 — Basilica caster protection (SC_BASILICA cell invulnerability)

> **Epic:** Combat parity · **Status:** ❌ Not started · **Size:** S · **Player-visible:** yes
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

- [ ] Apply SC_BASILICA / Basilica-cell damage immunity in `DamageService` (incoming
      attacks on a caster under SC_BASILICA / on a Basilica cell deal 0), which in turn
      means no cast interrupt (the gate is never reached).
- [ ] Confirm the AL_BASILICA ground unit applies the protective SC to entities on its cells.

## Done criteria

- ➡️ from COMBAT-27: a caster standing in Basilica takes no damage from a hostile attack
  (and therefore its cast is not interrupted).

## Test plan

- `Combat49BasilicaTests`: a target under SC_BASILICA takes 0 from an attack; a mid-cast
  Basilica caster's cast survives a swing.

## Notes / gotchas

- This is damage-prevention, NOT a cast-cancel exemption — keep it out of
  `InterruptCastOnDamage` (COMBAT-27) and put it on the damage-apply path.
