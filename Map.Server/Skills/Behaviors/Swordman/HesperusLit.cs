using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// LG_HESPERUSLIT — Royal Guard Hesperus:Lit (skill.cpp:LG_HESPERUSLIT).
/// Ratio: <c>baseRatio + (-100 + 300*lv) + VIT/6</c>; with
/// <c>SC_INSPIRATION</c> active: <c>baseRatio + (-100 + 450*lv) + VIT/6</c>.
/// </summary>
public sealed class HesperusLit : WeaponSkillImpl
{
    public HesperusLit() : base(SkillIds.LG_HESPERUSLIT) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var lvMul = ctx.Sc?.Get(src, StatusType.Inspiration) != null ? 450 : 300;
        return baseRatio + (-100 + lvMul * skillLevel) + src.Stats.Vit / 6;
    }
}
