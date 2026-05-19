using Core.Server.Packets;
using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Status;
using Map.Server.Visibility;

namespace Map.Server.Gm.Commands;

/// <summary>
/// <c>@alive</c> — restore caller from death state with full HP/SP.
/// rAthena <c>atcommand_alive</c> (atcommand.cpp:933) — runs through
/// <c>status_revive</c>. We mirror the effect inline here: clear dead
/// flag, full-heal, push the par-change packets.
/// </summary>
public sealed class AliveCommand(
    Map.Server.Combat.IPcDeathService death,
    ISessionManagerAccessor sessions,
    IVisibilityService visibility) : IGmCommand
{
    public string Name => "alive";
    public string Description => "@alive — revive yourself with full HP/SP.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (!death.IsDead(caller))
        {
            visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT { Message = "@alive: you're not dead." });
            return Task.CompletedTask;
        }
        // Mirror rAthena status_revive: full heal + restore action state.
        caller.Hp = caller.MaxHp;
        caller.Sp = caller.MaxSp;
        death.Respawn(caller); // re-spawns at current cell when dead-flag cleared

        var s = sessions.GetByEntityId(caller.Id);
        if (s != null)
        {
            s.EnqueuePacket(new ZC_PAR_CHANGE { VarId = SpId.SP_HP, Value = caller.Hp });
            s.EnqueuePacket(new ZC_PAR_CHANGE { VarId = SpId.SP_SP, Value = caller.Sp });
        }
        visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT { Message = "@alive: revived." });
        return Task.CompletedTask;
    }
}
