using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// SR_RAISINGDRAGON — Sura Raising Dragon. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/raisingdragon.cpp</c>.
///
/// <para>Self-buff that raises the Spirit Sphere cap to <c>5 + lv</c>,
/// fills the new cap with Spheres, and applies SC_EXPLOSIONSPIRITS
/// for the buff window.</para>
/// </summary>
public sealed class RaisingDragon : StatusSkillImpl
{
    private readonly IPlayerOrbService? _orbs;

    public RaisingDragon() : base(SkillIds.SR_RAISINGDRAGON) { }

    public RaisingDragon(IPlayerOrbService? orbs = null) : base(SkillIds.SR_RAISINGDRAGON)
    {
        _orbs = orbs;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity sd) return;

        var max = 5 + skillLevel;
        // rAthena: sc_start(SC_EXPLOSIONSPIRITS, 100, lv, skill_get_time(...))
        ctx.Sc?.Start(target, StatusType.Explosionspirits,
            val1: skillLevel, 0, 0, 0, durationMs: 60_000 + 30_000 * skillLevel, src);

        // Apply SC_RAISINGDRAGON itself (StatusSkillImpl's normal apply path).
        ctx.Sc?.Start(target, StatusType.Raisingdragon,
            val1: skillLevel, 0, 0, 0, durationMs: 60_000 + 30_000 * skillLevel, src);

        // Fill spirit balls to the new cap.
        _orbs?.Add(sd, OrbKind.Spirit, max);

        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
