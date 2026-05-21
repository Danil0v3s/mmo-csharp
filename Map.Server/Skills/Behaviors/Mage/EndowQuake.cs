using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SA_SEISMICWEAPON — Sage Endow Quake. Manual port of
/// <c>rathena-fork/src/map/skills/mage/endowquake.cpp</c>. Endows weapon
/// with the Earth element (SC_EARTHWEAPON). Renewal branch: 100 %
/// rate.
/// </summary>
public sealed class EndowQuake : SkillImpl
{
    public EndowQuake() : base(SkillIds.SA_SEISMICWEAPON) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(target, StatusType.Earthweapon, val1: skillLevel, 0, 0, 0, durationMs: 60_000 * skillLevel, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
