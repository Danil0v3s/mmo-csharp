# SC-15 — Soul Reaper / Soul Linker family consumers (orb-gain / aftercast / damage markers)

> **Epic:** Status parity hardening · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** none · **Blocks:** none · **Split from:** SC-04

## Problem

The Soul Reaper / Soul Linker SC family computes Vals (orb-gain chance, aftercast %, damage
markers) that no plugin reads:
- **Soulreaper** (`Val2 = 10+5*Val1`, Soul Sphere gain chance %).
- **Souldivision** (`Val2 = 10*Val1`, skill aftercast increase %).
- **Soulattack / Soulcurse / Soulenergy / Soulfairy / Soulfalcon / Soulgolem / Soulshadow** —
  damage / stat markers consumed by the respective Soul skills.

## Current state (C#)

- `Map.Server/Status/StatusEffectRegistry.cs` — Soulreaper (~1436), Souldivision (~1441), and
  the Soul* markers set Vals; no Soul Reaper/Linker plugin reads them.

## rAthena reference (source of truth)

- `status.cpp` `case SC_SOULREAPER:` / `SC_SOULDIVISION:` / `SC_SOUL*` init arms.
- Consumers: `skill.cpp` Soul Reaper skills (`SP_SOULENERGY` orb gain, `SOA_*` aftercast),
  `battle.cpp` Soul-mark damage amps.

## Scope — every sub-system that must be touched

- [ ] Wire the orb-gain chance (Soulreaper) into the Soul Sphere gain path.
- [ ] Wire the aftercast increase (Souldivision) into the skill-delay calc.
- [ ] Wire each Soul* damage/stat marker into its consuming Soul skill plugin, OR allowlist
      with a `status.cpp:line` consumer citation if a plugin is deferred to a named ticket.

## Done criteria

- Each Soul Reaper/Linker SC's Val is read by its consumer (orb gain, aftercast, damage amp),
  or explicitly allowlisted with a rAthena consumer citation.

## Test plan

- Per-SC unit tests asserting the orb-gain / aftercast / damage-amp outcome.

## Notes / gotchas

- This is a skill-plugin-side wiring effort (Soul Reaper / Soul Linker family), not a
  DamageService read — pairs with the SKILL family tickets.
