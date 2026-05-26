using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// TR_METALIC_FURY — Trouvere Metallic Fury. Manual port of
/// <c>rathena-fork/src/map/skills/archer/metallicfury.cpp</c>.
///
/// <para>Ratio: <c>+(-100 + 3850*lv)</c>; if the target carries
/// SC_SOUNDBLEND, adds <c>800*lv + 2*TR_STAGE_MANNER*SPL</c>.</para>
/// </summary>
public sealed class MetallicFury : RecursiveDamageSplashSkillImpl
{
    public MetallicFury() : base(SkillIds.TR_METALIC_FURY) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx, int miscflag)
    {
        var ratio = baseRatio + (-100 + 3850 * skillLevel);
        if (ctx.Sc != null && ctx.Sc.Get(target, StatusType.Soundblend) != null)
        {
            ratio += 800 * skillLevel;
            var stage = (src is PlayerEntity pc) ? (ctx.PlayerSkill?.CheckSkill(pc, SkillIds.TR_STAGE_MANNER) ?? 0) : 0;
            ratio += 2 * stage * src.Stats.Spl;
        }
        return ratio;
    }
}
