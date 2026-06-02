# COMBAT-59 — Wire IStatusChangeService into BattleCalculator (break the cycle) so SC combat reads activate in production

> **Epic:** Combat parity · **Status:** 🚧 In progress · **Size:** M · **Player-visible:** yes
> **Depends on:** none · **Blocks:** the live behavior of every SC-gated combat read
> **Filed by:** COMBAT-37 — Fear Breeze / Eternal Chain read `_sc`, which is null in
> the production `BattleCalculator` (same as COMBAT-17's Kagemusya).

## Problem

`BattleCalculator` reads `_sc` (`IStatusChangeService`) in ~15 combat spots, but the
production registration (`Program.cs:284`) **does not pass `sc:`** — because
`IStatusChangeService` → `IDamageService` → `IBattleCalculator` is a constructor
cycle. So `_sc` is **null in production** and every SC-gated combat read is **dormant
live** (it only works in unit tests that inject `sc:`). Dormant effects include:

- **Multi-attack:** SC_KAGEMUSYA (COMBAT-17), SC_FEARBREEZE + SC_E_CHAIN +
  SC_QD_SHOT_READY start (COMBAT-37).
- **Damage bumps:** SC_EDP, SC_BLOODYLUST, SC_RUSHWINDMILL, SC_PYROCLASTIC,
  SC_HEATBARREL (weapon); SC_MAGICPOWER, SC_MOONLITSERENADE (magic).
- **Other:** SC_MAXIMIZEPOWER (force-max roll), SC_MADOGEAR gate, SC_SIGNUMCRUCIS
  (def reduction vs undead/demon).

This is a major slice of the "SC-engine magnitude gaps" called out in CLAUDE.md.

## Current state (C#)

- `Map.Server/Combat/BattleCalculator.cs` — `private readonly IStatusChangeService? _sc;`
  read in TryCritical/Madogear/CalcMultiAttack/ComputeHandDamage/CalcMagicAttack.
- `Map.Server/Program.cs:284` — `new BattleCalculator(rng, cards, elements, zone, ammo)`
  with **no `sc:`** (cycle avoidance).
- Tests construct `new BattleCalculator(rng, sc: …)` directly, so all SC reads are
  covered by unit tests — only the live wiring is missing.

## rAthena reference

- `status_change` is read throughout `battle.cpp` (`battle_weapon_attack`,
  `battle_calc_multi_attack`, `battle_calc_weapon_attack`, …) — SCs are always live.

## Scope

- [ ] Break the cycle with the established `Lazy<IStatusChangeService>` pattern (as in
      `StatusCalcService` / `SkillAttackService`): add a lazy seam to `BattleCalculator`
      (e.g. an additional `Lazy<IStatusChangeService>? scLazy` ctor arg, with the `_sc`
      accessor preferring the explicit value then the lazy one — non-breaking for the
      existing `sc:` test call sites), and wire it in `Program.cs`.
- [ ] Verify the previously-dormant SC reads now fire live (Kagemusya/FearBreeze/
      EDP/MagicPower/Signum/etc.) without re-introducing the DI cycle at startup.

## Done criteria

- ➡️ from COMBAT-37: a live Ranger with SC_FEARBREEZE fires the extra arrows; a live
  Gunslinger's Eternal Chain procs — and the broader SC combat set (EDP, Magic Power,
  Signum Crucis, Kagemusya) is active in production.
- Map.Server boots with no DI cycle.

## Test plan

- A boot/integration test resolves `IBattleCalculator` and confirms `_sc` is non-null.
- Existing BattleCalculator SC unit tests stay green.
