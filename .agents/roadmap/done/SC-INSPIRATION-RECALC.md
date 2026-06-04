# SC-INSPIRATION-RECALC — SC_INSPIRATION buffs survive a stat recalc

> **Epic:** status · **Status:** ✅ Done (2026-06-04) · **Size:** S · **Player-visible:** yes
> **Depends on:** none · **Unlocks:** none

## The deliverable (definition of done, in one sentence)

> While SC_INSPIRATION is active, all of its bonuses — +Val2 Batk/MATK, +Val3 to every
> base stat, +4·Val1% MaxHP — remain applied after any CalcPc recalc (level-up, equip
> change, another SC starting), instead of silently disappearing.

## Player story / why it matters

A player under Inspiration (LG/RG buff) gets a big all-round boost. Today the bonus is
applied once in `OnStart`, but the moment anything triggers a `CalcPc` recalc the
derived fields are rebuilt from base and the Inspiration contribution to **Batk** and
**MATK** is wiped — the player loses a chunk of attack/magic power mid-buff with no
visible cause. (Discovered during SC-MAGNITUDE turn 9, which fixed the same class of
bug for the element-spirit option SCs + Sunstance, and confirmed Watk/Matk are NOT in
the generator's derived-reapply field set.)

## Current state — what exists vs. what's missing (per layer)

| Layer | Exists? | Where / what's missing |
|---|---|---|
| Service logic | partial | `Map.Server/Status/StatusEffectRegistry.cs` `Register(StatusType.Inspiration, …)` (~L2963). OnStart applies Batk+=Val2, MatkMin/Max+=Val2, Str/Agi/Vit/Int/Dex/Luk+=Val3, MaxHp+=4·Val1. OnEnd reverses. **No `OnRecalc` and no `OnRecalcPool`.** |

## rAthena reference (source of truth)

- `rathena/src/map/status.cpp` — `status_calc_batk`/`status_calc_matk` add `SC_INSPIRATION->val2`;
  the base-stat block adds `val3`; `status_calc_maxhp` adds the `4*val1` percent. Start arm
  (`case SC_INSPIRATION`) sets `val2 = 40*val1`, `val3 = 6*val1`.
- The relevant fact: in this C# port, `CalcPc` **rebuilds** `MatkMin/Max` (StatusCalcService:533/540)
  and `Batk` from base every recalc, and `ReapplyDerivedStatMods` re-runs each SC's `OnRecalc`
  afterwards. Bespoke handlers that touch Batk/Watk/Matk must therefore provide `OnRecalc`.

## Scope — every layer this capability needs (build all of it)

- [ ] Add `OnRecalc` to the Inspiration handler that re-applies the **derived** fields CalcPc
      rebuilds: `Batk += Val2`, `MatkMin/Max += Val2` (mirror Impositio's OnRecalc, L890).
- [ ] Verify whether the **primary-stat** (+Val3 Str/Agi/Vit/Int/Dex/Luk) and **MaxHp**
      (+4·Val1 %) contributions survive recalc through the existing COMBAT-10 param-base delta
      / COMBAT-73 `OnRecalcPool` mechanisms. If they do NOT, extend the handler:
      param-stat re-apply (or registration with the param-base tracker) and an `OnRecalcPool`
      MaxHp re-fold (mirror Service4u/Epiclesis). Do not leave a partial fix.
- [ ] Confirm OnEnd still reverses exactly once (no double-subtract after recalcs re-snapshot).

## Done criteria

- A test starts SC_INSPIRATION, runs CalcPc twice, and asserts Batk/MATK and (per the
  investigation) base stats + MaxHp are all still boosted and idempotent — mirroring
  `Combat53BespokeRefoldTests.Bespoke_derived_mod_survives_recalc_and_is_idempotent` and
  the COMBAT-73 MaxHp refold theory.

## Test plan

- `Map.Server.Tests/Status/` — an Inspiration recalc-survival test (CalcPc-integrated, like
  the Combat53 refold theory) covering every field the SC modifies.

## History

- 2026-06-04 — Filed from SC-MAGNITUDE turn 9 (rule 3): the Watk/Matk missing-OnRecalc audit
  fixed the 7 element-spirit options + Sunstance inline, but Inspiration's multi-field mix
  (derived Batk/MATK + primary stats + MaxHp%) needs per-field recalc-persistence analysis
  beyond a one-line OnRecalc, so it's carved out here.
- 2026-06-04 — **Done (SC-MAGNITUDE turn 10).** Investigation found the handler had **three** bugs,
  not just the recalc one: (1) val2 was added to **Batk**, but status.cpp:7141/7224 + status.yml put it
  on **Watk**+Matk; (2) MaxHp was a flat `+4*Val1` but status.cpp:3170 adds `4*Val1` as a **percent**;
  (3) no OnRecalc/OnRecalcPool. Confirmed via StatusCalcService:113-122 that the **primary-stat** bonus
  (+Val3) rides the COMBAT-10 param-base delta and survives recalc on its own — so OnRecalc re-applies
  ONLY Watk+Matk (re-adding the stats would double-count), and OnRecalcPool re-folds the MaxHp %. Fixed
  all three. Tests: `Inspiration_appliesWatkMatkStatsAndMaxHpPercent_notBatk`,
  `Inspiration_watkMatkSurviveRecalc_maxHpViaPool` (SC02), and updated the pinned
  `SC06StanceFormulaTests.Inspiration_AtkMatk_AndAllStat`. Full suite 4592 pass (1 = standing fixture).
