# IPC System Implementation Summary

## Problem Statement

The previous IPC implementation had a critical flaw: it would mark servers as "connected" even if they weren't running. When CharServer tried to connect to MapServer, it would create a gRPC channel and log success, even though MapServer was down. This created false positives and made it impossible to know the actual state of server connections.

## Solution Implemented

Created a comprehensive server-to-server connection tracking system with:

1. **Proper connection verification** - Actually checks if servers are reachable
2. **Type-based tracking** - Servers are categorized by type (Login, Char, Map, Web)
3. **Health monitoring** - Continuous health checks with automatic cleanup
4. **Easy iteration** - Query and iterate through servers by type

## Components Created

### 1. `ServerType` Enum
```
Core.Server/IPC/ServerType.cs
```
Defines server types: Login, Char, Map, Web

### 2. `ServerSession` Class
```
Core.Server/IPC/ServerSession.cs
```
Represents a single server connection with:
- Connection state tracking (`IsConnected`)
- Automatic health monitoring (every 5 seconds)
- Health check timeout detection (3 missed checks = 15 seconds)
- gRPC channel management
- Background health check loop

### 3. `ServerConnectionManager` Class
```
Core.Server/IPC/ServerConnectionManager.cs
```
Central manager for all server connections:
- Track connections by type using concurrent dictionaries
- Query servers by type: `GetSessionsByType(ServerType.Map)`
- Get connection counts: `GetConnectionCount(ServerType.Map)`
- Check if type has connections: `HasConnection(ServerType.Map)`
- Get server by name: `GetSessionByName("MapServer")`
- Automatic cleanup of dead connections
- Background monitoring loop

### 4. `ServerEndpointConfiguration` Class
```
Core.Server/IPC/ServerEndpointConfiguration.cs
```
Configuration model for server endpoints

### 5. Updated `IpcClient` Class
```
Core.Server/IPC/IpcClient.cs
```
Refactored to use `ServerConnectionManager` internally while maintaining backward compatibility:
- Exposes `ConnectionManager` property for direct access
- `GetChannel()` now returns `null` if server isn't actually connected
- Automatic server type detection from endpoint names

### 6. Updated `AbstractServer` Class
```
Core.Server/AbstractServer.cs
```
Added `ServerConnections` property:
```csharp
public ServerConnectionManager ServerConnections => IpcClient.ConnectionManager;
```

## Documentation Created

### 1. `Core.Server/IPC/README.md`
Comprehensive API documentation with:
- Component overview
- Usage patterns
- Configuration examples
- Health check details

### 2. `Core.Server/IPC/EXAMPLES.md`
9 practical code examples:
- Check server status
- Forward players to map servers
- Load balancing
- Broadcasting
- Token validation
- Health monitoring
- Graceful disconnection handling
- Server-specific routing
- Startup validation

### 3. `Core.Server/IPC/MIGRATION_GUIDE.md`
Complete migration guide covering:
- What changed
- Breaking changes (none - backward compatible)
- New features
- Migration steps
- Common patterns
- Testing procedures
- Troubleshooting

### 4. Updated `ARCHITECTURE.md`
Updated Inter-Process Communication section to document the new system

## Key Features

### Connection Verification
```csharp
// Before: Would always succeed even if server was down
var channel = GrpcChannel.ForAddress(endpoint);

// Now: Actually verifies connectivity
await Channel.ConnectAsync(cancellationToken);
IsConnected = true; // Only set if successful
```

### Health Monitoring
- Health check every 5 seconds
- Monitors gRPC channel state
- Auto-disconnects after 3 missed checks (15 seconds)
- Background monitoring task per connection
- Automatic cleanup of dead connections

### Type-Based Queries
```csharp
// Get all map servers
var mapServers = ServerConnections.GetSessionsByType(ServerType.Map);

// Check if any login server is connected
if (ServerConnections.HasConnection(ServerType.Login))
{
    // Login server available
}

// Count connected char servers
var charCount = ServerConnections.GetConnectionCount(ServerType.Char);
```

