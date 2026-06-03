# COMBAT-27 — SC-based no-cast-cancel states in the damage-interrupt gate

> **Epic:** Combat parity · **Status:** ✅ Done (2026-06-02) · **Size:** S · **Player-visible:** yes
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

- [x] Implemented the rAthena `unit_skillcastcancel` no-cancel gate in
      `InterruptCastOnDamage`: exempt when `NoCastCancel2` (unconditional) OR
      ((SC_UNLIMITEDHUMMINGVOICE || `NoCastCancel`) AND not GvG/BG). **Note:** SC_BASILICA
      is NOT a cast-cancel exemption in this rAthena (the actual SC is
      SC_UNLIMITEDHUMMINGVOICE) — a Basilica caster is uninterrupted because it takes no
      damage, which is a different mechanism ➡️ filed as **COMBAT-49**.
- [x] Split the equip flag into `NoCastCancel` (GvG-gated) vs `NoCastCancel2`
      (unconditional) on `EquipBonusBundle` + the extractor (COMBAT-23 had collapsed both);
      gate `NoCastCancel` on the target map's GvG/BG flag via the new `IsGvgOrBgMap`.
- [x] SC_UNLIMITEDHUMMINGVOICE (the engine's Free-Cast-equivalent no-cancel SC) wired.

## Done criteria

- ➡️ A caster standing in Basilica is NOT interrupted — this is damage-immunity, not a
  cast-cancel exemption in this rAthena; moved to **COMBAT-49**.
- A `bNoCastCancel` caster is exempt on a normal map but interrupted on a GvG/BG map ✅
  (note: this is rAthena's actual logic — the ticket's "exempt on GvG" wording was
  inverted, per the `unit.cpp` comment "flags being read the wrong way around"); a
  `bNoCastCancel2` caster is always exempt ✅.

## Test plan

- Caster with `SC_BASILICA` active takes a hit → cast survives, no cancel packet.
- `no_castcancel` caster on a non-GvG map → interrupted; same caster on a GvG-flagged map →
  not interrupted.
- `no_castcancel2` caster → never interrupted regardless of map.

## Notes / gotchas

- Map GvG flag lookup: use the existing `IMapFlagService` (already injected into `DamageService`).
- Keep the death variant (`onDeath:true`) unconditional — these exemptions are damage-variant only.

## History

- **2026-06-02** — inprogress→done. Ported the rAthena `unit_skillcastcancel` no-cancel
  gate into `DamageService.InterruptCastOnDamage`: `NoCastCancel2` exempts unconditionally;
  `NoCastCancel` / SC_UNLIMITEDHUMMINGVOICE exempt only off GvG/BG maps (new `IsGvgOrBgMap`).
  Split `EquipBonusBundle.NoCastCancel2` out of COMBAT-23's collapsed flag + the extractor.
  Found SC_BASILICA fictional-as-cast-cancel here (it's SC_UNLIMITEDHUMMINGVOICE) and the
  ticket's GvG wording inverted vs rAthena. Combat27NoCastCancelTests (4); unit suite 3838
  (1 fail = pre-existing INFRA-11 replay gate). Filed COMBAT-49 (Basilica caster
  damage-immunity).
