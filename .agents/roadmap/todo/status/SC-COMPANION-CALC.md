# SC-COMPANION-CALC — homun/merc/elem recompute derived stats on level-up

> **Epic:** status · **Status:** ❌ Not started · **Size:** S · **Player-visible:** yes
> **Depends on:** GP-HOMUN, GP-MERC, GP-ELEM · **Unlocks:** none

## The deliverable

> A homunculus / mercenary / elemental **level-up (or equip / SC change) recomputes its
> companion-specific derived stats** — MaxHp/MaxSp grow per the companion db factor math, not the flat
> mob-HP path (rAthena `status_calc_homunculus_` / `_mercenary_` / `_elemental_`).

## Why / current state

SC-IMMUNE finished its other leaves (effect-resist cards, weapon-swap refresh, nostatus lookup) but
left the companion calc refresh: `StatusCalcService.CalcHomunculus/CalcMercenary/CalcElemental` just
call `CalcMob` + set a level override. The companion-specific scaling (homun `HpFactor`/`SpFactor`
from `homunculus_db`; merc/elem db scaling) is **not** applied, so a companion's MaxHp doesn't grow on
level-up.

This is genuinely blocked: there is no `homunculus_db`/`mercenary_db`/`elemental_db` HpFactor loaded
into the runtime, and no companion **level-up path** to call the calc — both belong to the companion
lifecycle tickets (GP-HOMUN / GP-MERC / GP-ELEM), which are still in `todo`. Wiring the calc without
the db + the level-up trigger would be a stub.

## rAthena reference

- `rathena/src/map/status.cpp:2872` (`status_calc_homunculus_`), `:2887` (`status_calc_mercenary_`),
  `:2920` (`status_calc_elemental_`) — recompute from the companion db on level/equip change.
- `homunculus_db.yml` HpFactor / SpFactor; the merc/elem db scaling.

## Scope

- [ ] Load the companion db stat factors (HpFactor/SpFactor for homun; merc/elem db scaling) into the
      runtime (coordinated with GP-HOMUN/MERC/ELEM's data layer).
- [ ] `CalcHomunculus/CalcMercenary/CalcElemental` compute the companion-specific MaxHp/MaxSp from the
      factor + level (not the flat mob path).
- [ ] Call the calc from the companion level-up / equip / SC-change paths (GP-HOMUN/MERC/ELEM).

## Done criteria

- A homunculus/mercenary/elemental level-up grows its MaxHp per the db factor; a test pins the growth.

## Notes

- Split from SC-IMMUNE (the archived SC-22 "companion calc refresh" leaf). The weapon-swap
  `status_change_refresh` half of SC-22 landed in SC-IMMUNE; this is the companion half, which needs
  the companion lifecycle first.
