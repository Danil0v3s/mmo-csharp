using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>
/// SP_SHA — Esha. Manual port of
/// <c>rathena-fork/src/map/skills/taekwon/esha.cpp</c>.
/// Recursive splash; ratio <c>+(-100 + 5*lv)</c>. Applies SC_SP_SHA on
/// hit and SC_USE_SKILL_SP_SHA on caster (counter chain).
/// </summary>
public sealed class Esha : RecursiveDamageSplashSkillImpl
{
    public Esha() : base(SkillIds.SP_SHA) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 5 * skillLevel);
}
