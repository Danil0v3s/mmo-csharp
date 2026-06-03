# SC-MAGNITUDE — SC magnitudes correct (CalcFlags mis-map + generator-default review)

> **Epic:** status · **Status:** ❌ Not started · **Size:** L · **Player-visible:** yes
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

- [ ] Fix the remaining `CalcFlags: All` → 6-base-stat mis-mappings (~35 SCs).
- [ ] Complete the element-endow SC family (weapon element, not a stat buff).
- [ ] Convert the linear-wrong + bespoke + bulk generator-default SCs to their real magnitudes.

## Done criteria

- Each converted SC applies the rAthena magnitude (the archive lists the formulas); the
  `GeneratorDefaultAuditTests` worklist shrinks to the genuinely-default set; no SC silently
  buffs all six stats that shouldn't.

## Test plan

- Extend the archived SC-10/11/18/19/20 per-SC formula tests; the completeness/audit guards stay green.

## Notes

- Element-endow SCs set the weapon element the combat resolver reads — not an all-stat buff
  (archive SC-02). Deferred (after gameplay).
