# Server-to-Server IPC System

This directory contains the implementation for server-to-server communication using gRPC.

## Components

### ServerType Enum
Defines the types of servers in the system:
- `Login` - Authentication server
- `Char` - Character server
- `Map` - Map/Game server
- `Web` - Web API server

### ServerSession
Represents a connection to another server. Features:
- Health check monitoring
- Automatic disconnection detection
- gRPC channel management
- Connection state tracking

### ServerConnectionManager
Central manager for all server-to-server connections. Features:
- Track connections by type
- Iterate through servers by type
- Automatic cleanup of dead connections
- Health monitoring

### IpcClient (Legacy)
Backward-compatible wrapper around ServerConnectionManager.

## Usage Examples

### Basic Connection Access

```csharp
// In your server implementation (inherits from AbstractServer)
public class CharServerImpl : GameLoopServer
{
    // Access connections via the ServerConnections property
    public void ProcessSomething()
    {
        // Check if a specific server type is connected
        if (ServerConnections.HasConnection(ServerType.Map))
        {
            Logger.LogInformation("Map server is connected");
        }
        
        // Get connection count for a server type
        var mapServerCount = ServerConnections.GetConnectionCount(ServerType.Map);
        Logger.LogInformation("Connected to {Count} map servers", mapServerCount);
    }
}
```

### Iterate Through Connected Servers

```csharp
// Get all connected map servers
var mapServers = ServerConnections.GetSessionsByType(ServerType.Map);
foreach (var server in mapServers)
{
    Logger.LogInformation("Map Server: {Name} - Connected: {Connected}", 
        server.ServerName, server.IsConnected);
    
    // Use the gRPC channel
    var channel = server.Channel;
    // Create your gRPC client and make calls...
}

// Get a specific server by name
var loginServer = ServerConnections.GetSessionByName("LoginServer");
if (loginServer?.IsConnected == true)
{
    var channel = loginServer.Channel;
    // Make gRPC calls...
}

// Get all connected servers (any type)
var allServers = ServerConnections.GetAllConnectedSessions();
foreach (var server in allServers)
{
    Logger.LogInformation("{Type} Server '{Name}' is connected", 
        server.ServerType, server.ServerName);
}
```

### Making gRPC Calls

```csharp
// Example: Call the Login server
var loginServers = ServerConnections.GetSessionsByType(ServerType.Login);
var loginServer = loginServers.FirstOrDefault();

if (loginServer != null)
{
    // Create your gRPC client
    var client = new LoginGrpc.LoginGrpcClient(loginServer.Channel);
    
    // Make your call
    var response = await client.SomeMethodAsync(new SomeRequest 
    { 
        /* ... */ 
    });
}
```

### Broadcast to All Servers of a Type

```csharp
public async Task BroadcastToAllMapServersAsync(SomeMessage message)
{
    var mapServers = ServerConnections.GetSessionsByType(ServerType.Map);
    
    var tasks = mapServers.Select(async server =>
    {
        try
        {
            var client = new MapGrpc.MapGrpcClient(server.Channel);
            await client.SendMessageAsync(message);
            Logger.LogDebug("Sent message to map server '{Name}'", server.ServerName);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to send message to map server '{Name}'", 
                server.ServerName);
        }
    });
    
    await Task.WhenAll(tasks);
}
```

### Connection State Monitoring

```csharp
// In a background task or periodic check
public void MonitorServerConnections()
{
    foreach (ServerType serverType in Enum.GetValues<ServerType>())
    {
        var count = ServerConnections.GetConnectionCount(serverType);
        
        if (count == 0)
        {
            Logger.LogWarning("No {ServerType} servers connected!", serverType);
        }
        else
        {
            Logger.LogInformation("{Count} {ServerType} server(s) connected", 
                count, serverType);
        }
    }
}
```

## Configuration

Server endpoints are configured in `appsettings.json`:

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

The key name (e.g., "LoginServer", "MapServer") is used to determine the `ServerType` automatically.

## Health Checking

- Each connection performs periodic health checks (default: every 5 seconds)
- Connections that miss 3+ health checks are automatically cleaned up
- The `ServerConnectionManager` monitors all connections and removes dead ones
- Connection state is always available via `ServerSession.IsConnected`

## Thread Safety

All components are thread-safe and use concurrent collections internally.

