using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// GC_CROSSRIPPERSLASHER — Cross Ripper Slasher. Manual port of
/// <c>rathena-fork/src/map/skills/thief/crossripperslasher.cpp</c>.
/// Ratio <c>+(-100 + 80*lv + 3*Agi)</c>; <c>+val1*200</c> per active
/// SC_ROLLINGCUTTER spin on the caster. The skill requires
/// SC_ROLLINGCUTTER to be active — without it the cast fails silently.
/// </summary>
public sealed class CrossRipperSlasher : WeaponSkillImpl
{
    public CrossRipperSlasher() : base(SkillIds.GC_CROSSRIPPERSLASHER) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var ratio = baseRatio + (-100 + 80 * skillLevel) + src.Stats.Agi * 3;
        var cutter = ctx.Sc?.Get(src, StatusType.Rollingcutter);
        if (cutter != null)
            ratio += cutter.Val1 * 200;
        return ratio;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: requires SC_ROLLINGCUTTER on the caster to swing.
        if (ctx.Sc?.Get(src, StatusType.Rollingcutter) == null)
            return;
        base.CastendDamageId(src, target, skillLevel, ctx);
    }
}
