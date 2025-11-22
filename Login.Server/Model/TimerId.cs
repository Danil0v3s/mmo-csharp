namespace Login.Server.Model;

public readonly record struct TimerId(int Value);

public static class Timer
{
    public static TimerId INVALID_TIMER = new TimerId(-1);
}