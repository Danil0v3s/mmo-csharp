using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// HP_BASILICA — High Priest Basilica. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/basilica.cpp</c>.
///
/// <para>Renewal behavior: <see cref="StatusType.Basilica"/> applies
/// as a single-target SC (the caster stays rooted, the SC does the
/// PVP-block work). Pre-renewal placed a ground unit at the cast
/// cell — that branch isn't ported (the codebase targets renewal).</para>
/// </summary>
public sealed class Basilica : StatusSkillImpl
{
    public Basilica() : base(SkillIds.HP_BASILICA) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena renewal path: just apply the SC. The C# port follows
        // the renewal codepath.
        ctx.Sc?.Start(target, StatusType.Basilica,
            val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // Pre-renewal ground-unit placement is intentionally out of scope —
        // the codebase targets renewal where the SC carries the entire effect.
    }
}
