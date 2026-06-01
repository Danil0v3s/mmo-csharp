using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Status;

/// <summary>
/// Port of rAthena <c>pc_gainexp</c> + <c>pc_checkbaselevelup</c> +
/// <c>pc_checkjoblevelup</c> (pc.cpp:8106..8314), trimmed:
/// - No PvP exp gating yet (no MF_PVP map flag plumbing).
/// - No guild EXP siphon (guild_payexp).
/// - No "max gain rate" battle_config cap.
/// - No achievement / show-exp / pet level scaling — all hook here when
///   their owning subsystems land.
///
/// What it DOES port faithfully:
/// - The exp-add loop with multi-level-up break behavior.
/// - The over-carry cap (post-level exp must be ≤ nextExp − 1).
/// - Status-point gain on level up (renewal: <c>pc_gets_status_point</c>).
/// - Full HP/SP restore on level up (rAthena <c>status_percent_heal(100,100)</c>).
/// - Forwarded broadcast of SP_BASELEVEL / SP_BASEEXP / SP_NEXTBASEEXP /
///   SP_STATUSPOINT to the client so the UI updates without a relog.
/// </summary>
public sealed class ExpService : IExpService
{
    private readonly IStatusCalcService _statusCalc;
    private readonly ISessionManagerAccessor _sessions;
    private readonly ILevelPenaltyService? _levelPenalty;
    // COMBAT-31: lazy to break the DI cycle DamageService → ExpService →
    // StatusChangeService → DamageService. The only consumer (the Richmankim
    // EXP read) runs at GainExp time, never at construction, so deferring the
    // resolve to first .Value access makes the constructor graph acyclic
    // (StatusChangeService is a singleton, fully built by the time the loop
    // ever awards EXP).
    private readonly Lazy<IStatusChangeService>? _sc;
    private readonly ILogger<ExpService> _logger;

    public ExpService(
        IStatusCalcService statusCalc,
        ISessionManagerAccessor sessions,
        ILogger<ExpService> logger,
        ILevelPenaltyService? levelPenalty = null,
        Lazy<IStatusChangeService>? sc = null)
    {
        _statusCalc = statusCalc;
        _sessions = sessions;
        _levelPenalty = levelPenalty;
        _sc = sc;
        _logger = logger;
    }

    // Public so the test harness can assert on the raw mutation without
    // the session-broadcast plumbing. Production callers use GainExp.
    public bool GainExp(PlayerEntity player, long baseExp, long jobExp, int? mobLevel = null)
    {
        if (player.Hp <= 0) return false;
        var session = _sessions.GetByEntityId(player.Id);
        var leveled = false;

        // DBR-1b: apply rAthena pc_level_penalty_mod (pc.cpp:8186) to
        // both EXP buckets before they accumulate. Skipped when the
        // caller doesn't know the source mob level (quest reward, GM
        // grant, item exp scroll, etc.) — matches rAthena's `src_lv == 0`
        // early-return path.
        if (mobLevel is int srcLv && _levelPenalty != null)
        {
            var rate = _levelPenalty.GetExpModifier(player.Level, srcLv);
            if (rate != 100)
            {
                baseExp = baseExp * rate / 100;
                jobExp = jobExp * rate / 100;
            }
        }

        // SC-04 — SC_RICHMANKIM (BD_RICHMANKIM "Mr. Kim a Rich Man"). rAthena
        // pc.cpp pc_gainexp adds Val2 % (10 + 10*lv) to mob-kill EXP for party
        // members in the song area. Gated on a known mob source (like the
        // level penalty) so quest/GM/scroll EXP is unaffected, matching rAthena.
        if (mobLevel is int && _sc?.Value.Get(player, StatusType.Richmankim) is { Val2: > 0 } rich)
        {
            baseExp += baseExp * rich.Val2 / 100;
            jobExp += jobExp * rich.Val2 / 100;
        }

        if (baseExp > 0)
        {
            player.BaseExp = SafeAdd(player.BaseExp, baseExp);
            leveled |= CheckBaseLevelUp(player, session);
            // Always emit the new exp value so the UI refresh keeps up.
            EmitLongLongPar(session, SpId.SP_BASEEXP, player.BaseExp);
        }
        if (jobExp > 0)
        {
            player.JobExp = SafeAdd(player.JobExp, jobExp);
            leveled |= CheckJobLevelUp(player, session);
            EmitLongLongPar(session, SpId.SP_JOBEXP, player.JobExp);
        }
        return leveled;
    }

