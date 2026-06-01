using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Status;

/// <summary>
/// First-slice <see cref="IPlayerLifecycleHelpers"/>. The save
/// snapshot is handled by the existing autosave loop; OnRegReceived
/// hooks the script-var loader's completion.
/// </summary>
public sealed class PlayerLifecycleHelpers : IPlayerLifecycleHelpers
{
    private readonly ILogger<PlayerLifecycleHelpers> _logger;
    public PlayerLifecycleHelpers(ILogger<PlayerLifecycleHelpers> logger) => _logger = logger;

    public void OnRegReceived(PlayerEntity pc)
    {
        // rAthena triggers deferred init here — achievement load,
        // attendance check, etc. For our port the script-var loader
        // (IPlayerVarService.LoadAsync) is awaited synchronously
        // before play resumes, so this hook is a documented no-op.
    }

    public void MakeSaveStatus(PlayerEntity pc)
    {
        // The existing autosave loop already snapshots through
        // GameDbContext. This entry point exists so callers using
        // the canonical pc_makesavestatus name don't drift to
        // direct DbContext writes.
    }

    public void SetRestartValue(PlayerEntity pc, byte type)
    {
        // rAthena: type 1 = restart at 50% HP/SP (renewal respawn),
        // type 2 = restart at 100% (death penalty disabled). We
        // pick type 1 for first-slice respawns; PcDeathService
        // already full-heals so the visible result aligns.
        pc.Hp = type switch
        {
            2 => pc.MaxHp,
            _ => Math.Max(1, pc.MaxHp / 2),
        };
        pc.Sp = type switch
        {
            2 => pc.MaxSp,
            _ => Math.Max(1, pc.MaxSp / 2),
        };
    }
}

/// <summary>
/// First-slice <see cref="IPlayerStatHelpers"/>. SP_* lookup tables
/// + cash payment. The trait stat allocator (renewal POW/STA/etc.)
/// remains a stub until the trait status point pool ports.
/// </summary>
public sealed class PlayerStatHelpers : IPlayerStatHelpers
{
    private readonly ISessionManagerAccessor _sessions;
    private readonly IJobStatsCacheService? _jobStats;
    private readonly ILogger<PlayerStatHelpers> _logger;

    public PlayerStatHelpers(ISessionManagerAccessor sessions, ILogger<PlayerStatHelpers> logger, IJobStatsCacheService? jobStats = null)
    {
        _sessions = sessions;
        _jobStats = jobStats;
        _logger = logger;
    }

    public bool SetParam(PlayerEntity pc, ushort spId, long value)
    {
        switch (spId)
        {
            // COMBAT-10: GM setstat writes the persisted BASE (rAthena
            // sd->status.str). ShiftFinalParam moves the final Stats value +
            // the CalcPc param-base snapshot by the same delta so any SC on
            // top survives and the next recalc sees a zero param-base delta.
            case Core.Server.Packets.SpId.SP_STR: SetBase(pc, 0, value); return true;
            case Core.Server.Packets.SpId.SP_AGI: SetBase(pc, 1, value); return true;
            case Core.Server.Packets.SpId.SP_VIT: SetBase(pc, 2, value); return true;
            case Core.Server.Packets.SpId.SP_INT: SetBase(pc, 3, value); return true;
            case Core.Server.Packets.SpId.SP_DEX: SetBase(pc, 4, value); return true;
            case Core.Server.Packets.SpId.SP_LUK: SetBase(pc, 5, value); return true;
            case Core.Server.Packets.SpId.SP_HP: pc.Hp = (int)Math.Clamp(value, 0, pc.MaxHp); return true;
            case Core.Server.Packets.SpId.SP_SP: pc.Sp = (int)Math.Clamp(value, 0, pc.MaxSp); return true;
            case Core.Server.Packets.SpId.SP_BASELEVEL: pc.Level = (int)Math.Clamp(value, 1, MaxBaseLevel(pc)); return true;
            case Core.Server.Packets.SpId.SP_JOBLEVEL: pc.JobLevel = (int)Math.Clamp(value, 1, MaxJobLevel(pc)); return true;
            case Core.Server.Packets.SpId.SP_BASEEXP: pc.BaseExp = Math.Max(0, value); return true;
            case Core.Server.Packets.SpId.SP_JOBEXP: pc.JobExp = Math.Max(0, value); return true;
            case Core.Server.Packets.SpId.SP_STATUSPOINT: pc.StatusPoints = (int)Math.Max(0, value); return true;
            case Core.Server.Packets.SpId.SP_SKILLPOINT: pc.SkillPoints = (int)Math.Max(0, value); return true;
            case Core.Server.Packets.SpId.SP_ZENY:
                {
                    var s = _sessions.GetByEntityId(pc.Id);
                    if (s?.CharacterData == null) return false;
                    s.CharacterData.Zeny = (uint)Math.Clamp(value, 0, 1_000_000_000L);
                    return true;
                }
            default:
                _logger.LogDebug("pc_setparam: unhandled SP_{Id}={Val}", spId, value);
                return false;
        }
    }

