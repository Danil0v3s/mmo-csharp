using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Services;
using Map.Server.Visibility;
using Map.Server.World;

namespace Map.Server.Gm.Commands;

/// <summary>
/// <c>@mapinfo [level] [name]</c> — display map metadata. rAthena
/// <c>atcommand_mapinfo</c> (atcommand.cpp:3826). Slice: shows the
/// current map's name and PC count; rAthena's verbose output (mob
/// counts, NPC counts, mapflags) lands when those subsystems publish
/// the data — for now we surface the basics.
/// </summary>
public sealed class MapInfoCommand(
    IVisibilityService visibility,
    IPlayerMapService players,
    IMapWorldRegistry maps) : IGmCommand
{
    public string Name => "mapinfo";
    public string Description => "@mapinfo — display info on the current map.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        var map = maps.All.FirstOrDefault(m => (uint)m.Name.GetHashCode() == caller.MapId);
        if (map == null)
        {
            visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT { Message = "@mapinfo: unknown map." });
            return Task.CompletedTask;
        }
        var count = players.GetPlayersOnMap(caller.MapId).Count();
        visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT
        {
            Message = $"@mapinfo: {map.Name} ({map.Xs}x{map.Ys}) — {count} player(s) online.",
        });
        return Task.CompletedTask;
    }
}
