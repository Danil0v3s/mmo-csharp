using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>
/// TK_HIGHJUMP — High Jump. Manual port of
/// <c>rathena-fork/src/map/skills/taekwon/highjump.cpp</c>.
/// Caster jumps <c>skill_lv*2</c> cells in their facing direction
/// (4/3 on diagonals). Map-flag gating + cell teleport are TODO.
/// </summary>
public sealed class HighJump : SkillImpl
{
    public HighJump() : base(SkillIds.TK_HIGHJUMP) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
