# SC-FAMILIES — Sorcerer / Star-Emperor / Soul / Bard bespoke SC effects

> **Epic:** status · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** SC-MAGNITUDE · **Unlocks:** none

## The deliverable

> The class-family SCs with secondary effects beyond a stat number behave fully: Sorcerer
> elemental-option element-change + bolt-autocast, Star-Emperor stance banding, Inspiration
> debuff-clear/drain.

## What this absorbs (archive)

- `_archive/todo/status/SC-16` — Sorcerer `*_OPTION` secondary effects (element change + bolt-autocast + Wind/Petrology mods).
- `_archive/todo/status/SC-17` — Inspiration debuff-clear + drain tick; Banding real party-count + Def/Atk aggregate.

## rAthena reference

- `rathena/src/map/status.cpp` / `skill.cpp` — the Sorcerer `_OPTION` element-change + the
  spirit-sphere bolt autocast; `SC_INSPIRATION` debuff-clear + drain; `SC_BANDING` party-count
  aggregate (the archive cites the exact arms).

## Scope

- [ ] Sorcerer elemental-option: element change on the caster's attacks + bolt autocast +
      Wind/Petrology secondary mods.
- [ ] Inspiration: clear debuffs on start + the per-tick HP/SP drain.
- [ ] Banding: real party-count Def/Atk aggregate (replaces the best-effort count).

## Done criteria

- Each family SC produces its rAthena secondary effect (the archive lists them); the per-SC
  tests pass.

## Test plan

- Extend the archived SC-16/17 tests.

## Notes

- Builds on the landed magnitude fixes (archive SC-05/06) + SC-MAGNITUDE. Deferred.
