using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.DragonKnight;

/// <summary>
/// DK_DRAGONIC_AURA — Dragon Knight Dragonic Aura. Mirrors
/// <c>rathena-fork/src/map/skills/dragonknight/dragonicaura.cpp</c>.
///
/// Apply <see cref="StatusType.DragonicAura"/> on caster — boosts
/// subsequent Hack and Slasher hits with massive ATK + ignores
/// flee. Duration <c>30 + 30*lv</c> seconds.
/// </summary>
public sealed class DragonicAura : SkillImpl
{
    public DragonicAura() : base(SkillIds.DK_DRAGONIC_AURA) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(src, StatusType.DragonicAura, val1: skillLevel, 0, 0, 0,
            durationMs: (30 + 30 * skillLevel) * 1000, src);
    }
}
