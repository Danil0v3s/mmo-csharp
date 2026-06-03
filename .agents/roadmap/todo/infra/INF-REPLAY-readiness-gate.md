# INF-REPLAY — Replay-harness readiness gate (test infra)

> **Epic:** infra · **Status:** ❌ Not started · **Size:** S · **Player-visible:** no
> **Depends on:** none · **Unlocks:** the E2E replay tests (the standing 1 fail)

## The deliverable

> The `PacketReplayTests` / `ServerStackFixture` E2E harness boots the Login/Char/Map stack and
> gets past the Login internal-ping readiness handshake, so the end-to-end replay tests pass
> (this is the long-standing "1 fail = pre-existing INFRA-11" in every suite run).

## What this absorbs (archive)

- `_archive/todo/infra/INFRA-11` — PacketReplayTests Login/Char internal-ping readiness gate.

## rAthena reference

- n/a (test-infra) — the gate is the Login↔Char internal-ping handshake the fixture waits on.

## Scope

- [ ] Make `ServerStackFixture.WaitForTcpAsync`/`StartAsync` wait for the real readiness signal
      (the internal-ping handshake completing), not just the TCP port binding, so the Map stack is
      actually ready before the replay runs.

## Done criteria

- `dotnet test Map.Server.Tests` is fully green (0 fail) — the `ServerStackFixture` E2E replay no
  longer fails on readiness.

## Test plan

- The replay E2E test goes green; the rest of the suite unaffected.

## Notes

- Parallel, test-only. This is the single recurring failure noted in every recent History line.
