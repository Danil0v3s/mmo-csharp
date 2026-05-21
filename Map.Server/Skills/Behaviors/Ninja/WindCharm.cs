using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// KO_KAZEHU_SEIRAN — Wind Charm. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/windcharm.cpp</c>.
/// Adds a wind spirit charm (element WIND) to the player. Charm
/// grant service is TODO — animation only.
/// </summary>
public sealed class WindCharm : SkillImpl
{
    public WindCharm() : base(SkillIds.KO_KAZEHU_SEIRAN) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        // TODO: pc_addspiritcharm(sd, time, MAX_SPIRITCHARM, CHARM_TYPE_WIND).
    }
}
