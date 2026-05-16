# Inter-server IPC

All server-to-server communication goes over gRPC. Proto contracts live in [Core.Server/Protos](Core.Server/Protos):
- `login_service.proto` — char→login auth, account ops, IP bans, pincode, VIP
- `char_service.proto` — map↔char auth handoff, party/guild/storage/mail/auction/quest/achievement/pet/etc.
- `map_service.proto` — char→map prepare-player / push notifications

## Connection layer

Connection tracking is in [Core.Server/IPC/](Core.Server/IPC). The key types:

| Type | Purpose |
|---|---|
| [`ServerType`](Core.Server/IPC/ServerType.cs) | Enum: `Login`, `Char`, `Map`, `Web` |
| [`ServerSession`](Core.Server/IPC/ServerSession.cs) | One outbound gRPC channel + health-check loop |
| [`ServerConnectionManager`](Core.Server/IPC/ServerConnectionManager.cs) | Owns all sessions, indexes by type and by name |
| [`ServerEndpointConfiguration`](Core.Server/IPC/ServerEndpointConfiguration.cs) | Config record (type + endpoint URL) |
| [`IpcClient`](Core.Server/IPC/IpcClient.cs) | Per-server bootstrap; reads `OtherServerEndpoints`, builds sessions, owns the `ConnectionManager` |
| [`IServerConnectionService`](Core.Server/IPC/IServerConnectionService.cs) / [`ServerConnectionService`](Core.Server/IPC/ServerConnectionService.cs) | DI-friendly façade over `ServerConnectionManager`; this is what services should inject |

`AbstractServer` exposes the manager as `ServerConnections` ([AbstractServer.cs:20](Core.Server/AbstractServer.cs)). DI-managed services should depend on `IServerConnectionService` instead — see the wrapper services below.

## Configuration

Each server lists peer endpoints in `appsettings.json` under `Server.OtherServerEndpoints`:

```json
"OtherServerEndpoints": {
  "LoginServer": "http://localhost:6001",
  "CharServer":  "http://localhost:6002",
  "MapServer":   "http://localhost:6003"
}
```

The key name is matched case-insensitively to detect `ServerType`: `Login*` → `Login`, `Char*` → `Char`, `Map*` → `Map`, `Web*` → `Web`.

## Health checking

Per-session loop in [`ServerSession`](Core.Server/IPC/ServerSession.cs):
- Probes every **5 s** (`HealthCheckInterval = 5000`).
- After **3 consecutive misses** (~15 s), `IsConnected` flips to `false` and the manager evicts the session.
- Reconnection is attempted on the next bootstrap pass.

`ServerSession.IsHealthCheckTimedOut()` lets callers gate critical RPCs on freshness.

## Service-side wrappers

Each consumer wraps `IServerConnectionService` in a typed `*IpcService` that knows which peer to call:

- [`Login.Server/CharServerIpcService.cs`](Login.Server/CharServerIpcService.cs) — login → char push (force disconnect, state changes)
- [`Char.Server/Services/LoginServerIpcService.cs`](Char.Server/Services/LoginServerIpcService.cs) — char → login (auth, account data, ban broadcasts)
- [`Map.Server/Services/`](Map.Server/Services) — map → char calls

These services hide the channel-selection plumbing and return strongly-typed results.

## Usage patterns

**Get a peer by type (e.g. any connected map server):**
```csharp
var map = ServerConnections.GetSessionsByType(ServerType.Map).FirstOrDefault();
if (map?.IsConnected != true) return;
var client = new MapGrpc.MapGrpcClient(map.Channel);
```

**Broadcast to all peers of a type:**
```csharp
var tasks = ServerConnections.GetSessionsByType(ServerType.Map)
    .Select(s => new MapGrpc.MapGrpcClient(s.Channel).BroadcastAsync(msg));
await Task.WhenAll(tasks);
```

**Inject the manager into a DI service (preferred over `ServerConnections`):**
```csharp
public class MyService(IServerConnectionService connections) { … }
```

**Get a specific peer by name:**
```csharp
var login = ServerConnections.GetSessionByName("LoginServer");
```

## Threading

All collections are `ConcurrentDictionary`; reads are lock-free. Health-check loops run on background tasks and do not touch game state. Outgoing RPCs from the game loop should be `await`ed without holding any session lock.
