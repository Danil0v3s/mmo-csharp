using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.LordKnight;

/// <summary>
/// LK_BERSERK — Lord Knight Berserk (Frenzy). Mirrors
/// <c>rathena-fork/src/map/skills/lordknight/berserk.cpp</c>.
///
/// Apply <see cref="StatusType.Berserk"/>: triples MaxHp, sets HP
/// to max, +ASPD, locks SP regen, can't cast skills. Duration 3
/// minutes; SP drain consumes it earlier in practice.
/// </summary>
public sealed class Berserk : SkillImpl
{
    public Berserk() : base(SkillIds.LK_BERSERK) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(src, StatusType.Berserk, val1: skillLevel, 0, 0, 0,
            durationMs: 180_000, src);
    }
}
