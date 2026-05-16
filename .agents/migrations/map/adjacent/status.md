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

P6 wired the IPC for save/load. The map-side engine is what's missing.

## Pending

1. `StatusEffect` record + `StatusEffectKind` enum.
2. `StatusChangeEngine.Tick` — iterate active SCs, fire periodic events (e.g. SC_POISON → damage every N ms), expire when duration elapses.
3. Save on logout / autosave: serialize active SCs to bytes for `SaveStatusChangeDataAsync`.
4. Load on enter: deserialize from `RequestStatusChangeDataAsync` response, re-apply remaining duration.

### Acceptance
- Casting Blessing on a player adds SC_BLESS for the right duration; stats change.
- Walking with Poison applied → HP ticks down at the right rate.
- Logging out with an active SC → re-logs back in with remaining duration intact.

## History
- **2026-05-16** — Plan stub.
