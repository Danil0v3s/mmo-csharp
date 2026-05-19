using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Visibility;

namespace Map.Server.Gm.Commands;

/// <summary>
/// <c>@time</c> / <c>@date</c> / <c>@serverdate</c> / <c>@servertime</c>
/// — print the server's local time. rAthena <c>atcommand_servertime</c>
/// (atcommand.cpp:5316). We treat all four aliases as one command via
/// atcommands.yml routing.
/// </summary>
public sealed class TimeCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "servertime";
    public string Description => "@servertime — show the server's current time.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT
        {
            Message = $"Server time: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ({TimeZoneInfo.Local.DisplayName})",
        });
        return Task.CompletedTask;
    }
}
