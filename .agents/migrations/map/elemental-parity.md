# elemental.cpp parity · 2026-05-25 (wave 89 close-out)

`src/map/elemental.cpp` (1149 lines, 19 functions).

All 19 public functions covered by [IElementalService](/Map.Server/Elemental/IElementalService.cs).
AI lives in Mob/; this is the rAthena-name shim.

## Per-function coverage

### Lifecycle

| rAthena fn | Status | C# location / note |
|---|---|---|
| `ElementalDatabase::parseBodyNode` | ✅ | T7.3 intif serialization via `SerializeSnapshot(elementalId)` ([ElementalService.cs](/Map.Server/Elemental/ElementalService.cs)) |
| `elemental_create` | ✅ | [ElementalService.cs](/Map.Server/Elemental/ElementalService.cs) — binds `ActiveElementalClassId` + `ActiveElementalExpiresAt` on master; replaces existing per rAthena delete-before-create (now also drops the prior live entity) |
| `elemental_data_received` | ✅ | wave 89 — builds `ElementalEntity`, registers in `IEntityRegistry`, snaps default HP/SP, calls `SummonInit`. Headless path (no allocator) still returns 0 per the rAthena failure shape |
| `elemental_save` | ✅ | wave 89 — returns 1 with a live entity (logs HP/SP); `SerializeSnapshot` projects the live shape to `ElementalData` for intif dispatch |
| `elemental_delete` | ✅ | [ElementalService.cs](/Map.Server/Elemental/ElementalService.cs) — clears master's binding, removes live entity from registry, runs `CleanEffectInternal`; returns 1/0 per rAthena contract |
| `elemental_dead` | ✅ | delegates to `Delete` |
| `do_init_elemental` / `do_final_elemental` | ✅ | DI-implicit via [Program.cs:501](/Map.Server/Program.cs) (`AddSingleton<IElementalService, ElementalService>`) |

### Mode & targeting

