using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// SC_LAZINESS — Masquerade Laziness. Manual port of
/// <c>rathena-fork/src/map/skills/thief/masqueradelaziness.cpp</c>.
/// Same probabilistic SC application pattern as Ignorance.
/// </summary>
public sealed class MasqueradeLaziness : SkillImpl
{
    public MasqueradeLaziness() : base(SkillIds.SC_LAZINESS) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if ((target.Stats.Mode & MobMode.StatusImmune) != 0) return;
        var rate = Math.Clamp(src.Level / 10 + src.Stats.Dex / 8 + 50 + 10 * skillLevel
            - (target.Level / 10 + target.Stats.Agi / 5 + target.Stats.Luk / 10), skillLevel + src.Stats.Dex / 20, 100);
        if (System.Random.Shared.Next(100) < rate)
            ctx.Sc?.Start(target, StatusType.Laziness, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
