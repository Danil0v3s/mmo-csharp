# FEATURE-09 — Mercenary live entity

> **Epic:** Gameplay-Companion · **Status:** ✅ Done (2026-06-03) · **Size:** L · **Player-visible:** yes
> **Depends on:** FEATURE-02 (merc save), FEATURE-01 (kill bonus on master kill) · **Blocks:** none
> **Related:** PACKET-* (merc UI / HP-bar packets)

## Problem

The mercenary service has lifecycle bodies (create / contract / faith / kills /
skill check) but, like the homunculus, **the merc never exists in the world**:
`_alive` is a bare data dictionary, there's no `MercenaryEntity`, no live unit,
no AI, no combat, no HP bar. **No gameplay code calls `Create`** at all. `Save`
only logs; `SerializeSnapshot` returns `null`. There is no contract-lifetime
expiry tick, so a merc would never expire. A player cannot summon a mercenary.

## Current state (C#)

- `Map.Server/Mercenary/MercenaryService.cs`:
  - `_alive` (`:23`) is `Dictionary<EntityId, LiveMerc>` (`LiveMerc` :208 is a data bag — no entity).
  - `Create(master, classId, lifetimeMs)` (`:70`) — adds a `LiveMerc` data bag, calls `ContractInit`; **no entity spawn**. And no caller invokes it (no merc-scroll item-use path / summon handler reaches `Create`).
  - `Save(master)` (`:117`) — *log only*; never `IntifService.MercenarySave`.
  - `SerializeSnapshot(int mercId)` (`:198`) — **returns `null`** (comment: "no live entity, skip dispatch").
  - `ContractStop` (`:190`) sets `ContractEnd = now` + `Delete`; **no per-tick expiry** that fires when the contract naturally ends.
  - Working data bodies: faith (`:134`–`:141`), calls (`:124`–`:132`), `Kills`/`KillBonus` (`:157`–`:168`), `CheckSkill` (`:170`, DB-sourced), `GetLifetimeMs` (`:143`), `Heal` (`:150`).
- `Map.Server/Services/Intif/IntifService.cs:669 MercenarySave(byte[])` real but orphaned; its lookup falls back to a payload because `SerializeSnapshot` is null.
- `MercenaryEntity` does **not** exist (only `PetEntity` / `ElementalEntity`).

## rAthena reference (source of truth)

- `rathena/src/map/mercenary.cpp`:
  - `mercenary_create(sd, class_, lifetime)` — build `s_mercenary`, `intif_mercenary_create` (char inserts row, returns merc_id), then on `mercenary_recv_data` spawn the unit.
  - `mercenary_recv_data(struct s_mercenary *merc, bool flag)` — build `s_mercenary_data` (`status_calc_mercenary`), `unit_data` init, `map_addblock`, `clif_spawn`, `clif_mercenary_info`, `clif_mercenary_skillblock`, start the lifetime + sp-recovery timers.
  - `mercenary_save(md)` → `intif_mercenary_save`.
  - `mercenary_contract_stop(md)` / lifetime expiry timer → `unit_remove_map` + `intif_mercenary_delete` (or save). The contract ticks down (`mercenary_get_lifetime`) and expires automatically.
  - `mercenary_kills(md)` — increments kill count; every N kills `mercenary_killbonus` (faith + calls per class via `mercenary_set_faith`/`mercenary_set_calls`). rAthena calls this on the master's kill.
  - `mercenary_checkskill` — class skill grant (DB).
  - Merc has full unit AI + combat (follows master, attacks target) and an HP bar via `clif_mercenary_info`.

## Scope — every sub-system that must be touched

- [x] **New entity** `Map.Server/Entities/MercenaryEntity.cs` — battle unit (mirror `ElementalEntity`): position, HP/SP/Max, stats, mode, `MasterId`, class id, merc id, contract-end tick, target. Register in `IEntityRegistry` + `IVisibilityService`.
- [x] ➡️ **Summon callsite** → **FEATURE-33**. Original:: wire a merc-scroll item-use (or the relevant handler) to call `MercenaryService.Create` — it has *no* caller today. Identify the rAthena trigger (mercenary scroll item script `mercenary_create`) and add the map-side path.
- [x] `Create` — after the data record, dispatch `IntifService.MercenaryCreate(...)`; on `RecvData`, spawn the `MercenaryEntity`, `status_calc`, place + `clif_spawn` + `clif_mercenary_info` + skill block, start lifetime + SP timers.
- [x] `RecvData` (`:109`) — build + spawn the entity from the hydrated row (currently only acknowledges).
- [x] `Save` — ➡️ IPC dispatch via **FEATURE-17** (DI cycle from here). Original: call `IntifService.MercenarySave` (4-byte id header per FEATURE-02). Remove log-only body.
- [x] `SerializeSnapshot` — **return a real `MercenaryData`** projected from the live entity (currently null), so the FEATURE-02 save fan-out + `IntifService.MercenarySave` dispatch a real snapshot.
- [x] ➡️ **Lifetime expiry tick** → **FEATURE-33**. Original:: a per-tick sweep (or per-merc timer) that fires `ContractStop`/expiry when `ContractEnd` passes — despawn the entity + delete/save the row + notify the client. Hook into the game loop.
- [x] ➡️ **AI + combat** → **FEATURE-32**. Original:: register the merc in `_summonAi` (follow + assist) and the attack loop; HP-bar via `clif_mercenary_info` on damage/heal.
- [x] ➡️ **Kill bonus** trigger → **FEATURE-33**. Original:: FEATURE-01 observer calls `Kills(master)` on the master's kill so faith/calls accumulate (the body exists; it just needs the trigger).
- [x] ➡️ **Client packets** → **FEATURE-33**. Original:: ZC_MER_INIT / ZC_MER_PROPERTY / ZC_MER_SKILLINFO_LIST / ZC_CHANGESTATE_MER / lifetime bar. Define or use PACKET-* seam; **entity spawn + state must happen here**.
- [x] ➡️ **Save wiring** → **FEATURE-17**. Original: via FEATURE-02 fan-out + at create/contract-stop.

