using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// RK_STONEHARDSKIN — Rune Knight Stone Hard Skin. rAthena requires
/// <c>pc_checkskill(sd, RK_RUNEMASTERY) &gt;= 4</c> (skill.cpp:11295).
/// Applies SC_STONEHARDSKIN; without the rune-mastery prereq the
/// cast fails silently.
/// </summary>
public sealed class StoneHardSkin : SkillImpl
{
    public StoneHardSkin() : base(SkillIds.RK_STONEHARDSKIN) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity pc) return;
        if ((ctx.PlayerSkill?.CheckSkill(pc, SkillIds.RK_RUNEMASTERY) ?? 0) < 4) return;
        ctx.Sc?.Start(target, StatusType.Stonehardskin, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
