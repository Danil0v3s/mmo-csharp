# SC-01 — De-shadow the PresenceMarker re-registrations + harden the overwrite guard

> **Epic:** Status parity hardening · **Status:** ✅ Done (2026-06-01) · **Size:** M · **Player-visible:** yes
> **Depends on:** none · **Blocks:** SC-04

## Problem

`StatusEffectRegistry` registers ~90 SCs twice. A real `OnStart` body is registered
early, then a later `Register(type, PresenceMarker(...))` in the wave 5b/5c/5d family
methods re-registers the same SC with a no-op marker. The dictionary is last-write-wins,
so whether the real body survives depends entirely on **ctor call ordering** — not on
intent. Today the real bodies happen to win (see "Verified runtime state" below), but the
table is one re-order away from silently zeroing 90 effects, and the
`RegisterDefaultsForMissingTypes` "upgrade a shadowed NoOp" guard does **not** protect
against it because it only recognizes the shared `_NoOp` delegate by reference — a
`PresenceMarker()` body is a *fresh* lambda and slips through.

This ticket removes the dead/misleading double-registrations and replaces the brittle
ordering dependency with a build-time guard that refuses to overwrite a real `OnStart`
with a presence-only marker.

## Verified runtime state (important — corrects the original premise)

Ctor call order (`StatusEffectRegistry()` ctor):
- line 961 `RegisterWave4aBespokeFormulas()` — its body (lines 3856-3887) calls
  `RegisterWave5b*`/`RegisterWave5c*`/`RegisterWave5d*`/`RegisterWave5f*`, which contain
  the `PresenceMarker(...)` re-registrations (lines 4504-5771).
- line 992 `RegisterDefaultsForMissingTypes()`
- line 1002 `RegisterWave32Val2Val3Formulas()` (method body spans 1027-~3150) — holds the
  REAL bodies for Reflectdamage (1459), ShieldspellHp/Sp (1465/1471), Crescentelbow (1478),
  Banding (2342), Sunstance (2980), Starstance (2916), Inspiration (2590), Kaahi (1224),
  Kaizel (1211), Kaupe (1235), Longing (1051), Magicrod (1039), Poisonreact (1033), etc.
- line 1009 `RegisterWave60FinalAllowlistMigration()`
- line 1017 `RegisterWave61BespokeGeneratorOverrides()`

Because Wave32/60/61 run AFTER Wave4a, the real bodies win. A probe (Val1=5 on a fresh mob)
confirms: `Reflectdamage.Val2=50`, `ShieldspellHp.Val2=5`, `Banding Def+5`, `Sunstance Batk+5`,
`Kaahi.Val2=1000/Val3=25`, `Crescentelbow.Val2=75`. `StatusEffectCompletenessTests` passes
4/4 today. So this is a **latent correctness hazard + dead code**, not an active regression —
but it MUST be fixed because the next wave-method re-order would silently break 90 effects
and the guard would not catch it.

## Current state (C#)

- `Map.Server/Status/StatusEffectRegistry.cs:5781` — `PresenceMarker(ScfFlag flags)` returns a
  `StatusEffectHandler` whose `OnStart` is a **fresh** `(_, _, _) => { }` lambda (NOT the shared
  `_NoOp`).
- `Map.Server/Status/StatusEffectRegistry.cs:6435-6436` — shared `_NoOp` / `_NoOpEnd` delegates.
- `Map.Server/Status/StatusEffectRegistry.cs:6238-6324` — `RegisterDefaultsForMissingTypes()`.
  The shadow-detection guard at 6264-6272 is `ReferenceEquals(existing.OnStart, _NoOp)`. A
  `PresenceMarker` body fails this test, so a shadowed real body is treated as "explicit body
  wins" (line 6267 `continue`) — correct outcome here only by luck of ordering.
- `Map.Server/Status/StatusEffectRegistry.cs:6449` — `Register(type, handler) => _handlers[type] = handler;`
  (silent last-write-wins; no overwrite policy).
- The 90 double-registered SCs: every `Register(StatusType.X, PresenceMarker(...))` between
  lines 4504-5771 whose `X` also has an earlier real-body `Register(StatusType.X, new StatusEffectHandler(...))`.

## rAthena reference (source of truth)

Not a behavioral port — this is an internal table-integrity fix. The canonical per-SC
behavior already lives in the real bodies (e.g. `rathena/src/map/status.cpp` init arm
`SC_REFLECTDAMAGE` and the `DamageService` reflect read). The PresenceMarker re-registrations
were documentation scaffolding ("cite the consumer") that turned into accidental shadows.

