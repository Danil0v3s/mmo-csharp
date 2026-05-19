using Core.Server.Packets;
using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Status;
using Map.Server.Visibility;

namespace Map.Server.Gm.Commands;

/// <summary>
/// <c>@level &lt;delta&gt;</c> — adjust the caller's base level by the
/// signed delta and recalc stats. rAthena <c>atcommand_baselevelup</c>
/// (atcommand.cpp). Negative drops levels; positive grants them. Caps
/// at MaxLevel.
/// </summary>
public sealed class LevelCommand(
    IVisibilityService visibility,
    IStatusCalcService statusCalc,
    Map.Server.Status.ISessionManagerAccessor sessions
) : IGmCommand
{
    public string Name => "level";
    public string Description => "@level <delta> — adjust caller's base level (negative = drop).";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0 || !int.TryParse(args[0], out var delta))
        {
            visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT { Message = "@level: usage — @level <delta>" });
            return Task.CompletedTask;
        }
        var max = ExpTable.MaxBaseLevel;
        var newLevel = Math.Clamp(caller.Level + delta, 1, max);
        if (newLevel == caller.Level)
        {
            visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT { Message = $"@level: already {caller.Level}." });
            return Task.CompletedTask;
        }
        caller.Level = newLevel;

        // Recalc on level change — rAthena status_calc_pc(SCO_FORCE).
        statusCalc.CalcPc(caller, BuildInputs(caller));
        caller.Hp = caller.MaxHp;
        caller.Sp = caller.MaxSp;

        var session = sessions.GetByEntityId(caller.Id);
        if (session != null)
        {
            session.EnqueuePacket(new ZC_PAR_CHANGE { VarId = SpId.SP_BASELEVEL, Value = caller.Level });
            session.EnqueuePacket(new ZC_PAR_CHANGE { VarId = SpId.SP_HP, Value = caller.Hp });
            session.EnqueuePacket(new ZC_PAR_CHANGE { VarId = SpId.SP_MAXHP, Value = caller.MaxHp });
            session.EnqueuePacket(new ZC_PAR_CHANGE { VarId = SpId.SP_SP, Value = caller.Sp });
            session.EnqueuePacket(new ZC_PAR_CHANGE { VarId = SpId.SP_MAXSP, Value = caller.MaxSp });
        }
        visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT { Message = $"@level: now {caller.Level}." });
        return Task.CompletedTask;
    }

    private static PcBaseInputs BuildInputs(PlayerEntity p) => new(
        BaseLevel: p.Level,
        JobLevel: p.JobLevel,
        Str: p.Stats.Str, Agi: p.Stats.Agi, Vit: p.Stats.Vit,
        Int: p.Stats.IntStat, Dex: p.Stats.Dex, Luk: p.Stats.Luk,
        Pow: p.Stats.Pow, Sta: p.Stats.Sta, Wis: p.Stats.Wis,
        Spl: p.Stats.Spl, Con: p.Stats.Con, Crt: p.Stats.Crt,
        WeaponAtkMin: p.Stats.WatkMin, WeaponAtkMax: p.Stats.WatkMax,
        EquipDef: p.Stats.Def, EquipMdef: p.Stats.Mdef,
        AttackRange: p.Stats.AttackRange);
}
