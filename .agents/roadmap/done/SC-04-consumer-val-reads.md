# SC-04 — Wire the starved combat-consumer Val reads (Crescentelbow / Parrying / Aurablade / Gravitation / Kaahi / Kaupe / Longing / Magicrod / Poisonreact / Soul Reaper / Energycoat)

> **Epic:** Status parity hardening · **Status:** ✅ Done (2026-06-01) · **Size:** L · **Player-visible:** yes
> **Depends on:** SC-01 (de-shadow guarantees the Val* fields are populated) · **Blocks:** none

## Problem

A set of SCs register a real `OnStart` that computes a `Val2`/`Val3` magnitude, but **no
consumer reads it** — so the effect is inert. The inverse also exists: `DamageService` reads
several SC `Val*` that ARE populated (Reflectshield/Reflectdamage/Deathbound/Sacrifice/Devotion/
Autoguard/Kyrie/Defender/Providence/Siegfried) — that plumbing works, but only because the
de-shadowing (SC-01) keeps those `Val*` alive. This ticket closes the read side for the SCs whose
write side exists but whose effect is never applied.

## Verified consumer inventory (`Map.Server/Combat/DamageService.cs`)

Already reading SC Val* (KEEP — confirm SC-01 keeps the writes alive):
- Devotion `Val1` (guard entity id) — 165-168; written by `Skills/Behaviors/Swordman/Sacrifice.cs:25`
  and `Skills/Behaviors/MercenaryNpc/MercenarySacrifice.cs:20` (`val1 = (int)src.Id`).
- Kaizel `Val2` (revive HP%) — 237-241.
- Autoguard `Val1` (block chance) — 275-276.
- Kyrie `Val1`/`Val2` (HP pool / hit count) — 285-294.
- Defender `Val1` — 319-322 (`defPct = 5 + 5*Val1`).
- Providence `Val1` — 345-348 (`resistPct = 5*Val1`).
- Siegfried `Val2` — 358-361 (flat damage reduction). **NOTE:** SC-02 changes Siegfried Val2 to
  *elemental resistance*; coordinate so this read uses the right semantic (status-resist, not flat
  damage cut). Track the resolution in whichever ticket lands second.
- Reflectshield `Val2` — 384-387; Reflectdamage `Val2` — 394-397; Deathbound `Val2` (‰) — 403-406;
  Sacrifice `Val2` (hit count) — 415-420.

Bodies that COMPUTE a Val but have NO reader (the starved set):
- **Crescentelbow** (`StatusEffectRegistry.cs:1478`): `Val2 = 50 + 5*Val1` (reflect %). No combat
  read. rAthena: SR_CRESCENTELBOW reflects a % of received damage back, scaled by job level.
- **Parrying** (1191): blocks N melee hits / grants block chance. No reader.
- **Aurablade** (1176): adds fixed bonus damage per hit. No reader.
- **Gravitation** (1578): movement/ASPD/attack penalty while channeling. No reader.
- **Kaahi** (1224): `Val2 = 1000` (heal amount), `Val3 = 25` — periodic HP heal on melee hit. No
  OnPeriodic / on-hit reader.
- **Kaupe** (1235): dodge-next-attack (Val1 = chance %). No combat dodge read.
- **Magicrod** (1039): `Val2 = Val1*20` (SP gained on magic absorb). No magic-absorb reader.
- **Poisonreact** (1033): `Val2 = Val1/2` (envenom autocast count on being hit). No on-hit reader.
- **Longing** (1051): `Val2 = 500 - 100*Val1` (ASPD penalty while in ensemble). Verify ASPD path
  reads it.
- **Richmankim** (4058 / RegisterWave32): `Val2 = 10 + 10*Val1` (EXP bonus %). Verify EXP service
  reads it.
- **Soul Reaper family** (Soulreaper `Val2 = 10+5*Val1` @1436, Souldivision `Val2 = 10*Val1` @1441,
  Soulattack, Soulcurse, Soulenergy/Soulfairy/Soulfalcon/Soulgolem/Soulshadow): orb-gain chance /
  aftercast / damage markers with no consumer.
- **Energycoat** (5227, presence-only): rAthena reduces physical damage by a % at the cost of SP per
  hit, scaled by remaining SP tier. Currently a bare PresenceMarker with NO damage read.

## Current state (C#)

- `Map.Server/Combat/DamageService.cs:34` — comment notes SC consumers (SteelBody/Kyrie/AutoGuard)
  are optional. The reflect/devotion/kyrie/autoguard chain is implemented; the starved set is not.
- `Map.Server/Status/StatusEffectRegistry.cs` — the starved bodies at the lines above set Val2/Val3
  but nothing downstream reads them.
- `Skills/Behaviors/Swordman/Sacrifice.cs:25` — the canonical example of a skill populating a
  Devotion-style SC Val that combat then reads. Use this as the template for any SC needing
  skill-side Val population.

## rAthena reference (source of truth)

- `rathena/src/map/status.cpp` init arms compute each Val (Crescentelbow, Kaahi, Kaupe, Magicrod,
  Poisonreact, Longing, Richmankim, Soul* — search `case SC_<NAME>:`).
