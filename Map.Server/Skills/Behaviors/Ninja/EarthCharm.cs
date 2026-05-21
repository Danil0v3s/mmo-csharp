using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// KO_DOHU_KOUKAI — Earth Charm. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/earthcharm.cpp</c>.
/// Adds an earth spirit charm (element EARTH) to the player. Charm
/// grant service is TODO — animation only.
/// </summary>
public sealed class EarthCharm : SkillImpl
{
    public EarthCharm() : base(SkillIds.KO_DOHU_KOUKAI) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        // TODO: pc_addspiritcharm(sd, time, MAX_SPIRITCHARM, CHARM_TYPE_EARTH).
    }
}
