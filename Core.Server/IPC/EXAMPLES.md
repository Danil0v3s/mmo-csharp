# IPC Usage Examples

This document provides concrete examples of how to use the new server connection system.

## Example 1: Check for Connected Map Servers

```csharp
// In CharServerImpl.cs
public void CheckMapServerStatus()
{
    var mapServers = ServerConnections.GetSessionsByType(ServerType.Map);
    
    if (!mapServers.Any())
    {
        Logger.LogWarning("No map servers are currently connected!");
        return;
    }
    
    foreach (var mapServer in mapServers)
    {
        Logger.LogInformation("Map server '{Name}' is connected at SessionId {SessionId}",
            mapServer.ServerName, mapServer.SessionId);
    }
}
```

## Example 2: Forward Player to Map Server (Character Select)

```csharp
// In CharacterSelectHandler.cs
public async Task<bool> ForwardPlayerToMapServer(
    ClientSession playerSession, 
    int selectedCharacterId)
{
    // Get any connected map server
    var mapServer = ServerConnections.GetSessionsByType(ServerType.Map).FirstOrDefault();
    
    if (mapServer == null)
    {
        Logger.LogWarning("Cannot forward player - no map servers available");
        return false;
    }
    
    try
    {
        // Create gRPC client for the map server
        var mapClient = new MapGrpc.MapGrpcClient(mapServer.Channel);
        
        // Forward player info
        var response = await mapClient.PreparePlayerAsync(new PreparePlayerRequest
        {
            SessionId = playerSession.SessionId.ToString(),
            CharacterId = selectedCharacterId
        });
        
        if (response.Success)
        {
            Logger.LogInformation("Player forwarded to map server '{Name}'", 
                mapServer.ServerName);
            return true;
        }
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "Failed to forward player to map server '{Name}'",
            mapServer.ServerName);
    }
    
    return false;
}
```

## Example 3: Load Balance Across Multiple Map Servers

```csharp
// In CharServerImpl.cs
public ServerSession? SelectMapServerForPlayer(long accountId)
{
    var mapServers = ServerConnections.GetSessionsByType(ServerType.Map).ToList();
    
    if (!mapServers.Any())
    {
        Logger.LogWarning("No map servers available for load balancing");
        return null;
    }
    
    // Simple round-robin based on account ID
    var index = (int)(accountId % mapServers.Count);
    var selectedServer = mapServers[index];
    
    Logger.LogDebug("Selected map server '{Name}' for account {AccountId}",
        selectedServer.ServerName, accountId);
    
    return selectedServer;
}
```

## Example 4: Broadcast Message to All Servers of Type

```csharp
// In LoginServerImpl.cs
public async Task BroadcastServerMaintenanceNotice(string message)
{
    // Notify all char servers
    var charServers = ServerConnections.GetSessionsByType(ServerType.Char);
    var mapServers = ServerConnections.GetSessionsByType(ServerType.Map);
    
    var allServers = charServers.Concat(mapServers);
    
    foreach (var server in allServers)
    {
        try
        {
            // Assuming you have a common maintenance RPC method
            var client = new CommonGrpc.CommonGrpcClient(server.Channel);
            await client.MaintenanceNoticeAsync(new MaintenanceRequest
            {
                Message = message
            });
            
            Logger.LogInformation("Sent maintenance notice to {Type} server '{Name}'",
                server.ServerType, server.ServerName);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to send maintenance notice to {Type} server '{Name}'",
                server.ServerType, server.ServerName);
        }
    }
}
```

## Example 5: Validate Login Token with Login Server

```csharp
// In CharServerImpl.cs
public async Task<bool> ValidatePlayerToken(int accountId, string token)
{
    var loginServer = ServerConnections.GetSessionsByType(ServerType.Login).FirstOrDefault();
    
    if (loginServer == null)
    {
        Logger.LogError("Cannot validate token - no login server connected");
        return false;
    }
    
    try
    {
        var loginClient = new LoginGrpc.LoginGrpcClient(loginServer.Channel);
        var response = await loginClient.ValidateTokenAsync(new ValidateTokenRequest
        {
            AccountId = accountId,
            Token = token
        });
        
        return response.IsValid;
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "Error validating token with login server");
        return false;
    }
}
```

