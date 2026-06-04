# SC-MAGNITUDE — SC magnitudes correct (CalcFlags mis-map + generator-default review)

> **Epic:** status · **Status:** 🚧 In progress · **Size:** L · **Player-visible:** yes
> **Depends on:** none · **Unlocks:** SC-FAMILIES

## The deliverable

> Status changes apply their rAthena-exact magnitudes — no phantom "+Val1 to all six stats"
> mis-mappings, no linear-wrong generator defaults, complete element-endow SCs.

## What this absorbs (archive)

- `_archive/todo/status/SC-10` — triage remaining `CalcFlags: All` all-six-stat mis-mappings (~35 SCs).
- `_archive/todo/status/SC-11` — complete element-endow SCs (Aspersio/Shadow/Ghost/Enchantarms + magic).
- `_archive/todo/status/SC-18` — convert linear-wrong-magnitude generator-default SCs (a+b·Val1).
- `_archive/todo/status/SC-19` — bespoke/not-a-stat generator-default SCs (Jointbeat bitmask, tick drains, SC chains).
- `_archive/todo/status/SC-20` — bulk-triage the remaining generator-default SCs.

## rAthena reference

- `rathena/src/map/status.cpp` — `status_calc_*` per-SC arms (the real Val2/Val3 magnitudes);
  the `SCB_*` calc-flag mapping. The archived SC-07 built the `GeneratedStatModDefaultTypes`
  worklist enumeration + the `GeneratorDefaultAuditTests` guard.

## Scope

- [x] `CalcFlags: All` → 6-base-stat mis-mappings — already handled (SC-02/SC-10, guarded by
      `SC02CalcFlagAllTests`); verified at HEAD (weapon-endow + MATK%/resist/random-ring apply real effect).
- [x] **Worklist accounting fix (turn 1)**: the generator-default worklist *over-reported*. The generator
      (`RegisterDefaultsForMissingTypes`, ctor line 1061) lists an SC; the post-generator override waves
      (Wave 32/60/61, lines 1071-1086) replace its handler with the real magnitude **but never removed the
      worklist entry** — so ~91 already-converted SCs (Fortune/Whistle/Humming/Dontforgetme/Truesight/…)
      were still listed as "default". Added `_synthesizedHandlers` capture + `PruneOverriddenGeneratedTypes()`
      (end of ctor): the worklist drops any SC whose handler an override replaced. **236 → 145** = the
      genuinely-still-default review remainder.
- [~] Convert the remaining generator-default SCs to their real magnitudes. The clean well-known-formula
      SCs are already done; the **145 genuine remainder** is the hard/bespoke tail (Gospel/Basilica/Jointbeat/
      Dancing, Taekwon stances Ske/Swoo/Ska/Fusion, the SC_INC* script family, merc bonuses, …).
      **→ future turns** (per-SC rAthena-magnitude conversion).

## Done criteria

- Each converted SC applies the rAthena magnitude; the `GeneratorDefaultAuditTests` worklist shrinks to
  the genuinely-default set (turn 1: 236 → 145, now accurate); no SC silently buffs all six stats that
  shouldn't (✅ SC-02). **The 145 genuine remainder still needs per-SC conversion across future turns.**

## Test plan

- Extend the archived SC-10/11/18/19/20 per-SC formula tests; the completeness/audit guards stay green.

## Progress log (multi-turn)

- **2026-06-04 (turn 1)** — Fixed the worklist accounting: the generator-default worklist was
  over-reporting by ~91 SCs (the post-generator Wave 32/60/61 overrides fixed behaviour but left the
  worklist entry). New `_synthesizedHandlers` capture + `PruneOverriddenGeneratedTypes()` make
  `GeneratedStatModDefaultTypes` reflect only the SCs still served by the generic `+Val1` synthesis
  (236 → 145). Confirmed the simple/well-known-formula SCs (Fortune/Whistle/Humming/Dontforgetme/Truesight/
  Fleet/Spearquicken/…) are already converted; the 145 remainder is the hard/bespoke tail. 6 prune-guard
  tests (`SC07GeneratorAuditTests.OverriddenSc_IsPrunedFromGeneratorDefaultWorklist`); full suite 4561 pass
  (1 = standing replay-fixture). The loop resumes this card to convert the 145 remainder per-SC.