## Scope — every sub-system that must be touched

- [ ] **Make `PresenceMarker` reuse the shared `_NoOp`/`_NoOpEnd` delegates** instead of fresh
      lambdas, so `RegisterDefaultsForMissingTypes`'s `ReferenceEquals(OnStart, _NoOp)` guard and
      any future NoOp detection works uniformly. (`StatusEffectRegistry.cs:5781-5785`.)
- [ ] **Add an overwrite-refusal guard to `Register`** (or a private `RegisterMarker` variant):
      if a type already has a handler whose `OnStart` is NOT `_NoOp` (i.e. a real body), and the
      incoming handler's `OnStart` IS `_NoOp` (presence marker), the call MUST NOT overwrite the
      body — it should at most merge the `ScfFlag` classification onto the existing handler. This
      removes the ordering dependency entirely.
- [ ] **Delete the 24+ CalcFlag-bearing PresenceMarker re-registrations** whose real body lives
      elsewhere (they are pure dead code once the guard lands). Replace each family-method
      grouping comment with a one-line pointer to the real body's registration site instead of a
      `Register(... PresenceMarker(...))` call. Grouped list below.
- [ ] **Keep** the genuinely-presence-only PresenceMarker calls (no CalcFlag, single
      registration — e.g. `Devotion` @5266, `Energycoat` @5227): verified neither has a CalcFlag.
      These are the correct use of `PresenceMarker` and must remain.
- [ ] **Regression test** pinning that real bodies survive regardless of wave ordering (below).
- [ ] Re-run `dotnet test Map.Server.Tests` — completeness suite (4 tests) must stay green.

### The 90 double-registered SCs, grouped by owning family method

Each line: `StatusType` — PresenceMarker line (to delete) → real-body line (keeps).
CalcFlag column = whether `StatusCalcFlagDefaults` maps it (the test-relevant 28).

**RegisterWave5bStarEmperorFamily** (4642): `Sunstance` 4650→2980 (CF), `Starstance` 4651→2916 (CF),
`Lightofsun` 4656, `Lightofmoon` 4657, `Lightofstar` 4658, `Moonstar` 4662, `SunsetSun` 4666,
`StarBurst` 4667.

**RegisterWave5bSoulLinkerFamily** (4592): `Soulcollect` 4600, `Soulreaper` 4606, `Soulunity` 4611,
`Souldivision` 4616, `Soulattack` 4622, `Soulcurse` 4628.

**RegisterWave5bRoyalGuardFamily** (4681): `Reflectdamage` 4688→1459, `Banding` 4692→2342 (CF),
`BandingDefence` 4696, `Earthdrive` 4701, `Inspiration` 4707→2590 (CF), `ShieldspellHp` 4712→1465,
`ShieldspellSp` 4713→1471, `ShieldspellAtk` 4714→2818 (CF), `Hovering` 4719→2578 (CF).

**RegisterWave5bSuraFamily** (4732): `Gensou` 4739, `Crescentelbow` 4743→1478, `FallenAngel` 4747,
`TinderBreaker` 4752 (CF), `TinderBreaker2` 4753 (CF), `LightOfRegene` 4758.

**RegisterWave5cNinjaFamily** (4771): `Utsusemi` 4779→1485, `Bunsinjyutsu` 4784, `Nen` 4810 (CF),
`CursedcircleAtker` 4816, `CursedcircleTarget` 4817.

**RegisterWave5cSorcererSpheresFamily** (4830): `Heater`/`HeaterOption` 4837/4838 (Option=CF),
`Tropic`/`TropicOption` 4841/4842 (CF), `Aquaplay`/`AquaplayOption` 4845/4846 (CF),
`Cooler`/`CoolerOption` 4849/4850 (CF), `ChillyAir`/`ChillyAirOption` 4853/4854 (CF),
`Blast`/`BlastOption` 4857/4858 (CF), `WildStorm`/`WildStormOption` 4861/4862 (CF),
`Petrology`/`PetrologyOption` 4865/4866 (CF), `CursedSoil`/`CursedSoilOption` 4869/4870 (CF).

**RegisterWave5cGunslingerFamily** (4880): `HeatBarrel` 4917 (CF).

