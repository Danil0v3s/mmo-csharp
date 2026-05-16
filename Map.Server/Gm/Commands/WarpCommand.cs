using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Visibility;
using Map.Server.World;

namespace Map.Server.Gm.Commands;

/// <summary>
/// <c>@warp &lt;x&gt; &lt;y&gt;</c> — same-map teleport. Cross-map warp
/// involves the char-server handoff and lands when MS3 warps do; this
/// variant updates the caller's cell in place, broadcasts vanish at the
/// old position, and broadcasts standentry at the new one.
/// </summary>
public sealed class WarpCommand(
    IEntityRegistry entities,
    IMapWorldRegistry worldRegistry,
    IVisibilityService visibility
) : IGmCommand
{
    public string Name => "warp";
    public int MinGroupId => 60;
    public string Description => "@warp <x> <y> — teleport to a cell on the current map.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count < 2
            || !short.TryParse(args[0], out var x)
            || !short.TryParse(args[1], out var y))
        {
            visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT
            {
                Message = "@warp: usage — @warp <x> <y>",
            });
            return Task.CompletedTask;
        }

        var map = ResolveMap(caller.MapId);
        if (map == null || !map.IsWalkable(x, y))
        {
            visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT
            {
                Message = $"@warp: ({x},{y}) is not walkable.",
            });
            return Task.CompletedTask;
        }

        var fromX = caller.X;
        var fromY = caller.Y;
        // Old viewers see the caller vanish; new viewers see them spawn.
        // We can't use a single NotifyMoveDiff because teleport jumps the
        // entity by more than the view radius — the old and new view sets
        // are essentially disjoint.
        visibility.NotifyVanishedToArea(caller, VanishReason.Teleport);
        entities.Move(caller.Id, x, y);
        visibility.NotifySpawnedToArea(caller);

        // Echo the new position back to the caller as well so their client
        // ack matches what the server now believes.
        visibility.SendToSelf(caller, new ZC_NPCACK_MAPMOVE
        {
            MapName = map.Name,
            X = x,
            Y = y,
        });
        visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT
        {
            Message = $"@warp: teleported to ({x},{y}).",
        });
        return Task.CompletedTask;
    }

    private MapData? ResolveMap(uint mapId)
    {
        foreach (var m in worldRegistry.All)
        {
            if ((uint)m.Name.GetHashCode() == mapId) return m;
        }
        return null;
    }
}
