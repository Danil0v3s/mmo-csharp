using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// RG_CLEANER — Remover. Manual port of
/// <c>rathena-fork/src/map/skills/thief/remover.cpp</c>.
/// Removes graffiti units in splash radius. Graffiti removal is TODO.
/// </summary>
public sealed class Remover : SkillImpl
{
    public Remover() : base(SkillIds.RG_CLEANER) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
