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

Living migration tracking lives in [.agents/migrations/](.agents/migrations/), organized by server. Start with the [README](.agents/migrations/README.md) for the overall status table. When you finish a migration unit, append a History entry to the relevant doc in the same commit.

- [.agents/migrations/README.md](.agents/migrations/README.md) — index + status at a glance
- [.agents/migrations/char/](.agents/migrations/char/) — client packets, gRPC server, connect flow
- [.agents/migrations/login/](.agents/migrations/login/) — login server feature inventory
- [.agents/migrations/map/](.agents/migrations/map/) — map-side IPC integration (largest open surface)
- [.agents/migrations/inter/](.agents/migrations/inter/) — `inter.cpp` base + `int_*.cpp` modules

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
