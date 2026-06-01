# COMBAT-31 — Break the DamageService↔ExpService↔StatusChangeService DI cycle (Map.Server won't boot)

> **Epic:** Combat parity · **Status:** ❌ Not started · **Size:** S · **Player-visible:** yes (server can't start)
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

- [ ] Break ONE edge of the cycle without losing behavior. Preferred: make
      `ExpService`'s dependency on `IStatusChangeService` lazy — inject
      `Func<IStatusChangeService>` / `Lazy<IStatusChangeService>` (or resolve via
      `IServiceProvider` at call time) so the constructor graph is acyclic. The
      Richmankim EXP read (`ExpService` SC-04) happens at runtime, not
      construction, so lazy resolution is behavior-preserving.
- [ ] Alternatively, break `DamageService → ExpService` the same way (kill-EXP is
      also a runtime call), or `StatusChangeService → DamageService`. Pick the
      edge with the fewest call sites; document why.
- [ ] Verify all three services still resolve and the chosen lazy accessor is
      non-null at first use.

## Done criteria

- `dotnet run --project Map.Server` boots and binds TCP 5191 (no circular-dependency
  exception).
- `PacketReplayTests` runs past `ServerStackFixture.StartAsync` (given a live DB).
- Richmankim EXP bonus + kill-EXP award + SC-triggered damage paths still work
  (existing SC-04 / ExpService tests stay green).

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
