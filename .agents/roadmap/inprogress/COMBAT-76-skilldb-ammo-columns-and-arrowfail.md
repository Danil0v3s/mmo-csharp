# COMBAT-76 — skill_db ammo columns (per-skill ammotype/ammo_qty) + clif_arrow_fail

> **Epic:** combat · **Status:** 🚧 In progress · **Size:** M · **Player-visible:** yes
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

- [ ] Import the skill_db ammo columns (Tools.RathenaImporter + the SkillDbEntity/migration) and
      load them into `SkillDefinition.AmmoTypeMask`/`AmmoQuantity` in `SkillDbLoader`.
- [ ] Switch `SkillUsesAmmo`/the gate to the per-skill `GetAmmoType(skillId) != 0` mask (+ the
      ammo-type-vs-weapon match), and consume the per-level qty; add the NW_MAGAZINE_FOR_ONE +4.
- [ ] Send `clif_arrow_fail` for arrows + the inventory amount packet on consume.

## Done criteria

- A skill with `require.ammo == 0` consumes nothing; a multi-round skill spends its exact qty; an
  out-of-ammo bow skill sends clif_arrow_fail and a gun skill NEED_MORE_BULLET.

## Test plan

- `Combat76SkillAmmoDataTests`: a loaded skill_db ammo row drives gate/consume; the NW special +4.

## Notes / gotchas

- COMBAT-58 already supplies the qty-aware AmmoService + the cast-path gate/consume hooks — this
  ticket only supplies the data + the exact packet + the per-skill mask switch.
