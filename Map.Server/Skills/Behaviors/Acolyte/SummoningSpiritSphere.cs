using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// MO_CALLSPIRITS — Monk Summon Spirit Sphere. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/summoningspiritsphere.cpp</c>.
///
/// <para>Adds 1 Spirit Sphere to the caster (cap raised by
/// Raising Dragon SC). Cap = <c>skillLevel + SC_RAISINGDRAGON->val1</c>.</para>
/// </summary>
public sealed class SummoningSpiritSphere : SkillImpl
{
    private readonly IPlayerOrbService? _orbs;

    public SummoningSpiritSphere() : base(SkillIds.MO_CALLSPIRITS) { }

    public SummoningSpiritSphere(IPlayerOrbService? orbs = null) : base(SkillIds.MO_CALLSPIRITS)
    {
        _orbs = orbs;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity sd) return;
        int limit = skillLevel;
        var raising = ctx.Sc?.Get(sd, StatusType.Raisingdragon);
        if (raising != null) limit += raising.Val1;

        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        // rAthena: pc_addspiritball(sd, time, limit) — adds 1 ball, capped at limit.
        _orbs?.Add(sd, OrbKind.Spirit, 1);
    }
}
