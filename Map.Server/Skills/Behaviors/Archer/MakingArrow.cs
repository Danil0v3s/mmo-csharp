using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// AC_MAKINGARROW — Archer Make Arrow. Manual port of
/// <c>rathena-fork/src/map/skills/archer/makingarrow.cpp</c>.
/// Opens the arrow-craft picker. clif_arrow_create_list packet not
/// wired here — broadcast only.
/// </summary>
public sealed class MakingArrow : SkillImpl
{
    public MakingArrow() : base(SkillIds.AC_MAKINGARROW) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity) return;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        // Deferred: clif_arrow_create_list to surface the arrow-craft picker — needs craft-list packet.
    }
}
