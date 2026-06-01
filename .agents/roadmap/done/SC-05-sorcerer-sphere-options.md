# SC-05 — Sorcerer elemental-sphere `*_OPTION` SCs: fixed Eatk/Matk + element change (not +Val1 stat)

> **Epic:** Status parity hardening · **Status:** ✅ Done (2026-06-01) · **Size:** M · **Player-visible:** yes
> **Depends on:** SC-01 (de-shadow), SC-02 (CalcStatField extensions) · **Blocks:** none

## Problem

The Sorcerer elemental-spirit `*_OPTION` buffs (Heater/Tropic/Aquaplay/Cooler/ChillyAir/Blast/
WildStorm/Petrology/CursedSoil/WindStep/WindCurtain) are mis-implemented. rAthena assigns each a
**fixed Val2** (equipment-Atk or Matk amount, or an HP-rate %, or an element id, or a bolt skill
id) and often a **Val3** (element change). The C# port instead gives them the generator default
`+Val1` to a single stat (e.g. `HeaterOption → Batk`, `BlastOption → AspdRate`,
`PetrologyOption → MaxHp`), which is both the wrong field and the wrong magnitude — and the fixed
Val2/Val3 the combat path needs are never set.

Verified at runtime (Val1=5 probe): `HeaterOption` adds +5 Batk (generator default), Val2=0;
`BlastOption` adds +5 AspdRate, Val2=0; `WindStepOption` adds +5 AspdRate +5 Flee, Val2=0. rAthena
wants `HeaterOption Val2=120` (equip-Atk) consumed at `status.cpp:7160 watk += val2`.

## Verified rAthena formulas (status.cpp init arms ~11834-11889)

| SC | Val2 | Val3 | Meaning |
|---|---|---|---|
| `SC_PYROTECHNIC_OPTION` | 60 | — | Equip-Atk (Atk2) |
| `SC_HEATER_OPTION` | 120 | `ELE_FIRE` | Equip-Atk + weapon→fire |
| `SC_TROPIC_OPTION` | 180 | `MG_FIREBOLT` | Equip-Atk + autocast bolt |
| `SC_AQUAPLAY_OPTION` | 40 | — | Matk |
| `SC_COOLER_OPTION` | 80 | `ELE_WATER` | Matk + weapon→water |
| `SC_CHILLY_AIR_OPTION` | 120 | `MG_COLDBOLT` | Matk + autocast bolt |
| `SC_BLAST_OPTION` | 20 | `ELE_WIND` | (Matk) + weapon→wind |
| `SC_WILD_STORM_OPTION` | `MG_LIGHTNINGBOLT` | — | autocast bolt id |
| `SC_PETROLOGY_OPTION` | 5 | 50 | HP-rate % + (def) |
| `SC_CURSED_SOIL_OPTION` | 10 | `ELE_EARTH` | HP-rate % + weapon→earth |
| `SC_WIND_STEP_OPTION` | 50 | — | % speed + flee increase |
| `SC_WIND_CURTAIN_OPTION` | 100 | — | Elemental modifier % |

Consumer: `status.cpp:7160` `watk += sc->getSCE(SC_HEATER_OPTION)->val2;` (equip-Atk path); the
Matk options feed the matk calc; element-change options route through
`status_get_weapon_element` (see SC-02). HP-rate options feed the MaxHp%/bonus path.

## Current state (C#)

- `Map.Server/Status/StatusEffectRegistry.cs:4830-4871` — `RegisterWave5cSorcererSpheresFamily()`
  registers every base sphere (`Heater`, `Tropic`, …) and `*Option` as `PresenceMarker(sorcBuff)`.
- `Map.Server/Status/StatusCalcFlagDefaults.cs` — the `*Option` SCs are mapped to a single stat:
  `HeaterOption`(193)→Batk, `CoolerOption`(111)→Batk, `AquaplayOption`(55)→Batk,
  `ChillyAirOption`(97)→Batk, `BlastOption`(79)→AspdRate, `WildStormOption`(387)→AspdRate,
  `PetrologyOption`(273)→MaxHp, `CursedSoilOption`(116)→MaxHp, `TropicOption`(366)→Batk,
  `WindStepOption`(391)→AspdRate+Flee, `WindCurtainOption`(389)→six base stats.
  The generator synthesizes `+Val1` to these — wrong field and magnitude.
- No combat path reads any `*Option` Val2/Val3 today (`grep watk/matk += Option` → none in
  `Map.Server`).

## rAthena reference (source of truth)

