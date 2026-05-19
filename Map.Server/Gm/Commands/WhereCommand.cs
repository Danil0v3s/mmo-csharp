using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Visibility;
using Map.Server.World;

namespace Map.Server.Gm.Commands;

/// <summary>
/// <c>@where</c> — echoes the caller's current map + cell back to them.
/// Useful as a sanity check while wiring up cross-map flows.
/// </summary>
public sealed class WhereCommand(
    IVisibilityService visibility,
    IMapWorldRegistry worldRegistry
) : IGmCommand
{
    public string Name => "where";
    public string Description => "Show the caller's current map and cell.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        var mapName = ResolveMapName(caller.MapId) ?? $"0x{caller.MapId:X8}";
        visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT
        {
            Message = $"You are on {mapName} ({caller.X},{caller.Y}) facing dir {caller.Dir}.",
        });
        return Task.CompletedTask;
    }

    private string? ResolveMapName(uint mapId)
    {
        foreach (var m in worldRegistry.All)
        {
            if ((uint)m.Name.GetHashCode() == mapId) return m.Name;
        }
        return null;
    }
}
