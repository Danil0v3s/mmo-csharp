using Map.Server.Entities;
using Map.Server.Skills;
using Map.Server.Visibility;

namespace Map.Server.Gm.Commands;

/// <summary>
/// <c>@statall</c> / <c>@statsall</c> / <c>@allstats [n]</c> — set every
/// stat to the maximum. rAthena <c>atcommand_statall</c>
/// (atcommand.cpp). The C# port doesn't yet carry the full STR/AGI/
/// VIT/INT/DEX/LUK on PlayerEntity at the GM-tweakable level — that
/// lives behind status_calc_pc. The atcommand still has a clear
/// canonical entry: grant the configured stat cap and let the next
/// status calc resolve.
/// </summary>
public sealed class StatAllCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "statall";
    public string Description => "@statall — set every stat to max.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        // rAthena pc_setstat caps at battle_config.max_parameter
        // (default 99). The actual STR/AGI/VIT/INT/DEX/LUK fields
        // sit on the calc service; until that surface ports the
        // GM-side helper, give back unspent status points so the
        // player can allocate manually.
        var bonus = 99 * 6;
        caller.StatusPoints = Math.Max(caller.StatusPoints, bonus);
        GmCommandReply.Send(visibility, caller, $"@statall: {bonus} unspent status points granted (allocate via stat-up UI).");
        return Task.CompletedTask;
    }
}

/// <summary>
/// <c>@statsall</c> — alias of <c>@statall</c>.
/// </summary>
public sealed class StatsAllCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "statsall";
    public string Description => "@statsall — alias of @statall.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        var bonus = 99 * 6;
        caller.StatusPoints = Math.Max(caller.StatusPoints, bonus);
        GmCommandReply.Send(visibility, caller, $"@statsall: {bonus} unspent status points granted.");
        return Task.CompletedTask;
    }
}

/// <summary>
/// <c>@allstats [n]</c> — third alias of <c>@statall</c> with an
/// optional bonus override.
/// </summary>
public sealed class AllStatsCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "allstats";
    public string Description => "@allstats [n] — grant N unspent status points (default 594 = 99 cap × 6 stats).";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        int bonus = 99 * 6;
        if (args.Count > 0 && int.TryParse(args[0], out var parsed)) bonus = Math.Max(0, parsed);
        caller.StatusPoints = Math.Max(caller.StatusPoints, bonus);
        GmCommandReply.Send(visibility, caller, $"@allstats: {bonus} unspent status points granted.");
        return Task.CompletedTask;
    }
}

/// <summary>
/// <c>@statuspoint [n]</c> — add status points to the unspent pool.
/// rAthena <c>atcommand_statuspoint</c>.
/// </summary>
public sealed class StatusPointCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "statuspoint";
    public string Description => "@statuspoint <n> — add N status points to your unspent pool.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0 || !int.TryParse(args[0], out var n))
        {
            GmCommandReply.Send(visibility, caller, "@statuspoint: usage — @statuspoint <n>");
            return Task.CompletedTask;
        }
        // rAthena clamps to MAX_LEVEL_STATUS_POINT (~9999); we
        // keep the same shape — clamp negative + bound the sum.
        var sum = (long)caller.StatusPoints + n;
        if (sum < 0) sum = 0;
        if (sum > int.MaxValue) sum = int.MaxValue;
        caller.StatusPoints = (int)sum;
        GmCommandReply.Send(visibility, caller, $"@statuspoint: status points now {caller.StatusPoints}.");
        return Task.CompletedTask;
    }
}

/// <summary>
/// <c>@traitpoint [n]</c> — add trait (POW/STA/WIS/SPL/CON/CRT) points
/// to the renewal trait pool. rAthena <c>atcommand_traitpoint</c>.
/// </summary>
public sealed class TraitPointCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "traitpoint";
    public string Description => "@traitpoint <n> — add N trait points (renewal POW/STA/WIS/SPL/CON/CRT pool).";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0 || !int.TryParse(args[0], out var n))
        {
            GmCommandReply.Send(visibility, caller, "@traitpoint: usage — @traitpoint <n>");
            return Task.CompletedTask;
        }
        var sum = (long)caller.TraitPoints + n;
        if (sum < 0) sum = 0;
        if (sum > int.MaxValue) sum = int.MaxValue;
        caller.TraitPoints = (int)sum;
        GmCommandReply.Send(visibility, caller, $"@traitpoint: trait points now {caller.TraitPoints}.");
        return Task.CompletedTask;
    }
}