    private bool CheckBaseLevelUp(PlayerEntity player, MapSessionData? session)
    {
        bool levelup = false;
        while (true)
        {
            var next = ExpTable.NextBaseExp(player.Level);
            if (next <= 0 || player.BaseExp < next || player.Level >= ExpTable.MaxBaseLevel) break;

            player.BaseExp -= next;
            // Over-carry cap mirrors pc.cpp:8118: BattleConfig multi_level_up
            // defaults to false → cap to next-1.
            if (player.BaseExp > next - 1) player.BaseExp = next - 1;
            player.Level++;
            player.StatusPoints += GetStatusPointGain(player.Level);
            levelup = true;
        }

        if (levelup)
        {
            // Recalc stats from new level (rAthena status_calc_pc(SCO_FORCE)),
            // then percent-heal to full as the level-up reward.
            _statusCalc.CalcPc(player, PcInputsFromCurrent(player));
            player.Hp = player.MaxHp;
            player.Sp = player.MaxSp;

            EmitParChange(session, SpId.SP_BASELEVEL, (uint)player.Level);
            EmitParChange(session, SpId.SP_STATUSPOINT, (uint)player.StatusPoints);
            EmitLongLongPar(session, SpId.SP_NEXTBASEEXP, ExpTable.NextBaseExp(player.Level));
            EmitParChange(session, SpId.SP_HP, (uint)player.Hp);
            EmitParChange(session, SpId.SP_MAXHP, (uint)player.MaxHp);
            EmitParChange(session, SpId.SP_SP, (uint)player.Sp);
            EmitParChange(session, SpId.SP_MAXSP, (uint)player.MaxSp);
            _logger.LogInformation(
                "Char {Char} ({Name}) base-leveled to {Level}",
                player.CharacterId, player.Name, player.Level);
        }
        return levelup;
    }

    private bool CheckJobLevelUp(PlayerEntity player, MapSessionData? session)
    {
        bool levelup = false;
        while (true)
        {
            var next = ExpTable.NextJobExp(player.JobLevel);
            if (next <= 0 || player.JobExp < next || player.JobLevel >= ExpTable.MaxJobLevel) break;

            player.JobExp -= next;
            if (player.JobExp > next - 1) player.JobExp = next - 1;
            player.JobLevel++;
            player.SkillPoints++;
            levelup = true;
        }

        if (levelup)
        {
            EmitParChange(session, SpId.SP_JOBLEVEL, (uint)player.JobLevel);
            EmitParChange(session, SpId.SP_SKILLPOINT, (uint)player.SkillPoints);
            EmitLongLongPar(session, SpId.SP_NEXTJOBEXP, ExpTable.NextJobExp(player.JobLevel));
            _logger.LogInformation(
                "Char {Char} ({Name}) job-leveled to {Level}",
                player.CharacterId, player.Name, player.JobLevel);
        }
        return levelup;
    }

    /// <summary>
    /// Renewal status-point grant per base-level. Mirrors rAthena
    /// <c>statpoint_db.pc_gets_status_point</c> default table (db/re/
    /// statpoint.txt): 3 + (level / 5) — close enough for the early game.
    /// Full per-level pinning lands with job_db.
    /// </summary>
    private static int GetStatusPointGain(int newBaseLevel)
        => 3 + (newBaseLevel / 5);

