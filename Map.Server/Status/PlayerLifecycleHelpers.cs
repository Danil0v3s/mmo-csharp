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
        // Renewal trait stat pool (POW/STA/WIS/SPL/CON/CRT) — separate
        // pool from regular stats. Stubbed until the trait point
        // counter joins PlayerEntity; logged for visibility.
        _logger.LogDebug("pc_traitstatusup stub: char {Char} trait {Trait}", pc.CharacterId, traitId);
        return false;
    }

    public int MaxParameter(PlayerEntity pc, ushort spId) => 99;
    public int MaxBaseLevel(PlayerEntity pc) => ExpTable.MaxBaseLevel;
    public int MaxJobLevel(PlayerEntity pc) => ExpTable.MaxJobLevel;

    public bool PayCash(PlayerEntity pc, int price, int pointsKafra)
    {
        // Cash points + kafra points columns aren't on CharacterData
        // yet. Stub returns true so callers wire; real deduction
        // lands when the cashshop loader ports.
        _logger.LogDebug(
            "pc_paycash stub: char {Char} price {Price} kafra {Kafra}",
            pc.CharacterId, price, pointsKafra);
        return true;
    }
}
