using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// SS_AKUMUKESU — Nightmare Erasion. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/nightmareerasion.cpp</c>.
/// Splash dispel: ends SC_NIGHTMARE on every enemy in range.
/// </summary>
public sealed class NightmareErasion : SkillImpl
{
    public NightmareErasion() : base(SkillIds.SS_AKUMUKESU) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        ctx.Sc?.End(target, StatusType.Nightmare);
        // TODO: splash via skill_area_sub to clear nearby targets too.
    }
}
