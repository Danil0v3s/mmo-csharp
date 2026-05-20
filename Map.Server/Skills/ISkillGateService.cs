using Map.Server.Entities;

namespace Map.Server.Skills;

/// <summary>
/// Map-flag + state gating for skill casts. Canonical entry points
/// for rAthena <c>skill_isNotOk</c>, <c>skill_isNotOk_hom</c>,
/// <c>skill_isNotOk_mercenary</c>, <c>skill_isNotOk_npcRange</c>,
/// and <c>skill_pos_maxcount_check</c> (skill.cpp). The PC gate is
/// the busiest — it consults the nopvp / noskill / gvg / battleground
/// map flags + the requested-skill's NoCastMask + state bitfield.
/// </summary>
public interface ISkillGateService
{
    /// <summary>rAthena <c>skill_isNotOk</c> — PC pre-cast gate.</summary>
    bool IsNotOk(PlayerEntity caster, ushort skillId);

    /// <summary>rAthena <c>skill_isNotOk_hom</c>.</summary>
    bool IsNotOkHom(Entity homun, ushort skillId);

    /// <summary>rAthena <c>skill_isNotOk_mercenary</c>.</summary>
    bool IsNotOkMercenary(Entity merc, ushort skillId);

    /// <summary>rAthena <c>skill_isNotOk_npcRange</c>.</summary>
    bool IsNotOkNpcRange(Entity caster, ushort skillId, short x, short y);

    /// <summary>rAthena <c>skill_pos_maxcount_check</c> — cap concurrent ground units per caster.</summary>
    bool PosMaxcountCheck(Entity caster, ushort skillId, ushort skillLevel);
}
