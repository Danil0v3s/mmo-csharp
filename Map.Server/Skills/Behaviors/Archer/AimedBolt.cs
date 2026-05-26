using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// RA_AIMEDBOLT — Ranger Aimed Bolt. Manual port of
/// <c>rathena-fork/src/map/skills/archer/aimedbolt.cpp</c>.
///
/// <para>Ratio: <c>+(-100 + 500 + 20*lv)</c> baseline; with
/// SC_FEARBREEZE on the caster, swap to <c>+(-100 + 800 + 35*lv)</c>.</para>
/// </summary>
public sealed class AimedBolt : WeaponSkillImpl
{
    public AimedBolt() : base(SkillIds.RA_AIMEDBOLT) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx, int miscflag)
    {
        if (ctx.Sc != null && ctx.Sc.Get(src, StatusType.Fearbreeze) != null)
            return baseRatio + (-100 + 800 + 35 * skillLevel);
        return baseRatio + (-100 + 500 + 20 * skillLevel);
    }
}
