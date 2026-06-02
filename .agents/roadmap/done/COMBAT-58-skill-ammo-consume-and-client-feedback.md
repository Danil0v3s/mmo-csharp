# COMBAT-58 — Ammo consumption on ammo-using skills + out-of-ammo client feedback

> **Epic:** Combat parity · **Status:** ✅ Done (2026-06-02) · **Size:** M · **Player-visible:** yes
> **Depends on:** COMBAT-36 (the `IAmmoService` gate/consume seam)
> **Blocks:** none
> **Filed by:** COMBAT-36 — it wired ammo gate/consume into the auto-attack loop only.

## Problem

COMBAT-36 added `IAmmoService` (gate + one-round consume) and wired it into the
**auto-attack** loop (`AttackService.Tick`). Two pieces remain:

1. **Ammo-using skills don't consume / gate ammo.** Ranged skills (Double Strafe,
   Arrow Shower, Arrow Storm, the Gunslinger skills, …) fire without spending a round
   and without the no-ammo gate. rAthena calls `battle_consume_ammo(sd, skill, lv)`
   with `skill_get_ammo_qty(skill, lv)` (some skills spend more than one) in the
   skill-castend path, and `skill_check_condition_castend` blocks the cast with no ammo.
2. **No client feedback on the gate / decrement.** The auto-attack gate currently
   refuses the swing silently; rAthena sends `clif_arrow_fail` / `USESKILL_FAIL_
   NEED_MORE_BULLET`. And ammo decrement rides the same client-prediction + save-sync
   path as `ItemUseService` (no immediate amount packet) — fine for the common case,
   but an explicit amount/fail packet would match rAthena's feedback.

## Current state (C#)

- `Map.Server/Inventory/AmmoService.cs` — `HasUsableAmmo` / `ConsumeAmmo`; consume is
  one round (no `skill_get_ammo_qty`).
- `Map.Server/Combat/AttackService.cs` — calls the gate/consume per auto-swing only.
- Skill dispatch (`SkillAttackService` / resolvers / `SkillCastEndService`) does not
  consult `IAmmoService`.

## rAthena reference

- `battle.cpp battle_consume_ammo(sd, skill, lv)` + `skill_get_ammo_qty`.
- `skill.cpp skill_check_condition_castend` ammo gate; `clif_arrow_fail` /
  `clif_skill_fail(USESKILL_FAIL_NEED_MORE_BULLET)`.

## Scope

- [x] Consult `IAmmoService` in the ammo-using skill cast path: `SkillCastService.StartCast` +
      `StartCastAt` gate the cast (→ `SkillCastResult.NeedAmmo`) and `ResolveSkill` consumes at
      castend. `AmmoService` gained qty-aware `HasUsableAmmo(pc, qty)` / `ConsumeAmmo(pc, qty)`.
      ➡️ The per-skill `require.ammo` mask + per-level `skill_get_ammo_qty` are **unloaded** in
      skill_db, so reachability uses the weapon ammo gate (UsesAmmo) for weapon-damage skills and
      qty defaults to 1 — the exact per-skill data + the NW_MAGAZINE_FOR_ONE +4 moved to **COMBAT-76**.
- [x] Send the out-of-ammo feedback on the gate (`BroadcastSkillFail` with `Stuff` for arrows /
      `NeedMoreBullet` for guns). ➡️ The exact `clif_arrow_fail` (ZC_ACTION_FAILURE) packet for
      arrows + the explicit consume amount packet moved to **COMBAT-76** (the consume rides the
      established RemovedInventoryIds sync, like ItemUseService).

## Done criteria

- ➡️ from COMBAT-36: an ammo-using skill spends its rounds and is blocked when out of ammo. ✅ —
  a bow skill consumes a round at castend and is gated (NeedAmmo + fail packet) at 0; the exact
  per-skill qty (>1) ➡️ COMBAT-76.
- The client receives the out-of-ammo message and the updated ammo count. ✅ message via
  `BroadcastSkillFail`; the ammo count updates through the inventory sync (exact amount packet ➡️
  COMBAT-76).

## History

- 2026-06-02 — Wired the ammo gate + consume into the skill cast path: `AmmoService` qty-aware
  `HasUsableAmmo`/`ConsumeAmmo`; `SkillCastService.StartCast`/`StartCastAt` gate weapon-damage
  skills on an ammo-using weapon (→ `NeedAmmo` + `BroadcastSkillFail` Stuff/NeedMoreBullet), and
  `ResolveSkill` consumes a round at castend. `Combat58SkillAmmoTests` (6, green); full suite 4028
  pass (1 fail = pre-existing INFRA-11 replay gate). Filed COMBAT-76 (load skill_db ammotype/
  ammo_qty + the per-skill mask switch + NW_MAGAZINE_FOR_ONE +4 + the exact arrow-fail/amount packets).

## Test plan

- A ranged skill decrements ammo by its qty; blocked at 0.
- Gate emits the fail packet; consume emits the amount packet.
