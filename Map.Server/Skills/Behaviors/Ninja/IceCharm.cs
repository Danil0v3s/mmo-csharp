using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// KO_HYOUHU_HUBUKI — Ice Charm. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/icecharm.cpp</c>.
/// Adds a water spirit charm (element WATER) to the player. Charm
/// grant service is TODO — animation only.
/// </summary>
public sealed class IceCharm : SkillImpl
{
    public IceCharm() : base(SkillIds.KO_HYOUHU_HUBUKI) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        // TODO: pc_addspiritcharm(sd, time, MAX_SPIRITCHARM, CHARM_TYPE_WATER).
    }
}
