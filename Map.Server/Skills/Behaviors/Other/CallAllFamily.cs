using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// WE_CALLALLFAMILY — Call All Family. Manual port of
/// <c>rathena-fork/src/map/skills/other/callallfamily.cpp</c>.
/// Teleports partner + child to the caster's cell. Family/marriage
/// lookup deferred (family-link table not yet on PlayerEntity).
/// </summary>
public sealed class CallAllFamily : SkillImpl
{
    public CallAllFamily() : base(SkillIds.WE_CALLALLFAMILY) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity) return;
        // Deferred: pc_get_partner + pc_get_child lookups + pc_setpos to src cell — needs family-link table.
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
    }
}
