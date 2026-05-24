using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// RK_GIANTGROWTH — Rune Knight Giant Growth. rAthena requires
/// <c>pc_checkskill(sd, RK_RUNEMASTERY) &gt;= 1</c> (skill.cpp:11268); the
/// rune-mastery gate is the canonical "is the caster a Rune Knight
/// who actually trained runes" check. Applies SC_GIANTGROWTH.
/// </summary>
public sealed class GiantGrowth : SkillImpl
{
    public GiantGrowth() : base(SkillIds.RK_GIANTGROWTH) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity pc) return;
        if ((ctx.PlayerSkill?.CheckSkill(pc, SkillIds.RK_RUNEMASTERY) ?? 0) < 1) return;
        ctx.Sc?.Start(target, StatusType.Giantgrowth, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
