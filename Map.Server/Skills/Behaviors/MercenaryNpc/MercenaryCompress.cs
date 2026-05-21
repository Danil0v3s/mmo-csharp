using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.MercenaryNpc;

/// <summary>
/// MER_COMPRESS — Mercenary Compress. Manual port of
/// <c>rathena-fork/src/map/skills/mercenary/mercenary_compress.cpp</c>.
/// Cleanses SC_BLEEDING.
/// </summary>
public sealed class MercenaryCompress : SkillImpl
{
    public MercenaryCompress() : base(SkillIds.MER_COMPRESS) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.End(target, StatusType.Bleeding);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