    // COMBAT-10: rAthena pc_readparam(SP_STR..SP_LUK) returns the BASE
    // allocated stat (sd->status.str), not the buffed final — match it.
    public long ReadParam(PlayerEntity pc, ushort spId) => spId switch
    {
        Core.Server.Packets.SpId.SP_STR => pc.BaseParams.Str,
        Core.Server.Packets.SpId.SP_AGI => pc.BaseParams.Agi,
        Core.Server.Packets.SpId.SP_VIT => pc.BaseParams.Vit,
        Core.Server.Packets.SpId.SP_INT => pc.BaseParams.IntStat,
        Core.Server.Packets.SpId.SP_DEX => pc.BaseParams.Dex,
        Core.Server.Packets.SpId.SP_LUK => pc.BaseParams.Luk,
        Core.Server.Packets.SpId.SP_HP => pc.Hp,
        Core.Server.Packets.SpId.SP_MAXHP => pc.MaxHp,
        Core.Server.Packets.SpId.SP_SP => pc.Sp,
        Core.Server.Packets.SpId.SP_MAXSP => pc.MaxSp,
        Core.Server.Packets.SpId.SP_BASELEVEL => pc.Level,
        Core.Server.Packets.SpId.SP_JOBLEVEL => pc.JobLevel,
        Core.Server.Packets.SpId.SP_BASEEXP => pc.BaseExp,
        Core.Server.Packets.SpId.SP_JOBEXP => pc.JobExp,
        Core.Server.Packets.SpId.SP_STATUSPOINT => pc.StatusPoints,
        Core.Server.Packets.SpId.SP_SKILLPOINT => pc.SkillPoints,
        Core.Server.Packets.SpId.SP_ZENY => _sessions.GetByEntityId(pc.Id)?.CharacterData?.Zeny ?? 0,
        _ => 0,
    };

    public bool TraitStatusUp(PlayerEntity pc, ushort traitId)
    {
        // Renewal trait stat pool — costs 1 trait point per +1 (vs the
        // diminishing-cost regular stats). rAthena SP_POW=219, SP_STA=220,
        // SP_WIS=221, SP_SPL=222, SP_CON=223, SP_CRT=224.
        if (pc.TraitPoints <= 0) return false;
        // COMBAT-10: trait-up raises the persisted BASE trait stat (rAthena
        // pc_traitstatusup writes sd->status.pow..). ShiftFinalParam moves the
        // final Stats value + the param-base snapshot in lockstep (cap 100).
        int idx = traitId switch
        {
            219 => 6, 220 => 7, 221 => 8, 222 => 9, 223 => 10, 224 => 11, _ => -1,
        };
        if (idx < 0) return false;
        if (pc.BaseParams[idx] >= 100) return false;
        pc.ShiftFinalParam(idx, 1);
        pc.TraitPoints--;
        return true;
    }

