# Game Server Architecture

## Overview

This is a C# game server system with 4 server types:
- **LoginServer** (TCP, 20 FPS) - Handles authentication
- **CharServer** (TCP, 20 FPS) - Manages character selection/creation
- **MapServer** (TCP, 60 FPS) - Real-time gameplay
- **WebServer** (REST API) - Web-based management and API

## Architecture Hierarchy

```
IServer (interface)
  └── AbstractServer (base class)
      ├── GameLoopServer (abstract, adds game loop)
      │   ├── LoginServer
      │   ├── CharServer
      │   └── MapServer
      └── WebServer (no game loop, REST API only)
```

## Key Components

### Core.Server Infrastructure

**IServer Interface**
- Defines `Start/Stop/Restart` operations
- `ServerState` enum for lifecycle tracking

**AbstractServer**
- Common lifecycle management
- IPC client (gRPC) for inter-server communication
- Logging integration
- Configuration management

**GameLoopServer**
- Fixed-update loop at configurable FPS
- High-precision timing with `Stopwatch`
- Heartbeat monitoring per client
- Delta time calculation
- Non-blocking packet processing

### TCP Protocol

**Packet Format:**
- Fixed-length: `[2 bytes: Packet ID]`
- Variable-length: `[2 bytes: Packet ID][2 bytes: Size][Body]`

**Heartbeat:**
- Packet ID: `0x0001`
- Fixed-length (no body)
- Client must send every 15-20 seconds
- Server disconnects on timeout

### Client Session Management

**ClientSession**
- Async receive loop per client
- TCP stream reassembly with `PacketBuffer`
- Incoming/outgoing packet queues
- Heartbeat tracking with high-precision timestamps
- Graceful disconnect with reasons

**SessionManager**
- Thread-safe session collection
- Heartbeat timeout checking
- Bulk disconnect support

### Packet Handling

**PacketBuffer**
- Zero-copy parsing with `Memory<byte>` and `Span<byte>`
- ArrayPool for buffer management
- Automatic stream reassembly
- Compact operation to prevent memory waste

**PacketReader/Writer**
- `ref struct` for stack allocation
- No GC pressure in hot paths
- Support for primitives and strings

### Inter-Process Communication

**gRPC Services:**
- `LoginService` - Session validation, account info
- `CharacterService` - Character CRUD operations
- `MapService` - Map entry/exit, player info

**IpcClient:**
- Connection pooling
- Channel registry
- Automatic reconnection support

## Server Configurations

### LoginServer (Port 5001, gRPC 6001)
- 20 FPS game loop
- 30 second heartbeat timeout
- Validates credentials
- Issues session tokens

### CharServer (Port 5002, gRPC 6002)
- 20 FPS game loop
- 30 second heartbeat timeout
- Validates sessions via LoginServer gRPC
- Character management

### MapServer (Port 5003, gRPC 6003)
- **60 FPS game loop** for real-time gameplay
- 15 second heartbeat timeout (stricter)
- Player position updates
- Chat broadcasting
- Uses object pooling for packets

### WebServer (Port 5000)
- REST API with Swagger UI
- Account registration/login
- Server status monitoring
- No game loop needed

## Performance Optimizations

### MapServer (60 FPS)
- Minimize allocations in game loop
- `ArrayPool` for packet buffers
- `Span<T>` and `Memory<T>` for zero-copy
- Single-threaded game state
- Thread-safe queues for I/O

### All Servers
- Async socket I/O
- Separate async task per client for reading
- Main loop processes queued packets synchronously
- Proper TCP stream reassembly
- ConcurrentCollections for thread safety

## Socket I/O Strategy

**Async/Await Hybrid:**
1. Each `ClientSession` has async receive loop
2. Packets queued to `ConcurrentQueue`
3. Main game loop processes queued packets synchronously
4. Outgoing packets queued and flushed per frame

**Benefits:**
- Non-blocking socket operations
- Thread-safe without locks in hot path
- Game state remains single-threaded

## Game Loop Implementation

```
while (running):
    1. Process incoming packets (from queues)
    2. Update game logic (AI, physics, etc.)
    3. Check client heartbeats
    4. Flush outgoing packets
    5. Sleep to maintain target FPS
```

**Timing:**
- Uses `Stopwatch` for high-precision timing
- Calculates actual delta time each frame
- Sleeps remainder to hit target FPS

## Error Handling

**Disconnect Reasons:**
- `ClientDisconnect` - Normal close
- `HeartbeatTimeout` - No heartbeat received
- `SocketError` - Network error
- `ServerShutdown` - Graceful shutdown
- `Kicked` - Administrative action

**Graceful Shutdown:**
1. Cancel server token
2. Disconnect all clients
3. Notify other servers
4. Stop game loop
5. Close listener socket
6. Dispose resources

## Key Design Decisions

### Why Async Receive + Sync Game Loop?
- Prevents blocking on socket I/O
- Game state remains deterministic
- No locking needed in game logic
- Easy to reason about

### Why Different FPS per Server?
- LoginServer: Low frequency, I/O bound
- CharServer: Low frequency, DB queries
- MapServer: High frequency, real-time gameplay

### Why gRPC for IPC?
- Type-safe contracts
- HTTP/2 performance
- Built-in connection pooling
- Easy to add new services

### Why Separate WebServer?
- Different threading model (ASP.NET Core)
- REST API for web clients
- Swagger documentation
- No game loop overhead

## Potential Pitfalls

1. **TCP Stream Reassembly**: Must handle partial packets correctly
2. **Heartbeat Precision**: Use high-precision timing, not DateTime
3. **Buffer Reuse**: Copy data from buffers before reuse
4. **Session Cleanup**: Always dispose on disconnect
5. **FPS Consistency**: Account for processing time in sleep calculation
6. **GC Pressure**: Use ArrayPool and avoid allocations in hot paths

## Running the Servers

Each server has its own `appsettings.json` for configuration.

Start in order:
1. `dotnet run --project Login.Server`
2. `dotnet run --project Char.Server`
3. `dotnet run --project Map.Server`
4. `dotnet run --project Web.Server`

Access WebServer Swagger UI at: `http://localhost:5000/swagger`

## Future Enhancements

- Database integration (Entity Framework Core)
- Redis for session storage
- Load balancing multiple MapServers
- Metrics and monitoring (Prometheus)
- Player authentication with JWT
- Packet encryption (TLS/SSL)