### Iteration Support
```csharp
// Iterate through all map servers
foreach (var mapServer in ServerConnections.GetSessionsByType(ServerType.Map))
{
    var client = new MapGrpc.MapGrpcClient(mapServer.Channel);
    await client.SomeMethodAsync(request);
}
```

## Backward Compatibility

The old `IpcClient` API still works:
```csharp
// This still works, but now returns null if server isn't connected
var channel = IpcClient.GetChannel("MapServer");
```

Existing code doesn't need changes, but the new API is recommended:
```csharp
// Recommended new way
var mapServer = ServerConnections.GetSessionByName("MapServer");
if (mapServer?.IsConnected == true)
{
    var channel = mapServer.Channel;
}
```

## Thread Safety

All components are thread-safe:
- `ConcurrentDictionary` for session storage
- `ConcurrentDictionary` for type-based indexes
- No locks needed for reads
- Safe from multiple threads

## Performance

- Minimal overhead: one health check per connection every 5 seconds
- Background tasks for monitoring (non-blocking)
- No performance impact on game loop
- Async/await throughout

## Build Status

✅ All server projects build successfully:
- Core.Server
- Login.Server
- Char.Server  
- Map.Server

## Testing the New System

1. Start CharServer without MapServer running:
   - Should see warning: "Failed to establish connection to MapServer - server may not be running"

2. Start MapServer:
   - CharServer should connect within a few seconds
   - Log: "Connected to Map server 'MapServer' at..."

3. Stop MapServer while CharServer is running:
   - CharServer should detect disconnection within ~15 seconds
   - Log: "Connection to Map server 'MapServer' lost or timed out"

4. Check connection status:
```csharp
if (ServerConnections.HasConnection(ServerType.Map))
{
    Logger.LogInformation("Map servers available: {Count}",
        ServerConnections.GetConnectionCount(ServerType.Map));
}
```

## Files Modified

1. `Core.Server/IPC/IpcClient.cs` - Refactored to use ServerConnectionManager
2. `Core.Server/AbstractServer.cs` - Added ServerConnections property
3. `ARCHITECTURE.md` - Updated IPC section

## Files Created

1. `Core.Server/IPC/ServerType.cs`
2. `Core.Server/IPC/ServerSession.cs`
3. `Core.Server/IPC/ServerConnectionManager.cs`
4. `Core.Server/IPC/ServerEndpointConfiguration.cs`
5. `Core.Server/IPC/README.md`
6. `Core.Server/IPC/EXAMPLES.md`
7. `Core.Server/IPC/MIGRATION_GUIDE.md`
8. `IPC_IMPLEMENTATION_SUMMARY.md` (this file)

## Usage in Your Code

All servers that inherit from `AbstractServer` or `GameLoopServer` now have access to `ServerConnections`:

```csharp
public class CharServerImpl : GameLoopServer
{
    public async Task ForwardPlayerToMap(ClientSession player, int charId)
    {
        // Get available map server
        var mapServer = ServerConnections
            .GetSessionsByType(ServerType.Map)
            .FirstOrDefault();
        
        if (mapServer?.IsConnected != true)
        {
            Logger.LogWarning("No map servers available");
            return;
        }
        
        // Use the channel
        var client = new MapGrpc.MapGrpcClient(mapServer.Channel);
        await client.PreparePlayerAsync(new PreparePlayerRequest
        {
            CharacterId = charId
        });
    }
}
```

## Next Steps

1. Update your server implementations to use `ServerConnections` for IPC calls
2. Add connection validation in critical paths
3. Consider adding retry logic for important IPC calls
4. Monitor connection health in your game loop
5. Add alerting for missing critical server connections

## Notes

- The test project (Core.Server.Tests) has pre-existing errors unrelated to this implementation
- All production server projects compile and run successfully
- Configuration format remains unchanged
- No database changes required
- No client changes required

