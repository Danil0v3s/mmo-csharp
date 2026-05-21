using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// CH_SOULCOLLECT — Champion Zen / Soul Collect. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/zen.cpp</c>.
///
/// <para>Instantly fills the caster's Spirit Sphere counter to its
/// cap (5 by default, +Val1 of <see cref="StatusType.Raisingdragon"/>
/// when active — Raising Dragon raises the max).</para>
/// </summary>
public sealed class Zen : SkillImpl
{
    private readonly IPlayerOrbService? _orbs;

    public Zen() : base(SkillIds.CH_SOULCOLLECT) { }

    public Zen(IPlayerOrbService? orbs = null) : base(SkillIds.CH_SOULCOLLECT)
    {
        _orbs = orbs;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity sd) return;

        // rAthena: int32 limit = 5; if (SC_RAISINGDRAGON) limit += sc->val1;
        int limit = 5;
        var raising = ctx.Sc?.Get(sd, StatusType.Raisingdragon);
        if (raising != null) limit += raising.Val1;

        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);

        // rAthena: for (i=0; i<limit; i++) pc_addspiritball(sd, skill_get_time(...), limit);
        // We add limit balls at once; the orb service clamps to cap.
        _orbs?.Add(sd, OrbKind.Spirit, limit);
    }
}