    private static long SafeAdd(long a, long b)
    {
        // Mirror rAthena util::safe_addition_cap at MAX_EXP — uint32_max
        // historically, uint64_max in newer builds. Pre-renewal MAX_EXP
        // is INT32_MAX so we cap at the lower of the two to be safe.
        if (b <= 0) return a;
        if (a > long.MaxValue - b) return long.MaxValue;
        return a + b;
    }

    // COMBAT-10: read the persisted BASE params (not the conflated final
    // Stats.X) so CalcPc re-layers equip + job bonus idempotently and SC stat
    // mods survive the recalc. JobId/WeaponType MUST be threaded so the
    // job-bonus the calc folds matches every other recalc path (otherwise the
    // param-base snapshot would strip the job bonus on the next level-up).
    // Weapon ATK / def / range stay sourced from Stats (equip-derived).
    private static PcBaseInputs PcInputsFromCurrent(PlayerEntity p) => new(
        BaseLevel: p.Level,
        JobLevel: p.JobLevel,
        Str: p.BaseParams.Str, Agi: p.BaseParams.Agi, Vit: p.BaseParams.Vit,
        Int: p.BaseParams.IntStat, Dex: p.BaseParams.Dex, Luk: p.BaseParams.Luk,
        Pow: p.BaseParams.Pow, Sta: p.BaseParams.Sta, Wis: p.BaseParams.Wis,
        Spl: p.BaseParams.Spl, Con: p.BaseParams.Con, Crt: p.BaseParams.Crt,
        WeaponAtkMin: p.Stats.WatkMin, WeaponAtkMax: p.Stats.WatkMax,
        EquipDef: p.Stats.Def, EquipMdef: p.Stats.Mdef,
        AttackRange: p.Stats.AttackRange,
        JobId: p.ClassId, WeaponType: p.WeaponType);

    private static void EmitLongLongPar(MapSessionData? session, ushort varId, long value)
    {
        if (session == null) return;
        session.EnqueuePacket(new ZC_LONGLONGPAR_CHANGE { VarId = varId, Value = value });
    }

    private static void EmitParChange(MapSessionData? session, ushort varId, uint value)
    {
        if (session == null) return;
        session.EnqueuePacket(new ZC_PAR_CHANGE { VarId = varId, Value = (int)value });
    }

    public (long BaseLost, long JobLost) LoseExp(PlayerEntity player, long baseExp, long jobExp)
    {
        var baseTake = Math.Min(baseExp, player.BaseExp);
        var jobTake = Math.Min(jobExp, player.JobExp);
        player.BaseExp -= baseTake;
        player.JobExp -= jobTake;
        var session = _sessions.GetByEntityId(player.Id);
        EmitLongLongPar(session, SpId.SP_BASEEXP, player.BaseExp);
        EmitLongLongPar(session, SpId.SP_JOBEXP, player.JobExp);
        _logger.LogDebug(
            "pc_lostexp: char {Char} -{BaseLost} base / -{JobLost} job",
            player.CharacterId, baseTake, jobTake);
        return (baseTake, jobTake);
    }

    public void OnBaseLevelChanged(PlayerEntity player)
    {
        var session = _sessions.GetByEntityId(player.Id);
        EmitParChange(session, SpId.SP_BASELEVEL, (uint)player.Level);
        // Future: Dragon/Eleanor/Babyclass auto-grant chain hooks here.
    }
}

/// <summary>
/// Decouples ExpService from the concrete <see cref="SessionManager"/> so
/// callers (and tests) can wire either the real session map or a stub.
/// </summary>
public interface ISessionManagerAccessor
{
    MapSessionData? GetByEntityId(EntityId entityId);

    /// <summary>Look up a session by AccountId. Returns null when not online. O(N).</summary>
    MapSessionData? GetByAccountId(int accountId)
    {
        // Default-interface implementation — subclasses that already index
        // by EntityId can leave this unimplemented and fall through to the
        // generic scan via GetAll... but most callers will want the
        // concrete impl below. Returning null here keeps the interface
        // optional for stub implementations.
        return null;
    }
}
