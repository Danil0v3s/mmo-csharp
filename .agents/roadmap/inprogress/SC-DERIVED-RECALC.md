# SC-DERIVED-RECALC — bespoke SC handlers re-apply derived-stat mods after a recalc

> **Epic:** status · **Status:** 🚧 In progress · **Size:** L · **Player-visible:** yes
> **Depends on:** none · **Unlocks:** none

## The deliverable (definition of done, in one sentence)

> Every bespoke SC handler that modifies a **CalcPc-reset** derived field keeps its bonus
> applied after any stat recalc (level-up, equip change, another SC starting), instead of
> the bonus silently vanishing on the first recalc.

## Player story / why it matters

`CalcPc` (`StatusCalcService`) zeroes + rebuilds the derived stats every recalc — Hit, Flee,
Flee2, Cri, Def, Def2, Mdef, Mdef2, Batk, Patk, Smatk, Res, Mres, Hplus, Crate, plus
Watk/Matk and the MaxHp/MaxSp pools. After the rebuild it calls
`StatusChangeService.ReapplyDerivedStatMods`, which invokes each active SC's **`OnRecalc`**.
The generator-synthesized handlers all have an `OnRecalc` (COMBAT-33). But a **bespoke**
`Register(...)` whose `OnStart` mutates one of these fields and which provides **no
`OnRecalc`** loses its contribution the moment anything triggers a recalc — e.g. Truesight's
crit bonus disappears when you change a weapon. Confirmed via
`Combat53BespokeRefoldTests` (Truesight fails the recalc-survival assert).

This is the same bug class fixed for the Watk/Matk handlers in SC-MAGNITUDE turns 8-10; this
ticket sweeps the generator-reapply-set fields (Def/Hit/Flee/Cri/Batk/…) for bespoke handlers.

## Current state — what exists vs. what's missing (per layer)

| Layer | Exists? | Where / what's missing |
|---|---|---|
| Service logic | partial | `Map.Server/Status/StatusEffectRegistry.cs` — the handlers below modify a reset field in OnStart with no OnRecalc. **Batch 1 done** (SC-MAGNITUDE turn 11): `Fear` (Hit/Flee −20%), `Cloaking` (Cri). |

### Remaining handlers (final registration, reset-field, no OnRecalc)

Curse[Batk], Berserk[Batk,Flee+MaxHp pool], DragonicAura[Hit,Patk], GtChange[Batk],
Fling[Def,Def2], Neutralbarrier[Def,Mdef], GoldeneFerse[Flee], StoneWall[Def,Mdef],
PowerfulFaith[Patk], FirmFaith[Res], Bloodylust[Batk,Def,Def2], Eqc[Def2], Flashcombo[Batk],
HeatBarrel[Hit], PowerOfGaia[Def], Rushwindmill[Batk], SolidSkinOption[Def],
TelekinesisIntense[Batk], ToxinOfMandara[Res], WaterBarrier[Batk,Flee], Truesight[Cri,Hit],
Fleet[Batk], Magicpower[Smatk], Steelbody[Def,Mdef], Saturdaynightfever[Flee,Hit],
Soulshadow[Cri], Soulfalcon[Batk,Hit], Soulgolem[Def,Mdef], Soulenergy[Batk],
Twohandquicken[Cri,Hit], Signumcrucis[Def], Stone[Def,Mdef], Freeze[Def,Mdef],
Madnesscancel[Batk], DMachine[Def], AbyssSlayer[Hit], TemporaryCommunion[Hplus],
SunComfort[Def2], MoonComfort[Flee], Gloomyday[Flee], Zangetsu[Batk], Armorchange[Def,Mdef],
Stonehardskin[Def,Mdef].

## rAthena reference (source of truth)

- The C# recalc model, not an rAthena formula: `StatusCalcService.CalcPc` zeroes the derived
  fields (lines ~139-153) + rebuilds Watk/Matk (126-127, 533/540); `ReapplyDerivedStatMods`
  re-runs each SC's `OnRecalc`. Primary stats (Str…Crt) survive via the COMBAT-10 param-base
  delta (lines 113-122) and must NOT be re-applied. AspdRate is not reset. MaxHp/MaxSp use
  `OnRecalcPool`.

## Scope — every layer this capability needs (build all of it)

- [ ] For each handler above, add an `OnRecalc` (and `OnRecalcPool` where it touches MaxHp/MaxSp)
      that re-applies ONLY the **reset** fields its OnStart modified — never the primary stats,
      AspdRate, or anything CalcPc preserves (double-count risk).
- [ ] Percent-of-base and primary-stat-coupled SCs (e.g. `Magicpower` Smatk %, `Truesight`
      whose +5 Luk feeds Cri on rebuild, `Berserk` ×3 MaxHp + half-Flee snapshots) need
      per-field care: recompute the % on the rebuilt base / snapshot the delta. The strict
      `Combat53BespokeRefoldTests` idempotency assert fits the flat ones; use a unit-level
      OnStart→reset→OnRecalc test (like `SC02CalcFlagAllTests`) for the percent/coupled ones.

## Done criteria

- No bespoke handler modifies a CalcPc-reset field without re-applying it on recalc.
- Each fixed SC has a recalc-survival test (Combat53 theory for flat fields, or a unit test).

## Test plan

- Extend `Combat53BespokeRefoldTests` InlineData for the flat reset-field SCs; add
  `SC02CalcFlagAllTests`-style unit tests for the percent/primary-coupled ones.

## History

- 2026-06-04 — Filed + batch 1 (Fear, Cloaking) from SC-MAGNITUDE turn 11. Root cause: `Register`
  fully replaces a handler (no OnRecalc merge), and the generator only attaches OnRecalc to handlers
  it *synthesizes* — so a bespoke handler on a reset field with no OnRecalc loses its bonus on recalc.
  Audited via a final-registration scan of every handler touching a reset field. 43 remain.