- Consumer sites in rAthena:
  - Kaahi: `status.cpp` SC_KAAHI tick / `battle.cpp` on-hit heal (`val2` HP restored when struck).
  - Kaupe: `battle.cpp` `battle_calc_attack` flee/dodge — `SC_KAUPE` forces a miss at `val2`%.
  - Magicrod: `skill.cpp` magic-absorb path — `SP += val2` when absorbing.
  - Poisonreact: `battle.cpp` on melee-hit — autocast Envenom up to `val2` times.
  - Crescentelbow: `battle.cpp` reflect on melee — back-damage scaled by val2 + caster job level.
  - Energycoat: `battle.cpp battle_calc_damage` — physical damage `* (100 - reduce)/100`, reduce by
    SP tier; charges SP per hit.
  - Richmankim: `pc.cpp` EXP gain — `exp += exp * val2/100`.

## Scope — every sub-system that must be touched

- [x] **Kaahi**: ✅ on-hit heal in `DamageService.ApplyScPostResolve` — restores up to `Val2` HP,
      charges `Val3` SP (no-op if SP insufficient), gated on a living target (no revive).
      rAthena battle.cpp:10544.
- [x] **Kaupe**: ✅ in `DamageService.ApplyScDamageReduction` (checked first) — roll `Val2`% to fully
      block one hit, decrement `Val3` count, end at 0. rAthena battle.cpp:1555.
- [x] **Richmankim**: ✅ `ExpService.GainExp` now applies `+Val2`% to mob-kill EXP (gated on a known
      mob source, like the level penalty). rAthena pc.cpp pc_gainexp.
- [ ] **Energycoat**: ➡️ **Moved to SC-12** (renewal reduction formula not in battle.cpp where the
      audit looked — needs locating).
- [ ] **Crescentelbow**: ➡️ **Moved to SC-12** (reflect + knockback + autospell — more than a Val read).
- [ ] **Magicrod**: ➡️ **Moved to SC-13** (magic-absorb + SP gain — magic-pipeline consumer).
- [ ] **Poisonreact**: ➡️ **Moved to SC-13** (autocast Envenom on melee hit).
- [ ] **Aurablade / Gravitation / Parrying**: ➡️ **Moved to SC-14** (attacker flat-bonus / stat
      penalty / melee-block — not the target-reduction path).
- [ ] **Longing**: ➡️ the ASPD penalty read belongs to **COMBAT-28** (`status_calc_aspd` SC
      contributions); cited there.
- [ ] **Soul Reaper family**: ➡️ **Moved to SC-15** (orb-gain / aftercast / damage-marker reads in
      the Soul Reaper / Soul Linker plugins).

## Done criteria

- ✅ Kaupe dodges (`Val2`% block + count decrement + end), Kaahi heals on hit (charging SP, no
  revive), Richmankim boosts mob-kill EXP — each pinned in `SC04ConsumerValReadTests`.
- ✅ No SC in the starved set is left silently inert: the remaining ones (Energycoat/Crescentelbow
  → SC-12, Magicrod/Poisonreact → SC-13, Aurablade/Gravitation/Parrying → SC-14, Soul family →
  SC-15, Longing → COMBAT-28) are allowlisted with rAthena consumer citations in those tickets.
- ✅ Existing DamageService reads (Reflect*/Devotion/Kyrie/etc.) still pass.

## Test plan

- `DamageServiceScConsumerTests`: per-SC scenarios with a stubbed `IStatusChangeService` returning a
  populated SC; assert the damage / SP / EXP / heal outcome matches rAthena numbers.
- `KaupeTests`: Val1=100 forces a miss and ends the SC; Val1=0 no effect.
- `EnergycoatTests`: damage reduction scales with SP tier; SP decremented per hit.
- Regression: `DamageServiceTests` (existing reflect/kyrie/autoguard cases unchanged).

## Notes / gotchas

- The `StatusEffectHandler.OnStart` signature is `(target, sc, source)` — for SCs whose magnitude
  needs the caster (Crescentelbow job level, Kaahi caster level), pre-compute at apply time and
  store in Val2/Val3, mirroring `Sacrifice.cs`.
- Coordinate the Siegfried Val2 semantic with SC-02 (elemental-resist vs flat-reduction) — do not
  let the two tickets disagree on what `Siegfried.Val2` means.
- Energycoat is presence-only in `StatusCalcFlagDefaults` (no CalcFlag) — that is correct; its
  effect is combat-side, so the fix is in `DamageService`, not the registry stat-mod path.

## History

- 2026-06-01 · Wired the cleanest 3 starved consumers. **Kaupe** dodge (DamageService
  ApplyScDamageReduction, first check: roll Val2% → full block, decrement Val3, end at 0 —
  battle.cpp:1555). **Kaahi** on-hit heal (ApplyScPostResolve: charge Val3 SP, heal up to Val2,
  living-target gate so no revive — battle.cpp:10544). **Richmankim** +Val2% mob-kill EXP
  (ExpService.GainExp, gated on a known mob source). SC04ConsumerValReadTests (7). 3699 green.
  Filed SC-12 (Energycoat/Crescentelbow), SC-13 (Magicrod/Poisonreact), SC-14 (Aurablade/
  Gravitation/Parrying), SC-15 (Soul Reaper family); Longing's ASPD read cited to COMBAT-28.
