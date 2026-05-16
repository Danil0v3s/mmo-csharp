using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Spawn;
using Map.Server.Visibility;

namespace Map.Server.Gm.Commands;

/// <summary>
/// <c>@killmob</c> — instantly kills the nearest mob in the caller's AOI.
/// rAthena ships a richer <c>@killmonster</c> that affects the whole map,
/// optionally including drops; we start with the "kill one" variant since
/// it's the minimum viable test for the MS2 spawn / respawn loop.
/// </summary>
public sealed class KillMobCommand(
    IEntityRegistry entities,
    IMobSpawnService spawn,
    IVisibilityService visibility
) : IGmCommand
{
    public string Name => "killmob";
    public int MinGroupId => 60;
    public string Description => "Kill the nearest mob in your view range.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        var nearest = FindNearestMob(caller);
        if (nearest == null)
        {
            visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT
            {
                Message = "@killmob: no mob in view.",
            });
            return Task.CompletedTask;
        }

        spawn.KillMob(nearest.Id);
        visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT
        {
            Message = $"@killmob: killed {nearest.Name} (#{nearest.Id.Value}).",
        });
        return Task.CompletedTask;
    }

    private MobEntity? FindNearestMob(PlayerEntity caller)
    {
        var inView = entities.ForEachInRange(
            caller.MapId, caller.X, caller.Y,
            range: Visibility.VisibilityConfig.AreaSize,
            mask: EntityType.Mob);

        MobEntity? best = null;
        var bestDist = int.MaxValue;
        foreach (var e in inView)
        {
            if (e is not MobEntity m) continue;
            var dx = m.X - caller.X;
            var dy = m.Y - caller.Y;
            var dist = dx * dx + dy * dy;
            if (dist < bestDist)
            {
                bestDist = dist;
                best = m;
            }
        }
        return best;
    }
}
