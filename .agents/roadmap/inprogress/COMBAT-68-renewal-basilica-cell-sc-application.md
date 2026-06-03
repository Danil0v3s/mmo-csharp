# COMBAT-68 — Renewal Basilica ground-unit + cell-basilica SC_BASILICA_CELL application

> **Epic:** combat · **Status:** 🚧 In progress · **Size:** M · **Player-visible:** yes
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

## Scope — every sub-system that must be touched

- [ ] A renewal Basilica `ISkillUnitTickHandler` (ground unit placed at the cast cells) whose
      occupancy applies/removes `SC_BASILICA_CELL` on entities standing on the cells (the C#
      equivalent of `CELL_BASILICA` + `pc_cell_basilica`), via the existing SkillUnitService
      OnPlace/OnLeft hooks.
- [ ] Wire `HP_BASILICA` to place that unit at the caster's cell (renewal) alongside the
      `SC_BASILICA` self-buff, in `Acolyte/Basilica.cs` / the cast-end path.
- [ ] Confirm step-on/step-off correctly grants/clears `SC_BASILICA_CELL` (no leak after the
      group expires).

## Done criteria

- ➡️ from COMBAT-49: a caster (and allies) standing in a live Basilica take no damage from a
  hostile attack (and the cast is not interrupted) — end-to-end against the unit, not just a
  hand-applied SC.
- `SC_BASILICA_CELL` is removed when the entity leaves the cells or the group expires.
- No `// TODO` / `data-pending` / log-only no-op in the touched files.

## Test plan

- `Combat68BasilicaUnitTests`: place the Basilica unit; an entity on a cell gains
  `SC_BASILICA_CELL` (and is then damage-immune via COMBAT-49); stepping off / expiry clears it.

## Notes / gotchas

- COMBAT-49 already supplies the damage-immunity + the `MD_STATUSIMMUNE` / `SP_SOULEXPLOSION`
  exemptions — this ticket only needs to make `SC_BASILICA_CELL` get applied on-cell.
- Mirror the COMBAT-47/Pneuma OnPlace/OnLeft SC-application pattern in
  `Map.Server/Skills/Units/Handlers`.
