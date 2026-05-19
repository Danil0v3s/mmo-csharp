using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Services;
using Map.Server.Visibility;
using Map.Server.World;

namespace Map.Server.Gm.Commands;

/// <summary>
/// <c>@users</c> — per-map online counts. rAthena <c>atcommand_users</c>
/// (atcommand.cpp:3729). Prints a row per map with at least one PC plus
/// a grand-total footer.
/// </summary>
public sealed class UsersCommand(
    IVisibilityService visibility,
    IPlayerMapService players,
    IMapWorldRegistry maps) : IGmCommand
{
    public string Name => "users";
    public string Description => "@users — show per-map and total user counts.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        var total = players.Count;
        var rows = new List<string>();
        foreach (var map in maps.All)
        {
            var mapId = (uint)map.Name.GetHashCode();
            var n = players.GetPlayersOnMap(mapId).Count();
            if (n > 0) rows.Add($"{map.Name}: {n}");
        }
        visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT { Message = $"@users: {total} online." });
        foreach (var row in rows)
        {
            visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT { Message = row });
        }
        return Task.CompletedTask;
    }
}
