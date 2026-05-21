using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.ElementalNpc;

/// <summary>
/// EM_EL_EYES_OF_STORM — Elemental Eyes Of Storm. Manual port of
/// <c>rathena-fork/src/map/skills/elemental/eyesofstorm.cpp</c>.
/// Toggles SC_EYES_OF_STORM on target + SC_EYES_OF_STORM_OPTION on
/// the elemental.
/// </summary>
public sealed class EyesOfStorm : SkillImpl
{
    public EyesOfStorm() : base(SkillIds.EM_EL_EYES_OF_STORM) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc?.Get(target, StatusType.EyesOfStorm) != null
            || ctx.Sc?.Get(src, StatusType.EyesOfStormOption) != null)
        {
            ctx.Sc.End(target, StatusType.EyesOfStorm);
            ctx.Sc.End(src, StatusType.EyesOfStormOption);
            return;
        }
        ctx.Sc?.Start(src, StatusType.EyesOfStormOption, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Sc?.Start(target, StatusType.EyesOfStorm, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
    }
}
