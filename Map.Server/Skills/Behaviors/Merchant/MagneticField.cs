using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// NC_MAGNETICFIELD — Mechanic Magnetic Field. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/magneticfield.cpp</c>.
/// Splash SC_MAGNETICFIELD application. Splash dispatch TODO.
/// </summary>
public sealed class MagneticField : SkillImpl
{
    public MagneticField() : base(SkillIds.NC_MAGNETICFIELD) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(target, StatusType.Magneticfield, val1: skillLevel, val2: (int)src.Id, 0, 0, durationMs: 5000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
