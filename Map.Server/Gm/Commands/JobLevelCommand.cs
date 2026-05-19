using Core.Server.Packets;
using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Status;
using Map.Server.Visibility;

namespace Map.Server.Gm.Commands;

/// <summary>
/// <c>@joblevelup &lt;delta&gt;</c> — adjust caller job level. rAthena
/// <c>atcommand_joblevelup</c> (atcommand.cpp:1546). Negative drops levels;
/// caps at <c>MAX_JOB_LEVEL</c> (50 for 1st-class, 70+ for advanced).
/// We clamp to <see cref="ExpTable.MaxJobLevel"/>.
/// </summary>
public sealed class JobLevelCommand(
    IVisibilityService visibility,
    IStatusCalcService statusCalc,
    ISessionManagerAccessor sessions) : IGmCommand
{
    public string Name => "joblevelup";
    public string Description => "@joblevelup <delta> — adjust caller job level.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0 || !int.TryParse(args[0], out var delta))
        {
            visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT { Message = "@joblevelup: usage — @joblevelup <delta>" });
            return Task.CompletedTask;
        }
        var max = ExpTable.MaxJobLevel;
        var next = Math.Clamp(caller.JobLevel + delta, 1, max);
        if (next == caller.JobLevel)
        {
            visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT { Message = $"@joblevelup: already {caller.JobLevel}." });
            return Task.CompletedTask;
        }
        caller.JobLevel = next;
        statusCalc.CalcPc(caller, BuildInputs(caller));
        caller.Hp = caller.MaxHp;
        caller.Sp = caller.MaxSp;

        var s = sessions.GetByEntityId(caller.Id);
        if (s != null)
        {
            s.EnqueuePacket(new ZC_PAR_CHANGE { VarId = SpId.SP_JOBLEVEL, Value = caller.JobLevel });
            s.EnqueuePacket(new ZC_PAR_CHANGE { VarId = SpId.SP_HP, Value = caller.Hp });
            s.EnqueuePacket(new ZC_PAR_CHANGE { VarId = SpId.SP_SP, Value = caller.Sp });
        }
        visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT { Message = $"@joblevelup: now {caller.JobLevel}." });
        return Task.CompletedTask;
    }

    private static PcBaseInputs BuildInputs(PlayerEntity p) => new(
        BaseLevel: p.Level, JobLevel: p.JobLevel,
        Str: p.Stats.Str, Agi: p.Stats.Agi, Vit: p.Stats.Vit,
        Int: p.Stats.IntStat, Dex: p.Stats.Dex, Luk: p.Stats.Luk,
        Pow: p.Stats.Pow, Sta: p.Stats.Sta, Wis: p.Stats.Wis,
        Spl: p.Stats.Spl, Con: p.Stats.Con, Crt: p.Stats.Crt,
        WeaponAtkMin: p.Stats.WatkMin, WeaponAtkMax: p.Stats.WatkMax,
        EquipDef: p.Stats.Def, EquipMdef: p.Stats.Mdef,
        AttackRange: p.Stats.AttackRange);
}
