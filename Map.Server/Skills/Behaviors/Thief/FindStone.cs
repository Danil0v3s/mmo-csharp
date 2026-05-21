using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// TF_PICKSTONE — Find Stone. Manual port of
/// <c>rathena-fork/src/map/skills/thief/findstone.cpp</c>.
/// Grants 1× Stone (ITEMID_STONE) via pc_additem. Item grant is TODO.
/// </summary>
public sealed class FindStone : SkillImpl
{
    public FindStone() : base(SkillIds.TF_PICKSTONE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
