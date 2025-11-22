# rAthena Timer to C# Conversion Notes

This document describes the conversion of the rAthena timer system from C++ to C#.

## File Mapping

| C++ File | C# File | Description |
|----------|---------|-------------|
| `timer.hpp` | `TimerData.cs` | Timer data structures and enums |
| `timer.cpp` (heap) | `TimerHeap.cs` | Binary heap implementation |
| `timer.cpp` (main) | `TimerManager.cs` | Main timer management logic |
| `timer.cpp` (utils) | `TimeUtils.cs` | Time utility functions |
| - | `Example.cs` | Usage examples |

## Key Design Decisions

### 1. Zero-Allocation Design Preserved
- Pre-allocated arrays with dynamic growth (256-element chunks)
- Object pooling for timer IDs via free list
- Struct-based `TimerData` to avoid heap allocations
- Binary heap for O(log n) timer operations

### 2. C++ to C# Mappings

| C++ | C# | Notes |
|-----|-----|-------|
| `typedef TIMER_FUNC((*TimerFunc))` | `delegate int TimerFunc(...)` | Function pointer → delegate |
| `intptr_t` | `nint` | Native int type |
| `t_tick` (int64) | `long` | 64-bit tick counter |
| `nullptr` | `null` | Null reference |
| Macros (BHEAP_*) | TimerHeap class | Binary heap encapsulated |
| Global state | TimerManager class | Instance-based design |

### 3. Time Handling
- **C++**: Platform-specific (RDTSC, GetTickCount, clock_gettime, gettimeofday)
- **C#**: `Stopwatch.ElapsedMilliseconds` for high-resolution timing
- Tick caching preserved for performance (configurable limit)

### 4. Memory Management
- **C++**: Manual allocation with `CREATE`, `RECREATE`, `aFree`
- **C#**: Managed arrays with `Array.Resize`
- Both use pre-allocation and growth strategies to minimize allocations

### 5. Error Handling
- **C++**: Console output via `ShowError`, `ShowWarning`
- **C#**: Console.WriteLine + exception handling in timer callbacks

## API Differences

### C++ API
```cpp
int32 add_timer(t_tick tick, TimerFunc func, int32 id, intptr_t data);
int32 add_timer_interval(t_tick tick, TimerFunc func, int32 id, intptr_t data, int32 interval);
int32 delete_timer(int32 tid, TimerFunc func);
t_tick do_timer(t_tick tick);
```

### C# API
```csharp
int AddTimer(long tick, TimerFunc func, int id, nint data);
int AddTimerInterval(long tick, TimerFunc func, int id, nint data, int interval);
int DeleteTimer(int tid, TimerFunc func);
long DoTimer(long tick);
```

## Performance Characteristics

Both implementations share the same algorithmic complexity:

| Operation | Complexity | Notes |
|-----------|-----------|-------|
| Add timer | O(log n) | Binary heap push |
| Delete timer | O(n) + O(log n) | Linear search + heap removal |
| Modify timer | O(n) + O(log n) | Linear search + heap adjustment |
| Process timers | O(k log n) | k = expired timers |

## Notable Improvements

1. **Type Safety**: C# delegates provide type safety vs C++ function pointers
2. **Memory Safety**: No manual memory management, no buffer overflows
3. **Exception Handling**: Try-catch around timer callbacks prevents crashes
4. **Modern C#**: Uses nullable reference types, pattern matching, LINQ where appropriate

## Compatibility Notes

### Constants
- `INVALID_TIMER = -1`: Preserved
- `INFINITE_TICK = -1`: Preserved  
- `TIMER_MIN_INTERVAL = 20ms`: Preserved
- `TIMER_MAX_INTERVAL = 1000ms`: Preserved

### Timer Types
- `TIMER_ONCE_AUTODEL (0x01)`: One-shot timer with auto-deletion
- `TIMER_INTERVAL (0x02)`: Repeating interval timer
- `TIMER_REMOVE_HEAP (0x10)`: Internal flag for heap removal

## Usage Differences

### C++ Usage
```cpp
// Global state
add_timer(gettick() + 5000, callback, 0, 0);
do_timer(gettick());
```

### C# Usage
```csharp
// Instance-based
var timerManager = new TimerManager();
timerManager.AddTimer(timerManager.GetTick() + 5000, callback, 0, 0);
timerManager.DoTimer(timerManager.GetTick());
```

## Testing Recommendations

1. **Stress Test**: Create/destroy 10,000+ timers rapidly
2. **Timing Accuracy**: Verify timers fire within acceptable tolerance
3. **Memory Test**: Ensure no memory leaks over extended runtime
4. **Concurrent Test**: Verify behavior with rapid timer modifications
5. **Edge Cases**: Test timer modification while processing, canceling already-expired timers

## Migration Path

To migrate from C++ to C#:

1. Replace global timer functions with `TimerManager` instance
2. Convert function pointers to delegates
3. Update tick retrieval from `gettick()` to `timerManager.GetTick()`
4. Update initialization from `timer_init()` to `new TimerManager()`
5. Update cleanup from `timer_final()` to dispose pattern (if needed)

## Known Limitations

1. **Thread Safety**: Not thread-safe (same as C++ version). Wrap in locks if needed.
2. **Tick Overflow**: Will overflow after ~292 million years (64-bit signed milliseconds)
3. **Timer ID Reuse**: Timer IDs are reused from free list (same as C++)
4. **Platform Timing**: Relies on `Stopwatch` which may have platform-specific resolution

## Future Enhancements

Potential improvements not in the original C++ version:

1. Thread-safe variant using locks or concurrent collections
2. Async/await support for timer callbacks
3. LINQ-based timer queries
4. Built-in timer statistics and profiling
5. Grouped timer batching for better cache locality
6. Priority-based timer scheduling

