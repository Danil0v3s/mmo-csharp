using Core.Server.Packets;
using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Status;
using Map.Server.Visibility;

namespace Map.Server.Gm.Commands;

/// <summary>
/// <c>@heal [hp] [sp]</c> — restore HP/SP. rAthena <c>atcommand_heal</c>
/// (atcommand.cpp). No args ⇒ full heal. Positive ⇒ heal that amount,
/// negative ⇒ damage. Cap to MaxHp/MaxSp.
/// </summary>
public sealed class HealCommand(
    IVisibilityService visibility,
    Map.Server.Status.ISessionManagerAccessor sessions
) : IGmCommand
{
    public string Name => "heal";
    public string Description => "@heal [hp] [sp] — restore HP/SP (no args = full heal).";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        int? hp = null, sp = null;
        if (args.Count >= 1 && int.TryParse(args[0], out var h)) hp = h;
        if (args.Count >= 2 && int.TryParse(args[1], out var s)) sp = s;

        // No-args == full heal (rAthena: heal both to max).
        if (hp == null && sp == null)
        {
            caller.Hp = caller.MaxHp;
            caller.Sp = caller.MaxSp;
        }
        else
        {
            if (hp is { } dh) caller.Hp = Math.Clamp(caller.Hp + dh, 0, caller.MaxHp);
            if (sp is { } ds) caller.Sp = Math.Clamp(caller.Sp + ds, 0, caller.MaxSp);
        }

        // Push HP/SP back to the client. rAthena uses ZC_PAR_CHANGE with
        // SP_HP / SP_SP after status_calc_pc; we have the same packet.
        var session = sessions.GetByEntityId(caller.Id);
        if (session != null)
        {
            session.EnqueuePacket(new ZC_PAR_CHANGE { VarId = SpId.SP_HP, Value = caller.Hp });
            session.EnqueuePacket(new ZC_PAR_CHANGE { VarId = SpId.SP_SP, Value = caller.Sp });
        }
        visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT
        {
            Message = $"@heal: HP {caller.Hp}/{caller.MaxHp}, SP {caller.Sp}/{caller.MaxSp}.",
        });
        return Task.CompletedTask;
    }
}
