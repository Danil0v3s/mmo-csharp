# Project: mmo-csharp

A C# rewrite of an rAthena-style MMO server stack. Four .NET 8 processes (Login, Char, Map, Web), gRPC between servers, MySQL/MariaDB for persistence. Migration is in progress — many design decisions are driven by 1:1 parity with the original C++ implementation under `rathena/` (not in this repo).

## Layout

| Path | Role |
|---|---|
| [Login.Server/](Login.Server) | Authentication, account state, IP bans, char-server registry |
| [Char.Server/](Char.Server) | Character list / create / select, pincode, map handoff |
| [Map.Server/](Map.Server) | Real-time gameplay loop (60 FPS) |
| [Web.Server/](Web.Server) | REST API (ASP.NET Core), Swagger at `:5000/swagger` |
| [Core.Server/](Core.Server) | Shared TCP / packet / IPC infrastructure |
| [Core.Database/](Core.Database) | EF Core entities, repositories, migrations |
| [Core.Timer/](Core.Timer) | Timer abstractions |
| [Tools.LoginTcpClient/](Tools.LoginTcpClient) | CLI smoke-test client |
| `Core.Server.Tests/`, `Char.Server.Tests/` | xUnit tests |

## Architecture

System overview, ports, FPS, packet format, class hierarchy, run instructions:

@Architecture.md

## Inter-server IPC

gRPC connection management, health checks, DI surface, proto layout:

@Ipc.md

## Database

EF Core setup, migration commands, repository injection pattern:

@Core.Database/MIGRATIONS.md
@Core.Database/USAGE_EXAMPLES.md

## rAthena parity status

**The active worklist is [.agents/roadmap/](.agents/roadmap/README.md)** — one
self-contained development ticket per work item, re-baselined 2026-06-01 from a
full code-vs-rAthena scan. Read the roadmap README's "Honest ground truth" table
first: prior "100% parity" claims measured per-function code presence, not working
features. Real remaining surface includes the client→map packet bridge (only ~39
`CZ_*` handlers), gameplay-subsystem behavior + persistence wiring, combat formula
depth (cards/skill-ratios/`RE_LVL_DMOD`), the SC-engine magnitude gaps, and the
NPC scripting runtime (~3% of builtins live). When you finish a ticket, flip its
Status header and append a History line.

- [.agents/roadmap/README.md](.agents/roadmap/README.md) — **canonical** ticket index + ground truth
- [.agents/migrations/](.agents/migrations/README.md) — **archive/reference only** (rAthena citations + history; status columns are NOT authoritative)
  - `char/` client packets + gRPC + connect flow (this layer is solid)
  - `login/` login feature inventory · `inter/` `int_*.cpp` routing · `map/*-parity.md` per-`.cpp` function refs

## Conventions

- **Parity first.** When porting an rAthena handler, match validation gates, state transitions, and failure modes exactly — not "what makes sense in C#." Diverging behavior is a bug, not a feature.
- **No in-memory shortcuts for persisted state.** Party, guild, mail, auction, quest, pet, etc. all go through `GameDbContext` / repositories. The previous round of work removed `ConcurrentDictionary`-backed stubs; don't reintroduce them.
- **Packet handlers** discover via the `[PacketHandler]` attribute + `IPacketHandler<TSession, TPacket>` interface. New packets need a definition in [Core.Server/Packets](Core.Server/Packets) and a handler in the relevant server's `Handlers/` folder.
- **IPC consumers** depend on `IServerConnectionService` (not `ServerConnectionManager` directly) so they're DI-mockable. Typed wrappers like `LoginServerIpcService` / `CharServerIpcService` encapsulate channel selection.
- **Repositories** are injected directly (`ICharacterRepository`, etc.). There is no `IUnitOfWork`; use `GameDbContext.SaveChangesAsync()` or `BeginTransactionAsync()` for multi-step writes.
- **Logging** uses `Microsoft.Extensions.Logging.ILogger<T>`. Log at info for lifecycle, warn for recoverable issues, error for unexpected failures.
- **Threading.** Game state is single-threaded per server. Only the socket I/O boundary and IPC layer are concurrent. Don't introduce locks in handler code — queue work to the game loop instead.

## Testing

```
dotnet test                                      # all tests
dotnet test Char.Server.Tests                    # char-server suite only
dotnet test Core.Server.Tests
```

When adding char-server behavior with parity implications, add a regression test in `Char.Server.Tests/` (handler test or `Services/*ParityTests.cs`).
