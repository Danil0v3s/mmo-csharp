# FEATURE-10 — Elemental persistence + lifetime expiry

> **Epic:** Gameplay-Companion · **Status:** ✅ Done (2026-06-03) · **Size:** M · **Player-visible:** yes
> **Depends on:** FEATURE-02 (elemental save) · **Blocks:** none
> **Related:** PACKET-* (elemental status packets)

## Problem

The elemental is the most complete companion — a real `ElementalEntity` spawns
into the world with modes, skill gating, and a lifetime field. But it's
**in-memory only**: no `ElementalSave`/`Load` IPC is ever called, so the
elemental is lost on relog (acceptable in rAthena for some cases, but the save
hook is still expected), and the **lifetime expiry tick that should despawn it
when the summon timer runs out is unconfirmed** — `SummonExpiresAtTick` is
stamped but nothing prunes the entity when it passes.

## Current state (C#)

- `Map.Server/Elemental/ElementalService.cs` — strong: `Create` (`:56`), `DataReceived` (`:81`, spawns `ElementalEntity` into the registry), `ChangeMode`/`ChangeModeAck` (`:180`/`:206`), `Action`/`SetTarget`/`UnlockTarget` (`:252`/`:272`/`:289`), `Heal` (`:305`), `GetLifetimeMs` (`:341`), `SummonInit` (`:354`, stamps `ele.SummonExpiresAtTick`), `SerializeSnapshot` (`:381`, real `ToElementalData` projection).
- `Save(master)` (`:135`) — *log only* (`_logger.LogDebug("elemental_save...")`); comment says "the actual gRPC send is owned by `IntifService.ElementalSave`" — but **`IntifService.ElementalSave` is never called from here** (or anywhere in the game loop).
- `DataReceived` sets `ElementalId = 0` (`:99`, "assigned post-save") and the headless path returns 0 (`:92`) — no real load round-trip.
- `SummonExpiresAtTick` is set in `SummonInit` (`:358`) but there is **no lifetime-expiry sweep** in the elemental service or the game loop that despawns the entity when it passes (the AI loop `_summonAi.Tick` follows/assists but expiry pruning is unconfirmed).
- `Map.Server/Services/Intif/IntifService.cs`: real `ElementalCreate` (`:747`), `ElementalRequest` (`:764`), `ElementalSave` (`:774`), `ElementalDelete` (`:789`) — orphaned (no `ElementalService` caller).

## rAthena reference (source of truth)

- `rathena/src/map/elemental.cpp`:
  - `elemental_create(sd, class_, lifetime)` → `intif_elemental_create` (char inserts row, returns ele_id).
  - `elemental_data_received(struct s_elemental *ele, bool flag)` → build the unit, `status_calc_elemental`, place, `clif_spawn`, `elemental_summon_init` (sets `summon_timer`).
  - `elemental_save(ed)` → `intif_elemental_save`.
  - `elemental_summon_init` — `add_timer(ele->summon_time, elemental_summon_end, ...)`; `elemental_summon_end` → `elemental_delete` when the lifetime expires.
  - `elemental_delete(ed)` → `intif_elemental_delete` + `unit_remove_map` + clear `sd->ed`.
  - Note: rAthena persists the elemental row (so it survives a controlled save), but the elemental is typically deleted on its lifetime end; the save call is still part of the lifecycle (e.g. on map-change/save).

## Scope — every sub-system that must be touched

- [x] ➡️ `Create` IPC + ele_id → **FEATURE-34** (DI cycle from here). Original: dispatch `IntifService.ElementalCreate(...)` so the char side allocates a real `elemental_id`; on the load/create response, set `ele.ElementalId` (currently hardcoded 0 in `DataReceived`).
- [x] ➡️ `DataReceived` load round-trip + hydrated stats → **FEATURE-34**. Original: wire the real load round-trip: when re-summoning, pull the saved row via `IntifService.ElementalRequest`; build the entity from the hydrated payload (HP/SP/stats), not the placeholder `MaxHp = master.MaxHp/3` (`:112`). Keep the spawn path.
- [x] ➡️ `Save` IPC → **FEATURE-17** fan-out (DI cycle). `SerializeSnapshot` already real. Original: call `IntifService.ElementalSave(...)` (4-byte id header per FEATURE-02). Remove the log-only body. `SerializeSnapshot` is already real.
- [x] **Lifetime expiry tick**: implement the despawn — a per-tick sweep in the elemental service (a `Tick(nowTick)` called from `MapServerImpl`) or a per-elemental timer that, when `Environment.TickCount64 >= ele.SummonExpiresAtTick`, calls `Delete(master)` (vanish + remove + `IntifService.ElementalDelete`). Confirm whether `_summonAi.Tick` already prunes; if not, add the sweep.
- [x] ➡️ **Delete** char-row IPC → **FEATURE-34**. Local vanish done. Original: call `IntifService.ElementalDelete` on teardown (currently `Delete` :157 only removes locally).
- [x] ➡️ **Client packets** → PACKET-* (marked seam). Original:: ZC_EL_INIT / ZC_EL_PAR_CHANGE (HP/SP update on heal — the `clif_elemental_updatestatus` seam at `:313`). Define or use PACKET-* seam; **entity + state mutation stay here**.
- [x] ➡️ **Save wiring** → **FEATURE-17**. Original: via FEATURE-02 fan-out + on map-change.