- **2026-06-04 (turn 2)** — Per-SC conversion + an audit of the 145 remainder. Converted **SC_GUARD_STANCE**
  (IG_GUARD_STANCE): Val2 = 50+50·Val1 (DEF↑), Val3 = 50·Val1 (Watk↓) — a real-stat handler replacing the
  generator's wrong +Val1-to-both default (status.cpp:12445; status.yml CalcFlags Watk+Def). Worklist
  145 → 144. 2 tests (`SC02CalcFlagAllTests.GuardStance_*`); full suite 4563 pass (1 = standing replay-fixture).
  **Audit of the remaining 144:** only ~29 have a `status_change_start` val2 arm at all; of those, most are
  (a) tick/state machines (Gospel/Basilica/Run/Dancing/Insignia/Rebound), (b) blocked on a missing C#
  field — there is **no real MoveSpeed% stat** (the codebase uses an AspdRate proxy), so the speed SCs
  (GnCartboost/Catnippowder/Arclousedash/…) can't be faithfully converted, or (c) use 4th-job stats
  (P.ATK/S.MATK) not yet modeled. The rest of the 144 are **correctly** served by the +Val1 default (their
  skill sets Val1 directly). So the genuinely-clean-convertible subset is small and largely exhausted;
  the remaining tail needs deliberate per-SC work + some infrastructure (a MoveSpeed% field) — a long,
  human-paced grind rather than a fast bulk sweep.
- **2026-06-04 (turn 3)** — Converted **SC_HISS** (SU_HISS): flat +50 Perfect Dodge (Flee2), replacing
  the wrong +Val1 default (status.cpp:12301; status.yml CalcFlag Flee2). Worklist 144 → 143. 2 tests
  (`SC02CalcFlagAllTests.Hiss_*`); full suite 4565 pass (1 = standing replay-fixture). **Clean-convertible
  subset now exhausted:** Hiss was the last simple single-real-stat SC in the val2-arm subset. The
  movement-speed cluster (GnCartboost/Catnippowder/Arclousedash/Walkspeed/DoramWalkspeed) is blocked on a
  missing MoveSpeed% field ➡️ filed **SC-MOVESPEED-FIELD**; the rest of the 143 are correctly-default,
  tick/state machines, or use unmodeled 4th-job trait stats (P.ATK/S.MATK/Spl). **Honest status:** the
  structural work + every clean conversion are done; closing the worklist further requires the MoveSpeed%
  infra + 4th-job-stat modeling (separate tickets) — SC-MAGNITUDE has reached the limit of what an
  autonomous sweep can faithfully convert.
- **2026-06-04 (turn 4)** — Corrected the turn-3 MoveSpeed premise + fixed SC_GN_CARTBOOST. The movement
  speed system is **not** missing — `StatusCalcService.ComputeScSpeed` (status_calc_speed) already
  implements the full SC speed_rate accumulator and CalcPc overwrites `BattleStats.Speed` with it; it
  reads each speed SC's real magnitude directly (GnCartboost.Val2@718, Catnippowder.Val3, Arclousedash.Val3,
  DoramWalkspeed.Val1@723, Walkspeed.Val1@739). So the `Speed`-CalcFlag generator stat-mod is redundant
  (immediately overwritten), not blocked-on-infra → **deleted the false-premise SC-MOVESPEED-FIELD ticket.**
  Fixed **SC_GN_CARTBOOST**: its OnStart now sets Val2 = 50/75/100 by level so ComputeScSpeed has a value
  (the +Val1 generator default never set Val2, so the speed-up read 0 — the bonus was broken). Worklist
  143 → 142. (DoramWalkspeed/Walkspeed stay generator-default: their +Val1-to-Speed is harmlessly
  overwritten, and the Register no-downgrade-to-NoOp guard blocks converting them to presence-only.)
  3 tests + a completeness-allowlist entry; full suite 4567 pass (1 = standing replay-fixture).
