# GP-MVPFAME — MVP rewards + fame ranking work end-to-end

> **Epic:** gameplay · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** GP-PARTY (kill-credit fan-out) · **Unlocks:** none

## The deliverable

> When an MVP boss dies, **the top-damage player gets the MVP item + bonus EXP + the MVP
> animation/announce; player fame (blacksmith/alchemist/taekwon rankings) accrues and the
> ranking boards are queryable** — live client, persisting fame across logout.

## Player story

The mob-death observer computes MVP/top-damage (archive FEATURE-01), but the MVP *reward
packets* (item drop, special-EXP, the on-screen MVP effect) aren't emitted, party/in-range
kill credit for the reward isn't fanned out, and the fame-ranking subsystem (which also feeds
the Taekwon-ranker ×3 HP and the blacksmith/alchemist boards) doesn't exist.

## Current state — per layer

| Layer | State | Where |
|---|---|---|
| MVP compute | ✅ verify | `Map.Server/Mob/MobDeathObserver.cs` — top-damage + MVP (archive FEATURE-01) |
| MVP reward packets | ❌ | item/special-exp/effect emit (archive FEATURE-18) |
| Kill-credit fan-out | ❌ | party / in-range credit for quest+ach+MVP (archive FEATURE-19) |
| Fame ranking | ❌ | fame accrual + ranking board + Taekwon ranker (archive FEATURE-16) |
| Persistence | ❌ | fame points + ranking rows |

## rAthena reference

- `rathena/src/map/mob.cpp` — `mob_dead` MVP block: `mvp_sd` (top damage), MVP item roll,
  `pc_gainexp` MVP bonus EXP, `clif_mvp_item`/`clif_mvp_exp`/`clif_mvp_effect`.
- `rathena/src/map/pc.cpp` — `pc_setglobalreg` fame (`pc_addfame`), `pc_famelist_*`
  (blacksmith/alchemist/taekwon boards), `pc_setpos` ranking; Taekwon ranker → the ×3 HP/SP gate.
- `rathena/src/map/clif.cpp` — `clif_fame_blacksmith`/`_alchemist`/`_taekwon`,
  `clif_ranking_pk`/rank list packets.

## Dependencies — and how to satisfy

- **GP-PARTY** — prerequisite for the party kill-credit fan-out (in-range members share the
  quest/ach/MVP credit). Land party first.
- Packet-bridge pattern — foundation (MVP effect + ranking packets).

## Scope — every layer

- [ ] **MVP rewards**: on MVP death, emit the MVP item drop + bonus-EXP + the MVP effect/announce
      to the top-damage player (archive FEATURE-18).
- [ ] **Kill-credit fan-out**: party + in-range members get quest/achievement/MVP credit
      (archive FEATURE-19).
- [ ] **Fame ranking**: fame accrual on the qualifying actions (forge/brew/PK/Taekwon), the
      ranking boards, and populate `PlayerEntity.IsTaekwonRanker` (feeds the archived COMBAT-30
      ×3 HP/SP that's currently dormant).
- [ ] **CZ/ZC**: ranking-list request + the board packets; MVP effect packets.
- [ ] **Persistence**: fame points + ranking rows round-trip.

## Done criteria

- Killing an MVP grants the top-damage player the MVP item + bonus EXP + the on-screen MVP
  effect; party/in-range members share quest/ach credit.
- Forging/brewing/PK/Taekwon accrues fame; the ranking board shows the top players; a top
  Taekwon gets the ×3 HP/SP.
- Fame persists across logout.

## Test plan

- Service: MVP top-damage reward, kill-credit fan-out, fame accrual + board sort.
- Handler tests: ranking request → board.
- Persistence: fame round-trip; Taekwon-ranker ×3 HP applies after ranking.
- Live: MVP kill reward; fame board.

## Notes / gotchas

- `MobDmgList.TopDamageAttacker()` already gives the MVP winner (archive FEATURE-01).
- The Taekwon ×3 HP gate (`IsTaekwonRanker`) is already wired in CalcPc (archive COMBAT-30) — this
  ticket just populates the flag from the real ranking.
