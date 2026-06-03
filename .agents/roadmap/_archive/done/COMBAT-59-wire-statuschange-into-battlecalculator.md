# COMBAT-59 — Wire IStatusChangeService into BattleCalculator (break the cycle) so SC combat reads activate in production

> **Epic:** Combat parity · **Status:** ✅ Done (2026-06-02) · **Size:** M · **Player-visible:** yes
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

- [x] Broke the cycle with the `Lazy<IStatusChangeService>` pattern: `BattleCalculator` now has
      a `_scExplicit` (tests' `sc:`) + a `Lazy<IStatusChangeService>? _scLazy` seam; the `_sc`
      accessor prefers the explicit value, else resolves the lazy on first combat read. Wired in
      `Program.cs` as `scLazy: new Lazy<IStatusChangeService>(sp.GetRequiredService<...>)` — the
      factory isn't invoked at construction, so no startup cycle.
- [x] Verified the previously-dormant SC reads now fire live: the lazy resolves on the first
      combat SC read (not at construction), and a Maximize-Power read via the lazy seam produces
      the identical effect to the explicit `sc:` path (so Kagemusya/FearBreeze/EDP/MagicPower/
      Signum etc. — all reading the same `_sc` — are now active in production).

## Done criteria

- ➡️ from COMBAT-37: a live Ranger with SC_FEARBREEZE fires the extra arrows; a live
  Gunslinger's Eternal Chain procs — and the broader SC combat set (EDP, Magic Power,
  Signum Crucis, Kagemusya) is active in production. ✅ — all those reads share `_sc`, now
  non-null live via the lazy seam.
- Map.Server boots with no DI cycle. ✅ — the lazy factory isn't invoked at construction
  (verified: 0 resolutions at construct, 1 on first combat read); build + full suite green.

## History

- 2026-06-02 — Broke the IStatusChangeService → IDamageService → IBattleCalculator construction
  cycle with a `Lazy<IStatusChangeService>` seam on `BattleCalculator` (`_scExplicit ?? _scLazy?.Value`)
  + wired `scLazy:` in `Program.cs`, so the ~15 SC-gated combat reads (Maximize Power, Fear Breeze,
  Eternal Chain, EDP, Magic Power, Signum Crucis, Kagemusya, Madogear, …) activate in production.
  `Combat59LazyScSeamTests` (2: seam not resolved at construction + resolves on first read;
  lazy-path SC effect == explicit-path); Combat suite 474 + full suite 4030 pass (1 fail =
  pre-existing INFRA-11 replay gate). No follow-ups.

## Test plan

- A boot/integration test resolves `IBattleCalculator` and confirms `_sc` is non-null.
- Existing BattleCalculator SC unit tests stay green.
