# TimerManager Integration Guide

This guide shows how to use the TimerManager in your server implementations.

## Dependency Injection Setup

The TimerManager has been registered as a singleton in all server DI containers:

- **Login.Server** ✓
- **Char.Server** ✓
- **Map.Server** ✓

## Usage in Server Implementation

### 1. Inject TimerManager via Constructor

```csharp
public class LoginServerImpl : AbstractServer<LoginSessionData, LoginServerConfiguration>
{
    private readonly TimerManager _timerManager;
    private readonly ILogger _logger;

    public LoginServerImpl(
        LoginServerConfiguration config,
        SessionManager sessionManager,
        ILogger logger,
        TimerManager timerManager) // Inject here
        : base(config, sessionManager, logger)
    {
        _timerManager = timerManager;
        _logger = logger;
    }
}
```

### 2. Register Timer Callbacks

```csharp
public override async Task StartAsync(CancellationToken cancellationToken)
{
    // Register function names for debugging
    _timerManager.AddTimerFuncList(OnAccountTimeoutCheck, "AccountTimeoutCheck");
    _timerManager.AddTimerFuncList(OnSessionCleanup, "SessionCleanup");
    
    // Start timers
    long currentTick = _timerManager.GetTick();
    
    // Check account timeouts every 30 seconds
    _timerManager.AddTimerInterval(
        currentTick + 30000,
        OnAccountTimeoutCheck,
        0,
        0,
        30000
    );
    
    // Clean up old sessions every 5 minutes
    _timerManager.AddTimerInterval(
        currentTick + 300000,
        OnSessionCleanup,
        0,
        0,
        300000
    );
    
    await base.StartAsync(cancellationToken);
}
```

### 3. Implement Timer Callbacks

```csharp
private int OnAccountTimeoutCheck(int timerId, long tick, int id, nint data)
{
    _logger.LogDebug("Running account timeout check at tick {Tick}", tick);
    
    // Check for timed-out accounts
    // Your logic here...
    
    return 0;
}

private int OnSessionCleanup(int timerId, long tick, int id, nint data)
{
    _logger.LogInformation("Cleaning up old sessions at tick {Tick}", tick);
    
    // Clean up expired sessions
    // Your logic here...
    
    return 0;
}
```

### 4. Process Timers in Game Loop

For servers that have a game loop (like MapServer):

```csharp
protected override async Task RunGameLoopAsync(CancellationToken cancellationToken)
{
    while (!cancellationToken.IsCancellationRequested)
    {
        long currentTick = _timerManager.GetTick();
        
        // Process all expired timers
        long nextTimerInterval = _timerManager.DoTimer(currentTick);
        
        // Your game logic here...
        UpdatePlayers();
        UpdateNPCs();
        UpdateWorld();
        
        // Calculate sleep time
        long sleepTime = Math.Min(nextTimerInterval, _frameDelay);
        if (sleepTime > 0)
        {
            await Task.Delay((int)sleepTime, cancellationToken);
        }
    }
}
```

## Common Use Cases

### 1. Session Timeout Management

```csharp
// When a session is created
public void OnSessionCreated(Guid sessionId)
{
    long timeoutTick = _timerManager.GetTick() + 300000; // 5 minutes
    int timerId = _timerManager.AddTimer(
        timeoutTick,
        OnSessionTimeout,
        0,
        (nint)GCHandle.Alloc(sessionId)
    );
    
    // Store timerId associated with session for later cancellation
    _sessionTimers[sessionId] = timerId;
}

private int OnSessionTimeout(int timerId, long tick, int id, nint data)
{
    var handle = GCHandle.FromIntPtr(data);
    var sessionId = (Guid)handle.Target!;
    handle.Free();
    
    _logger.LogInformation("Session {SessionId} timed out", sessionId);
    DisconnectSession(sessionId);
    
    return 0;
}

// When session receives activity, reset the timer
public void OnSessionActivity(Guid sessionId)
{
    if (_sessionTimers.TryGetValue(sessionId, out int oldTimerId))
    {
        // Extend the timer by 5 minutes
        _timerManager.AddTickTimer(oldTimerId, 300000);
    }
}

// When session disconnects, cancel the timer
public void OnSessionDisconnected(Guid sessionId)
{
    if (_sessionTimers.TryRemove(sessionId, out int timerId))
    {
        _timerManager.DeleteTimer(timerId, OnSessionTimeout);
    }
}
```

