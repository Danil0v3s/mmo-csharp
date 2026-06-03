# FEATURE-19 — Party / in-range kill credit for quest + achievement objectives

> **Epic:** Feature unlock · **Status:** ❌ Not started · **Size:** S · **Player-visible:** yes
> **Depends on:** FEATURE-01 (death observer + per-contributor fan-out) · **Blocks:** none

## Problem

FEATURE-01's mob-death observer credits quest + achievement kill objectives to the
**damage contributors** (distinct PCs in the mob's damage log, plus the last-hitter).
rAthena `mob_dead` is broader: it also credits **party members of the killer who are
on the same map and within `AREA_SIZE` range**, even if they dealt no damage
(`mob.cpp` builds `tmpsd[]` from the party `pt` loop, not only the dmglog). So a
party member standing next to the kill currently gets no quest/achievement progress
unless they personally hit the mob.

## Current state (C#)

- `Map.Server/Mob/MobDeathObserver.cs` `ResolveContributors(...)` — returns the
  distinct live PCs found in the damage-log snapshot, plus the killer. No party
  expansion, no range scan.

## rAthena reference (source of truth)

- `rathena/src/map/mob.cpp` `mob_dead` — the `tmpsd[]` build: iterates the killer's
  party members (`party_foreachsamemap` / the `pt` loop) and adds those on the same
  map within `AREA_SIZE` to the credit set, in addition to the damage-log PCs. Quest
  + achievement objective updates then run over `tmpsd[]`.

## Scope

- [ ] In `ResolveContributors`, when the killer is in a party, add party members on
      the same map within `AREA_SIZE` (14) of the dead mob to the credit set
      (dedup with the dmglog PCs). Use `IEntityRegistry.ForEachInRange` + the party
      membership lookup.
- [ ] Keep the existing damage-log contributors. Pet catch stays killer-only.
- [ ] Verify rAthena's exact gate (same-map + within range + party share rules) and
      match it.

## Done criteria

- A party member of the killer, on the same map within range, who dealt **no**
  damage to the mob still gets quest + achievement kill credit; one outside range or
  not in the party does not.
- Solo kills (no party) behave exactly as today.

## Test plan

- `Map.Server.Tests` `MobDeathObserverTests` — party member in range (credited) vs
  out of range / not in party (not credited), with no damage dealt.

## Notes / gotchas

- Reuse the party membership service already used by `IPartyShareService`. Range =
  rAthena `AREA_SIZE` (14). Don't double-credit a PC that is both a damage
  contributor and a party member.