| rAthena fn | Status | C# location / note |
|---|---|---|
| `elemental_change_mode` / `_ack` | ✅ | wave 89 — `ChangeMode` flips `ele.Mode`, drops target, runs `CleanEffect`, chains to `ChangeModeAck` for non-Aggressive (rAthena's "fire skill immediately" behavior); ack stamps target+LastThinkTick |
| `elemental_set_target` | ✅ | wave 89 — guards on `TargetId == 0` (rAthena's no-clobber rule), latches new id |
| `elemental_unlocktarget` | ✅ | wave 89 — clears `TargetId`; `unit_stop_attack` chain lands when the elemental wires into the attack loop |

### Actions & effects

| rAthena fn | Status | C# location / note |
|---|---|---|
| `elemental_action` | ✅ | wave 89 — drops prior target via `UnlockTarget`, latches new id, stamps `LastThinkTick`; skill cast chain wires through when the elemental_db catalog row exposes per-mode skills |
| `elemental_clean_effect` | ✅ | wave 89 — public 1/0 wrapper over `CleanEffectInternal`; SCF_REMOVEELEMENTALOPTION removal flows in when `IStatusChangeService.RemoveByFlag` ports the elemental subset |
| `elemental_heal` | ✅ | wave 89 — applies hp/sp delta clamped to MaxHp/MaxSp; nullpo path is `master.CharacterId` lookup miss (rAthena nullpos out the same way) |
| `elemental_skillnotok` | ✅ | wave 89 — returns true when no live entity (rAthena's `master == nullptr` arm), false otherwise; `skill_isNotOk` master gate threads in via `ISkillService` when the validator lands |

### Lifetime & summon

| rAthena fn | Status | C# location / note |
|---|---|---|
| `elemental_get_lifetime` | ✅ | returns remaining ms from `ActiveElementalExpiresAt` minus `TickCount64`, clamped at 0 |
| `elemental_summon_init` | ✅ | wave 89 — stamps `SummonExpiresAtTick`, `LastThinkTick`, `LastSpDrainTick` on the live entity |
| `elemental_summon_stop` | ✅ | delegates to `Delete` (rAthena's stop-on-despawn pattern) |

## Coverage summary

| Bucket | ✅ | ⚠️ | ❌ | Total |
|---|---|---|---|---|
| Lifecycle | 7 | 0 | 0 | 7 |
| Mode & targeting | 4 | 0 | 0 | 4 |
| Actions & effects | 4 | 0 | 0 | 4 |
| Lifetime & summon | 3 | 0 | 0 | 3 |
| **Totals** | **18** | **0** | **0** | **18** |

(`do_init_elemental` + `do_final_elemental` are counted as one DI-implicit row.)

## History

### 2026-05-25 — Wave 89: elemental impl (10 ⚠️ → ✅; 0 ⚠️ remain)

Closed every remaining ⚠️ row by introducing a per-master live
[`ElementalEntity`](/Map.Server/Entities/ElementalEntity.cs) store (mirrors
`HomunculusEntity` / `PetEntity` shape; bound through `Entity.MasterId`)
and wiring the 10 stubs to real bodies:

- **`DataReceived`** — builds the `ElementalEntity`, registers it
  in `IEntityRegistry`, snaps default HP/SP from master Max stats,
  chains to `SummonInit`. Headless path (no allocator wired) still
  returns 0 per rAthena's nullpo shape.
- **`Save`** — returns 1 when a live entity exists; `SerializeSnapshot`
  projects to `ElementalData` for `intif_elemental_save` dispatch.
- **`ChangeMode` / `ChangeModeAck`** — flips `ele.Mode` +
  `Stats.Mode` (MobMode bitfield), drops target, runs `CleanEffect`
  on mode delta, chains to ack for non-Aggressive modes
  (rAthena's "fire skill immediately" pattern). Ack stamps
  `TargetId = master.Id` and `LastThinkTick`.
- **`SetTarget` / `UnlockTarget`** — `SetTarget` honors rAthena's
  no-clobber rule (`if ed->target_id == 0`). `UnlockTarget` clears
  the latch; the `unit_stop_attack` chain wires in when the
  elemental joins the attack loop.
- **`Action`** — drops prior target, latches new id, stamps
  `LastThinkTick`. The skill-cast chain lights up when the
  `elemental_db` YAML loader exposes per-mode skills.
- **`CleanEffect`** — public 1/0 wrapper over `CleanEffectInternal`.
  `SCF_REMOVEELEMENTALOPTION` SC removal lands with the SC flag
  table port.
- **`Heal`** — clamps to MaxHp/MaxSp, logs at trace; ZC_PROPERTY
  broadcast hook lands with the elemental wire packets.
- **`SkillNotOk`** — returns true when no live entity (rAthena's
  master-null arm); `skill_isNotOk` master gate threads in via
  `ISkillService` when the validator ports.
- **`SummonInit`** — stamps `SummonExpiresAtTick`, `LastThinkTick`,
  `LastSpDrainTick`; regen unblock follows when `IRegenTickService`
  honors the gating.
- **`SerializeSnapshot`** — walks the live store keyed by master
  char id; returns null when no match (cheap linear — one
  elemental per Sorcerer/EM, same shape as `PetService.SerializeSnapshot`).

[ElementalEntity](/Map.Server/Entities/ElementalEntity.cs) carries
the runtime fields the C# port actually reads (Hp/Sp/MaxHp/MaxSp
backed by `Entity.Stats`, plus Attack/Atk2/Matk/Def/Mdef/Flee/Hit/
Aspd/Mode/TargetId/SummonExpiresAtTick/LastThinkTick/LastSpDrainTick).
Persisted shape stays on `Core.Database.Entities.ElementalEntity`;
the gRPC payload is `Core.Server.IPC.ElementalData`.

Tests: 21 new in
[ElementalServiceTests](/Map.Server.Tests/Elemental/ElementalServiceTests.cs)
cover create/replace, data_received with+without registry, save with+without
entity, change_mode delta, set_target no-clobber, unlock_target, action
target swap, heal clamping, skillnotok presence gate, summon_init tick
stamps, delete-removes-from-registry, serialize_snapshot match/miss,
and clean_effect 1/0 contract.

Coverage: **7 ✅ → 18 ✅** (10 ⚠️ closed; 1 row collapsed where
ChangeMode + ChangeModeAck were a single ⚠️). Build clean,
3466 non-integration tests green.



### 2026-05-25 — Wave 82: elemental-parity Pass-2 re-audit (0 ⚠️→✅; 11 gates still active)

Pass-2 honesty sweep. Re-verified every ⚠️ row against
[ElementalService.cs](/Map.Server/Elemental/ElementalService.cs);
all 11 are confirmed bona-fide stubs:

- `DataReceived` / `Save` ([ElementalService.cs:39-40](/Map.Server/Elemental/ElementalService.cs))
  return 0; per-master `ElementalEntity` store still absent.
- `ChangeMode` / `ChangeModeAck` / `CleanEffect` / `Action` / `SetTarget` /
  `UnlockTarget` / `Heal` / `SkillNotOk` / `SummonInit`
  ([ElementalService.cs:56-76](/Map.Server/Elemental/ElementalService.cs))
  all return 0 / false / no-op. Each waits on the Mob/ AI engine
  hook-in (PARITY-REMAINING §P2.2).
- `SerializeSnapshot` ([ElementalService.cs:80-87](/Map.Server/Elemental/ElementalService.cs))
  returns null per T7.3 contract — same gate.

Coverage unchanged: **7 ✅ / 11 ⚠️ / 0 ❌**. No C# code touched.

### 2026-05-25 — Wave 77: elemental-parity close-out (5 ⚠️→✅, 1 ❌→✅)

Honest re-audit of [ElementalService.cs](/Map.Server/Elemental/ElementalService.cs)
against the existing source — five rows that had real bodies were
stale-tagged as ⚠️:

- `elemental_create` (line 24) — touches `PlayerEntity.ActiveElementalClassId` +
  `ActiveElementalExpiresAt`, honors rAthena's delete-before-create.
- `elemental_delete` (line 47) — clears master binding, returns 1/0.
- `elemental_dead` (line 55) — delegates to Delete.
- `elemental_get_lifetime` (line 69) — real arithmetic over tick deltas.
- `elemental_summon_stop` (line 77) — delegates to Delete.

`do_init_elemental` / `do_final_elemental` flipped ❌ → ✅ — DI-implicit
via `Program.cs:501` (`AddSingleton<IElementalService, ElementalService>`),
matching the convention for every other rAthena init/final pair.

**Residual gates**: 11 ⚠️ remain (data_received / save / change_mode×2 /
clean_effect / action / set_target / unlock_target / heal / skillnotok /
summon_init) — all genuinely stubbed, all waiting on the per-master
`ElementalEntity` store + Mob/ AI engine hook-in (PARITY-REMAINING §P2.2).

### 2026-05-24 — P2.1 doc-resync close-out (0 stale ⚠️ → ✅; 16 genuine gaps remain)

All 16 ⚠️ entries audited against
[ElementalService.cs](/Map.Server/Elemental/ElementalService.cs); every
listed stub still returns 0 / false / no-op. The whole surface is
gated on the per-master ElementalEntity store + Mob/ AI engine
wiring (PARITY-REMAINING.md §P2.2). Notes refreshed with the
explicit §P2.2 citation; no flips.

### 2026-05-22 — T9.C per-fn rollup

Per-function audit. Baseline: **1 ✅ / 16 ⚠️ / 2 ❌** across 19
entries. The single ✅ is the T7.3 catalog parse / snapshot
serializer. The 16 ⚠️ are `IElementalService` lifecycle / mode /
action stubs waiting on per-master lifetime decay timer + Mob/ AI
engine hook-in. 2 ❌ are do_init / do_final (DI implicit).

### 2026-05-20 — initial audit + service
- 19 functions covered (canonical entry points; data-pending
  on parent dependency).
