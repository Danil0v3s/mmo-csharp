using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Skills;

/// <summary>
/// Default <see cref="IPlayerSkillService"/>. Validates against the
/// loaded <see cref="ISkillDb"/> so unknown ids don't slip into the
/// learned table and bork the client's skill window.
///
/// rAthena's <c>pc_skill</c> also calls <c>status_calc_pc(SCO_NONE)</c>
/// for passive skills so any stat bonus tied to the skill flips in. We
/// rely on the equip/recalc path catching the change indirectly — full
/// passive recalc lands when bonus_script (Phase 5) ships.
/// </summary>
public sealed class PlayerSkillService : IPlayerSkillService
{
    private readonly ISkillDb _db;
    private readonly ILogger<PlayerSkillService> _logger;

    public PlayerSkillService(ISkillDb db, ILogger<PlayerSkillService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public bool Grant(PlayerEntity pc, ushort skillId, int level, GrantKind kind = GrantKind.Permanent)
    {
        var def = _db.Get(skillId);
        if (def == null)
        {
            _logger.LogWarning("pc_skill: unknown skill {Id}", skillId);
            return false;
        }

        var current = pc.LearnedSkills.GetValueOrDefault(skillId);
        var target = kind switch
        {
            GrantKind.TemporaryAdd => Math.Min(current + level, def.MaxLevel),
            _ => level,
        };
        target = Math.Clamp(target, 0, def.MaxLevel);

        if (target == 0)
        {
            pc.LearnedSkills.Remove(skillId);
            _logger.LogInformation("pc_skill: char {Char} lost skill {Id}", pc.CharacterId, skillId);
            return true;
        }
        pc.LearnedSkills[skillId] = (byte)target;
        _logger.LogInformation(
            "pc_skill: char {Char} learned skill {Id} lv {Lv} ({Kind})",
            pc.CharacterId, skillId, target, kind);
        return true;
    }

    public void Revoke(PlayerEntity pc, ushort skillId)
        => Grant(pc, skillId, 0, GrantKind.Permanent);

    public void CalcSkillTree(PlayerEntity pc)
    {
        // No-op until skill_tree.yml / job_tree.yml port. LearnedSkills
        // is hydrated from char-server at session enter, so the visible
        // state already matches what the player paid for. Future
        // iteration walks parent → child prereqs and auto-grants the
        // free skills the rAthena tree marks at level 0.
    }

    public void CleanSkillTree(PlayerEntity pc)
    {
        // rAthena clears only PERMANENT_GRANTED entries. We don't track
        // skill_flag yet — full implementation will iterate and drop
        // selectively. Until then this is a documented no-op so callers
        // (job change, GM reset) can be wired in.
    }

    public bool TryPlagiarize(PlayerEntity pc, ushort skillId, ushort skillLevel)
    {
        var def = _db.Get(skillId);
        if (def == null) return false;
        pc.PlagiarizedSkillId = skillId;
        pc.PlagiarizedSkillLevel = (byte)skillLevel;
        return true;
    }

    public void PlagiarismReset(PlayerEntity pc, byte type)
    {
        pc.PlagiarizedSkillId = 0;
        pc.PlagiarizedSkillLevel = 0;
    }

    public bool Validate(PlayerEntity pc, ushort skillId, int level)
    {
        var def = _db.Get(skillId);
        if (def == null) return false;
        return level >= 0 && level <= def.MaxLevel;
    }

    public int CheckImperialGuard(PlayerEntity pc, ushort skillId)
        => pc.LearnedSkills.GetValueOrDefault(skillId);

    public int CheckSummoner(PlayerEntity pc, ushort skillType)
        => pc.LearnedSkills.GetValueOrDefault(skillType);
}
