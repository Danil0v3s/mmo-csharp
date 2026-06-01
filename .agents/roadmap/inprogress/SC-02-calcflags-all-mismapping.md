# SC-02 — Fix `CalcFlags: All` mis-mapping (element-endow / MATK% / resist / flat-combat)

> **Epic:** Status parity hardening · **Status:** 🚧 In progress · **Size:** L · **Player-visible:** yes
> **Depends on:** none · **Blocks:** SC-07

## Problem

The SC-flag generator collapsed rAthena's `CalcFlags: All: true` (a "recalculate every
derived stat" *trigger*, NOT "buff all six base stats") into a literal
`{ Str, Agi, Vit, IntStat, Dex, Luk }` field list for ~56 SCs in `StatusCalcFlagDefaults`.
`RegisterDefaultsForMissingTypes` then synthesizes an OnStart that adds `Val1` to all six
base stats. For SCs whose real effect is something else entirely, this is wrong:

- **Weapon element endow** (`Fireweapon` / `Waterweapon` / `Windweapon` / `Earthweapon`):
  rAthena applies NO base-stat mod — they override the weapon's attack element. The C# port
  gives them a bogus +Val1 STR/AGI/VIT/INT/DEX/LUK buff.
- **MATK%** (`Incmatkrate`): rAthena maps `SCB_MATK` (`SP_MATK_RATE` += val1), not six base stats.
- **Status/elemental resist** (`Siegfried`, `Nibelungen`): bespoke `Val2`/`Val3` resist values,
  not stats.
- **Flat combat** (`Berserk`): +200 flat Batk and HP×3, not +Val1 to six stats. (Berserk already
  has a real body at `StatusEffectRegistry.cs:724`; confirm it overrides the generator — it does,
  because the generator only fills *missing* types, and the explicit body wins.)

A player under Fire Weapon currently gets a phantom all-stat buff and the *wrong* weapon element
(the combat element resolver never reads the endow SC — see below).

## Current state (C#)

- `Map.Server/Status/StatusCalcFlagDefaults.cs` — the mis-mapped `CalcFlags: All` entries map to
  the six base stats. Confirmed offenders (line refs in that file):
  - Endow: `Earthweapon` (133), `Fireweapon` (150), `Waterweapon` (381), `Windweapon` (393).
  - MATK%: `Incmatkrate` (211) → six base stats (WRONG; should be MATK%).
  - Resist: `Siegfried` (309), `Nibelungen` (265) → six base stats (WRONG; bespoke Val2/Val3).
  - Plus ~50 other `CalcFlags: All` SCs (Basilica 74, Catnippowder 90, Cheerup 96, Climax* 99-103,
    Contents4 110, DeadlyDefeasance 120, FullThrottle 161, Harmonize 190, HeavenAndEarth 194,
    Itemscript 223, Marionette/Marionette2 245-246, Spirit 328, Truesight 367, Vigor 373,
    Universestance 369, etc.). Many of these ARE genuinely all-stat (Marionette, Spirit, Truesight
    add to base stats) — the audit must triage each (see Scope).
- `Map.Server/Status/StatusEffectRegistry.cs:6313-6322` — generator OnStart/OnEnd that applies
  `Val1` to each listed `CalcStatField`.
- `Map.Server/Status/StatusEffectRegistry.cs:724` — `Berserk` real body (overrides generator).
- `Map.Server/Combat/BattleElementService.cs:28-31` — `GetWeaponElement` reads
  `attacker.Stats.WeaponElement` ONLY; it does NOT consult the endow SCs. So even after the stat
  bug is removed, the element override is missing.
- `CalcStatField` enum (`StatusCalcFlagDefaults.cs:406-438`) has NO `Matk`, no element, no
  dedicated resist field — so MATK% / endow / resist cannot be expressed by the generator today.

## rAthena reference (source of truth)

- **Endow:** `rathena/src/map/status.cpp:8630-8660` `status_get_weapon_element()` — returns
  `ELE_WATER` if `SC_WATERWEAPON`, `ELE_EARTH` if `SC_EARTHWEAPON`, `ELE_FIRE` if `SC_FIREWEAPON`,
  `ELE_WIND` if `SC_WINDWEAPON` (also `SC_ENCHANTARMS->val1`, `SC_ASPERSIO`→holy,
  `SC_SHADOWWEAPON`→dark, `SC_GHOSTWEAPON`→ghost). No stat mod. Magic endow at 4779-4784 adds
  `magic_atk_ele[ELE_x] += val1`.
- **MATK%:** `status.cpp:4890-4891` `SC_INCMATKRATE` → `pc_bonus(sd, SP_MATK_RATE, val1)`.
- **Resist:** `status.cpp:10728` `SC_SIEGFRIED: val2 = val1*3` (elemental resist), `val3 = val1*5`
  (status-ailment resist); read at `status.cpp:9634-9638` (sc_def += val3*100). `SC_NIBELUNGEN`
  (10725) `val2 = rnd()%RINGNBL_MAX` — a random ring effect selector, NOT an all-stat buff;
  consumed across many arms by `val2 == RINGNBL_*` (e.g. 6573 ALLSTAT, 7086 ATKRATE, 4896 MATKRATE).
