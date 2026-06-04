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
| Service logic | partial | `Map.Server/Status/StatusEffectRegistry.cs` — the handlers below modify a reset field in OnStart with no OnRecalc. **Batch 1** (turn 11): Fear, Cloaking. **Batch 2** (turn 12): Zangetsu, Madnesscancel, Signumcrucis, GoldeneFerse, Flashcombo, PowerfulFaith, Soulshadow, HeatBarrel, Eqc, PowerOfGaia, SolidSkinOption. **Batch 3** (turn 13): Rushwindmill, TelekinesisIntense, Soulenergy, ToxinOfMandara, MoonComfort, Gloomyday, Soulfalcon, Stone, Freeze, Steelbody, Soulgolem, StoneWall, Armorchange, Stonehardskin, Curse, FirmFaith. |

### Remaining handlers (final registration, reset-field, no OnRecalc) — ~11 flat + 5 careful

Flat reset-field (clean, do next): DragonicAura[Hit,Patk], Fling[Def,Def2], Neutralbarrier[Def,Mdef %],
Bloodylust[Batk,Def,Def2], WaterBarrier[Batk,Flee], Saturdaynightfever[Flee,Hit],
Twohandquicken[Cri,Hit], DMachine[Def,Res], AbyssSlayer[Patk,Smatk,Hit],
TemporaryCommunion[Patk,Smatk,Hplus], SunComfort[Def2].

Need per-field care (percent / primary-stat coupling / MaxHp pool): GtChange[Batk %],
Fleet[Batk %], Magicpower[Smatk %], Truesight[Cri,Hit — +5 Luk feeds Cri], Berserk[Batk + Flee/2
+ ×3 MaxHp pool].

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
- 2026-06-04 (turn 12) — **Batch 2**: gave OnRecalc to 11 flat reset-field handlers — Zangetsu/
  Madnesscancel/Flashcombo (Batk), Signumcrucis (Def −), GoldeneFerse (Flee), PowerfulFaith (Patk),
  Soulshadow (Cri), HeatBarrel (Hit), Eqc (Def2), PowerOfGaia/SolidSkinOption (Def). The last three
  already had an `OnRecalcPool` for MaxHp but the derived re-fold (the noted-but-never-wired "COMBAT-111
  axis") was missing — now wired. Each only re-applies the *reset* field (AspdRate/MaxHp left to their
  own passes). Also extended the `Combat53BespokeRefoldTests.Read()` helper to cover Patk/Smatk/Res/Mres/
  Hplus/Crate/Flee2/Mdef2 so these fields are testable. All 11 verified in the Combat53 theory; full suite
  4605 pass (1 = standing replay-fixture). ~32 remain.
- 2026-06-04 (turn 13) — **Batch 3**: 16 more flat reset-field handlers — Rushwindmill/TelekinesisIntense/
  Soulenergy (Batk), ToxinOfMandara/FirmFaith (Res), MoonComfort/Gloomyday (Flee), Soulfalcon (Batk+Hit),
  Stone/Freeze/Steelbody/Soulgolem/StoneWall/Armorchange/Stonehardskin (Def/Mdef pairs), Curse (Batk−Batk/4).
  Done via a scripted per-handler OnRecalc injection (located each by name, mirrored its OnStart deltas) +
  a manual FirmFaith edit (it already had an OnRecalcPool). 14 verified in the Combat53 theory; the two
  subtract-debuffs whose PC base is 0 (ToxinOfMandara Res−, Curse Batk−) got mob-based unit tests in SC02
  instead. Full suite 4639 pass (1 = standing replay-fixture). ~11 flat + 5 careful remain.
