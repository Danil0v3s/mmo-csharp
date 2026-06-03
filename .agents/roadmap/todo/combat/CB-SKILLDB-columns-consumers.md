# CB-SKILLDB — skill_db columns (unit-flags / crit / weapon-state) + consumers

> **Epic:** combat · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** none · **Unlocks:** SK-ENGINE (shares the skill_db loader)

## The deliverable

> The remaining `skill_db` columns load and their consumers fire: unit-flags placement rules,
> skill crit-eligibility, weapon-type mask + required-state. Retires the curated overlays. **Combat last.**

## What this absorbs (archive)

- `_archive/todo/combat/COMBAT-107` — remaining `UF_*` placement rules (NoOverlap/PathCheck/NoFootSet).
- `_archive/todo/combat/COMBAT-109` — general `bMagicAtkEle` equip bonus (magic_atk_ele by skill element).
- `_archive/todo/combat/COMBAT-111` — bespoke `OnRecalc` remainder (multi-field/+AspdRate/+primary/+trait).
- `_archive/todo/combat/COMBAT-112` — recursive-splash victims skip `ApplyWeaponSkillPlantZone`.
- `_archive/todo/combat/COMBAT-113` — `skill_db` Requirements: Weapon-type mask + RequiredState columns.
- `_archive/todo/combat/COMBAT-114` — skill crit-eligibility (most skills don't crit) — skill_db crit flag.

## rAthena reference

- `rathena/src/map/skill.cpp` — the `skill_db` Unit.Flag / Requirements / DamageFlags parse;
  `status.cpp` `OnRecalc` derived-stat handlers.

## Scope

- [ ] Generic `skill_db` Unit.Flag column loader (+ bit-order fix) + the placement-rule consumers.
- [ ] Weapon-type mask + RequiredState columns + the `e_require_state` map.
- [ ] Skill crit-eligibility flag + the crit-on-skill gate; `bMagicAtkEle`; the bespoke
      `OnRecalc` remainder; recursive-splash plant-zone skip.

## Done criteria

- The cited columns load from `db/re/skill_db.yml`; the placement/crit/weapon-state consumers
  behave per rAthena; the curated `SkillDb.LoadingFinished` overlays are retired where the real
  column now covers them; the `Combat*Tests` pass.

## Test plan

- Extend the archived COMBAT-107/109/111/112/113/114 tests.

## Notes

- The `skill_db` loader work overlaps SK-ENGINE's duration/val reads — share the loader. Combat-last.
