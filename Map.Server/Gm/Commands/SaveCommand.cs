using Core.Server.Packets.Out.ZC;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Visibility;
using Map.Server.World;

namespace Map.Server.Gm.Commands;

/// <summary>
/// <c>@save</c> — set caller's current cell as the savepoint. rAthena
/// <c>atcommand_save</c> (atcommand.cpp:702) routes through
/// <c>pc_setsavepoint</c>. We mirror via <c>IPcDeathService.SetSavepoint</c>.
/// </summary>
public sealed class SaveCommand(
    IVisibilityService visibility,
    IMapWorldRegistry maps,
    IPcDeathService death) : IGmCommand
{
    public string Name => "save";
    public string Description => "@save — mark your current cell as your save point.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        var map = maps.All.FirstOrDefault(m => (uint)m.Name.GetHashCode() == caller.MapId);
        if (map == null) return Task.CompletedTask;
        death.SetSavepoint(caller.CharacterId, map.Name, caller.X, caller.Y);
        visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT
        {
            Message = $"@save: savepoint set to {map.Name} ({caller.X},{caller.Y}).",
        });
        return Task.CompletedTask;
    }
}
