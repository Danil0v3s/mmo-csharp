using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Visibility;

namespace Map.Server.Gm.Commands;

/// <summary>
/// <c>@uptime</c> — duration since process start. rAthena
/// <c>atcommand_uptime</c> (atcommand.cpp:3704) reads <c>start_time</c>;
/// .NET exposes <c>Environment.TickCount64</c> from boot or
/// <c>Process.StartTime</c> for wall-clock-relative.
/// </summary>
public sealed class UptimeCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "uptime";
    public string Description => "@uptime — how long the server has been running.";

    private static readonly DateTime Start = System.Diagnostics.Process.GetCurrentProcess().StartTime;

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        var span = DateTime.Now - Start;
        visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT
        {
            Message = $"Uptime: {span.Days}d {span.Hours}h {span.Minutes}m {span.Seconds}s",
        });
        return Task.CompletedTask;
    }
}
