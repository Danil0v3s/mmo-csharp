# INF-AUTOSPELL — Sage Autospell works end-to-end

> **Epic:** infra · **Status:** ❌ Not started · **Size:** S · **Player-visible:** yes
> **Depends on:** none · **Unlocks:** none

## The deliverable

> A Sage can **cast Autospell, pick a learned bolt from the menu, gain `SC_AUTOSPELL`, and have it
> auto-cast that bolt on melee hits** — live client.

## What this absorbs (archive)

- `_archive/todo/infra/INFRA-04` — Sage Autospell skill (`SC_AUTOSPELL` attach + on-hit proc).

## rAthena reference

- `rathena/src/map/skill.cpp` — `SA_AUTOSPELL` (the bolt-selection menu + `SC_AUTOSPELL` attach);
  `rathena/src/map/battle.cpp` — the on-hit autospell proc.

## Scope

- [ ] **Skill**: `SA_AUTOSPELL` — selection menu (learned bolt level → options), attach `SC_AUTOSPELL`.
- [ ] **CZ handler**: the autospell selection response.
- [ ] **On-hit proc**: roll + auto-cast the chosen bolt on melee hit (DamageService hook).

## Done criteria

- Casting Autospell → menu → pick Fire Bolt → melee hits proc Fire Bolt at the rAthena rate.

## Test plan

- Skill + on-hit proc tests + a selection handler test.

## Notes

- Parallel. Small, self-contained.
