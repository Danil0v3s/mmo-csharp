using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// KO_KAHU_ENTEN — Fire Charm. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/firecharm.cpp</c>.
/// Adds a fire spirit charm (element FIRE) to the player up to
/// MAX_SPIRITCHARM. Charm grant service is TODO — animation only.
/// </summary>
public sealed class FireCharm : SkillImpl
{
    public FireCharm() : base(SkillIds.KO_KAHU_ENTEN) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        // TODO: pc_addspiritcharm(sd, time, MAX_SPIRITCHARM, CHARM_TYPE_FIRE) — needs IPlayerOrbsService API for fire charm.
    }
}
