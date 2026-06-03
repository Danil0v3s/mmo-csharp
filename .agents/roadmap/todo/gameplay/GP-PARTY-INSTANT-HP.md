# GP-PARTY-INSTANT-HP — Instant party HP-bar update on damage/heal

> **Epic:** gameplay · **Status:** ❌ Not started · **Size:** S · **Player-visible:** yes
> **Depends on:** GP-PARTY · **Unlocks:** none

## The deliverable

> A party member's HP bar on teammates' screens updates **immediately** when they take damage or
> heal, not just on the next ~1 s sync tick — matching rAthena `clif_party_hp` firing from the
> damage/heal path.

## Player story

GP-PARTY broadcasts party HP via `PartySyncService` on the rAthena `party_send_xy_timer` cadence
(~1 s, change-gated). That makes the bar correct but up to ~1 s stale during fast fights. rAthena
additionally calls `clif_party_hp` synchronously from `status_damage`/`status_heal` for an instant
update.

## Current state — per layer

| Layer | State | Where |
|---|---|---|
| Timer sync | ✅ | `Map.Server/Party/PartySyncService.cs` (~1 s, change-gated) |
| Instant-on-damage | ❌ | no `clif_party_hp` hook in the damage/heal path |

## rAthena reference

- `rathena/src/map/status.cpp` `status_damage`/`status_heal` → `clif_party_hp(sd)` when
  `sd->status.party_id` and the visible HP changed.

## Scope

- [ ] Add a `NotifyHp(PlayerEntity)` broadcast (reuse the `ZC_NOTIFY_HP_TO_GROUPM` build +
      `IPartyMapService` fan-out from `PartySyncService`) and call it from the HP-change sites
      (`DamageService`, heal, natural-heal) for party members, gated on a visible-HP change.
- [ ] Keep the timer sync as the position/back-stop path.

## Done criteria

- Taking damage updates the party HP bar on teammates' screens within a frame, not the next sync tick.

## Test plan

- A damage event on a party member enqueues `ZC_NOTIFY_HP_TO_GROUPM` to same-map teammates immediately.

## Notes

- Split from GP-PARTY: the bar already works + updates within ~1 s; this is the instant-responsiveness
  refinement (hooking the damage/heal path rather than only the periodic sync).
