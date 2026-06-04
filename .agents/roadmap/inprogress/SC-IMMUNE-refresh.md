# SC-IMMUNE — Immunity matrix + refresh/spread wiring

> **Epic:** status · **Status:** 🚧 In progress · **Size:** S · **Player-visible:** yes
> **Depends on:** none · **Unlocks:** none

## The deliverable

> Status-immunity (card-bonus tolerance matrix) + the companion calc-refresh + `status_change_refresh`
> + robust `nostatus` map-id lookup all behave per rAthena.

## What this absorbs (archive)

- `_archive/todo/status/SC-21` — `status_isimmune` PC card-bonus tolerance matrix.
- `_archive/todo/status/SC-22` — companion calc refresh + `status_change_refresh` wiring + robust `nostatus` map-id lookup.

## rAthena reference

- `rathena/src/map/status.cpp` — `status_isimmune` (the per-eff tolerance from card bonuses),
  `status_change_refresh`, the `nostatus` mapflag gate.

## Scope

- [ ] PC card-bonus status-tolerance matrix in `status_isimmune`.
- [ ] Companion `status_calc_*` refresh + `status_change_refresh` wiring.
- [ ] Robust `nostatus` map-id lookup.

## Done criteria

- A player with effect-resist cards resists the matching SC by the card amount; companions
  refresh their calc on the right triggers; `nostatus` maps block the listed SCs.

## Test plan

- Extend the archived SC-21/22 tests.

## Notes

- Completes the SC-08 P0.5 leaves. Deferred.
