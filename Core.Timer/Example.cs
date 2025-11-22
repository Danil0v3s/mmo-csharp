namespace Core.Timer.Examples;

/// <summary>
/// Example usage of the TimerManager
/// </summary>
public static class TimerExample
{
    public static void RunExample()
    {
        var timerManager = new TimerManager();
        
        // Register timer function names for debugging
        timerManager.AddTimerFuncList(OneShotTimerCallback, "OneShotTimer");
        timerManager.AddTimerFuncList(IntervalTimerCallback, "IntervalTimer");
        timerManager.AddTimerFuncList(CancelableTimerCallback, "CancelableTimer");

        // Example 1: One-shot timer
        Console.WriteLine("Adding one-shot timer (fires in 2 seconds)");
        long currentTick = timerManager.GetTick();
        timerManager.AddTimer(currentTick + 2000, OneShotTimerCallback, 1, 0);

        // Example 2: Interval timer
        Console.WriteLine("Adding interval timer (fires every 1 second)");
        int intervalTimerId = timerManager.AddTimerInterval(
            currentTick + 1000, 
            IntervalTimerCallback, 
            2, 
            0, 
            1000
        );

        // Example 3: Timer that we'll cancel
        Console.WriteLine("Adding cancelable timer (will be cancelled before firing)");
        int cancelableTimerId = timerManager.AddTimer(
            currentTick + 5000, 
            CancelableTimerCallback, 
            3, 
            0
        );

        // Run the timer loop for 10 seconds
        Console.WriteLine("Starting timer loop...\n");
        long endTime = currentTick + 10000;
        int intervalCallCount = 0;

        while (timerManager.GetTick() < endTime)
        {
            currentTick = timerManager.GetTick();
            long nextInterval = timerManager.DoTimer(currentTick);

            // Cancel the timer after 3 interval fires
            if (intervalCallCount >= 3 && cancelableTimerId != TimerManager.InvalidTimer)
            {
                Console.WriteLine("Canceling the 5-second timer");
                timerManager.DeleteTimer(cancelableTimerId, CancelableTimerCallback);
                cancelableTimerId = TimerManager.InvalidTimer;
            }

            // Sleep until next timer
            Thread.Sleep((int)Math.Min(nextInterval, 50));
        }

        // Cleanup
        if (intervalTimerId != TimerManager.InvalidTimer)
        {
            timerManager.DeleteTimer(intervalTimerId, IntervalTimerCallback);
        }

        Console.WriteLine("\nExample completed!");
    }

    private static int OneShotTimerCallback(int timerId, long tick, int id, nint data)
    {
        Console.WriteLine($"[OneShotTimer] Timer {timerId} fired! ID={id}");
        return 0;
    }

    private static int IntervalTimerCallback(int timerId, long tick, int id, nint data)
    {
        Console.WriteLine($"[IntervalTimer] Timer {timerId} fired! ID={id} at tick {tick}");
        return 0;
    }

    private static int CancelableTimerCallback(int timerId, long tick, int id, nint data)
    {
        Console.WriteLine($"[CancelableTimer] This should not appear! Timer was supposed to be cancelled.");
        return 0;
    }

    /// <summary>
    /// Example of a game loop using TimerManager
    /// </summary>
    public static void GameLoopExample()
    {
        var timerManager = new TimerManager();
        bool running = true;
        int updateCount = 0;

        // Register callbacks
        timerManager.AddTimerFuncList(GameUpdateCallback, "GameUpdate");
        timerManager.AddTimerFuncList(SaveGameCallback, "SaveGame");
        
        // Add interval timer for game updates (50ms = 20 FPS)
        long currentTick = timerManager.GetTick();
        timerManager.AddTimerInterval(currentTick + 50, GameUpdateCallback, 0, 0, 50);
        
        // Add interval timer for auto-save (every 30 seconds)
        timerManager.AddTimerInterval(currentTick + 30000, SaveGameCallback, 0, 0, 30000);

        Console.WriteLine("Game loop started. Press Ctrl+C to stop.");

        // Main game loop
        while (running && updateCount < 200) // Stop after 200 updates (~10 seconds)
        {
            currentTick = timerManager.GetTick();
            long nextInterval = timerManager.DoTimer(currentTick);
            
            // Other game logic here...
            
            // Sleep until next timer
            Thread.Sleep((int)Math.Min(nextInterval, TimerManager.TimerMinInterval));
            updateCount++;
        }

        Console.WriteLine("Game loop stopped.");
    }

    private static int GameUpdateCallback(int timerId, long tick, int id, nint data)
    {
        // Game update logic here
        // Console.WriteLine($"Game update at tick {tick}");
        return 0;
    }

    private static int SaveGameCallback(int timerId, long tick, int id, nint data)
    {
        Console.WriteLine($"[AutoSave] Saving game at tick {tick}");
        return 0;
    }
}

