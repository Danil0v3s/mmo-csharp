using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// TF_DETOXIFY — Detoxify. Manual port of
/// <c>rathena-fork/src/map/skills/thief/detoxify.cpp</c>.
/// Removes SC_POISON and SC_DPOISON from the target.
/// </summary>
public sealed class Detoxify : SkillImpl
{
    public Detoxify() : base(SkillIds.TF_DETOXIFY) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        ctx.Sc?.End(target, StatusType.Poison);
        ctx.Sc?.End(target, StatusType.DeadlyPoison);
    }
}
