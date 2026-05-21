using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// RA_WUGRIDER — Ranger Warg Rider. Manual port of
/// <c>rathena-fork/src/map/skills/archer/wargrider.cpp</c>.
/// Toggles OPTION_WUG ↔ OPTION_WUGRIDER (option service TODO).
/// </summary>
public sealed class WargRider : SkillImpl
{
    public WargRider() : base(SkillIds.RA_WUGRIDER) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity) return;
        // TODO: swap OPTION_WUG ↔ OPTION_WUGRIDER via IPlayerOptionService.
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
