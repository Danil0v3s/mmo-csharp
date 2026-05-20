using Map.Server.Entities;

namespace Map.Server.Skills;

/// <summary>
/// Cast time + delay calculator. Canonical entry points for rAthena
/// <c>skill_castfix</c>, <c>skill_castfix_sc</c>, <c>skill_vfcastfix</c>
/// and <c>skill_delayfix</c> (skill.cpp:20193 — 20565).
///
/// The renewal damage path uses <see cref="VfCastFix"/> (variable +
/// fixed) while pre-renewal builds use <see cref="CastFix"/> +
/// <see cref="CastFixSc"/>. <see cref="DelayFix"/> applies after-cast
/// delay scaling for both. We expose all four so the cast lifecycle in
/// <see cref="ISkillCastService"/> can flag-switch when the renewal /
/// pre-renewal mode lands in <see cref="IBattleConfigService"/>.
/// </summary>
public interface ISkillCastTimingService
{
    /// <summary>rAthena <c>skill_castfix</c> — pre-renewal cast time.</summary>
    int CastFix(Entity caster, ushort skillId, ushort skillLevel);

    /// <summary>rAthena <c>skill_castfix_sc</c> — pre-renewal SC overlay.</summary>
    int CastFixSc(Entity caster, int time, byte flag = 0);

    /// <summary>rAthena <c>skill_vfcastfix</c> — renewal variable+fixed cast.</summary>
    int VfCastFix(Entity caster, int variableTime, ushort skillId, ushort skillLevel);

    /// <summary>rAthena <c>skill_delayfix</c> — after-cast delay.</summary>
    int DelayFix(Entity caster, ushort skillId, ushort skillLevel);
}
