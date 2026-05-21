using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// WE_CALLALLFAMILY — Call All Family. Manual port of
/// <c>rathena-fork/src/map/skills/other/callallfamily.cpp</c>.
/// Teleports partner + child to the caster's cell. Family/marriage
/// lookup + pc_setpos pipeline are TODO.
/// </summary>
public sealed class CallAllFamily : SkillImpl
{
    public CallAllFamily() : base(SkillIds.WE_CALLALLFAMILY) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity) return;
        // TODO: pc_get_partner + pc_get_child lookups, then pc_setpos to src cell.
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
    }
}