- **2026-06-04 (turn 5)** — Converted the **mercenary stat-bonus cluster** (the clean Val2-formula SCs):
  **SC_MERC_ATKUP** (Val2 = 15·Val1 → Watk, status.cpp:7119 — the generator had wrongly mapped it to
  **Batk**), **SC_MERC_FLEEUP** (Val2 = 15·Val1 → Flee, :7418), and **SC_MERC_HPUP** (Val2 = 5·Val1
  MaxHP-%, heals full HP on apply — :3160/:12910, percent-pool pattern mirroring Service4u/Epiclesis).
  Worklist **142 → 139**. (SC_MERC_HITUP and SC_MERC_SPUP were already converted by COMBAT-73/89, so they
  were never on the worklist — verified, not re-done.) Also fixed an **incomplete completeness probe**:
  `StatusEffectCompletenessTests.SnapshotStats` omitted `WatkMin/WatkMax`, so any Watk-only handler read
  as a silent no-op — added the two fields (a Watk-only stat-mod is now detected). 4 tests
  (`MercAtkup/MercFleeup/MercHpup_*` + the 5-way `MercCluster_isConverted` theory); full suite 4575 pass
  (1 = standing replay-fixture).
- **2026-06-04 (turn 6)** — Converted **SC_DORAM_MATK** (SU_SPIRITOFLAND): `matk += Val1` (status.cpp:7215;
  Val1 carries the caster base_level per skill.cpp:13020/14749). The generator had it mapped to **Batk**
  (wrong stat) — converted to a flat MATK add on MatkMin/MatkMax, mirroring the Incmatkrate pattern.
  Worklist **139 → 138**. (Audited the rest of the Doram/summoner-song cluster: SC_DORAM_FLEE2 `flee2+=Val1`
  and the regular DoramFlee2/Flee2 mapping are **correctly** generator-served; SC_CHATTERING is already
  off-worklist with a deliberate Batk mapping pinned by COMBAT-53; the song SCs Moonlitserenade/Echosong/
  Symphony use job-level-dependent Val3 formulas — left for a deliberate pass.) Also extended the
  completeness probe again: `SnapshotStats` now includes `MatkMin/MatkMax` (a Matk-only handler was
  otherwise read as a silent no-op, same class of gap as the turn-5 Watk fix). 2 tests
  (`DoramMatk_addsFlatMatk_byVal1_notBatk` + `_isConverted`); full suite 4577 pass (1 = standing replay-fixture).
- **2026-06-04 (turn 7)** — Fixed two **MATK SCs that were mis-applied to Batk** (physical base ATK), so
  they did nothing for magic damage and the wrong magnitude for physical: **SC_IZAYOI** (`matk += 25*Val1`,
  status.cpp:7237 — was `+Val1 Batk`) and **SC_SOULFAIRY** (`matk += Val2`, Val2 = 10*Val1, status.cpp:7223
  — was `+Val1 Batk`). Both now target MatkMin/MatkMax with the rAthena magnitude. These were already
  *off* the generator-default worklist (they had bespoke-but-wrong handlers), so the **count stays 138** —
  but two real magic-vs-physical bugs are gone. Method for finding them: cross-referenced every SC in
  rAthena `status_calc_matk` against the C# `StatusCalcFlagDefaults`/handler mapping; the matk→Batk
  mis-maps are genuine bugs (a Batk bonus is invisible to magic), whereas watk→Batk maps are functionally
  equivalent for flat post-calc bonuses (total ATK unchanged) and were left alone. Remaining matk→Batk
  candidates (ClimaxDesHu, Moonlitserenade, Zangetsu, FireInsignia) carry elemental/job-level/dual-stat
  complications — deferred to a deliberate pass. 2 tests (`Izayoi_addsMatk_by25xVal1_notBatk`,
  `Soulfairy_addsMatk_byVal2_10xVal1_notBatk`); full suite 4579 pass (1 = standing replay-fixture).
