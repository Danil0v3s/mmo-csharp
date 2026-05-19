# MS3 · Status changes

**Phase:** MS3 (adjacent)
**Depends on:** [combat.md](combat.md), [skills.md](skills.md)
**Blocks:** anything modified by status (movement speed, damage, ASPD)

Status changes are buffs/debuffs/state effects: poison, freeze, sleep, blessing, agi up, food buffs, refresh, deluge, item-procced effects, etc. rAthena's [status.cpp](/Volumes/1TB/Projetos/rathena/src/map/status.cpp) is **16K lines**. Most status effects need ~50 lines of C#; the engine that ticks them is the bigger thing.

## Source of truth

- [rathena/src/map/status.cpp](/Volumes/1TB/Projetos/rathena/src/map/status.cpp) — SCC engine (`status_change_start`, `status_change_end`, `status_change_timer`)
- [rathena/src/map/status.hpp](/Volumes/1TB/Projetos/rathena/src/map/status.hpp) — `enum sc_type` (300+ status effect types)

## Scope (MS3 first pass)

**In scope:**
- `StatusChangeEngine` ticking active SCs (refresh, expire, periodic effects like poison ticks).
- `StatusEffectDb` — per-effect metadata: duration calc, stack rules, blocked-by, ends-on-death, breaks-when-hit.
- Apply / remove SC: `status_change_start(target, sc, val1..4, duration, flag)`.
- Persistence via existing IPC (`SaveStatusChangeDataAsync` / `RequestStatusChangeDataAsync` — already wired in P6).
- Visibility of SC icons on entity (client side `ZC_MSG_STATE_CHANGE`).
- A starter set of ~30 SCs (the common ones: SC_POISON, SC_BLESSING, SC_AGI_UP, SC_PROVOKE, SC_INCATKRATE, food buffs).

**Out of scope:**
- All 300+ effects covered at once — pick the 30 that combat/skills need, then iterate.
- Costume-effect SCs (visual-only).

## Done

- **StatusType enum** ([Map.Server/Status/StatusType.cs](../../../../Map.Server/Status/StatusType.cs)) — subset mirroring rAthena `enum sc_type` indices for lossless persistence round-trip.
- **StatusChange record** ([StatusChange.cs](../../../../Map.Server/Status/StatusChange.cs)) — val1..4 + ExpiresAt + NextTick + PeriodMs (mirrors `status_change_entry`).
- **StatusEffectRegistry + StatusEffectHandler** ([StatusEffectRegistry.cs](../../../../Map.Server/Status/StatusEffectRegistry.cs)) — strategy table: per-SC `OnStart` / `OnEnd` / `OnPeriodic` callbacks. New SCs Register() without touching the engine.
- **StatusChangeService** ([StatusChangeService.cs](../../../../Map.Server/Status/StatusChangeService.cs)) — `Start` / `End` / `Get` / `Tick`. Refresh-on-restart matches rAthena `SCSTART_NOAVOID` default. Pumped from the map game loop BEFORE AI / attack so DoT-induced death is processed before downstream readers see the entity.
- 5 starter SCs registered: **SC_POISON** (1.5s DoT, 1.5% MaxHp/tick), **SC_BLESSING** (+STR/INT/DEX), **SC_INCREASEAGI** / **SC_DECREASEAGI** (±AGI), **SC_HEAL_OVERTIME** (HP regen with MaxHp cap).
- **NaturalHealService** ([NaturalHealService.cs](../../../../Map.Server/Status/NaturalHealService.cs)) — baseline HP/SP regen (renewal `status_natural_heal`). 6s/8s cadence, sitting bonus, walking gate, full-pool short-circuit.
- 5 SC tests in [Map.Server.Tests/Status/StatusChangeServiceTests.cs](../../../../Map.Server.Tests/Status/StatusChangeServiceTests.cs) + 5 natural-heal tests.

## Pending

1. Long tail of SC_* (currently 5 of ~300). Add via `StatusEffectRegistry.Register` calls — engine is stable.
2. **Persistence** — `SaveStatusChangeDataAsync` / `RequestStatusChangeDataAsync` IPC is wired (P6) but the codec for serializing the active SC list to bytes is not yet implemented. Lands with the storage-codec slice's approach (BinaryWriter-style versioned blob).
3. **Wire SC icons to client** — `ZC_MSG_STATE_CHANGE` not yet emitted on Start/End. The handlers know enough to fire it; just needs the packet shape pinned to PACKETVER 20220401.

### Acceptance
- ✅ Blessing applies +N STR/INT/DEX, reverts on end / expiry.
- ✅ Poison ticks 1.5% MaxHp damage at 1.5s cadence via the damage pipeline.
- ⚠️ SC persistence across logout (engine ready, codec pending).

## History
- **2026-05-16** — Plan stub.
- **2026-05-19** — Engine + 5 starter SCs shipped. Pump runs from the map game loop ahead of AI/attack. Time source plumbed through `nowTick` so deterministic tests share the clock with the engine. Persistence codec deferred but the IPC surface for it is unchanged.