- **Flat combat:** Berserk adds flat Batk and HP×3 in rAthena's status_calc; the C# real body at
  724 must match (+200 Batk, MaxHp×3) — verify, do not regress.

## Scope — every sub-system that must be touched

- [ ] **Add `CalcStatField.MatkRate` (MATK%) and `CalcStatField.RecalcOnly` (no-op marker)** to the
      enum (`StatusCalcFlagDefaults.cs:406`). `RecalcOnly` means "rAthena said `CalcFlags: All` as a
      recalc trigger; do NOT synthesize a stat mod." The generator switch
      (`StatusEffectRegistry.cs:6356-6425`) gets a `MatkRate` arm (`Stats.MatkRate += delta` or the
      equivalent field) and a `RecalcOnly` arm that does nothing.
- [ ] **Re-classify the endow SCs** (`Fireweapon`/`Waterweapon`/`Windweapon`/`Earthweapon`) to
      presence-only (remove from `StatusCalcFlagDefaults` or map to a new presence sentinel), and
      add explicit `Register` bodies that store the element in a field combat reads. Add them to
      `_behaviorElsewhereAllowlist` with the `status.cpp:8630` citation if OnStart stays no-op.
- [ ] **Wire `BattleElementService.GetWeaponElement`** to check the endow SCs (Fire/Water/Wind/
      Earth/Enchantarms/Aspersio/Shadowweapon/Ghostweapon, matching the rAthena precedence order in
      status.cpp:8632-8657) BEFORE falling back to `Stats.WeaponElement`. Mirror the magic-endow
      path if `GetMagicElement` exists.
- [ ] **Re-map `Incmatkrate`** to `CalcStatField.MatkRate` (single field, += Val1).
- [ ] **Give `Siegfried` a bespoke `Register` body**: `Val2 = Val1*3` (elemental resist),
      `Val3 = Val1*5` (status-ailment resist), no base-stat mod. Ensure a consumer reads Val2/Val3
      (the status-resist application path — `status.cpp:9634-9638`). If no consumer exists yet,
      allowlist with citation and file the consumer wiring under SC-04.
- [ ] **Give `Nibelungen` a bespoke `Register` body**: `Val2 = rnd() % RINGNBL_MAX` (port the
      `RINGNBL_*` enum); document the per-effect reads. Remove the six-base-stat mapping.
- [ ] **Confirm Berserk** real body (724) is +200 flat Batk + MaxHp×3 + the flee/regen penalties
      per rAthena; correct if it currently uses the generator-style +Val1.
- [ ] **Triage the remaining ~50 `CalcFlags: All` entries**: for each, open the rAthena
      status_calc / start arm and decide all-stat (keep) vs RecalcOnly vs bespoke. Produce a
      checklist in the PR description. Likely-genuine all-stat: Marionette*, Spirit, Truesight,
      Vigor, HeavenAndEarth, Catnippowder, Cheerup, Climax*, Fortune, Knowledge, Providence. Likely
      RecalcOnly/bespoke: anything that in rAthena only triggers a recalc without a base-stat delta.

## Done criteria

- A character under Fire Weapon deals fire-element melee damage (BattleElementService returns
  `ELE_FIRE`) and has NO phantom +Val1 STR/AGI/VIT/INT/DEX/LUK.
- `Incmatkrate` Val1=5 raises MATK% by 5, not the six base stats.
- `Siegfried` Val1=5 → Val2=15, Val3=25; status-resist consumer applies them.
- `Nibelungen` Val1=5 → Val2 in `[0, RINGNBL_MAX)`; no base-stat mod.
- Every reclassified SC either has a real body, a generator field that matches rAthena, or an
  allowlist entry with a `status.cpp:line` citation. No SC silently keeps the wrong six-stat map.
- `StatusEffectCompletenessTests` green (reclassified endow SCs must still satisfy the CalcFlag
  gate via allowlist or by having no CalcFlag).

## Test plan

- `WeaponEndowElementTests`: apply each endow SC, assert `GetWeaponElement` returns the right
  element and base stats are unchanged; assert precedence (Enchantarms > Water > Earth > Fire >
  Wind, matching status.cpp ordering).
- `IncmatkrateTests`: Val1=5 → MATK% +5, base stats unchanged.
- `SiegfriedResistTests` / `NibelungenTests`: pin the Val2/Val3 formulas.
- `BerserkFormulaTests`: pin +200 Batk, MaxHp×3.
- Re-run `StatusEffectCompletenessTests`, `StatusCalcServiceTests`.

## Notes / gotchas

- `CalcFlags: All` in rAthena status.yml is a recalc trigger — the generator script that produced
  `StatusCalcFlagDefaults` must also be updated (it's `<auto-generated>`; see the header at
  `StatusCalcFlagDefaults.cs:1-17`). Re-running the generator naively will re-introduce the bug;
  the generator needs an `All → RecalcOnly` rule plus a per-SC override table for the bespoke ones.
- Marionette/Marionette2 already have bespoke packed-Val3/Val4 bodies — leave them; just confirm
  the generator default is not also firing (the real body wins via dictionary overwrite).
- Verify the exact `Stats` field name for MATK% before adding the enum arm (`BattleStats.cs`).
