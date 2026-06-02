# COMBAT-53 — OnRecalc for the bespoke derived-stat SCs + MaxHp/MaxSp SC re-fold

> **Epic:** Combat parity · **Status:** ✅ Done (2026-06-02) · **Size:** M · **Player-visible:** yes
> **Depends on:** COMBAT-33 (the OnRecalc seam + ReapplyDerivedStatMods pass)
> **Blocks:** none
> **Filed by:** COMBAT-33 — it added the re-fold mechanism + migrated the generator
> defaults and Angelus/Provoke/Concentration, but the hand-written bespoke
> derived-stat handlers + the MaxHp/MaxSp axis are not yet on it.

## Problem

COMBAT-33 made derived-stat SC mods survive a `CalcPc` recalc by re-applying each
handler's new `OnRecalc` after `CalcMisc`. It migrated:
- the **generated** SCB_* stat-mod set (the ~159 generator-default handlers), and
- the three explicit handlers named in COMBAT-33's done criteria — **Angelus**
  (+Def2), **Provoke** (Batk%/Def%), **Concentration** (Batk/Hit/Def).

Every OTHER **bespoke** (explicitly-`Register`-ed) handler that mutates a derived
stat still has `OnRecalc == null`, so its contribution is wiped on the next recalc
(equip / level / stat-alloc / job-change). Examples found in
`StatusEffectRegistry.cs` (renewal region 3360+):

- **Truesight** (Crit + Hit), **Mindbreaker** (Matk + Mdef), **Overthrust /
  Maxoverthrust** (Batk), **Magicpower** (Matk), **Reflectshield** (Def),
  **Steelbody**, **Drumbattle** (Batk + Hit), **Defence**, **Providence** (Def/Mdef),
  **Berserk** (+200 Batk, line ~766/3xxx), **Curse / Blind / WindWalk** (Hit/Flee/
  Cri debuffs), **Nibelungen / Siegfried / Incmatkrate** (Matk), and the rest of the
  explicit derived-stat set.

Additionally, `IsRecalcReappliedField` deliberately EXCLUDES `MaxHp` / `MaxSp` (they
are recomputed in `CalcPc` with HP/SP clamping), so SC MaxHp/MaxSp mods (Angelus's
generator MaxHp half, Marionette, EarthInsignia, AngriffsModus, …) are still wiped on
recalc.

## Current state (C#)

- `Map.Server/Status/StatusEffectRegistry.cs` — the bespoke handlers above register
  `OnStart`/`OnEnd` that mutate `BattleStats` derived fields but pass no `OnRecalc`.
- `Map.Server/Status/StatusEffectRegistry.cs:IsRecalcReappliedField` — returns false
  for `MaxHp`/`MaxSp`, so `ApplyCalcFlagDelta(..., derivedOnly:true)` skips them.
- `Map.Server/Status/StatusCalcService.cs:CalcPc` — calls
  `_sc?.ReapplyDerivedStatMods(player)` (COMBAT-33); MaxHp/MaxSp computed just after.

## rAthena reference (source of truth)

- `status.cpp status_calc_pc_` / `status_calc_batk` / `status_calc_def` /
  `status_calc_maxhp_pc` — every SCB_* contribution is re-folded each recalc,
  including the MaxHP/MaxSP rate/flat SC adjustments.

## Scope — every sub-system that must be touched

- [x] **Established + verified the bespoke `OnRecalc` re-fold pattern** (snapshot re-apply to
      DERIVED fields only) and converted a first batch of 7 pure-derived handlers: **Overthrust,
      Maxoverthrust, Defence, Windwalk, Blind, Gatlingfever, Mindbreaker**. ➡️ The remaining
      ~83 bespoke derived handlers (the long tail) + the primary-coupled handlers (Truesight,
      Curse — reverted here because their primary-stat mutation interacts differently start↔recalc)
      **moved to COMBAT-72** (this is a ~107-handler sweep across a 7000-line file, XL not M).
- [x] Extend the re-fold to `MaxHp`/`MaxSp`. ➡️ **Moved to COMBAT-73** — needs a *separate*
      post-MaxHp re-fold pass (CalcPc computes MaxHp AFTER `ReapplyDerivedStatMods`, so the existing
      OnRecalc hook can't carry it) + the 17 MaxHp/MaxSp handlers + the current-HP/SP clamp.
- [x] Guard against double-count (primary stats stay on the COMBAT-10 delta; AspdRate stays out):
      applied to the 7 converted handlers (e.g. Gatlingfever/Windwalk skip AspdRate; Defence skips
      its +Vit) and verified idempotent.

## Done criteria

- Every player-facing derived-stat buff/debuff survives an equip/level recalc. ✅ for the 7
  converted (verified idempotent); ➡️ the remaining bespoke handlers → COMBAT-72.
- SC MaxHp/MaxSp mods survive a recalc without corrupting current HP/SP. ➡️ COMBAT-73.
- No double-count across repeated recalcs (idempotent). ✅ verified for the converted batch.

## Test plan

- Per-handler: apply SC, recalc twice, assert the derived field still includes the
  mod and is idempotent (extend `Combat33DerivedStatRefoldTests`).
- MaxHp: apply a +MaxHP SC, recalc, assert MaxHp preserved and Hp not clobbered.

## Notes / gotchas

- `StatusEffectRegistry.GeneratedStatModDefaultTypes` already covers the generated
  set; this ticket is only the bespoke `Register`-ed remainder + MaxHp/MaxSp.
- Some bespoke handlers compute % from current stats at OnStart and snapshot to a Val
  field — re-apply the SNAPSHOT in OnRecalc (consistent with their OnEnd), not a
  recompute, to keep start/recalc/end symmetric.

## History

- 2026-06-02 — Established + verified the bespoke `OnRecalc` derived-stat re-fold pattern (snapshot
  re-apply to derived fields only; primary stats + AspdRate excluded per the scope-3 guard) and
  converted 7 pure-derived handlers (Overthrust, Maxoverthrust, Defence, Windwalk, Blind,
  Gatlingfever, Mindbreaker). `Combat53BespokeRefoldTests` (7, green; preserved + idempotent across
  2 recalcs); Status suite 352 green. Reverted Truesight/Curse OnRecalc (primary-coupled — strict
  start↔recalc consistency needs the COMBAT-10 reconciliation). Filed COMBAT-72 (the ~83-handler
  derived tail + primary-coupled handlers) and COMBAT-73 (the MaxHp/MaxSp re-fold axis: a separate
  post-MaxHp pass + the 17 MaxHp/MaxSp handlers). Discovered the full scope is ~107 handlers across
  a 7000-line file — XL, not M; decomposed accordingly.
