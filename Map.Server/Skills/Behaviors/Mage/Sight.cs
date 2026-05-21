using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// MG_SIGHT — Mage Sight. Applies SC_SIGHT (anti-hide detection) on
/// self with skill id stored in Val2 for unhide identification.
/// </summary>
public sealed class Sight : SkillImpl
{
    public Sight() : base(SkillIds.MG_SIGHT) { }
    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        bool landed = ctx.Sc?.Start(target, StatusType.Sight,
            val1: skillLevel, val2: SkillId, 0, 0, durationMs: 30_000, src) != null;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel, landed);
    }
}
