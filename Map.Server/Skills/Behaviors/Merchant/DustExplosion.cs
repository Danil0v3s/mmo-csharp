using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// BO_DUST_EXPLOSION — Biolo Dust Explosion. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/dustexplosion.cpp</c>.
/// Ratio: <c>+(-100 + 450 + 600*lv) + 5*POW</c>; when the caster has
/// SC_RESEARCHREPORT active, adds <c>+200*lv</c>.
/// </summary>
public sealed class DustExplosion : RecursiveDamageSplashSkillImpl
{
    public DustExplosion() : base(SkillIds.BO_DUST_EXPLOSION) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var ratio = baseRatio + (-100 + 450 + 600 * skillLevel) + 5 * src.Stats.Pow;
        if (ctx.Sc?.Get(src, StatusType.Researchreport) != null)
            ratio += 200 * skillLevel;
        return ratio;
    }
}
