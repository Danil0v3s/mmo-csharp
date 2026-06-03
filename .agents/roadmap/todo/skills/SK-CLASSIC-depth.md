# SK-CLASSIC — 1st/2nd class depth polish + dash broadcast

> **Epic:** skills · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** SK-ENGINE · **Unlocks:** none

## The deliverable

> The Mage/Archer/Thief/Swordman/Merchant/Acolyte (1st/2nd) skills get depth-polished to match
> rAthena exactly, the CanDamage path routes through the target resolver, and dash/knockback
> slides broadcast.

## What this absorbs (archive)

- `_archive/todo/skills/SKILL-12` — family polish: Mage/Archer/Thief/Swordman/Merchant/Acolyte (depth).
- `_archive/todo/skills/SKILL-16` — route CanDamage through BattleTargetResolver + attack-vs-mechanic-damage split + BG teams.
- `_archive/todo/skills/SKILL-18` — dash/knockback slide broadcast (ZC_HIGHJUMP) on UnitOps.MovePos.

## rAthena reference

- `rathena/src/map/skill.cpp` — the 1st/2nd class `case` arms; `battle_check_target` for CanDamage;
  `clif_slide`/`clif_blown` for the slide broadcast.

## Scope

- [ ] Depth-polish the 1st/2nd class skill ratios/effects to rAthena.
- [ ] Route CanDamage through `BattleTargetResolver` (attack-vs-mechanic split + BG teams).
- [ ] Broadcast the dash/knockback slide on `UnitOps.MovePos`.

## Done criteria

- The polished skills match rAthena numbers; CanDamage uses the resolver; a dash slide is visible
  client-side; tests pin them.

## Test plan

- Per-skill tests + a slide-broadcast test.

## Notes

- Deferred. Builds on SKILL-03's resolver + SK-ENGINE.
