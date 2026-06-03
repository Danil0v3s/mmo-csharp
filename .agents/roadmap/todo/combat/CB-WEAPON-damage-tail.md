# CB-WEAPON — Weapon/melee damage matches rAthena at the remaining edges

> **Epic:** combat · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** SK-ENGINE (ctx-aware ratio funnel for splash/plain plugins) · **Unlocks:** none

## The deliverable

> Every weapon/melee skill's displayed + applied damage matches rAthena at the edges the
> archived COMBAT pass left open (splash/plain per-skill ratios + div, the five-accumulator
> element split, the remaining `RE_LVL_DMOD` per-arm cases).

## Player story

Combat damage is mostly correct (archived COMBAT-01..96 ported ratios, cardfix, `RE_LVL_DMOD`,
dual-wield, multi-hit). A formula tail remains where the number is slightly off for certain
skills. Unlike features, combat is already end-to-end (the number reaches the client) — this
ticket closes the remaining numeric gaps. **Deferred: combat last.**

## What this absorbs (archive — line-level refs there)

- `_archive/todo/combat/COMBAT-97` — PC five-accumulator damage parts (element split + ×2 status + percentAtk).
- `_archive/todo/combat/COMBAT-54` — per-arm `RE_LVL_DMOD` for splash/plain 120/150 arms (needs the SK-ENGINE ratio funnel).
- `_archive/todo/combat/COMBAT-60` — per-skill `div_` remainder (splash/SkillImpl arms + miscflag/ctx hook + positive-div multiply).
- `_archive/todo/combat/COMBAT-41` — bespoke per-skill magic/misc element overrides (some weapon-adjacent).

## rAthena reference

- `rathena/src/map/battle.cpp` — `battle_calc_weapon_attack` (the literal five-accumulator
  split + DEF-at-end reorder), `battle_calc_attack_skill_ratio`/`battle_calc_skillratio`
  (per-skill), the `RE_LVL_DMOD` macros per arm, `battle_calc_multi_attack` div.

## Dependencies — and how to satisfy

- **SK-ENGINE** — prerequisite for COMBAT-54/60: the splash (`RecursiveDamageSplashSkillImpl`)
  and plain `SkillImpl` plugins' `CalculateSkillRatio` aren't consumed by the damage funnel
  until the ctx-aware ratio funnel lands (archive SKILL-17). Land SK-ENGINE first.

## Scope

- [ ] Five-accumulator weapon-attack split + DEF-at-end reorder (full `battle_calc_weapon_attack`
      fidelity).
- [ ] Per-arm `RE_LVL_DMOD` for the splash/plain 120/150 arms (via the SK-ENGINE funnel).
- [ ] Per-skill `div_` remainder (splash/SkillImpl arms + miscflag/ctx hook + positive-div multiply).
- [ ] Weapon-adjacent bespoke element overrides.

## Done criteria

- The representative skills in each archived sub-ticket compute the rAthena-exact damage at the
  cited levels (the archive lists the numbers); the relevant `Combat*Tests` pass.
- No regression in the landed COMBAT-01..96 numbers.

## Test plan

- Port/extend the per-item tests named in the archived COMBAT-97/54/60/41 tickets; full combat
  suite green.

## Notes / gotchas

- This is granular formula coverage — each sub-item is independently verifiable against the
  archive's rAthena citations. Combat is last; pull this only after gameplay + status + skills.
