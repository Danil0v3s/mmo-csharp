# FEATURE-31 — Homunculus hunger timer + client packets

> **Epic:** Gameplay-Companion · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** FEATURE-08 (live homun entity) · **Blocks:** none

## Problem

FEATURE-08 spawns the homun but does not yet run the **hunger timer** (intimacy drops while starving,
homun reverts/runs at intimacy 0) nor emit the **client packets** (HP/SP bar, feed, skill window,
vaporize state) — so the player sees the homun but no live HP bar / hunger UI.

## Current state (C#)

- `Map.Server/Homunculus/HomunculusService.cs` — hunger fields exist on the per-master record + on
  `HomunculusEntity`, but no per-tick decay (cf. `PetService.Tick`); the spawn/vanish helpers leave a
  marked seam for the clif packets.
- No `ZC_PROPERTY_HOMUN` / `ZC_HO_PAR_CHANGE` / `ZC_FEED_HOM` / `ZC_HOSKILLINFO_LIST` /
  `ZC_CHANGESTATE_MER` packets emitted.

## rAthena reference

- `rathena/src/map/homunculus.cpp` `hom_hungry` timer; `clif_send_homdata` / `clif_hominfo` /
  `clif_homskillinfoblock` / `clif_hom_food`.

## Scope

- [ ] Per-homun hunger decay tick (mirror `PetService.Tick`): hunger ↓, intimacy ↓ while starving,
      revert/run at intimacy 0. Hook into the game loop.
- [ ] Emit the homun client packets at the spawn/HP-change/feed/vaporize seams left by FEATURE-08
      (define in `Core.Server/Packets` or via the PACKET-* seam).

## Done criteria

- The homun's HP/SP bar + hunger update live on the client; hunger decays and the homun reverts at
  intimacy 0.

## Test plan

- `HomunculusServiceTests` — hunger tick decays hunger/intimacy; the feed/HP packets are enqueued.