    /// <summary>
    /// COMBAT-10 — GM setstat helper: set a BASE primary stat to an absolute
    /// value and shift the final Stats value + the CalcPc param-base snapshot
    /// by the same delta (preserving any SC contribution layered on top).
    /// </summary>
    private static void SetBase(PlayerEntity pc, int idx, long value)
    {
        var target = (int)Math.Clamp(value, 0, short.MaxValue);
        pc.ShiftFinalParam(idx, target - pc.BaseParams[idx]);
    }

    public int MaxParameter(PlayerEntity pc, ushort spId)
    {
        // rAthena pc_maxparameter (pc.cpp:5060) — picks between
        // battle_config.max_parameter (99) and max_third_parameter
        // (130) based on the class. We resolve via the session's
        // CharacterData.ClassId since the class mask isn't ported yet.
        var session = _sessions.GetByEntityId(pc.Id);
        if (session?.CharacterData == null) return 99;
        var classId = (int)session.CharacterData.ClassId;
        // Renewal 3rd-job range starts at 4030 (mainline) / 4054 (trans).
        // 4th class is 4077+ — same cap 130. Pre-3rd → 99.
        if (classId >= 4030 && classId <= 4076) return 130;
        if (classId >= 4077) return 130;
        return 99;
    }

    public int MaxBaseLevel(PlayerEntity pc)
    {
        // rAthena pc_maxbaselv looks up maxbase[class] in job_stats.yml.
        // DBR-1d: when IJobStatsCacheService is wired, prefer the seeded
        // job_max_level_db row; fall back to the legacy class-id bucket
        // ranges (3rd → 175, 4th → 250, else 99) when the row is missing.
        var session = _sessions.GetByEntityId(pc.Id);
        if (session?.CharacterData == null) return 99;
        var classId = (int)session.CharacterData.ClassId;
        if (_jobStats != null)
        {
            var aegis = JobAegisMapper.AegisByJobId(classId);
            if (aegis != null)
            {
                var capped = _jobStats.GetMaxBaseLevel(aegis);
                if (capped > 0) return capped;
            }
        }
        if (classId >= 4077) return 250;
        if (classId >= 4030) return 175;
        return Math.Min(99, ExpTable.MaxBaseLevel);
    }

    public int MaxJobLevel(PlayerEntity pc)
    {
        // Renewal defaults (when no DB row is present):
        //   Novice → 10, 1st → 50, 2nd / trans-2nd → 70, 3rd → 60, 4th → 50.
        // DBR-1d: prefer the job_max_level_db row when seeded.
        var session = _sessions.GetByEntityId(pc.Id);
        if (session?.CharacterData == null) return 50;
        var classId = (int)session.CharacterData.ClassId;
        if (_jobStats != null)
        {
            var aegis = JobAegisMapper.AegisByJobId(classId);
            if (aegis != null)
            {
                var capped = _jobStats.GetMaxJobLevel(aegis);
                if (capped > 0) return capped;
            }
        }
        if (classId == 0) return 10;                  // Novice
        if (classId >= 4077) return 50;               // 4th
        if (classId >= 4030 && classId <= 4076) return 60; // 3rd
        if (classId >= 7 && classId <= 25) return 70; // 2nd / trans-2nd
        if (classId >= 4001 && classId <= 4029) return 70;
        return 50;                                    // 1st-class baseline
    }

    public bool PayCash(PlayerEntity pc, int price, int pointsKafra)
    {
        // rAthena pc_paycash (pc.cpp:5475): kafra points are consumed
        // first, remainder from cash points. Refuses on insufficient
        // total. Both pools live on PlayerEntity.
        if (price < 0 || pointsKafra < 0) return false;
        var kafra = Math.Min(pointsKafra, pc.KafraPoints);
        kafra = Math.Min(kafra, price);
        var cashOwed = price - kafra;
        if (cashOwed > pc.CashPoints) return false;
        pc.KafraPoints -= kafra;
        pc.CashPoints -= cashOwed;
        _logger.LogInformation(
            "pc_paycash: char {Char} paid kafra={Kafra} cash={Cash} (remaining K={K} C={C})",
            pc.CharacterId, kafra, cashOwed, pc.KafraPoints, pc.CashPoints);
        return true;
    }
}