## Done criteria

- `Create` allocates a real `elemental_id` char-side and `DataReceived` populates `ele.ElementalId` (no longer 0).
- The elemental's HP/SP/stats come from the hydrated row (or `status_calc`), not the `MaxHp/3` placeholder.
- The elemental despawns automatically when its lifetime (`SummonExpiresAtTick`) passes — entity vanishes + `IntifService.ElementalDelete` fires.
- `Save` dispatches `IntifService.ElementalSave` with the real snapshot; elemental state can round-trip a save.
- No log-only `Save`, no orphaned elemental IPC wrapper.

## Test plan

- `Map.Server.Tests` (extend `ElementalServiceTests`):
  - lifetime tick despawns the entity + calls `IntifService.ElementalDelete` once `SummonExpiresAtTick` passes (inject clock);
  - `Save` calls `IntifService.ElementalSave` with a non-null snapshot;
  - `Create`→`DataReceived` sets a real `ElementalId` from a stubbed char response.
- Integration with `_summonAi` (no double-despawn) and FEATURE-02 (save fan-out).
- Manual/live: summon an elemental (Sorcerer), watch it fight, confirm it despawns when the lifetime ends.

## SerializeSnapshot field map (already real)

`ToElementalData` (`ElementalService.cs:400`) already projects the live entity onto `Core.Server.IPC.ElementalData`: `ElementalId, CharacterId, ClassId, Mode, Hp, Sp, MaxHp, MaxSp, Attack, Attack2, Matk, Aspd, Def, Mdef, Flee, Hit, LifeTime`. The save wiring (FEATURE-02) just needs to pass the live `ElementalId` (once `Create`/`DataReceived` populate it) so `IntifService.ElementalSave`'s `BitConverter.ToInt32(data,0)` lookup resolves this snapshot rather than the empty fallback (`IntifService.cs:780`).

## Where the lifetime field already lives

- `ElementalEntity.SummonExpiresAtTick` is set in `SummonInit` (`ElementalService.cs:358`) from `master.ActiveElementalExpiresAt` (computed in `Create` :66 from `lifetimeMs`).
- `GetLifetimeMs` (`:341`) already returns the remaining ms.
- What's missing is the consumer: nothing calls `Delete` when `Environment.TickCount64 >= SummonExpiresAtTick`. Confirm whether `_summonAi.Tick` (`MapServerImpl.cs:307`) prunes; if not, the elemental lives forever.

## Notes / gotchas

- This is the lightest companion ticket because the entity + AI + modes already work — it's persistence + expiry only.
- Confirm exactly one expiry path: don't add a service-level sweep if `_summonAi.Tick` already prunes on `SummonExpiresAtTick` — pick one and remove the other to avoid double-delete.
- `DataReceived` headless path returns 0 (`:92`) when `_entities`/`_ids` are null — keep that test seam.
- The placeholder HP formula (`master.MaxHp/3`, `:112`) is explicitly temporary — replace with the char-hydrated values / `status_calc_elemental` when wiring the load.
- `Dead` delegates to `Delete` (`:171`); `SummonStop` also delegates to `Delete` (`:370`). Route the expiry sweep through `Delete` too so teardown (clean-effect + registry removal + IPC delete) is single-sourced.
- One elemental per master is enforced in `Create` (`:58`, delete-before-create) — preserve.
- The mode skill picker (`ChangeModeAck` :206, `Action` :252) is gated on the elemental_db per-class skill map, which isn't loaded yet — those return 1 and stamp `LastThinkTick` without casting. That gap is a separate (skill-engine) concern, not this persistence ticket; don't expand scope into it.

## History

- 2026-06-03 · Implemented the headline missing piece — the **lifetime expiry sweep**: new
  `IElementalService.Tick(nowTick)` despawns every elemental whose `SummonExpiresAtTick` has passed
  (routed through the same teardown as `Delete` — clean SCs + registry removal + master-binding clear,
  single expiry path), hooked into `MapServerImpl`'s game loop after `_pet.Tick`. Confirmed `_summonAi`
  does NOT prune (no double-delete). `SerializeSnapshot` was already real. `ElementalServiceTests` +2;
  full suite 4357 pass (1 fail = pre-existing INFRA-11). The Create/DataReceived/Delete IPC round-trips
  + hydrated stats are a DI cycle from here (IntifService ctor-depends on IElementalService) → filed
  **FEATURE-34**; Save IPC → **FEATURE-17** fan-out; client packets → PACKET-*.
