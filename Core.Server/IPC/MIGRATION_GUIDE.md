# IPC System Migration Guide

## What Changed

The old IPC system would immediately report "connected" to a server even if that server wasn't running. This created false positives where servers appeared connected when they were actually down.

The new system:
1. **Verifies connections** - Actually checks if the server is reachable before marking it as connected
2. **Types connections** - Tracks servers by type (Login, Char, Map, Web)
3. **Health monitoring** - Continuously monitors connection health and auto-disconnects dead servers
4. **Supports iteration** - Easy to iterate through all servers of a specific type

## Breaking Changes

### None (Backward Compatible)

The old `IpcClient` API still works exactly the same way:
- `IpcClient.ConnectToServersAsync()` - Still works
- `IpcClient.GetChannel(serverName)` - Still works
- `IpcClient.DisconnectAsync()` - Still works

## New Features

### 1. Access Server Connections by Type

```csharp
// In any server implementation that inherits from AbstractServer
public class YourServer : GameLoopServer
{
    public void YourMethod()
    {
        // Access via the new ServerConnections property
        var mapServers = ServerConnections.GetSessionsByType(ServerType.Map);
        
        foreach (var server in mapServers)
        {
            // Use server.Channel for gRPC calls
        }
    }
}
```

### 2. Check Connection Status

```csharp
// Check if any server of a type is connected
if (ServerConnections.HasConnection(ServerType.Map))
{
    // Map server is available
}

// Get connection count
var mapCount = ServerConnections.GetConnectionCount(ServerType.Map);

// Get a specific server by name
var loginServer = ServerConnections.GetSessionByName("LoginServer");
if (loginServer?.IsConnected == true)
{
    // Use loginServer.Channel
}
```

### 3. Server Connection Manager

All servers now expose `ServerConnections` property:

```csharp
public ServerConnectionManager ServerConnections => IpcClient.ConnectionManager;
```

Use this to:
- Iterate through connected servers
- Check connection counts
- Query by server type
- Get specific servers by name

## Migration Steps

### If You Were Using `IpcClient.GetChannel()`

**Before:**
```csharp
var channel = IpcClient.GetChannel("MapServer");
if (channel != null)
{
    var client = new MapGrpc.MapGrpcClient(channel);
    // Make call
}
```

**After (Recommended):**
```csharp
var mapServer = ServerConnections.GetSessionByName("MapServer");
if (mapServer?.IsConnected == true)
{
    var client = new MapGrpc.MapGrpcClient(mapServer.Channel);
    // Make call
}
```

The key difference: the new way checks `IsConnected` which reflects actual connectivity, not just that a channel was created.

### If You Need to Iterate Through Servers

**New Capability:**
```csharp
// Get all map servers
foreach (var mapServer in ServerConnections.GetSessionsByType(ServerType.Map))
{
    Logger.LogInformation("Map server: {Name}, Connected: {Status}",
        mapServer.ServerName, mapServer.IsConnected);
}
```

### If You Need Connection Health Information

**New Capability:**
```csharp
var server = ServerConnections.GetSessionByName("MapServer");
if (server != null)
{
    Logger.LogInformation(
        "Server: {Name}, Type: {Type}, Connected: {Connected}, LastHealthCheck: {LastCheck}",
        server.ServerName,
        server.ServerType,
        server.IsConnected,
        server.LastHealthCheckTime);
}
```

## Configuration

No configuration changes needed. The system uses existing `OtherServerEndpoints` in `appsettings.json`:

```json
{
  "Server": {
    "OtherServerEndpoints": {
      "LoginServer": "http://localhost:6001",
      "MapServer": "http://localhost:6003"
    }
  }
}
```

The system automatically detects server type from the endpoint name:
- Name contains "Login" → `ServerType.Login`
- Name contains "Char" → `ServerType.Char`
- Name contains "Map" → `ServerType.Map`
- Name contains "Web" → `ServerType.Web`

## Health Monitoring

The system now:
- Performs health checks every 5 seconds
- Marks servers as disconnected after 3 missed checks (15 seconds)
- Automatically cleans up dead connections
- Logs warnings when servers disconnect

You don't need to do anything - it's all automatic.

## Common Patterns

### Pattern 1: Forward Player to Available Map Server

```csharp
var mapServer = ServerConnections
    .GetSessionsByType(ServerType.Map)
    .FirstOrDefault(s => s.IsConnected && !s.IsHealthCheckTimedOut());

if (mapServer != null)
{
    // Forward player
}
```

### Pattern 2: Broadcast to All Servers of Type

```csharp
var tasks = ServerConnections
    .GetSessionsByType(ServerType.Map)
    .Select(async server =>
    {
        var client = new MapGrpc.MapGrpcClient(server.Channel);
        await client.BroadcastAsync(message);
    });

await Task.WhenAll(tasks);
```

### Pattern 3: Validate Critical Connections on Startup

```csharp
protected override async Task OnStartingAsync(CancellationToken cancellationToken)
{
    await base.OnStartingAsync(cancellationToken);
    
    // Allow time for connections
    await Task.Delay(2000, cancellationToken);
    
    // Validate
    if (!ServerConnections.HasConnection(ServerType.Login))
    {
        throw new Exception("Cannot start - Login server not available");
    }
}
```

## Testing

To test the new system:

1. Start only the Char server
2. Check logs - it should warn that Map server is not running
3. Start the Map server
4. Char server should automatically connect within a few seconds
5. Stop the Map server
6. Char server should detect disconnection within ~15 seconds

## Troubleshooting

### "Server not connecting"
- Ensure the target server is actually running
- Check the endpoint in `appsettings.json`
- Look for gRPC connection errors in logs

### "False disconnections"
- Check network stability
- Verify gRPC ports are not blocked by firewall
- Review health check timeout settings (default: 15s)

### "Can't find my server"
- Use `ServerConnections.GetAllConnectedSessions()` to list all
- Verify the server name matches `appsettings.json` key
- Check that server type detection is working (see logs)

## Performance

The new system:
- Minimal overhead (one health check per connection every 5s)
- Thread-safe concurrent collections
- No blocking operations
- Async/await throughout

## Further Reading

- See `Core.Server/IPC/README.md` for detailed API documentation
- See `Core.Server/IPC/EXAMPLES.md` for code examples
- See `ARCHITECTURE.md` for system architecture

