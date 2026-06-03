# COMBAT-68 — Renewal Basilica ground-unit + cell-basilica SC_BASILICA_CELL application

> **Epic:** combat · **Status:** ✅ Done (2026-06-03) · **Size:** M · **Player-visible:** yes
> **Depends on:** COMBAT-49 (the SC_BASILICA_CELL damage-immunity check) · **Blocks:** none
> **Filed by:** COMBAT-49 — the cell-apply path that makes the immunity reachable in prod.

## Problem

COMBAT-49 implemented the renewal `battle_calc_damage` immunity: a target with
`SC_BASILICA_CELL` takes no attack damage (so a mid-cast caster isn't interrupted). But
nothing in the C# port ever *applies* `SC_BASILICA_CELL`, so the immunity is dormant on a
live server:

- The renewal `HP_BASILICA` cast (`Acolyte/Basilica.cs`) applies only `StatusType.Basilica`
  (the caster's self-buff) — there is **no Basilica ground unit**, no `CELL_BASILICA` cell
  marking, and no `pc_cell_basilica` equivalent that grants `SC_BASILICA_CELL` to entities
  standing on the sanctuary cells. So no entity ever carries `SC_BASILICA_CELL`, and the
  COMBAT-49 block never fires in real play.

## Current state (C#)

- `Map.Server/Skills/Behaviors/Acolyte/Basilica.cs:CastendNoDamageId` — `ctx.Sc.Start(target,
  StatusType.Basilica, …)`; `CastendPos2` is an intentional no-op ("renewal carries the effect
  in the SC"). No ground-unit group, no cell flag, no per-cell SC.
- `Map.Server/Skills/Units/Handlers/` — no Basilica unit handler.
- `Map.Server/Movement/PlayerPositionHelpers.cs:IsBasilicaCell` — already probes
  `StatusType.BasilicaCell` (the proxy), but nothing applies it.
- `Map.Server/Combat/DamageService.cs:IsBasilicaImmune` (COMBAT-49) — reads
  `StatusType.BasilicaCell`; correct, but dormant without this ticket.

## rAthena reference (source of truth)

- `skill.cpp` renewal `HP_BASILICA` (`skill_castend_nodamage_id` self-buff group ~8267) starts
  `SC_BASILICA` on the caster + creates the unit group; `skill_unitsetmapcell(…, CELL_BASILICA, …)`
  (~21830/21893) marks/clears the cells.
- `pc.cpp` `pc_cell_basilica` (~15031): if the PC is on a `CELL_CHKBASILICA` cell and lacks
  `SC_BASILICA_CELL`, `sc_start(SC_BASILICA_CELL, INFINITE_TICK)`; if off-cell and has it, end it.
  Called from `unit.cpp:625` (step) + `pc.cpp:7037`.
- Monolithic-switch caveat: canonical source is the `skill.cpp`/`pc.cpp` switch arms (no
  `rathena-fork/src/map/skills/...` split files here).

## ⚠️ Premise correction (discovered during implementation)

**The renewal Basilica ground-unit + `SC_BASILICA_CELL` this ticket asks for does not exist in
renewal.** Verified in rAthena:
- HP_BASILICA renewal goes through the self-buff arm (`#ifdef RENEWAL` at skill.cpp:8267) — it
  places **no ground unit**.
- The `CELL_BASILICA` cell-marking (skill.cpp:21830) is **`#ifndef RENEWAL`** — renewal never
  marks a Basilica cell, so `pc_cell_basilica` (which grants `SC_BASILICA_CELL` only off a marked
  cell) **never applies `SC_BASILICA_CELL` in renewal**.
- The renewal effect is the self-buff `SC_BASILICA` (status.yml `CalcFlags: All` + `States:
  NoAttack`): an offensive element buff (weapon addele Dark/Undead, magic addele Holy;
  status.cpp:4768) + the NoAttack caster state.

So the COMBAT-49 `SC_BASILICA_CELL` immunity is **pre-renewal behavior, correctly dormant in
renewal by design** (not a bug). Building a renewal unit that applies `SC_BASILICA_CELL` would be
*unfaithful* (inventing a mechanism renewal doesn't have).

## Scope — every sub-system that must be touched

- [x] Documented the premise correction in `Acolyte/Basilica.cs` (was mislabeled "PVP-block
      work") + `DamageService.IsBasilicaImmune` (now notes it is inert in renewal by design) so
      this is not re-filed; fixed the Basilica self-buff duration to `30000+30000*lv`.
- [ ] ~~Renewal Basilica ground unit + `SC_BASILICA_CELL` application~~ — **not applicable to
      renewal** (see correction above). The cell-immunity mechanism is pre-renewal-only.
- [ ] The real renewal `SC_BASILICA` effects (offensive element buff + NoAttack). ➡️ Moved to
      COMBAT-87 — they need an SC→element-fold recalc seam + a NoAttack gate, neither of which
      exists (an `OnRecalc` that wrote `EquipBonuses.AddEle` would leak, since `CalcPc` does not
      reset the bundle).

## Done criteria

- Renewal HP_BASILICA applies `SC_BASILICA` (self-buff), not `SC_BASILICA_CELL`, and places no
  unit — pinned by `Combat68BasilicaRenewalTests` ✅.
- The COMBAT-49 `SC_BASILICA_CELL` immunity is documented as pre-renewal / dormant-by-design ✅.
- The real renewal element-buff + NoAttack effects ➡️ COMBAT-87.
- No `// TODO` / `data-pending` / log-only no-op in the touched files ✅.

## Test plan

- `Combat68BasilicaUnitTests`: place the Basilica unit; an entity on a cell gains
  `SC_BASILICA_CELL` (and is then damage-immune via COMBAT-49); stepping off / expiry clears it.

## Notes / gotchas

- COMBAT-49 already supplies the damage-immunity + the `MD_STATUSIMMUNE` / `SP_SOULEXPLOSION`
  exemptions — this ticket only needs to make `SC_BASILICA_CELL` get applied on-cell.
- Mirror the COMBAT-47/Pneuma OnPlace/OnLeft SC-application pattern in
  `Map.Server/Skills/Units/Handlers`.

## History

- 2026-06-03 · Premise correction (like COMBAT-66): verified renewal HP_BASILICA has no ground
  unit and never applies `SC_BASILICA_CELL` (the `CELL_BASILICA` marking at skill.cpp:21830 is
  `#ifndef RENEWAL`), so COMBAT-49's immunity is pre-renewal-only and correctly dormant in
  renewal. Fixed the misleading `Acolyte/Basilica.cs` doc ("PVP-block work" → the actual renewal
  SC_BASILICA element-buff + NoAttack) + its self-buff duration (`30000+30000*lv`), and clarified
  `DamageService.IsBasilicaImmune`. Combat68BasilicaRenewalTests (2); skills+combat suite 3117
  green, full suite 4085 pass (1 fail = pre-existing INFRA-11 replay gate). Filed COMBAT-87 for
  the real renewal effects (offensive element buff + NoAttack — both need new SC infrastructure).
