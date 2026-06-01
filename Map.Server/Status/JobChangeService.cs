using Core.Server.Packets;
using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Visibility;
using Microsoft.Extensions.Logging;

namespace Map.Server.Status;

/// <summary>
/// First-slice <see cref="IJobChangeService"/>. Mutates the session's
/// CharacterData.Class, refreshes the visible sprite, full-heals,
/// then pushes the par-change packets so the client UI flips.
///
/// Stats recalc is deliberately partial: we just push HP/SP/MaxHP/MaxSP
/// after the calc service ran. The full status_calc_pc reset (job-base
/// HP table, ASPD, attack range) will land when <c>status.cpp</c>
/// job-table porting catches up.
/// </summary>
public sealed class JobChangeService : IJobChangeService
{
    private readonly IPlayerLookService _look;
    private readonly ISessionManagerAccessor _sessions;
    private readonly IStatusCalcService _statusCalc;
    private readonly ILogger<JobChangeService> _logger;

    public JobChangeService(
        IPlayerLookService look,
        ISessionManagerAccessor sessions,
        IStatusCalcService statusCalc,
        ILogger<JobChangeService> logger)
    {
        _look = look;
        _sessions = sessions;
        _statusCalc = statusCalc;
        _logger = logger;
    }

    public bool Change(PlayerEntity pc, int classId)
    {
        if (classId < 0) return false;
        var session = _sessions.GetByEntityId(pc.Id);
        if (session?.CharacterData == null) return false;
        if (session.CharacterData.ClassId == (uint)classId) return false;

        var old = (int)session.CharacterData.ClassId;
        session.CharacterData.ClassId = (uint)classId;

        // Sprite refresh — rAthena clif_changelook(LOOK_BASE) per
        // clif.cpp:3929 inside pc_jobchange end-section.
        _look.ChangeLook(pc, LookType.Base, (ushort)classId);

        // Recalc stats from the new class baseline + full heal.
        // DBR-1c: include the new ClassId so JobAspdCache picks the right
        // (job, weapon) ASPD row.
        _statusCalc.CalcPc(pc, BuildInputs(pc, classId));
        pc.Hp = pc.MaxHp;
        pc.Sp = pc.MaxSp;
        session.EnqueuePacket(new ZC_PAR_CHANGE { VarId = SpId.SP_HP, Value = pc.Hp });
        session.EnqueuePacket(new ZC_PAR_CHANGE { VarId = SpId.SP_MAXHP, Value = pc.MaxHp });
        session.EnqueuePacket(new ZC_PAR_CHANGE { VarId = SpId.SP_SP, Value = pc.Sp });
        session.EnqueuePacket(new ZC_PAR_CHANGE { VarId = SpId.SP_MAXSP, Value = pc.MaxSp });

        _logger.LogInformation(
            "pc_jobchange: char {Char} class {Old} → {New}",
            pc.CharacterId, old, classId);
        return true;
    }

    // COMBAT-10: base params from BaseParams (not conflated Stats); weapon/def
    // from Stats. WeaponType threaded for the renewal ASPD job_aspd lookup.
    private static PcBaseInputs BuildInputs(PlayerEntity p, int jobId = 0) => new(
        BaseLevel: p.Level, JobLevel: p.JobLevel,
        Str: p.BaseParams.Str, Agi: p.BaseParams.Agi, Vit: p.BaseParams.Vit,
        Int: p.BaseParams.IntStat, Dex: p.BaseParams.Dex, Luk: p.BaseParams.Luk,
        Pow: p.BaseParams.Pow, Sta: p.BaseParams.Sta, Wis: p.BaseParams.Wis,
        Spl: p.BaseParams.Spl, Con: p.BaseParams.Con, Crt: p.BaseParams.Crt,
        WeaponAtkMin: p.Stats.WatkMin, WeaponAtkMax: p.Stats.WatkMax,
        EquipDef: p.Stats.Def, EquipMdef: p.Stats.Mdef,
        AttackRange: p.Stats.AttackRange,
        JobId: jobId, WeaponType: p.WeaponType);
}
