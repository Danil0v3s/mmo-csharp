# COMBAT-76 — skill_db ammo columns (per-skill ammotype/ammo_qty) + clif_arrow_fail

> **Epic:** combat · **Status:** ✅ Done (2026-06-03) · **Size:** M · **Player-visible:** yes
> **Depends on:** COMBAT-58 (the skill ammo gate/consume seam) · **Blocks:** none
> **Filed by:** COMBAT-58 — the precise per-skill ammo data + the arrow-fail packet it could not wire (data unloaded).

## Problem

COMBAT-58 wired the ammo gate + consume into the skill cast path, but the skill_db
`require.ammo` (ammotype mask) and `require.ammo_qty` columns are **not loaded** in the C#
skill_db (`SkillDb.GetAmmoType` / `GetAmmoQty` return 0). So COMBAT-58 keys reachability on the
WEAPON ammo gate (`WeaponTypeCodes.UsesAmmo` for weapon-damage skills) and consumes a fixed **1**
round. Three pieces remain:

1. **Per-skill ammo mask + qty.** Load the skill_db `RequiredAmmo` (mask) + `RequiredAmmoAmount`
   (per-level qty) so the gate/consume use the exact rAthena `require.ammo` / `skill_get_ammo_qty`
   per skill (a non-ammo bow skill stops consuming; a multi-round skill spends >1).
2. **NW_MAGAZINE_FOR_ONE + W_GATLING → +4 rounds** (battle_consume_ammo special case).
3. **Exact client packets.** rAthena sends `clif_arrow_fail(ARROWFAIL_NO_AMMO)` (ZC_ACTION_FAILURE,
   0x013b) for arrows specifically (COMBAT-58 uses the skill-fail packet with the `Stuff` cause for
   both), and an explicit inventory amount packet on consume (COMBAT-58 rides the RemovedInventoryIds
   sync). Wire `clif_arrow_fail` + the amount packet to match.

## Current state (C#)

- `Map.Server/Skills/SkillDb.cs:GetAmmoType`/`GetAmmoQty` — read `AmmoTypeMask`/`AmmoQuantity`,
  which `SkillDbLoader` does not populate (always 0).
- `Map.Server/Skills/SkillCastService.cs:SkillUsesAmmo`/`AmmoGateFails` (COMBAT-58) — weapon-type
  heuristic; qty `Math.Max(1, GetAmmoQty)` ⇒ 1.
- `Map.Server/Inventory/AmmoService.cs` — qty-aware HasUsableAmmo/ConsumeAmmo (COMBAT-58).

## rAthena reference (source of truth)

- `skill_db.yml` `Requirements: { Ammo:, AmmoAmount: }`; `skill_get_ammotype` / `skill_get_ammo_qty`.
- `battle.cpp battle_consume_ammo` (the NW_MAGAZINE_FOR_ONE +4 special).
- `clif.cpp clif_arrow_fail` (ARROWFAIL_NO_AMMO) + `clif_skill_fail` NEED_MORE_BULLET.

## Scope — every sub-system that must be touched

- [x] Load the skill_db ammo mask + qty into `SkillDefinition.AmmoTypeMask`/`AmmoQuantity`. →
      Done via a curated `CuratedAmmo` overlay in `SkillDb.LoadingFinished` (the COMBAT-62 pattern),
      sourced 1:1 from `db/re/skill_db.yml` (61 skills). ➡️ The "real" DB-column path
      (`Tools.RathenaImporter` + `SkillDbEntity`/migration + `SkillDbLoader`) is **moved to COMBAT-92**
      (fold the ammo + Inf2 overlays into a Requirements column loader) — the behavior/done-criteria
      are fully met by the overlay.
- [x] Switch `SkillUsesAmmo`/the gate to the per-skill `GetAmmoType(skillId) != 0` mask (+ the
      ammo-type-vs-weapon match), and consume the per-level qty; add the NW_MAGAZINE_FOR_ONE +4. →
      `SkillUsesAmmo` keys on the explicit mask OR the weapon heuristic; `SkillAmmoQty` uses the real
      per-level qty + the NW_MAGAZINE_FOR_ONE/W_GATLING +4 + the 2016 renewal extra-ammo +1 (gate
      only). New mask-aware `AmmoService.HasUsableAmmo/ConsumeAmmo(pc, qty, ammoMask)` makes
      Kunai/Shuriken/Cannonball skills gate/consume weapon-independently.
- [x] Send `clif_arrow_fail` for arrows + NEED_MORE_BULLET / NEED_EQUIPMENT_KUNAI by ammo type. →
      New `ZC_ACTION_FAILURE` (0x013b) packet + `BroadcastArrowFail`; gate selects the packet by the
      effective ammo mask. ➡️ The immediate **inventory amount-update packet** on a partial consume
      is **moved to COMBAT-94** (no such packet exists in the codebase; every consume path — ammo +
      items — rides the periodic sync today).

## Done criteria

- A skill with `require.ammo == 0` consumes nothing; a multi-round skill spends its exact qty; an
  out-of-ammo bow skill sends clif_arrow_fail and a gun skill NEED_MORE_BULLET. ✅ all met (the
  fail-cause WIRE values were corrected for the two ammo causes only; ➡️ the full
  `SkillFailCause` ↔ `e_useskill_fail_cause` reconciliation is **COMBAT-93**).

## Test plan

- `Combat76SkillAmmoDataTests`: a loaded skill_db ammo row drives gate/consume; the NW special +4.

## Notes / gotchas

- COMBAT-58 already supplies the qty-aware AmmoService + the cast-path gate/consume hooks — this
  ticket only supplies the data + the exact packet + the per-skill mask switch.

## History

- 2026-06-03 — Loaded the 61-skill ammo mask/qty via a curated `CuratedAmmo` overlay
  (SkillDb.LoadingFinished, COMBAT-62 pattern, from db/re/skill_db.yml). Rewired the cast-path gate +
  consume to the per-skill `GetAmmoType`/`GetAmmoQty` (mask + qty), with the NW_MAGAZINE_FOR_ONE +
  W_GATLING +4 (skill.cpp:19920) and the renewal extra-ammo +1 gate charge (skill.cpp:19602). Added
  mask-aware `AmmoService.HasUsableAmmo/ConsumeAmmo(pc, qty, ammoMask)` so Kunai/Shuriken/Cannonball
  skills gate/consume weapon-independently. New `ZC_ACTION_FAILURE` (0x013b) + `BroadcastArrowFail`;
  the gate now emits arrow_fail (arrows/no-ammo), NEED_MORE_BULLET (bullet/shell/grenade), or
  NEED_EQUIPMENT_KUNAI (kunai) per the effective mask, with the two ammo fail-causes corrected to
  their exact rAthena wire values (84/34). Added SkillIds.RL_P_ALTER (2563). Combat76SkillAmmoDataTests
  (9) + updated Combat58 (bow→arrow_fail); full suite 4133 pass (1 fail = pre-existing INFRA-11 replay
  gate). Filed COMBAT-92 (real Requirements column loader), COMBAT-93 (SkillFailCause enum
  reconciliation), COMBAT-94 (immediate amount-update packet on consume).
