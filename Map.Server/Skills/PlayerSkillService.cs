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
}
