# SC-MOVESPEED-FIELD — a real movement-speed-% stat so speed SCs convert faithfully

> **Epic:** status · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** none · **Unlocks:** the speed-SC magnitude conversions in SC-MAGNITUDE

## The deliverable

> The C# status stats carry a **movement-speed-% field** (rAthena `SCB_SPEED` / `status->speed`), so
> movement-speed status changes apply their **real** magnitude instead of the current **AspdRate proxy**.

## Why / current state

SC-MAGNITUDE is converting the generator-default SCs to their rAthena magnitudes. A whole cluster of
them adjust **movement speed** — but `Map.Server/Status/BattleStats.cs` has **no MoveSpeed% field**, so
the existing speed SCs approximate it with the ASPD-rate field (e.g. `SC_CARTBOOST` at
`StatusEffectRegistry.cs:956`: *"+20 MoveSpeed%; AspdRate proxy since we don't have a dedicated MoveSpeed%
field yet"*; `SC_WINDWALK` `:733-736` does the same). Because the proxy is wrong-stat, these speed SCs
**cannot be converted faithfully** and are left as generator-defaults in the SC-MAGNITUDE worklist:

- `SC_GN_CARTBOOST` (val2 = 50/75/100 by level), `SC_CATNIPPOWDER` (val3 = 25·val1),
  `SC_ARCLOUSEDASH` (val3 = 25), `SC_WALKSPEED`, `SC_DORAM_WALKSPEED`, … plus the already-proxied
  `SC_CARTBOOST` / `SC_WINDWALK` would become exact.

## rAthena reference

- `rathena/src/map/status.cpp` — `status->speed`, `SCB_SPEED` recompute in `status_calc_bl_`, the
  per-SC `val2/val3` speed-rate reads in `status_calc_speed`.

## Scope

- [ ] Add a movement-speed-% field to `BattleStats` (and the base `speed` recompute path).
- [ ] Wire the speed SCs (Cartboost/Windwalk/GnCartboost/Catnippowder/Arclousedash/Walkspeed/…) to it
      with their real magnitudes; drop the AspdRate proxy.
- [ ] The movement layer consumes the field (faster/slower walk).

## Done criteria

- A speed buff/debuff changes the player's actual walk speed by the rAthena percentage; the proxied SCs
  apply their exact magnitude; the SC-MAGNITUDE worklist drops the speed cluster.

## Notes

- Filed by SC-MAGNITUDE (turn 3). The proxy is a long-standing convention (SC_CARTBOOST/SC_WINDWALK);
  this is the shared infrastructure that lets the speed SCs convert faithfully. Movement-side, not combat
  damage.
