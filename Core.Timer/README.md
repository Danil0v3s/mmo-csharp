# Core.Timer

High-performance timer system for scheduling callbacks with zero-allocation design. Based on the rAthena timer implementation.

## Features

- **Zero-allocation design**: Pre-allocated arrays and object pooling for high performance
- **Timer identification**: Each timer gets a unique ID for cancellation and modification
- **Two timer types**:
  - One-shot timers: Execute once and auto-delete
  - Interval timers: Execute repeatedly at fixed intervals
- **Binary heap**: Efficient timer queue with O(log n) operations
- **Tick caching**: Reduces system calls for time retrieval
- **Debug support**: Register timer function names for debugging

## Usage

### Basic Setup

```csharp
using Core.Timer;

// Create a timer manager
var timerManager = new TimerManager();

// Define a timer callback
int MyTimerCallback(int timerId, long tick, int id, nint data)
{
    Console.WriteLine($"Timer {timerId} fired at tick {tick}");
    return 0;
}

// Register the function name (optional, for debugging)
timerManager.AddTimerFuncList(MyTimerCallback, "MyTimerCallback");
```

### One-Shot Timer

```csharp
// Fire once after 5 seconds
long fireTime = timerManager.GetTick() + 5000;
int timerId = timerManager.AddTimer(fireTime, MyTimerCallback, 0, 0);
```

### Interval Timer

```csharp
// Fire every 1 second
long firstFireTime = timerManager.GetTick() + 1000;
int timerId = timerManager.AddTimerInterval(firstFireTime, MyTimerCallback, 0, 0, 1000);
```

### Processing Timers

```csharp
// In your game loop
while (running)
{
    long currentTick = timerManager.GetTick();
    long nextInterval = timerManager.DoTimer(currentTick);
    
    // Sleep until next timer (optional)
    if (nextInterval > 0)
    {
        Thread.Sleep((int)Math.Min(nextInterval, 100));
    }
}
```

### Canceling a Timer

```csharp
timerManager.DeleteTimer(timerId, MyTimerCallback);
```

### Modifying Timer Expiration

```csharp
// Add 2 seconds to the timer
timerManager.AddTickTimer(timerId, 2000);

// Or set absolute tick
timerManager.SetTickTimer(timerId, timerManager.GetTick() + 5000);
```

### Passing Custom Data

```csharp
int MyTimerWithData(int timerId, long tick, int userId, nint userData)
{
    Console.WriteLine($"User {userId} timer fired");
    // userData can be a pointer to managed object (use GCHandle)
    return 0;
}

int userId = 12345;
timerManager.AddTimer(timerManager.GetTick() + 1000, MyTimerWithData, userId, 0);
```

## Performance Characteristics

- **Add timer**: O(log n)
- **Delete timer**: O(n) for lookup + O(log n) for heap removal
- **Modify timer**: O(n) for lookup + O(log n) for heap adjustment
- **Process timers**: O(k log n) where k is the number of expired timers
- **Memory**: Pre-allocated arrays grow in chunks of 256 elements

## Thread Safety

`TimerManager` is **not thread-safe**. If you need to use it from multiple threads, wrap calls in locks or create separate instances per thread.

## Constants

- `InvalidTimer = -1`: Returned when timer creation fails
- `InfiniteTick = -1`: Special tick value for infinite delay
- `TimerMinInterval = 20ms`: Minimum interval for timer processing
- `TimerMaxInterval = 1000ms`: Maximum interval for timer processing