**RegisterWave5dGuillotineCross / ShadowChaser / GeneticMechanic / Warlock / ArchBishopSura /
WandererMinstrel / FourthClass** (4929-5202): `Hallucination` 4935, `Venomimpress` 4940,
`Magicmushroom` 4971, `Deathhurt` 4972, `Oblivioncurse` 4986, `Manhole` 5013, `Bloodylust` 5017 (CF),
`Reproduce` 5021, `Stripaccessory` 5025 (CF), `GraniticArmor` 5042, `MagmaFlow` 5047,
`Pyroclastic` 5052 (CF), `Madogear` 5057 (CF), `HellsPlant` 5061, `VacuumExtreme` 5081,
`VacuumExtremePostdelay` 5085, `TeargasSob` 5105, `Burnt` 5109, `Rushwindmill` 5127 (CF),
`Sevenwind` 5131, `Moonlitserenade` 5149 (CF), `Leradsdew` 5153, `Lightningwalk` 5158,
`WindStep`/`WindStepOption` 5162/5163 (CF), `WindCurtain`/`WindCurtainOption` 5164/5165 (CF),
`MidnightMoon` 5188, `SkyEnchant` 5189, `ShinkirouCall` 5190 (CF), `Windsign` 5194, `Nightmare` 5198,
`EarthCare` 5202.

> NOTE: of these, the 28 marked **(CF)** are the ones that would FAIL
> `Every_CalcFlag_SC_has_a_real_stat_mod_handler` if their PresenceMarker ever won the
> overwrite race (they carry a CalcFlag and are not allowlisted). The other ~62 have no
> CalcFlag and only risk zeroing a `Val2`/`Val3` that a combat consumer reads (covered by SC-04).
> Verify each (CF) row's real-body line before deleting its marker.

## Done criteria

- `PresenceMarker` uses the shared `_NoOp`/`_NoOpEnd` delegates (reference-equal).
- `Register` (or a marker-specific path) refuses to replace a real `OnStart` with a `_NoOp`
  marker; it may OR the marker's `ScfFlag` into the existing handler.
- All 90 dead `Register(StatusType.X, PresenceMarker(...))` calls whose body lives elsewhere are
  removed; the family-method comments remain (or point at the real-body line).
- No `// TODO`, no commented-out `Register` left behind.
- `StatusEffectCompletenessTests` (4) green; the runtime probe values above are unchanged.

## Test plan

- New `StatusEffectShadowGuardTests`:
  - `RealBody_survives_presence_marker_overwrite`: directly `Register(T, realBody)` then
    `Register(T, PresenceMarker(flags))`; assert `Get(T).OnStart` still mutates (Val1=5 probe).
  - `Marker_flags_merge_onto_existing_body`: assert the `ScfFlag` from the marker is OR'd in.
  - Parametrize over the 28 (CF) types: each `Get(T)` must produce a non-no-op probe.
- Re-run full `StatusEffectCompletenessTests` + `MarionetteFormulaTests` (Marionette/Marionette2
  remain the only two legitimate allowlist entries).

## Notes / gotchas

- `Marionette` / `Marionette2` are the ONLY live `_behaviorElsewhereAllowlist` entries; do not
  touch them — their OnStart is intentionally a no-op for Val1=5 (they read packed Val3/Val4).
- Reflectdamage is the clearest player-visible payoff: `DamageService.cs:394-399` reads
  `Reflectdamage.Val2`; the real body at 1459 sets `Val2 = 10*Val1`. If the marker ever won,
  reflect would silently do nothing.
- Do NOT just delete the PresenceMarker calls without adding the guard — the guard is what makes
  the table order-independent and prevents recurrence.

## History

- **2026-06-01** — Done. `PresenceMarker` now returns the shared `_NoOp`/`_NoOpEnd`
  delegates (was fresh lambdas). `Register` gained an overwrite guard: a presence
  marker (`OnStart == _NoOp`) can never replace a real `OnStart` body — it OR-merges
  its `ScfFlag` onto the existing handler instead, making the table order-independent.
  Removed **128** dead duplicate `PresenceMarker` re-registrations (the ticket
  estimated ~90; a programmatic marker-vs-body scan found 128, all with a real body
  elsewhere). Marionette/Marionette2 left untouched per the ticket (allowlisted; real
  Val3/Val4-decode bodies at L2634/2660 win regardless). New
  `StatusEffectShadowGuardTests` (16): pins `PresenceMarker == _NoOp`, real-body
  survival across both registration orders, flag-merge, marker-over-marker, and a
  12-type parametrized check that formerly-shadowed CalcFlag SCs still mutate.
  `StatusEffectCompletenessTests` 4/4 + `MarionetteFormulaTests` green; full
  Map.Server suite **3569/3569**. No follow-ups required (the 62 non-CF Val2/Val3
  consumer reads were already SC-04's scope). Commits: start `51274d7`, finish `<this>`.
