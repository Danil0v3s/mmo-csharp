using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Movement;
using Map.Server.Services;
using Map.Server.Visibility;
using Map.Server.World;

namespace Map.Server.Gm.Commands;

/// <summary>
/// <c>@recall &lt;name&gt;</c> — teleport a target player to the caller.
/// rAthena <c>atcommand_recall</c> (atcommand.cpp:1875). Refuses if the
/// caller's map has <c>nowarpto</c> or the target's map has <c>nowarp</c>
/// (parity: not enforced here yet — see [parity-audit map flag gaps]).
/// </summary>
public sealed class RecallCommand(
    IVisibilityService visibility,
    IPlayerMapService players,
    IMapWorldRegistry maps,
    IPcSetposService setpos) : IGmCommand
{
    public string Name => "recall";
    public string Description => "@recall <name> — teleport a player to your position.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0)
        {
            visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT { Message = "@recall: usage — @recall <name>" });
            return Task.CompletedTask;
        }
        var target = players.GetAllPlayers()
            .FirstOrDefault(p => string.Equals(p.Name, args[0], StringComparison.OrdinalIgnoreCase));
        if (target == null)
        {
            visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT { Message = $"@recall: '{args[0]}' not online." });
            return Task.CompletedTask;
        }
        var map = maps.All.FirstOrDefault(m => (uint)m.Name.GetHashCode() == caller.MapId);
        if (map == null) return Task.CompletedTask;
        setpos.Setpos(target, map.Name, caller.X, caller.Y);
        visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT { Message = $"@recall: {target.Name} recalled." });
        return Task.CompletedTask;
    }
}
