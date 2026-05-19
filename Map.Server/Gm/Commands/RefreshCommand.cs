using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Visibility;

namespace Map.Server.Gm.Commands;

/// <summary>
/// <c>@refresh</c> — re-broadcast caller's standentry to its AOI.
/// rAthena <c>atcommand_refresh</c> (atcommand.cpp:5295) re-issues
/// <c>clif_refresh</c> which re-renders the player's appearance + nearby
/// units. We replay vanish + spawn to the AOI — same visual effect for
/// the surrounding clients without a setpos round-trip.
/// </summary>
public sealed class RefreshCommand(
    IVisibilityService visibility) : IGmCommand
{
    public string Name => "refresh";
    public string Description => "@refresh — re-broadcast your appearance to nearby clients.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        visibility.NotifyVanishedToArea(caller, VanishReason.Teleport);
        visibility.NotifySpawnedToArea(caller);
        visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT { Message = "@refresh: ok." });
        return Task.CompletedTask;
    }
}
