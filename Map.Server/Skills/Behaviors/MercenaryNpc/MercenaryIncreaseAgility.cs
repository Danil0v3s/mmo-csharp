using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.MercenaryNpc;

/// <summary>
/// MER_INCAGI — Mercenary Increase Agility. Manual port of
/// <c>rathena-fork/src/map/skills/mercenary/mercenary_increaseagility.cpp</c>.
/// On SC_CHANGEUNDEAD player targets, deals damage. Otherwise applies
/// SC_INCREASEAGI.
/// </summary>
public sealed class MercenaryIncreaseAgility : SkillImpl
{
    public MercenaryIncreaseAgility() : base(SkillIds.MER_INCAGI) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        if (target is PlayerEntity && ctx.Sc?.Get(target, StatusType.Changeundead) != null)
            return; // TODO: BF_MISC damage.
        ctx.Sc?.Start(target, StatusType.IncreaseAgi, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
    }
}
