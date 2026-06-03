# CB-DEFENSE — Cardfix / resist / race2 defensive completeness

> **Epic:** combat · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** none · **Unlocks:** none

## The deliverable

> Target-side damage reduction (cardfix remainder, race2 grouping, SetDef/MDef-by-race,
> state-no-recover, drop-item bonuses, on-skill effects) matches rAthena. **Combat last.**

## What this absorbs (archive)

- `_archive/todo/combat/COMBAT-98` — race2 cardfix: melee per-group multiply + pet race2.
- `_archive/todo/combat/COMBAT-99` — thread the real `BF_*` damage flag into `CalcCardFix`.
- `_archive/todo/combat/COMBAT-100` — per-race vellum vanish (`bHPVanishRaceRate`).
- `_archive/todo/combat/COMBAT-101` — drop-item bonus tables (`bAddMonsterDropItem/Class/Group`).
- `_archive/todo/combat/COMBAT-102` — `bSetDefRace`/`bSetMDefRace`.
- `_archive/todo/combat/COMBAT-103` — `bStateNoRecoverRace`.
- `_archive/todo/combat/COMBAT-104` — `bAddEffOnSkill`.

## rAthena reference

- `rathena/src/map/battle.cpp` — `battle_calc_cardfix` (race2/SubDefEle/magic arrays/BF flag),
  `battle_calc_damage`; `status_get_race2` classifier; `pc.cpp` bonus consumers.

## Scope

- [ ] race2 classifier (`status_get_race2`) + mob RaceGroups data + melee per-group cardfix.
- [ ] Thread the real `BF_WEAPON/MAGIC/MISC` flag + skill range into `CalcCardFix`.
- [ ] `bSetDefRace`/`bSetMDefRace`, `bStateNoRecoverRace`, `bHPVanishRaceRate`, drop-item
      bonus tables, `bAddEffOnSkill`.

## Done criteria

- The cases in each archived sub-ticket reduce/redirect damage by the rAthena-exact amounts;
  the `Combat*Tests` pass; no regression in landed cardfix numbers.

## Test plan

- Extend the per-item tests from the archived COMBAT-98..104 tickets.

## Notes

- Needs the `mob RaceGroups` data (race2) — a small `*_db` add. Granular; combat-last.
