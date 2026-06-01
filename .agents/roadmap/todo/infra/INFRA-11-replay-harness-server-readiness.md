# INFRA-11 — PacketReplayTests: Login/Char internal-ping readiness gate

> **Epic:** Infra / test harness · **Status:** ❌ Not started · **Size:** M · **Player-visible:** no
> **Depends on:** COMBAT-31 (Map.Server boot) · **Blocks:** the replay E2E regression guard
> **Filed by:** COMBAT-31 on 2026-06-01 (the next gate the harness hits after the Map DI cycle was fixed).

## Problem

`PacketReplayTests` boots the full server stack (Login + Char + Map as real
`dotnet --no-build` processes against a live MariaDB) via `ServerStackFixture`.
After COMBAT-31 fixed the Map.Server DI cycle, the harness now boots the Map
server (binds 5191) but fails at:

```
System.TimeoutException : Login server (port 6900) did not report ready within 30s.
  ServerStackFixture.WaitForServerReadyAsync (line 168)
```

`WaitForServerReadyAsync` (ServerStackFixture.cs:131) is a protocol-level
readiness probe: it connects, writes `PingBytes`, and expects a 3-byte
`ZC_INTERNAL_PONG` (header + `1`). The Login server **binds** 6900 (confirmed:
boots standalone in ~8s) but does not complete this internal ping/pong handshake
within the 30s window under the harness's concurrent multi-process launch.

So the replay E2E regression guard still cannot run to completion in this
environment — but the failure is now an integration-orchestration / readiness
concern, NOT a DI cycle (Map.Server boots cleanly; Login/Char boot standalone).

## Current state (C#)

- `Map.Server.Tests/Replay/ServerStackFixture.cs:131` — `WaitForServerReadyAsync`
  ping/pong probe (Login 30s, Char 30s, Map 120s windows).
- `:189 StartAsync` — launches each server with `dotnet --no-build`, then
  `WaitForTcpAsync(port, 30s)` (TCP bind only).
- Login/Char must answer the internal-ping packet with `ZC_INTERNAL_PONG` for
  the probe to pass. Verify each server actually registers that handler and that
  the response shape matches `PingBytes`/`ZC_INTERNAL_PONG`.

## Scope — every sub-system that must be touched

- [ ] Determine why Login does not return `ZC_INTERNAL_PONG` within 30s: is the
      internal-ping handler missing/registered late, is it gated on inter-server
      gRPC registration (Char↔Login) that hasn't completed, or is it pure
      multi-process startup contention in CI?
- [ ] If a handler is missing/late, wire the internal ping → pong on Login + Char
      (mirror whatever Map does — Map's 120s window suggests it answers).
- [ ] If it's contention/timing, raise the readiness budget and/or serialize
      server launches, and confirm `--no-build` artifacts are pre-built before the
      fixture runs.
- [ ] Make the harness skip cleanly (Skip, not Fail) when no MariaDB is reachable
      so DB-less unit runs don't report a red.

## Done criteria

- `PacketReplayTests` boots all three servers and runs the recorded fixture
  (`dhxj.log`) to its assertions against a live MariaDB.
- DB-less environments report the replay test as Skipped, not Failed.

## Test plan

- Run `docker compose up -d` + `dotnet ef database update` + `dotnet test
  --filter PacketReplayTests` → green.

## Notes / gotchas

- COMBAT-31 verified the three servers each boot standalone (Login 8s, Map 6s)
  and bind their TCP ports, so this is specifically about the ping/pong readiness
  handshake + multi-process orchestration, not server boot.
