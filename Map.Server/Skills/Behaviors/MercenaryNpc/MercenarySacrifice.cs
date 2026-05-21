using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.MercenaryNpc;

/// <summary>
/// ML_DEVOTION — Mercenary Devotion / Sacrifice. Manual port of
/// <c>rathena-fork/src/map/skills/mercenary/mercenary_sacrifice.cpp</c>.
/// Only the mercenary's master can be devoted. Applies SC_DEVOTION with
/// val1 = caster id. Most level / class / HellPower / multi-source
/// gates are TODO.
/// </summary>
public sealed class MercenarySacrifice : SkillImpl
{
    public MercenarySacrifice() : base(SkillIds.ML_DEVOTION) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (target is not PlayerEntity) return;
        ctx.Sc?.Start(target, StatusType.Devotion, val1: (int)src.Id, val2: 0, 0, 0, durationMs: 30_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
