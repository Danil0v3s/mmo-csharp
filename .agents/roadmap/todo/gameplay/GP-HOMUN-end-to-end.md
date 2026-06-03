# GP-HOMUN — Homunculus works end-to-end

> **Epic:** gameplay · **Status:** ❌ Not started · **Size:** L · **Player-visible:** yes
> **Depends on:** none · **Unlocks:** SK-HOMUN (homun skills), SCR-DOMAIN

## The deliverable

> A player can **summon a homunculus that follows + fights, feed it (hunger/intimacy), it
> gains EXP + levels + stats, evolves/mutates, learns skills, vaporizes/recalls, and dies/
> resurrects** — live client, surviving logout with all of that state intact.

## Player story

The *entity* slice is real (live `HomunculusEntity` spawns on Call/RecvData, vanishes on
Vaporize/Dead/Delete, re-spawns on Resurrect — archive FEATURE-08). But it doesn't act (no AI/
combat), doesn't grow (no exp/level/stat curve), has no hunger timer, no skill-up, and no client
packets, and its state isn't saved.

## Current state — per layer

| Layer | State | Where |
|---|---|---|
| Data | ✅ | `homunculus_db` seeded (class/stats/evo) |
| Entity + lifecycle | ✅ verify | `Map.Server/Entities/HomunculusEntity.cs` + `HomunculusService` spawn/vanish (archive FEATURE-08) |
| AI / combat | ❌ | follow + attack the master's target (archive FEATURE-29) |
| Growth / exp | ❌ | exp table + level + stat growth + evolve/mutate (archive FEATURE-30) |
| Hunger timer | ❌ | hunger decay + intimacy + starvation (archive FEATURE-31) |
| CZ handlers | ❌ | menu/feed/skill-up/name/delete missing (archive PACKET-04) |
| ZC emits | ❌ | homun info/stats/exp/intimacy/hunger/skill-tree missing |
| Persistence | ❌ | homun row (stats/exp/intimacy/hunger/skills) save/load (FEATURE-17 save fan-out) |

## rAthena reference

- `rathena/src/map/homunculus.cpp` — `hom_call`/`hom_vaporize`/`hom_recv_data`, `hom_gainexp`/
  `hom_levelup`, `hom_evolution`/`hom_mutate`, `hom_hungry` (hunger timer + intimacy),
  `hom_food`, `hom_addspiritball`, `merc_hom_skillup`, the homun AI (`unit_walktobl`/attack via
  `mob`-style FSM).
- `rathena/src/map/clif.cpp` — `CZ_COMMAND_MER` (menu/delete), feed, skill-up; emit
  `clif_hominfo`, `clif_homskillinfoblock`, `clif_send_homdata`, `clif_homunculus_*`.
- `char` homun persistence.

## Dependencies — and how to satisfy

- Packet-bridge pattern — foundation.
- Companion save IPC — `IntifService.HomunSave` exists; wire it via the save fan-out (do NOT
  inject `IIntifService` into `HomunculusService` — DI cycle; the save dispatch rides the
  fan-out, archive FEATURE-17).
- AI reuse — mirror the mob AI FSM for follow + target-inherit.

## Scope — every layer

- [ ] **AI/combat**: homun follows the master, inherits/attacks the master's target, uses
      auto-skills (archive FEATURE-29).
- [ ] **Growth**: exp gain on kill (share), level-up, stat growth per `homunculus_db`, evolve
      + mutate (archive FEATURE-30).
- [ ] **Hunger/intimacy**: hunger decay timer, feed raises hunger+intimacy, starvation drops
      intimacy / vaporizes (archive FEATURE-31).
- [ ] **CZ handlers**: menu (rest/delete), feed, skill-up, rename.
- [ ] **ZC emits**: homun info, stat/exp/intimacy/hunger updates, skill-tree block.
- [ ] **Persistence**: homun row (class/level/exp/stats/intimacy/hunger/skills) load on call /
      save on mutate + logout.

## Done criteria

- Player summons a homun → it follows + attacks the master's target → gains EXP from kills →
  levels + grows stats; feeding raises hunger/intimacy; it can skill-up + evolve; vaporize/
  recall works; on death it can be resurrected.
- Relog → same homun (level/exp/intimacy/hunger/skills) re-summons.

## Test plan

- Handler tests: menu/feed/skill-up → service.
- Service: exp/level curve, hunger decay, evolve gate (extend archived HomunculusSpawnTests).
- Persistence round-trip.
- Live: summon → fight → level → feed → evolve → vaporize → relog.

## Notes / gotchas

- HP/SP back the `Entity.Stats`; `MasterId` links to the owner (archive FEATURE-08).
- Intimacy bands gate evolution + the combat bonus.
