using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.ElementalNpc;

/// <summary>
/// EL_CIRCLE_OF_FIRE — Elemental Circle of Fire. Manual port of
/// <c>rathena-fork/src/map/skills/elemental/circleoffire.cpp</c>.
/// Toggles SC_CIRCLE_OF_FIRE on target + SC_CIRCLE_OF_FIRE_OPTION on
/// the elemental.
/// </summary>
public sealed class CircleOfFire : SkillImpl
{
    public CircleOfFire() : base(SkillIds.EL_CIRCLE_OF_FIRE) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 200;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc?.Get(target, StatusType.CircleOfFire) != null
            || ctx.Sc?.Get(src, StatusType.CircleOfFireOption) != null)
        {
            ctx.Sc.End(target, StatusType.CircleOfFire);
            ctx.Sc.End(src, StatusType.CircleOfFireOption);
            return;
        }
        ctx.Sc?.Start(src, StatusType.CircleOfFireOption, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Sc?.Start(target, StatusType.CircleOfFire, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
    }
}
