# FEATURE-18 — MVP reward client packets (item / special-exp / effect)

> **Epic:** Feature unlock · **Status:** ❌ Not started · **Size:** S · **Player-visible:** yes
> **Depends on:** FEATURE-01 (MVP state mutation lands the exp/drop/announce) · **Blocks:** none

## Problem

FEATURE-01 awards MVP exp + one MVP drop to the top-damage PC and fires a world
announce (`ZC_BROADCAST2`), but the dedicated rAthena MVP client packets are not
sent: the killer does not see the "you got the MVP item" popup, the MVP-exp number,
or the on-screen MVP crown effect. The reward *state* is correct; only the bespoke
client feedback packets are missing.

## Current state (C#)

- `Map.Server/Mob/MobDeathObserver.cs` `AwardMvp(...)` — awards `IExpService.GainExp`
  (MVP exp), drops one MVP item via `IItemDropService.DropOnFloor(isMvpDrop:true)`,
  and calls `Announce(...)` → `ZC_BROADCAST2`. There is a marked comment where the
  ZC_MVP_* effect packets would emit.
- `Core.Server/Packets/Out/ZC/` — `ZC_BROADCAST2` exists; **no** `ZC_MVP`,
  `ZC_MVP_GETTING_ITEM`, or `ZC_MVP_GETTING_SPECIAL_EXP`.

## rAthena reference (source of truth)

- `rathena/src/map/clif.cpp` `clif_mvp_item` (0x10a), `clif_mvp_exp` (0x10b),
  `clif_mvp_effect` (0x10c) — sent to `mvp_sd` in `mob_dead`'s MVP block.
- `rathena/src/map/packets.hpp` — `PACKET_ZC_MVP_GETTING_ITEM` (0x10a, 4 bytes:
  item id), `PACKET_ZC_MVP_GETTING_SPECIAL_EXP` (0x10b, 6 bytes: exp), `PACKET_ZC_MVP`
  (0x10c, 6 bytes: account id of the MVP).

## Scope

- [ ] Add `ZC_MVP_GETTING_ITEM` (0x10a), `ZC_MVP_GETTING_SPECIAL_EXP` (0x10b),
      `ZC_MVP` (0x10c) packet classes in `Core.Server/Packets/Out/ZC/` with the
      rAthena field layout.
- [ ] Emit them to the MVP PC from `MobDeathObserver.AwardMvp` (via
      `IVisibilityService.SendToSelf`) at the marked seam: item popup on the MVP
      drop, special-exp number on the MVP exp award, and the MVP effect on the
      top-damage PC.
- [ ] Keep the existing `ZC_BROADCAST2` world announce.

## Done criteria

- The MVP killer receives the item-get popup, the MVP-exp number, and the MVP
  on-screen effect when an MVP mob dies, matching rAthena's three packets.
- No regression to the FEATURE-01 exp/drop/announce state.

## Test plan

- `Map.Server.Tests` `MobDeathObserverTests` — assert the three ZC packets are
  enqueued to the MVP PC's session (inject a recording session) on an MVP kill.

## Notes / gotchas

- The MVP PC = the top cumulative-damage attacker (already resolved in
  `AwardMvp`). Only that one PC gets the item/exp/effect; the announce is world-wide.
