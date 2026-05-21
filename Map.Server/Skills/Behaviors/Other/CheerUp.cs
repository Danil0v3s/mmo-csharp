using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// WE_CHEERUP — Baby Cheer Up. Manual port of
/// <c>rathena-fork/src/map/skills/other/cheerup.cpp</c>.
/// Applies SC_CHEERUP only to parents in 7×7 splash. Adoption lookup
/// + splash are TODO; we apply to the named target.
/// </summary>
public sealed class CheerUp : StatusSkillImpl
{
    public CheerUp() : base(SkillIds.WE_CHEERUP) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(target, StatusType.Cheerup, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
