# COMBAT-31 — Break the DamageService↔ExpService↔StatusChangeService DI cycle (Map.Server won't boot)

> **Epic:** Combat parity · **Status:** ✅ Done (2026-06-01) · **Size:** S · **Player-visible:** yes (server can't start)
> **Depends on:** none · **Blocks:** PacketReplayTests integration harness, live Map.Server boot
> **Filed by:** COMBAT-10 on 2026-06-01 (discovered while running the replay regression guard).

## Problem

`dotnet run --project Map.Server` throws at startup:

```
System.InvalidOperationException: A circular dependency was detected for the
service of type 'Map.Server.Combat.IDamageService'.
MapServerImpl -> IMobAiService(MobAiService) -> ISkillCastService(SkillCastService)
 -> SkillResolverRegistry -> IEnumerable<ISkillResolver> -> ISkillResolver(WeaponSkillResolver)
 -> IDamageService(DamageService) -> IExpService(ExpService)
 -> IStatusChangeService(StatusChangeService) -> IDamageService
```

The offending edge is **`ExpService → StatusChangeService`** (added by SC-04 so
Richmankim could read the EXP-rate SC) combined with the existing
**`StatusChangeService → DamageService`** and **`DamageService → ExpService`**
edges. .NET's `ServiceProvider` cannot construct the graph and the process dies
before it binds TCP 5191.

Confirmed **pre-existing** (reproduces on a clean tree without COMBAT-10). It was
masked in CI because the only test that boots the real server stack
(`PacketReplayTests`) also requires a live MySQL on :3306; in DB-less
environments it fails earlier at the DB-connect step and is discounted, so the
DI cycle was never surfaced. The non-integration unit suite (3732 tests) never
builds the real DI container, so it stays green.

## Current state (C#)

- `Map.Server/Status/ExpService.cs` — constructor injects `IStatusChangeService? sc`
  (SC-04). This is the new edge that closes the cycle.
- `Map.Server/Status/StatusChangeService.cs` — constructor takes `IDamageService`.
- `Map.Server/Combat/DamageService.cs` — constructor takes `IExpService` (kill-EXP
  award path).
- `Map.Server/Program.cs:~2334` — `GetRequiredService<...>()` triggers the throw.

## rAthena reference (source of truth)

N/A — this is a C#-DI composition bug, not a parity gap. rAthena resolves these
via direct function calls (no IoC container), so no cycle exists there.

## Scope — every sub-system that must be touched

- [x] **Break the named cycle** — ✅ `ExpService`'s `IStatusChangeService` is now
      `Lazy<IStatusChangeService>` (Richmankim read at `.Value` runtime). Registered
      a `Lazy<IStatusChangeService>` factory in DI. (1 test site updated.)
- [x] **Break the second (mutual) cycle** — ✅ booting revealed a direct
      `DamageService ↔ StatusChangeService` cycle too (DamageService gained an
      `IStatusChangeService sc` in SC-04/SC-08; StatusChangeService takes
      `IDamageService` for DoT). Made DamageService's `sc` lazy via a `_scLazy`
      field + `_sc` computed property (every existing `_sc.X` call site unchanged).
      3 test rigs updated. Chose the DamageService side (7 candidate sites) over
      StatusChangeService's `damage` (21 sites).
- [x] **Break the third (skill) cycle** — ✅ then revealed
      `SkillBehaviorRegistry → SkillImpl(HolyLight) → SkillAttackService →
      SkillBehaviorRegistry`. Made `SkillAttackService`'s `behaviors` lazy (same
      `_behaviorsLazy` + property trick; 1 test site) rather than touching the 129
      plugins that depend on `ISkillAttackService`. Registered a
      `Lazy<SkillBehaviorRegistry>` factory.
- [x] **Verify all services resolve** — ✅ `dotnet run --project Map.Server` boots
      and binds TCP 5191 in ~6s with zero circular-dependency exceptions; the lazy
      accessors are non-null at first use (singletons fully built by then).

## Done criteria

- ✅ `dotnet run --project Map.Server` boots and binds TCP 5191 — verified, no
  circular-dependency exception (was: died at construction on a clean tree).
- ◑ `PacketReplayTests` now boots **past** the Map server (binds 5191); the
  remaining timeout is the Login/Char internal-ping **readiness handshake**
  (`WaitForServerReadyAsync` ping → `ZC_INTERNAL_PONG`), a separate integration
  gate unrelated to the DI cycle (Login/Char/Map each boot standalone — Login 8s,
  Map 6s). ➡️ **INFRA-11**.
- ✅ Richmankim EXP / kill-EXP / SC-triggered damage paths still work — SC-04 /
  SC-08 / ExpService unit tests green (full unit suite 3732 passed).

## Test plan

- Add a DI smoke test that builds the Map.Server service provider and resolves
  `IDamageService`, `IExpService`, `IStatusChangeService`, `IMobAiService` without
  throwing (mirrors the real composition root).
- Re-run `PacketReplayTests` against a live MariaDB to confirm the stack boots.

## Notes / gotchas

- `IStatusChangeService? sc = null` as an optional constructor param does NOT make
  .NET DI skip it — if the service is registered, the container injects it and the
  cycle forms. Optionality must be expressed via `Lazy<>`/`Func<>`/`IServiceProvider`.
- Check for OTHER latent cycles once this one is broken (the container reports only
  the first it hits).

## History

- 2026-06-01 · Broke three accumulated DI cycles that prevented Map.Server from
  booting (each surfaced after fixing the prior — the container reports only the
  first). (1) ExpService→StatusChangeService made lazy (Lazy<IStatusChangeService>
  + DI factory). (2) DamageService↔StatusChangeService mutual cycle: DamageService's
  `sc` made lazy via a `_scLazy` field + `_sc` computed property (all 30 call sites
  unchanged). (3) SkillBehaviorRegistry↔SkillAttackService (via HolyLight plugin):
  SkillAttackService's `behaviors` made lazy (same property trick) + DI factory.
  Map.Server now boots and binds TCP 5191 in ~6s, zero circular-dependency
  exceptions (verified by launching the process; reproduced the original throw on a
  clean stash first). 4 test rigs wrapped their sc/behaviors arg in Lazy; unit suite
  3732 green. The replay E2E now boots past the Map server but hits the Login/Char
  internal-ping readiness handshake → filed INFRA-11.
