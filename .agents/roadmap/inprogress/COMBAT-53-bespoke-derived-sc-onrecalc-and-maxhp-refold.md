# COMBAT-53 — OnRecalc for the bespoke derived-stat SCs + MaxHp/MaxSp SC re-fold

> **Epic:** Combat parity · **Status:** 🚧 In progress · **Size:** M · **Player-visible:** yes
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

- [ ] Add an `OnRecalc` to every bespoke derived-stat handler that re-applies its
      snapshot/flat contribution (mirror the Angelus/Provoke/Concentration pattern
      from COMBAT-33). Sweep the explicit `Register` calls; for each that writes a
      derived field, add the matching `OnRecalc`.
- [ ] Extend the re-fold to `MaxHp`/`MaxSp`: re-apply SC MaxHp/MaxSp mods after the
      `CalcPc` MaxHp/MaxSp block, preserving the current-HP/SP clamp semantics, and
      add `MaxHp`/`MaxSp` to the re-applied set (or a dedicated MaxHp re-fold hook).
- [ ] Guard against double-count exactly as COMBAT-33 did (primary stats stay on the
      COMBAT-10 delta; AspdRate stays out — it is not reset by CalcPc).

## Done criteria

- Every player-facing derived-stat buff/debuff (Truesight, Overthrust, Magicpower,
  Reflectshield, Drumbattle, Berserk, …) survives an equip/level recalc.
- SC MaxHp/MaxSp mods survive a recalc without corrupting current HP/SP.
- No double-count across repeated recalcs (idempotent).

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
