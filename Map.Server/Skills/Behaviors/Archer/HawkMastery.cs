using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// WH_HAWK_M — Wind Hawk Mastery (toggle Falcon look). Manual port of
/// <c>rathena-fork/src/map/skills/archer/hawkmastery.cpp</c>. Player-
/// option toggle TODO; broadcast only.
/// </summary>
public sealed class HawkMastery : SkillImpl
{
    public HawkMastery() : base(SkillIds.WH_HAWK_M) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity) return;
        // TODO: toggle OPTION_FALCON via IPlayerOptionService.
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
