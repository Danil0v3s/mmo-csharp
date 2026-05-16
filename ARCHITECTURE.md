# Architecture

C# port of an rAthena-style MMO server. Four processes communicate over TCP (client-facing) and gRPC (server-to-server).

## Process layout

| Server | TCP port | gRPC port | Target FPS | Heartbeat timeout | Project |
|---|---|---|---|---|---|
| Login  | 6900 | 6001 | 20 | 30 s | `Login.Server` |
| Char   | 6121 | 6002 | 20 | 30 s | `Char.Server` |
| Map    | 5191 | 6003 | 60 | 15 s | `Map.Server` |
| Web    | 5000 | —    | —  | —    | `Web.Server` |

Ports and timeouts live in each project's `appsettings.json` under `Server.*`. The Web server has no game loop and no client TCP socket — it's an ASP.NET Core REST API (Swagger at `http://localhost:5000/swagger`).

MariaDB is provisioned by [docker-compose.yml](docker-compose.yml) on `:3306`; the C# processes are launched manually (`dotnet run --project <Server>`).

## Class hierarchy

```
IServer                                    Core.Server/IServer.cs
  └── AbstractServer                       Core.Server/AbstractServer.cs
        ├── GameLoopServer (abstract)      Core.Server/GameLoopServer.cs
        │     ├── LoginServerImpl
        │     ├── CharServerImpl
        │     └── MapServerImpl
        └── WebServerImpl
```

`AbstractServer` owns lifecycle, logging, configuration, and the IPC client. `GameLoopServer` adds the fixed-FPS tick loop, heartbeat checks, and packet pumps.

## Client TCP protocol

Packets are length-prefixed binaries originating from the rAthena packet set.

- **Fixed-length packet:** `[2 bytes: packet id][body…]` where the body length is derived from the registered packet size.
- **Variable-length packet:** `[2 bytes: packet id][2 bytes: total size][body…]`.

Packet definitions live in [Core.Server/Packets](Core.Server/Packets). Registered sizes for variable packets come from [Core.Server/Packets/appsettings.packets.json](Core.Server/Packets/appsettings.packets.json).

Heartbeats:
- **Map ↔ client:** `CZ_HEARTBEAT = 0x0360`, fixed-length, no body. See [Core.Server/Packets/In/CZ_HEARTBEAT.cs](Core.Server/Packets/In/CZ_HEARTBEAT.cs).
- **Char ↔ client:** `CH_KEEP_ALIVE = 0x0187`, fixed-length, account-id payload. Handled by [Char.Server/Handlers/CharKeepAliveHandler.cs](Char.Server/Handlers/CharKeepAliveHandler.cs).

The server drops the connection if no heartbeat arrives within the per-server `HeartbeatTimeout` window.

### I/O model

Each `ClientSession` runs an async `ReceiveLoopAsync` that reads from the socket, reassembles packets via [PacketBuffer](Core.Server/Network/PacketBuffer.cs) (`ArrayPool<byte>`-backed, zero-copy with `Memory<byte>` / `Span<byte>`), and enqueues `IncomingPacket` to a `ConcurrentQueue`. The game-loop thread drains that queue per tick and dispatches to handlers via [PacketHandlerRegistry](Core.Server/Network/PacketHandlerRegistry.cs). Outgoing packets are queued per session and flushed at end-of-tick. Game state stays single-threaded; only the I/O boundary is concurrent.

## Inter-server communication

All cross-server calls go over gRPC. See [Ipc.md](Ipc.md) for the connection manager, health checks, and DI surface. Proto contracts live in [Core.Server/Protos](Core.Server/Protos) (`login_service.proto`, `char_service.proto`, `map_service.proto`).

## Persistence

MySQL 8 / MariaDB via Pomelo EF Core. Shared in [Core.Database](Core.Database). Entities ([Entities/](Core.Database/Entities), 74 files), configurations ([Configurations/](Core.Database/Configurations)), repositories ([Repositories/Api/](Core.Database/Repositories/Api)), migrations in [Core.Database/Migrations](Core.Database/Migrations). Each consuming server calls `services.AddGameDatabase(...)` from [ServiceCollectionExtensions.cs](Core.Database/ServiceCollectionExtensions.cs).

Migration usage: [Core.Database/MIGRATIONS.md](Core.Database/MIGRATIONS.md). Repository injection pattern: [Core.Database/USAGE_EXAMPLES.md](Core.Database/USAGE_EXAMPLES.md).

## rAthena parity

Parity progress tracked in [.agents/migrations/](.agents/migrations/), organized by server:
- [.agents/migrations/README.md](.agents/migrations/README.md) — index + status table
- [.agents/migrations/char/](.agents/migrations/char/) — client packets, gRPC server, connect flow
- [.agents/migrations/login/](.agents/migrations/login/) — login server status
- [.agents/migrations/map/](.agents/migrations/map/) — map-side IPC integration
- [.agents/migrations/inter/](.agents/migrations/inter/) — `inter.cpp` base + per-module flows

The Login server packet/auth surface is largely complete: IP bans, DNSBL, client-hash, MD5 / double-MD5 passwords, character-server registration over gRPC. See [Login.Server/UseCase/LoginMmoAuth.cs](Login.Server/UseCase/LoginMmoAuth.cs) and [Login.Server/Security/LoginSecurityService.cs](Login.Server/Security/LoginSecurityService.cs).

## Disconnect reasons

Defined alongside session code in [Core.Server/Network](Core.Server/Network). Common values: `ClientDisconnect`, `HeartbeatTimeout`, `SocketError`, `ServerShutdown`, `Kicked`, `UnhandledPacket`, `PacketHandlerError`.

## Running locally

1. `docker compose up -d` (starts MariaDB on 3306, user/password `ragnarok`).
2. `dotnet ef database update --project Core.Database` once, to apply migrations.
3. Start the servers in any order — they retry IPC connections:
   ```
   dotnet run --project Login.Server
   dotnet run --project Char.Server
   dotnet run --project Map.Server
   dotnet run --project Web.Server
   ```