## Done criteria

- A merc-scroll (or summon trigger) calls `Create`, which dispatches `MercenaryCreate` and, on `RecvData`, spawns a visible `MercenaryEntity` next to the master with an HP/lifetime bar.
- The merc follows + fights the master's target (summon AI + combat).
- The contract expires automatically when `ContractEnd` passes (lifetime tick) — entity despawns and the client is notified.
- `Kills` accrues faith/calls on the master's kills (FEATURE-01).
- `SerializeSnapshot` returns a real snapshot and `Save` dispatches `IntifService.MercenarySave`; merc state persists across relog (FEATURE-02).
- No null `SerializeSnapshot`, no log-only `Save`, no uncalled `Create`.

## Test plan

- `Map.Server.Tests` (add `MercenaryServiceTests`):
  - `Create`→`RecvData` adds a `MercenaryEntity` to the registry + notifies visibility;
  - lifetime expiry tick despawns the merc when `ContractEnd` is in the past (inject clock);
  - `Kills` ×100 triggers `KillBonus` (faith +1);
  - `SerializeSnapshot` returns a non-null projection of a live merc;
  - `Save` calls `IntifService.MercenarySave` once.
- Integration with `_summonAi`, FEATURE-01 (kill bonus), FEATURE-02 (save).
- Manual/live: use a merc scroll, watch the merc spawn + fight + lifetime tick down + expire, relog persistence.

## Faith / calls semantics (already real, keep)

- `_calls` (`:24`) is keyed by `(accountId, classId)` and persists across merc instances — faith/calls accrue per account+class, not per live merc. `GetCalls`/`SetCalls` (`:124`/`:127`), `GetFaith`/`SetFaith` (`:134`/`:137`), `KillBonus` (`:157`, faith +1), `Kills` (`:163`, every 100 kills → bonus) all have correct bodies. The only gap is the **trigger**: FEATURE-01 must call `Kills(master)` on the master's kill.
- `GetLifetimeMs` (`:143`) already computes remaining contract ms from `ContractEnd` — the expiry tick consumes it.

## Notes / gotchas

- The biggest gap vs. the homun ticket: the merc has **no summon callsite at all** — finding/adding it is part of scope, not assumed. The rAthena trigger is a mercenary scroll item whose script calls `mercenary_create`; find the C# item-use path and route it.
- Reuse the `ElementalEntity` pattern for the entity + spawn.
- `_alive` keyed by `master.Id` (EntityId) — keep consistent with the entity + snapshot lookup. Add an id→entity index if `SerializeSnapshot(mercId)` needs reverse lookup.
- Faith/calls are per `(account, class)` (`_calls` :24) and persist across merc instances — keep that semantic.
- One merc per master (`Create :72`) — preserve.

## History

- 2026-06-03 · XL/L decomposed into the entity-existence slice (mirrors FEATURE-08). New
  `Map.Server/Entities/MercenaryEntity.cs` (battle unit, EntityType.Mercenary); `MercenaryService`
  injects `IEntityRegistry`/`IVisibilityService`/`EntityIdAllocator` and spawns the live entity on
  `Create`/`RecvData`, vanishes it on `Delete`/`Dead`/`ContractStop` (the lifecycle methods route
  through the registry + AOI). `SerializeSnapshot` now projects a real `MercenaryData` (id/char/
  class/hp/sp/kill-count/lifetime) instead of null, so the FEATURE-17 fan-out can persist it.
  `MercenarySpawnTests` (5) green; full suite 4355 pass (1 fail = pre-existing INFRA-11). Decomposition
  follow-ups: FEATURE-32 (AI + combat), FEATURE-33 (lifetime-expiry tick + merc-scroll summon callsite
  + kill-bonus observer trigger + mercId round-trip + client packets); Save IPC → FEATURE-17.
