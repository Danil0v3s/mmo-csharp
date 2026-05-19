using Core.Server.Packets.Out.ZC;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Visibility;

namespace Map.Server.Gm.Commands;

/// <summary>
/// <c>@kill</c> — instakill the caller. rAthena <c>atcommand_kill</c>
/// (atcommand.cpp:920) routes through <c>status_damage</c> with the
/// caller's full HP as the damage value.
/// </summary>
public sealed class KillCommand(
    IDamageService damage,
    IVisibilityService visibility) : IGmCommand
{
    public string Name => "kill";
    public string Description => "@kill — instakill yourself (drops you to 0 HP).";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        // rAthena uses status_damage with the unit's HP — same as our
        // ApplyDamage path with damage = current HP (no source = no exp
        // penalty attribution to anyone else).
        damage.ApplyDamage(caller, caller.Hp);
        visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT { Message = "@kill: killed." });
        return Task.CompletedTask;
    }
}
