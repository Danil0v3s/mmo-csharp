using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Movement;
using Map.Server.Services;
using Map.Server.Visibility;
using Map.Server.World;

namespace Map.Server.Gm.Commands;

/// <summary>
/// <c>@jumpto &lt;name&gt;</c> — teleport to another online player.
/// rAthena <c>atcommand_jumpto</c> (atcommand.cpp:464). Resolves the
/// target by case-insensitive name; falls through <c>pc_setpos</c> for
/// the cross-map case so any nodbwarp / nowarpto check on the target
/// map applies.
/// </summary>
public sealed class JumpToCommand(
    IVisibilityService visibility,
    IPlayerMapService players,
    IMapWorldRegistry maps,
    IPcSetposService setpos) : IGmCommand
{
    public string Name => "jumpto";
    public string Description => "@jumpto <name> — teleport to a player by name.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0)
        {
            visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT { Message = "@jumpto: usage — @jumpto <name>" });
            return Task.CompletedTask;
        }
        var target = players.GetAllPlayers()
            .FirstOrDefault(p => string.Equals(p.Name, args[0], StringComparison.OrdinalIgnoreCase));
        if (target == null)
        {
            visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT { Message = $"@jumpto: '{args[0]}' not online." });
            return Task.CompletedTask;
        }
        var map = maps.All.FirstOrDefault(m => (uint)m.Name.GetHashCode() == target.MapId);
        if (map == null) return Task.CompletedTask;
        setpos.Setpos(caller, map.Name, target.X, target.Y);
        visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT { Message = $"@jumpto: → {target.Name} on {map.Name}." });
        return Task.CompletedTask;
    }
}
