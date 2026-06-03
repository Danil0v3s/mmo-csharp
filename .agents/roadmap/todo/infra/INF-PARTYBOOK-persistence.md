# INF-PARTYBOOK — Party booking persists

> **Epic:** infra · **Status:** ❌ Not started · **Size:** S · **Player-visible:** yes
> **Depends on:** GP-PARTY · **Unlocks:** none

## The deliverable

> A player can **post/search/join party-booking ads (the "looking for party" board), and the
> board persists** — live client.

## What this absorbs (archive)

- `_archive/todo/infra/INFRA-06` — party-booking persistence.

## rAthena reference

- `rathena/src/map/clif.cpp` — the `CZ_PARTY_BOOKING_*` set; `char` booking persistence.

## Scope

- [ ] **Service + persistence**: post/update/delete a booking ad; search by level/job/purpose.
- [ ] **CZ/ZC**: register/search/delete/notify packets.

## Done criteria

- A player posts an LFP ad → others search + see it → it persists across the poster's logout until
  expiry/cancel.

## Test plan

- Service + persistence round-trip + handler tests.

## Notes

- Parallel. Pairs with GP-PARTY.
