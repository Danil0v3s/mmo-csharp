using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.LordKnight;

/// <summary>
/// LK_TENSIONRELAX — Lord Knight Tension Relax. Mirrors
/// <c>rathena-fork/src/map/skills/lordknight/tensionrelax.cpp</c>.
///
/// Apply <see cref="StatusType.Tensionrelax"/> on caster — sits
/// the character and accelerates HP regen massively. Toggled
/// off on movement or hostile action. Indefinite duration.
/// </summary>
public sealed class TensionRelax : SkillImpl
{
    public TensionRelax() : base(SkillIds.LK_TENSIONRELAX) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(src, StatusType.Tensionrelax, val1: skillLevel, 0, 0, 0,
            durationMs: -1, src);
    }
}