### 2. Periodic Database Saves

```csharp
// Start auto-save timer
private void StartAutoSave()
{
    _timerManager.AddTimerFuncList(OnAutoSave, "AutoSave");
    
    long currentTick = _timerManager.GetTick();
    _autoSaveTimerId = _timerManager.AddTimerInterval(
        currentTick + 60000, // First save in 1 minute
        OnAutoSave,
        0,
        0,
        300000 // Then every 5 minutes
    );
}

private int OnAutoSave(int timerId, long tick, int id, nint data)
{
    _logger.LogInformation("Auto-saving player data...");
    
    try
    {
        // Save all player data to database
        SaveAllPlayerData();
        _logger.LogInformation("Auto-save completed successfully");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Auto-save failed");
    }
    
    return 0;
}
```

### 3. Delayed Actions

```csharp
// Delayed kick after warning
public void KickPlayerDelayed(int playerId, int delaySeconds, string reason)
{
    _logger.LogWarning("Player {PlayerId} will be kicked in {Delay} seconds: {Reason}",
        playerId, delaySeconds, reason);
    
    long kickTick = _timerManager.GetTick() + (delaySeconds * 1000);
    _timerManager.AddTimer(kickTick, OnDelayedKick, playerId, (nint)GCHandle.Alloc(reason));
}

private int OnDelayedKick(int timerId, long tick, int playerId, nint data)
{
    var handle = GCHandle.FromIntPtr(data);
    var reason = (string)handle.Target!;
    handle.Free();
    
    _logger.LogInformation("Kicking player {PlayerId}: {Reason}", playerId, reason);
    KickPlayer(playerId, reason);
    
    return 0;
}
```

### 4. Cooldown Management

```csharp
// Skill cooldown system
public bool TryUseSkill(int playerId, int skillId, int cooldownMs)
{
    var key = (playerId, skillId);
    
    // Check if skill is on cooldown
    if (_skillCooldowns.ContainsKey(key))
    {
        return false;
    }
    
    // Add cooldown timer
    long cooldownEnd = _timerManager.GetTick() + cooldownMs;
    int timerId = _timerManager.AddTimer(
        cooldownEnd,
        OnSkillCooldownEnd,
        playerId,
        skillId
    );
    
    _skillCooldowns[key] = timerId;
    return true;
}

private int OnSkillCooldownEnd(int timerId, long tick, int playerId, nint skillId)
{
    var key = (playerId, (int)skillId);
    _skillCooldowns.TryRemove(key, out _);
    
    _logger.LogDebug("Skill {SkillId} cooldown ended for player {PlayerId}",
        skillId, playerId);
    
    return 0;
}
```

## Best Practices

1. **Always register function names** for easier debugging:
   ```csharp
   _timerManager.AddTimerFuncList(MyCallback, "MyCallback");
   ```

2. **Store timer IDs** if you need to cancel or modify timers later:
   ```csharp
   private readonly Dictionary<Guid, int> _sessionTimers = new();
   ```

3. **Free GCHandles** when passing managed objects as data:
   ```csharp
   var handle = GCHandle.FromIntPtr(data);
   var obj = handle.Target;
   handle.Free(); // Important!
   ```

4. **Handle exceptions** in timer callbacks to prevent crashes:
   ```csharp
   private int OnTimer(int timerId, long tick, int id, nint data)
   {
       try
       {
           // Your logic
       }
       catch (Exception ex)
       {
           _logger.LogError(ex, "Timer callback failed");
       }
       return 0;
   }
   ```

5. **Cancel timers on cleanup**:
   ```csharp
   public override async Task StopAsync()
   {
       // Cancel all active timers
       foreach (var timerId in _activeTimers)
       {
           _timerManager.DeleteTimer(timerId, MyCallback);
       }
       
       await base.StopAsync();
   }
   ```

## Performance Tips

- Use `GetTick()` instead of `GetTickNoCache()` for better performance
- Prefer interval timers over repeatedly adding one-shot timers
- Batch timer operations when possible
- Keep timer callbacks fast and non-blocking

## Thread Safety

⚠️ **Important**: TimerManager is NOT thread-safe. All timer operations should be performed on the same thread (typically the game loop thread). If you need to schedule timers from other threads, use a thread-safe queue to defer the operation to the main thread.