- Init arms: `rathena/src/map/status.cpp` cases `SC_*_OPTION` at ~11834-11889 (table above).
- Equip-Atk consumer: `status.cpp:7160`. Element-change: `status.cpp:8630` `status_get_weapon_element`.

## Scope — every sub-system that must be touched

- [x] **Replace the generator mapping** — ✅ removed all 12 `*_OPTION` from
      `StatusCalcFlagDefaults` and gave each an explicit bespoke `Register` body with the fixed
      `Val2` (idempotent guard). Generator-count test floor lowered (337).
- [x] **Equip-Atk options** (Pyrotechnic 60 / Heater 120 / Tropic 180): ✅ applied flat to
      `WatkMin/Max` in OnStart (revert in OnEnd).
- [x] **Matk options** (Aquaplay 40 / Cooler 80 / ChillyAir 120 / Blast 20): ✅ applied flat to
      `MatkMin/Max`.
- [ ] **Element-change options** (Heater→fire/Cooler→water/Blast→wind/CursedSoil→earth): ➡️ **Moved
      to SC-16** (reuse SC-02's weapon-element precedence).
- [x] **HP-rate options** (Petrology 5% / CursedSoil 10%): ✅ applied as a MaxHp % (Val4 = abs-delta
      scratch, revert in OnEnd) — not flat +Val1.
- [ ] **Bolt-autocast options** (Tropic MG_FIREBOLT / ChillyAir MG_COLDBOLT / WildStorm
      MG_LIGHTNINGBOLT): ➡️ **Moved to SC-16**. The skill id IS stored (WildStorm Val2,
      Tropic/ChillyAir as noted) for the autocast consumer.
- [ ] **WindStep Val2=50** / **WindCurtain Val2=100**: ✅ correct Val2 now stored (presence-only, no
      phantom +Val1); the %-speed/flee + elemental-modifier consumers ➡️ **Moved to SC-16**.
- [x] **Generator override** — ✅ removed from `StatusCalcFlagDefaults` so a re-run won't reintroduce
      the +Val1 mapping.

## Done criteria

- ✅ Each `*_OPTION` sets the exact rAthena fixed Val2 and applies the equip-Atk / MATK / MaxHP%
  effect (Heater +120 watk, Aquaplay +40 matk, Petrology +5% MaxHp, …) — pinned in
  `SorcererOptionFormulaTests`. *(Element-change + bolt-autocast + Wind%/Petrology-def ➡️ SC-16.)*
- ✅ No `*_OPTION` produces a phantom `+Val1` Batk/AspdRate/MaxHp (asserted per option).
- ✅ `StatusEffectCompletenessTests` + generator-count test green.

## Test plan

- `SorcererOptionFormulaTests`: per-option assert Val2/Val3 == rAthena constant; assert the equip-Atk
  / Matk / MaxHp% / element-change outcome.
- `WeaponEndowElementTests` (shared with SC-02): Heater/Cooler/Blast/CursedSoil change weapon
  element with the correct precedence.
- Regression: `StatusEffectCompletenessTests`, `StatusCalcServiceTests`.

## Notes / gotchas

- The base sphere SCs (`Heater`, `Tropic`, …, without `Option`) are markers on the elemental
  summon, not the PC — they stay presence-only (PresenceMarker) and have no CalcFlag; leave them.
- Many `*_OPTION` Val2 values are **fixed constants independent of Val1** — do NOT scale by Val1.
- Port the `MG_FIREBOLT`/`MG_COLDBOLT`/`MG_LIGHTNINGBOLT` and `ELE_*` constants from the existing
  C# skill-id / element enums; do not hardcode raw ints without the named constant.

## History

- 2026-06-01 · Rewrote the 12 Sorcerer *_OPTION bodies from the generator's phantom +Val1 to the
  fixed rAthena Val2: equip-Atk (Pyrotechnic 60 / Heater 120 / Tropic 180 → WatkMin/Max), MATK
  (Aquaplay 40 / Cooler 80 / ChillyAir 120 / Blast 20 → MatkMin/Max), HP-rate (Petrology 5% /
  CursedSoil 10% → MaxHp%); WildStorm/WindStep/WindCurtain presence-only with correct Val2
  (MG_LIGHTNINGBOLT / 50 / 100). Deleted the duplicate PyrotechnicOption stub; removed all 12
  from StatusCalcFlagDefaults (generator floor 345→330). SorcererOptionFormulaTests (16). 3711
  green. Filed SC-16 for the secondary effects (element change, bolt autocast, WindStep/
  WindCurtain %-effects, Petrology DEF).
