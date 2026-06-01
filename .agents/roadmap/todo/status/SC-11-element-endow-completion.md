# SC-11 — Complete weapon/magic element-endow SCs (Aspersio / Shadow / Ghost / Enchantarms + magic)

> **Epic:** Status parity hardening · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** none · **Blocks:** none
> **Filed by:** SC-02 on 2026-06-01 (endow beyond the 4 elemental weapons it fixed).

## Problem

SC-02 wired the 4 elemental weapon endows (`Fireweapon`/`Waterweapon`/`Windweapon`/
`Earthweapon`) to override `Stats.WeaponElement` (which `BattleCalculator` reads).
rAthena's `status_get_weapon_element` (status.cpp:8630) resolves several **more**
endow sources, none of which currently change the attacker's element:

- `SC_ASPERSIO` → Holy weapon.
- `SC_SHADOWWEAPON` → Dark weapon.
- `SC_GHOSTWEAPON` → Ghost weapon.
- `SC_ENCHANTARMS` → element = `sc->val1` (variable, set by the granting skill).
- (Precedence among simultaneous endows: rAthena resolves dynamically in a fixed
  order; SC-02's mutate-the-stored-element approach is "last-applied wins", which is
  fine while these are mutually exclusive but not strictly ordered.)

There is also a **magic** endow path: `status.cpp:4779-4784` adds
`magic_atk_ele[ELE_x] += val1` (consumed by `GetMagicElement`).

## Current state (C#)

- `Map.Server/Status/StatusEffectRegistry.cs` — `Aspersio`, `Shadowweapon`,
  `Ghostweapon`, `Enchantarms` are presence-only markers (no element effect).
- `Map.Server/Combat/BattleElementService.cs:GetWeaponElement` reads only
  `Stats.WeaponElement`; `GetMagicElement`/`GetMiscElement` return Neutral.
- `Map.Server/Combat/BattleCalculator.cs` reads `s.WeaponElement` for the weapon
  attribute-fix; magic/misc element resolution is not endow-aware.

## rAthena reference

- `status.cpp:8630-8660` `status_get_weapon_element` — precedence + sources above.
- `status.cpp:4779-4784` magic-endow array.

## Scope

- [ ] Give `Aspersio`/`Shadowweapon`/`Ghostweapon` bespoke bodies that set
      `Stats.WeaponElement` to Holy/Dark/Ghost (store prev in Val2, restore OnEnd) —
      same mechanism SC-02 used for the elemental weapons.
- [ ] `Enchantarms`: set `Stats.WeaponElement` from `Val1` (the granting skill's
      element), restore OnEnd.
- [ ] Decide whether to keep the mutate-stored-element approach or move element
      resolution into `BattleElementService.GetWeaponElement` (dynamic, precedence-
      ordered) and route `BattleCalculator` through it. If the latter, thread
      `IStatusChangeService` into `BattleElementService` and inject it into the
      damage path (3 `s.WeaponElement` read sites in `BattleCalculator`).
- [ ] Magic endow: wire `GetMagicElement` to read the magic-endow SC(s); route the
      magic-damage element through it.

## Done criteria

- Aspersio → holy melee; Shadow/Ghost weapon → dark/ghost; Enchantarms → Val1 element.
- Magic endow changes magic-skill element.
- Base stats unchanged by any of them.

## Test plan

- Extend `SC02CalcFlagAllTests`-style element tests to Aspersio/Shadow/Ghost/Enchantarms.
- A magic-element test if `GetMagicElement` is routed.

## Notes / gotchas

- Recalc-survival of the mutated element is the same transient concern as all SC
  stat mods → **COMBAT-09**.
- If precedence matters in practice (multiple endows active at once), the dynamic
  `GetWeaponElement` approach is the faithful one; otherwise mutate-stored is simpler.
