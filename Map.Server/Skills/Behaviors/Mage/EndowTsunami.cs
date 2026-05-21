using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SA_FROSTWEAPON — Sage Endow Tsunami. Manual port of
/// <c>rathena-fork/src/map/skills/mage/endowtsunami.cpp</c>. Endows weapon
/// with the Water element (SC_WATERWEAPON). Renewal: 100 % rate.
/// </summary>
public sealed class EndowTsunami : SkillImpl
{
    public EndowTsunami() : base(SkillIds.SA_FROSTWEAPON) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(target, StatusType.Waterweapon, val1: skillLevel, 0, 0, 0, durationMs: 60_000 * skillLevel, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
