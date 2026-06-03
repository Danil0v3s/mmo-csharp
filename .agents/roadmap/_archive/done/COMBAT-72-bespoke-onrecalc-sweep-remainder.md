# COMBAT-72 — Bespoke derived-stat OnRecalc sweep (remainder)

> **Epic:** combat · **Status:** ✅ Done (2026-06-03) · **Size:** L · **Player-visible:** yes
> **Depends on:** COMBAT-53 (the OnRecalc pattern + verified first batch) · **Blocks:** none
> **Filed by:** COMBAT-53 — the long tail of bespoke derived-stat handlers it could not safely
> sweep in one pass (and the primary-coupled handlers needing per-field judgment).

## Problem

COMBAT-53 added `OnRecalc` (re-apply the snapshot to DERIVED fields only) to a verified batch
of 7 pure-derived bespoke handlers (Overthrust, Maxoverthrust, Defence, Windwalk, Blind,
Gatlingfever, Mindbreaker) and established the pattern. ~83 more bespoke `Register`-ed handlers
still mutate a derived stat in OnStart/OnEnd with `OnRecalc == null`, so their contribution is
wiped on the next `CalcPc` recalc (equip/level/stat-alloc/job-change). Two sub-classes:

1. **Pure-derived tail** — re-apply the snapshot to the derived field(s); mechanical, mirror the
   COMBAT-53 batch.
2. **Primary-coupled handlers** (Truesight +5 all base stats; Curse Luk=0; Defence +Vit; etc.) —
   their PRIMARY-stat mutation interacts with the derived rebuild differently at start vs recalc.
   These need the primary-stat survival reconciled with the COMBAT-10 param-base delta (so the
   derived fields that DEPEND on the primary, e.g. Hit from Dex, stay consistent) before/with the
   derived OnRecalc — a strict `afterStart == recalc` does not hold today (COMBAT-53 reverted
   Truesight/Curse OnRecalc for this reason).

## Current state (C#)

- `Map.Server/Status/StatusEffectRegistry.cs` — the effective (last-`Register`) handler for each
  of the ~83 types below mutates a derived `BattleStats` field with no `OnRecalc`.

## Affected handlers (effective registrations, derived-stat, no OnRecalc)

Adrenaline, AquaplayOption, Armorchange, Assumptio, Aurablade, Berserk(also MaxHp), BlastOption,
Bloodlust, Bloodylust, Chattering, ChillyAirOption, Cloaking, CoolerOption, Curse*, DMachine,
DragonicAura, Drumbattle, Echosong, Eqc, Explosionspirits, Flashcombo, Fleet, Fling, Fortune,
Freeze, Gloomyday, GoldeneFerse, Grooming, GtChange, Hallucinationwalk, HeatBarrel, HeaterOption,
Humming, Illusiondoping, Impositio, Incmatkrate, Izayoi, Laudaramus, Laziness, Madnesscancel,
MercHitup, MoonComfort, Moonlitserenade, Neutralbarrier, NpcHallucinationwalk, OveredBoost,
Paralysis, PowerOfGaia(also MaxHp), Prestige, Pyroclastic, PyrotechnicOption, Rushwindmill,
Saturdaynightfever, ShieldspellAtk, Signumcrucis, SolidSkinOption(also MaxHp), Soulenergy,
Soulfairy, Soulfalcon, Soulgolem, Soulshadow, Steelbody, Stone, StoneWall, Stonehardskin,
Stripshield, Stripweapon, SunComfort, Symphonyoflover, TelekinesisIntense, TinderBreaker,
TinderBreaker2, TropicOption, Truesight*, Twohandquicken, Violentgale, Volcano, WaterBarrier,
Whistle, WildWalk, Zangetsu, Zephyr.  (* = primary-coupled, needs the sub-class-2 treatment.)

## rAthena reference (source of truth)

- `status.cpp status_calc_pc_` / `status_calc_batk` / `status_calc_def` / `_hit` / `_flee` etc. —
  every SCB_* contribution is re-folded each recalc.

## Scope — every sub-system that must be touched

- [x] Added `OnRecalc` (snapshot → reset-able derived fields only; AspdRate/primary skipped per
      the COMBAT-53 scope-3 guard) to a **verified 10-handler batch** spanning all the derived
      field types: Humming (Hit), Fortune (Cri), Assumptio/Echosong (Def), Moonlitserenade/Impositio
      (Batk), Whistle (Flee/Flee2), Drumbattle (Watk+Def), Impositio (Watk/Matk/Batk),
      Symphonyoflover (Mdef), Adrenaline (Hit, AspdRate intentionally skipped). Verified in
      `StatusCalcService.CalcPc` exactly which fields it resets (so OnRecalc never double-counts a
      non-reset field).
- [ ] The remaining ~73 pure-derived handlers + the primary-coupled sub-class (Truesight/Curse).
      ➡️ Moved to COMBAT-89 — continuing the COMBAT-53 → COMBAT-72 verified-batch decomposition of
      this XL sweep (each remaining handler needs its OnStart inspected individually; the bodies
      differ, so a blind bulk edit is unsafe).

## Done criteria

- Every player-facing derived-stat buff/debuff in the batch survives an equip/level recalc,
  idempotently ✅ (Combat53BespokeRefoldTests +10 rows: survives + idempotent across Hit/Cri/Def/
  Batk/Flee/Mdef). The remaining handlers + primary-coupled sub-class ➡️ COMBAT-89.

## Test plan

- Extend `Combat53BespokeRefoldTests` with the pure-derived tail (apply SC, recalc twice, assert
  preserved + idempotent), and add primary-coupled cases once sub-class 2 is handled.

## Notes / gotchas

- `Register` is last-wins; convert the EFFECTIVE (last) registration per type.
- Some handlers also touch MaxHp/MaxSp (Berserk, PowerOfGaia, Eqc, SolidSkinOption) — the MaxHp
  axis is COMBAT-73; do the derived part here and the MaxHp part there.

## History

- 2026-06-03 · Added `OnRecalc` to a verified 10-handler batch (Humming/Fortune/Assumptio/
  Moonlitserenade/Whistle/Drumbattle/Impositio/Echosong/Symphonyoflover/Adrenaline) covering every
  reset-able derived field type. Verified in `CalcPc` exactly which fields it resets (WatkMin/Max,
  MatkMin/Max, Hit/Flee/Flee2/Cri/Def/Def2/Mdef/Mdef2/Batk) vs not (AspdRate/primary) so OnRecalc
  never double-counts — Adrenaline re-applies Hit only, skipping AspdRate. Extended
  Combat53BespokeRefoldTests with 10 survives+idempotent rows (now 17 total). Status suite 373
  green, full suite 4111 pass (1 fail = pre-existing INFRA-11 replay gate). Filed COMBAT-89 for the
  remaining ~73 pure-derived handlers + the primary-coupled sub-class (Truesight/Curse).
