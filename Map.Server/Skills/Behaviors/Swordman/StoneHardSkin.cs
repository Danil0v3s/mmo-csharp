using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// RK_STONEHARDSKIN — Rune Knight Stone Hard Skin. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/stonehardskin.cpp</c>.
/// Requires RK_RUNEMASTERY ≥ 4 (TODO). Applies SC_STONEHARDSKIN.
/// </summary>
public sealed class StoneHardSkin : SkillImpl
{
    public StoneHardSkin() : base(SkillIds.RK_STONEHARDSKIN) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity) return;
        // Deferred: rAthena requires pc_checkskill(sd, RK_RUNEMASTERY) >= 4;
        // RK_RUNEMASTERY is not yet in SkillIds. Once added, gate this branch
        // on ctx.PlayerSkill?.CheckSkill(sd, SkillIds.RK_RUNEMASTERY) >= 4.
        ctx.Sc?.Start(target, StatusType.Stonehardskin, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
