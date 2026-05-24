using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// TF_STEAL — Steal (skill.cpp:TF_STEAL arm). Calls
/// <c>pc_steal_item(sd, target, skill_lv)</c> via the thread-through
/// <see cref="SkillBehaviorContext.Steal"/> service. Animation
/// broadcast happens regardless; the SC service rolls success/failure
/// against the mob's drop table.
/// </summary>
public sealed class Steal : SkillImpl
{
    public Steal() : base(SkillIds.TF_STEAL) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        if (src is PlayerEntity pc && target is MobEntity mob)
        {
            ctx.Steal?.TrySteal(pc, mob);
        }
    }
}
