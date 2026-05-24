using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.MercenaryNpc;

/// <summary>
/// MER_SCAPEGOAT — Mercenary Scapegoat. Manual port of
/// <c>rathena-fork/src/map/skills/mercenary/mercenary_scapegoat.cpp</c>.
/// Sacrifices the mercenary by transferring its current HP to the
/// master (heal), then dealing MaxHp damage to itself. Master link
/// + heal pipeline deferred (mercenary master-link not yet on
/// MercenaryEntity).
/// </summary>
public sealed class MercenaryScapegoat : SkillImpl
{
    public MercenaryScapegoat() : base(SkillIds.MER_SCAPEGOAT) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // Deferred: heal mer.master by src.Stats.Hp, then damage src by src.Stats.MaxHp — needs mercenary→master link plumbed into ctx.
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