- **2026-06-04 (turn 8)** — Fixed **SC_SHIELDSPELL_ATK** (LG_SHIELDSPELL lv3): Val2 = 150 flat added to
  **both Watk and Matk** (status.cpp:7139/:7227; status.yml CalcFlags Watk+Matk) — the prior handler added
  Val1 (=skill level) to **Batk** only. **Plus a systemic fix:** while verifying the recalc path I found
  that CalcPc *rebuilds* MatkMin/Max from base stats every recalc (StatusCalcService:533/540), so any
  Matk handler **without an OnRecalc re-apply silently loses its bonus** after the first CalcPc. That
  affected the turn-6/7 conversions (DoramMatk/Izayoi/Soulfairy) **and** the pre-existing **Incmatkrate** —
  all four were missing OnRecalc. Added OnRecalc to all of them (Incmatkrate recomputes its % on the
  rebuilt base; the flat ones re-add their delta). ShieldspellAtk got both Watk+Matk OnRecalc. Worklist
  unchanged (**138** — all off-worklist), but five magic/atk buffs now actually persist. Adjusted the
  Combat53 refold theory (its Batk-only `Read()` infra can't observe Watk/Matk) and added recalc-survival
  coverage in SC02. 3 tests (`ShieldspellAtk_addsFlat150_toWatkAndMatk_notBatk` + recalc assert,
  `FlatMatk_survivesRecalc_viaOnRecalc` theory); full suite 4582 pass (1 = standing replay-fixture).
- **2026-06-04 (turn 9)** — Generalised the turn-8 finding into a **full audit**: scripted a scan of every
  handler touching WatkMin/Max or MatkMin/Max and flagged those without an `OnRecalc` (Watk/Matk are NOT in
  the generator's derived-reapply field set, yet CalcPc rebuilds them, so a bespoke handler silently loses
  its bonus on recalc). Found 9 genuine cases; fixed **8** inline by adding the matching OnRecalc:
  the seven **element-spirit option** SCs — Aquaplay/Blast/Chilly/Cooler (MATK += Val2) and
  Heater/Pyrotechnic/Tropic (Watk += Val2) — plus **Sunstance** (Batk+Watk percent, re-applied on the
  rebuilt base). The 9th, **SC_INSPIRATION**, mixes derived (Batk/MATK) with primary-stat and MaxHp%
  contributions that need per-field recalc-persistence analysis ➡️ filed **SC-INSPIRATION-RECALC** (rule 3).
  Worklist unchanged (**138** — all off-worklist), but eight more Watk/Matk buffs now persist across recalc.
  8 tests (`MatkOption_/WatkOption_survivesRecalc_viaOnRecalc` theories + `Sunstance_watkPercent_…`); full
  suite 4590 pass (1 = standing replay-fixture).
- **2026-06-04 (turn 10)** — Closed out the **SC-INSPIRATION-RECALC** follow-up (→ done/). Investigating it
  revealed Inspiration had **three** bugs: val2 went to **Batk** (status.cpp:7141/7224 + status.yml say
  **Watk**+Matk); MaxHp was a **flat** `+4*Val1` (status.cpp:3170 adds it as a **percent**); and it had no
  recalc re-application. Fixed all three (OnRecalc re-applies Watk+Matk; OnRecalcPool re-folds the MaxHp %).
  Also nailed down the recalc model definitively: StatusCalcService:113-122 shows **primary stats survive via
  the COMBAT-10 param-base delta** (the recalc shifts by the param-base change, preserving the SC delta on
  s[i]) — so primary-stat handlers never need OnRecalc, and only the *reset* fields (Watk/Matk/Def/derived/
  pools) do. **This means the Watk/Matk missing-OnRecalc audit (turns 8-10) is now complete.** 3 tests;
  full suite 4592 pass (1 = standing replay-fixture).

## Notes

- Element-endow SCs set the weapon element the combat resolver reads — not an all-stat buff
  (archive SC-02). Deferred (after gameplay).