## Example 6: Monitor Connection Health

```csharp
// In your GameLoopServer's Update method
protected override async Task GameLoopUpdate(CancellationToken cancellationToken)
{
    // ... your normal game loop logic ...
    
    // Periodically check server connections
    if (_tickCounter % 100 == 0) // Every 100 ticks
    {
        LogServerConnectionStatus();
    }
}

private void LogServerConnectionStatus()
{
    Logger.LogInformation(
        "Server Connections - Login: {Login}, Char: {Char}, Map: {Map}",
        ServerConnections.GetConnectionCount(ServerType.Login),
        ServerConnections.GetConnectionCount(ServerType.Char),
        ServerConnections.GetConnectionCount(ServerType.Map));
    
    // Check for missing critical connections
    if (this is CharServerImpl && !ServerConnections.HasConnection(ServerType.Login))
    {
        Logger.LogWarning("Critical: No connection to Login server!");
    }
    
    if (this is CharServerImpl && !ServerConnections.HasConnection(ServerType.Map))
    {
        Logger.LogWarning("Warning: No map servers connected - players cannot enter game");
    }
}
```

## Example 7: Graceful Handling of Server Disconnection

```csharp
// In CharServerImpl.cs
public async Task ProcessCharacterSelection(ClientSession session, int charSlot)
{
    var mapServer = ServerConnections.GetSessionsByType(ServerType.Map).FirstOrDefault();
    
    if (mapServer == null || !mapServer.IsConnected)
    {
        // Send error packet to client
        session.EnqueuePacket(new HC_REFUSE_ENTER
        {
            ErrorCode = 1, // Server closed
        });
        
        Logger.LogWarning("Character selection failed - no map server available");
        return;
    }
    
    // Check health before making call
    if (mapServer.IsHealthCheckTimedOut())
    {
        Logger.LogWarning("Map server '{Name}' appears unhealthy, selecting alternative",
            mapServer.ServerName);
        
        // Try to find another map server
        mapServer = ServerConnections.GetSessionsByType(ServerType.Map)
            .FirstOrDefault(s => s.IsConnected && !s.IsHealthCheckTimedOut());
        
        if (mapServer == null)
        {
            session.EnqueuePacket(new HC_REFUSE_ENTER { ErrorCode = 1 });
            return;
        }
    }
    
    // Proceed with forwarding to map server...
}
```

## Example 8: Get Server by Name (Specific Server)

```csharp
// If you have multiple map servers with specific names
public async Task RouteToSpecificMap(string mapServerName, long characterId)
{
    var mapServer = ServerConnections.GetSessionByName(mapServerName);
    
    if (mapServer == null || !mapServer.IsConnected)
    {
        Logger.LogError("Cannot route to map server '{Name}' - not connected", 
            mapServerName);
        return;
    }
    
    var client = new MapGrpc.MapGrpcClient(mapServer.Channel);
    await client.LoadCharacterAsync(new LoadCharacterRequest
    {
        CharacterId = characterId
    });
}
```

## Example 9: Startup Connection Validation

```csharp
// In CharServerImpl.cs OnStartingAsync
protected override async Task OnStartingAsync(CancellationToken cancellationToken)
{
    await base.OnStartingAsync(cancellationToken);
    
    // Wait a moment for IPC connections to establish
    await Task.Delay(2000, cancellationToken);
    
    // Validate critical connections
    if (!ServerConnections.HasConnection(ServerType.Login))
    {
        Logger.LogWarning("Started without connection to Login server - this may cause issues");
    }
    
    var mapCount = ServerConnections.GetConnectionCount(ServerType.Map);
    if (mapCount == 0)
    {
        Logger.LogWarning("No map servers connected - players will not be able to enter game");
    }
    else
    {
        Logger.LogInformation("Connected to {Count} map server(s)", mapCount);
    }
}
```

