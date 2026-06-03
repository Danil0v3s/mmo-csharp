# GP-ELEM — Elemental works end-to-end

> **Epic:** gameplay · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** none · **Unlocks:** SK-CLASSIC (Sorcerer elemental skills)

## The deliverable

> A Sorcerer can **summon an elemental spirit that follows in its chosen mode (passive/
> defensive/offensive), fights/supports per mode, expires on its summon timer, and is
> dismissed** — live client, surviving logout while the summon timer is active.

## Player story

The lifetime-expiry sweep is real (elementals past `SummonExpiresAtTick` despawn via the game
loop — archive FEATURE-10). Missing: the create/load/delete IPC round-trip, the mode-based AI,
and the client packets/skill wiring.

## Current state — per layer

| Layer | State | Where |
|---|---|---|
| Data | ✅ | `elemental_db` seeded |
| Lifetime sweep | ✅ verify | `Map.Server/Elemental/ElementalService.Tick` in the game loop (archive FEATURE-10) |
| Create / IPC | ❌ | create/load/delete IPC round-trip (DI-cycle constrained, archive FEATURE-34) |
| Mode AI | ❌ | passive/defensive/offensive behaviour + the elemental action skills |
| CZ handlers | ❌ | elemental action/mode command |
| ZC emits | ❌ | elemental info/mode/HP updates |

## rAthena reference

- `rathena/src/map/elemental.cpp` — `elemental_create`, `elemental_delete`, `elemental_change_mode`
  (EL_MODE_PASSIVE/ASSIST/ATTACK), `elemental_action` (mode-driven), the summon `life_time`,
  `elemental_clean_effect`. Summoned via the Sorcerer `SO_*` skills.
- `rathena/src/map/clif.cpp` — elemental info/owner packets; mode-change command.

## Dependencies — and how to satisfy

- Packet-bridge pattern — foundation.
- Elemental save/create IPC — wire create/load/delete (the DI cycle means create/load dispatch
  rides the fan-out, not an injected `IIntifService`; archive FEATURE-34).
- Summon callsite — the Sorcerer summon skills (`SO_SUMMON_*`) → `elemental_create` (soft-links
  to SK-CLASSIC for the skill bodies; wire the create path here).

## Scope — every layer

- [ ] **Create/IPC**: elemental create on summon, load on enter, delete on dismiss/expire —
      round-trip (archive FEATURE-34).
- [ ] **Mode AI**: passive (follow only), defensive (guard the master), offensive (attack the
      master's target) + the per-mode elemental skills.
- [ ] **CZ handler**: mode-change / action command.
- [ ] **ZC emits**: elemental info (owner-linked), mode, HP/SP updates.
- [ ] **Persistence**: elemental row + remaining summon time across logout.

## Done criteria

- A Sorcerer summons an elemental → it follows in the chosen mode → defensive guards / offensive
  attacks the master's target → it expires on the summon timer or is dismissed.
- Relog with an active summon → the elemental resumes with the remaining time.

## Test plan

- Handler tests: mode command → service.
- Service: mode AI selection, expiry (extend archived ElementalServiceTests).
- Persistence round-trip.
- Live: summon → mode switch → expiry; relog mid-summon.

## Notes / gotchas

- The lifetime sweep already runs in `MapServerImpl` after `_pet.Tick` (archive FEATURE-10) —
  don't add a second prune path.
- `ElementalEntity` HP/SP back `Entity.Stats`, `MasterId` links the owner.
