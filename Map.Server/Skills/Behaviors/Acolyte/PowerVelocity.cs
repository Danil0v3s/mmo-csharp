using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// SR_POWERVELOCITY — Sura Power Velocity. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/powervelocity.cpp</c>.
///
/// <para>Transfers all of the caster's Spirit Spheres to the target
/// player (capped at 5 on the target). Caster loses every sphere
/// in the process — mutually exclusive with Ki Translation since
/// this one empties the caster.</para>
/// </summary>
public sealed class PowerVelocity : SkillImpl
{
    private readonly IPlayerOrbService? _orbs;

    public PowerVelocity() : base(SkillIds.SR_POWERVELOCITY) { }

    public PowerVelocity(IPlayerOrbService? orbs = null) : base(SkillIds.SR_POWERVELOCITY)
    {
        _orbs = orbs;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (target is not PlayerEntity dstsd) return;

        if (src is PlayerEntity sd)
        {
            var dstSpheres = _orbs?.Get(dstsd, OrbKind.Spirit) ?? 0;
            if (dstSpheres <= 5)
            {
                var transfer = Math.Min(5 - dstSpheres, _orbs?.Get(sd, OrbKind.Spirit) ?? 0);
                if (transfer > 0)
                {
                    _orbs?.Add(dstsd, OrbKind.Spirit, transfer);
                    _orbs?.Remove(sd, OrbKind.Spirit, _orbs?.Get(sd, OrbKind.Spirit) ?? 0);
                }
            }
        }
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
