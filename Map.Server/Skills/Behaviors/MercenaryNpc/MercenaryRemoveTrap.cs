using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.MercenaryNpc;

/// <summary>
/// MA_REMOVETRAP — Mercenary Remove Trap. Manual port of
/// <c>rathena-fork/src/map/skills/mercenary/mercenary_removetrap.cpp</c>.
/// Removes any skill unit flagged INF2_ISTRAP. Trap-unit lookup +
/// delete are TODO.
/// </summary>
public sealed class MercenaryRemoveTrap : SkillImpl
{
    public MercenaryRemoveTrap() : base(SkillIds.MA_REMOVETRAP) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // TODO: skill_delunit on target if it's a trap unit.
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
