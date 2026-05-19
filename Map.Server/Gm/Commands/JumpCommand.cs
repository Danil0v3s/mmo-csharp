using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Visibility;
using Map.Server.World;

namespace Map.Server.Gm.Commands;

/// <summary>
/// <c>@jump [x] [y]</c> — teleport to a random walkable cell on the
/// current map (or the specified cell). rAthena <c>atcommand_jump</c>
/// (atcommand.cpp:556). Same-map only; cross-map is <c>@warp</c>.
/// </summary>
public sealed class JumpCommand(
    IEntityRegistry entities,
    IMapWorldRegistry worldRegistry,
    IVisibilityService visibility) : IGmCommand
{
    public string Name => "jump";
    public string Description => "@jump [x] [y] — teleport on the current map (random cell if omitted).";

    private static readonly Random Rng = new();

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        var map = worldRegistry.All.FirstOrDefault(m => (uint)m.Name.GetHashCode() == caller.MapId);
        if (map == null)
        {
            visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT { Message = "@jump: unknown map." });
            return Task.CompletedTask;
        }
        short x = 0, y = 0;
        if (args.Count >= 2 && short.TryParse(args[0], out x) && short.TryParse(args[1], out y))
        {
            if (!map.IsWalkable(x, y))
            {
                visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT { Message = $"@jump: ({x},{y}) not walkable." });
                return Task.CompletedTask;
            }
        }
        else
        {
            // rAthena: scans until it hits a walkable cell, cap at 200 attempts.
            for (var i = 0; i < 200; i++)
            {
                x = (short)Rng.Next(0, map.Xs);
                y = (short)Rng.Next(0, map.Ys);
                if (map.IsWalkable(x, y)) break;
            }
        }

        visibility.NotifyVanishedToArea(caller, VanishReason.Teleport);
        entities.Move(caller.Id, x, y);
        visibility.NotifySpawnedToArea(caller);
        visibility.SendToSelf(caller, new ZC_NPCACK_MAPMOVE { MapName = map.Name, X = x, Y = y });
        visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT { Message = $"@jump: ({x},{y})." });
        return Task.CompletedTask;
    }
}