/// <summary>
/// <c>@skillpoint &lt;n&gt;</c> — add skill points to the unspent pool.
/// rAthena <c>atcommand_skillpoint</c>.
/// </summary>
public sealed class SkillPointCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "skillpoint";
    public string Description => "@skillpoint <n> — add N skill points to your unspent pool.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0 || !int.TryParse(args[0], out var n))
        {
            GmCommandReply.Send(visibility, caller, "@skillpoint: usage — @skillpoint <n>");
            return Task.CompletedTask;
        }
        var sum = (long)caller.SkillPoints + n;
        if (sum < 0) sum = 0;
        if (sum > int.MaxValue) sum = int.MaxValue;
        caller.SkillPoints = (int)sum;
        GmCommandReply.Send(visibility, caller, $"@skillpoint: skill points now {caller.SkillPoints}.");
        return Task.CompletedTask;
    }
}

/// <summary>
/// <c>@stats</c> — display caller's current stat summary. rAthena
/// <c>atcommand_stats</c>.
/// </summary>
public sealed class StatsCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "stats";
    public string Description => "@stats — display your level, HP/SP and unspent points.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        GmCommandReply.Send(visibility, caller,
            $"@stats: Lv {caller.Level}/{caller.JobLevel}  HP {caller.Hp}/{caller.MaxHp}  SP {caller.Sp}/{caller.MaxSp}  " +
            $"StatPts {caller.StatusPoints}  SkillPts {caller.SkillPoints}  TraitPts {caller.TraitPoints}.");
        return Task.CompletedTask;
    }
}

/// <summary>
/// <c>@allskill</c> — grant every skill in the caller's job tree at
/// max level. rAthena <c>atcommand_allskill</c>. C# stub: drop a
/// reasonable upper bound across the GD/aura skills + matching
/// per-class set. Until the skill_tree.yml loader ports we just give
/// the caller a generous skill point pool to allocate manually.
/// </summary>
public sealed class AllSkillCommand(
    IPlayerSkillService skills,
    IVisibilityService visibility) : IGmCommand
{
    public string Name => "allskill";
    public string Description => "@allskill — calc the caller's skill tree at max.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        // rAthena calls pc_allskillup which iterates the tree;
        // the skill tree loader isn't fully online yet, so the
        // canonical entry point is CalcSkillTree (which the
        // service knows how to no-op until tree YAML loads).
        skills.CalcSkillTree(caller);
        // Generous unspent skill points as a fallback so players
        // can allocate manually via the skill-up UI.
        caller.SkillPoints = Math.Max(caller.SkillPoints, 100);
        GmCommandReply.Send(visibility, caller, "@allskill: skill tree recalculated; +unspent skill points granted as fallback.");
        return Task.CompletedTask;
    }
}

/// <summary>
/// <c>@questskill &lt;skillId&gt;</c> — grant a quest-tagged skill via
/// PermanentGranted. rAthena <c>atcommand_questskill</c>.
/// </summary>
public sealed class QuestSkillCommand(
    IPlayerSkillService skills,
    IVisibilityService visibility) : IGmCommand
{
    public string Name => "questskill";
    public string Description => "@questskill <skillId> — grant a quest-flagged skill.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0 || !ushort.TryParse(args[0], out var sid))
        {
            GmCommandReply.Send(visibility, caller, "@questskill: usage — @questskill <skillId>");
            return Task.CompletedTask;
        }
        if (skills.Grant(caller, sid, level: 1, GrantKind.PermanentGranted))
            GmCommandReply.Send(visibility, caller, $"@questskill: skill {sid} granted.");
        else
            GmCommandReply.Send(visibility, caller, $"@questskill: skill {sid} not granted (invalid id?).");
        return Task.CompletedTask;
    }
}

/// <summary>
/// <c>@lostskill &lt;skillId&gt;</c> — remove a learned skill. rAthena
/// <c>atcommand_lostskill</c>.
/// </summary>
public sealed class LostSkillCommand(
    IPlayerSkillService skills,
    IVisibilityService visibility) : IGmCommand
{
    public string Name => "lostskill";
    public string Description => "@lostskill <skillId> — remove a learned skill.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0 || !ushort.TryParse(args[0], out var sid))
        {
            GmCommandReply.Send(visibility, caller, "@lostskill: usage — @lostskill <skillId>");
            return Task.CompletedTask;
        }
        skills.Revoke(caller, sid);
        GmCommandReply.Send(visibility, caller, $"@lostskill: skill {sid} removed.");
        return Task.CompletedTask;
    }
}
