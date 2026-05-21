using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SA_LIGHTNINGLOADER — Sage Endow Tornado. Manual port of
/// <c>rathena-fork/src/map/skills/mage/endowtornado.cpp</c>. Endows weapon
/// with the Wind element (SC_WINDWEAPON). Renewal: 100 % rate.
/// </summary>
public sealed class EndowTornado : SkillImpl
{
    public EndowTornado() : base(SkillIds.SA_LIGHTNINGLOADER) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(target, StatusType.Windweapon, val1: skillLevel, 0, 0, 0, durationMs: 60_000 * skillLevel, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
