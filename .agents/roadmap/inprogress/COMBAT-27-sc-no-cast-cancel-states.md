# COMBAT-27 — SC-based no-cast-cancel states in the damage-interrupt gate

> **Epic:** Combat parity · **Status:** 🚧 In progress · **Size:** S · **Player-visible:** yes
> **Depends on:** COMBAT-08 (done) · **Blocks:** none

## Problem

COMBAT-08 wired the damage-driven cast interrupt and its no-cancel gate, but the gate only
checks the `bNoCastCancel` equip flag (`EquipBonusBundle.NoCastCancel`). rAthena
`unit_skillcastcancel` additionally exempts casters under specific status changes — most
importantly **SC_BASILICA** (within the priest's Basilica) and the **Free Cast**-style
"cannot be cast-cancelled" states — and applies the GvG-map qualifier to `bNoCastCancel`
(vs the unconditional `bNoCastCancel2`). Today a Basilica caster's spell is still interrupted
by a hit. The COMBAT-08 code marks the spot: `DamageService.InterruptCastOnDamage` comment
`// SC-based no-cancel states (SC_BASILICA / Free Cast) → COMBAT-27`.

## Current state (C#)

- `Map.Server/Combat/DamageService.cs` `InterruptCastOnDamage(target, onDeath:false)` — gates on
  `SkillDb.GetCastCancel(skillId)` and `PlayerEntity.EquipBonuses.NoCastCancel`; **no SC check**.
- `Map.Server/Status/StatusType.cs` — confirm `Basilica` / relevant SC enum members exist.
- The `bNoCastCancel` vs `bNoCastCancel2` distinction (map-flag-gated vs unconditional) is
  currently collapsed into the single `EquipBonusBundle.NoCastCancel` bool by COMBAT-23.

## rAthena reference (source of truth)

Canonical: `unit.cpp` `unit_skillcastcancel` (the early-return block).

- For players: `return 0` (no cancel) when `sd->special_state.no_castcancel2`, **or**
  (`sc->getSCE(SC_BASILICA)` and not the death variant), **or**
  (`sd->special_state.no_castcancel` **and** `map_flag_gvg2(bl->m)` / battle_config gvg flag).
- The skill `castcancel` flag (`skill_get_castcancel`) is the damage-variant gate (already
  honored in COMBAT-08).

## Scope — every sub-system that must be touched

- [ ] In `InterruptCastOnDamage` (damage variant), exempt a caster with `SC_BASILICA` active.
- [ ] Split the equip flag into `no_castcancel` (GvG-gated) vs `no_castcancel2` (unconditional)
      if COMBAT-23 surfaces both; gate `no_castcancel` on the target's map GvG flag.
- [ ] Add any other rAthena no-cancel SCs present in the engine (Free Cast equivalent).

## Done criteria

- A caster standing in Basilica is NOT interrupted by a damaging hit (cast survives).
- A `bNoCastCancel` (GvG-only) caster is interrupted on a non-GvG map but exempt on a GvG map;
  a `bNoCastCancel2` caster is always exempt.

## Test plan

- Caster with `SC_BASILICA` active takes a hit → cast survives, no cancel packet.
- `no_castcancel` caster on a non-GvG map → interrupted; same caster on a GvG-flagged map →
  not interrupted.
- `no_castcancel2` caster → never interrupted regardless of map.

## Notes / gotchas

- Map GvG flag lookup: use the existing `IMapFlagService` (already injected into `DamageService`).
- Keep the death variant (`onDeath:true`) unconditional — these exemptions are damage-variant only.
