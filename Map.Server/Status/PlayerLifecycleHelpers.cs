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
    private readonly ILogger<PlayerStatHelpers> _logger;
    public PlayerStatHelpers(ISessionManagerAccessor sessions, ILogger<PlayerStatHelpers> logger)
    {
        _sessions = sessions;
        _logger = logger;
    }

    public bool SetParam(PlayerEntity pc, ushort spId, long value)
    {
        switch (spId)
        {
            case Core.Server.Packets.SpId.SP_STR: pc.Stats.Str = (short)Math.Clamp(value, 0, short.MaxValue); return true;
            case Core.Server.Packets.SpId.SP_AGI: pc.Stats.Agi = (short)Math.Clamp(value, 0, short.MaxValue); return true;
            case Core.Server.Packets.SpId.SP_VIT: pc.Stats.Vit = (short)Math.Clamp(value, 0, short.MaxValue); return true;
            case Core.Server.Packets.SpId.SP_INT: pc.Stats.IntStat = (short)Math.Clamp(value, 0, short.MaxValue); return true;
            case Core.Server.Packets.SpId.SP_DEX: pc.Stats.Dex = (short)Math.Clamp(value, 0, short.MaxValue); return true;
            case Core.Server.Packets.SpId.SP_LUK: pc.Stats.Luk = (short)Math.Clamp(value, 0, short.MaxValue); return true;
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

    public long ReadParam(PlayerEntity pc, ushort spId) => spId switch
    {
        Core.Server.Packets.SpId.SP_STR => pc.Stats.Str,
        Core.Server.Packets.SpId.SP_AGI => pc.Stats.Agi,
        Core.Server.Packets.SpId.SP_VIT => pc.Stats.Vit,
        Core.Server.Packets.SpId.SP_INT => pc.Stats.IntStat,
        Core.Server.Packets.SpId.SP_DEX => pc.Stats.Dex,
        Core.Server.Packets.SpId.SP_LUK => pc.Stats.Luk,
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
        switch (traitId)
        {
            case 219: pc.Stats.Pow = (short)Math.Min(pc.Stats.Pow + 1, 100); break;
            case 220: pc.Stats.Sta = (short)Math.Min(pc.Stats.Sta + 1, 100); break;
            case 221: pc.Stats.Wis = (short)Math.Min(pc.Stats.Wis + 1, 100); break;
            case 222: pc.Stats.Spl = (short)Math.Min(pc.Stats.Spl + 1, 100); break;
            case 223: pc.Stats.Con = (short)Math.Min(pc.Stats.Con + 1, 100); break;
            case 224: pc.Stats.Crt = (short)Math.Min(pc.Stats.Crt + 1, 100); break;
            default: return false;
        }
        pc.TraitPoints--;
        return true;
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
        // Renewal defaults:
        //   Novice / 1st / 2nd / trans-2nd → 99
        //   3rd jobs → 175
        //   4th jobs → 250 (cap)
        var session = _sessions.GetByEntityId(pc.Id);
        if (session?.CharacterData == null) return 99;
        var classId = (int)session.CharacterData.ClassId;
        if (classId >= 4077) return 250;
        if (classId >= 4030) return 175;
        return Math.Min(99, ExpTable.MaxBaseLevel);
    }

    public int MaxJobLevel(PlayerEntity pc)
    {
        // Renewal defaults:
        //   Novice → 10
        //   1st class → 50
        //   2nd / trans-2nd → 70
        //   3rd job → 60
        //   4th job → 50
        var session = _sessions.GetByEntityId(pc.Id);
        if (session?.CharacterData == null) return 50;
        var classId = (int)session.CharacterData.ClassId;
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
