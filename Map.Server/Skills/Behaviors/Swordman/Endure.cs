using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// SM_ENDURE — Swordsman Endure. Mirrors
/// <c>rathena-fork/src/map/skills/swordman/endure.cpp</c>.
///
/// Applies <see cref="StatusType.Endure"/> on the caster:
///   Val1 = lv, Val2 = 7 hits remaining before auto-expire.
/// Duration <c>10 + 10 * lv</c> seconds.
/// </summary>
public sealed class Endure : SkillImpl
{
    public Endure() : base(SkillIds.SM_ENDURE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc == null) return;
        var durationMs = 10_000 + 10_000 * skillLevel;
        // Endure is self-buff regardless of which target the client picked.
        ctx.Sc.Start(src, StatusType.Endure, val1: skillLevel, val2: 7, val3: 0, val4: 0, durationMs, src);
    }
}
