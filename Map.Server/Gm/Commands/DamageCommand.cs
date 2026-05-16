using Core.Server.Packets.Out.ZC;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Visibility;

namespace Map.Server.Gm.Commands;

/// <summary>
/// <c>@damage &lt;amount&gt;</c> — deal flat damage to the nearest mob in
/// the caller's view. Exists as the scaffolding test path for
/// <see cref="IDamageService"/>; the auto-attack loop (rAthena
/// <c>unit_attack_timer</c>) replaces this for real combat.
/// </summary>
public sealed class DamageCommand(
    IEntityRegistry entities,
    IDamageService damage,
    IVisibilityService visibility
) : IGmCommand
{
    public string Name => "damage";
    public int MinGroupId => 60;
    public string Description => "@damage <amount> — apply flat damage to the nearest mob in view.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count < 1 || !int.TryParse(args[0], out var amount) || amount < 0)
        {
            visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT
            {
                Message = "@damage: usage — @damage <amount>",
            });
            return Task.CompletedTask;
        }

        var target = FindNearestMob(caller);
        if (target == null)
        {
            visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT
            {
                Message = "@damage: no mob in view.",
            });
            return Task.CompletedTask;
        }

        var dealt = damage.ApplyDamage(target, amount, caller);
        visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT
        {
            Message = $"@damage: dealt {dealt} to {target.Name} ({target.Hp}/{target.MaxHp} HP).",
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
