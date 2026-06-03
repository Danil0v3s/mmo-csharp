# SC-CONSUMERS — Starved SC consumer reads wired

> **Epic:** status · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** none · **Unlocks:** none

## The deliverable

> SCs whose stored Val is never read now actually do their thing in the combat/heal/regen paths
> (Energy Coat, Crescent Elbow, Magic Rod, Poison React, Aura Blade, Gravitation, Parrying, Soul
> Reaper/Linker family).

## What this absorbs (archive)

- `_archive/todo/status/SC-12` — Energycoat SP-tier reduction + Crescentelbow reflect.
- `_archive/todo/status/SC-13` — Magicrod magic-absorb + Poisonreact autocast-Envenom.
- `_archive/todo/status/SC-14` — Aurablade / Gravitation / Parrying combat reads.
- `_archive/todo/status/SC-15` — Soul Reaper/Linker family consumers.

## rAthena reference

- `rathena/src/map/battle.cpp` / `status.cpp` — the consumer-side reads for each SC (the archive
  cites the exact functions: `battle_calc_damage` Energy Coat tier, Magic Rod absorb, etc.).

## Scope

- [ ] Wire each starved SC's Val read into its consumer (damage reduce/reflect/absorb/autocast/
      heal/regen path), per the archived sub-tickets.

## Done criteria

- Each SC produces its rAthena effect in play (e.g. Energy Coat reduces damage by the SP-tier
  amount; Magic Rod absorbs the next bolt to SP); the per-SC tests pass.

## Test plan

- Extend the archived SC-12/13/14/15 consumer tests.

## Notes

- Pattern established by archive SC-04 (Kaupe/Kaahi/Richmankim). Deferred.
