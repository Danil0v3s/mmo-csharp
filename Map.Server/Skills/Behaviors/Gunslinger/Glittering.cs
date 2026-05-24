using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// GS_GLITTERING — Gunslinger Glittering Coin. Manual port of
/// <c>rathena-fork/src/map/skills/gunslinger/glittering.cpp</c>.
/// (20 + 10*lv)% to gain a coin orb, otherwise lose one.
/// </summary>
public sealed class Glittering : SkillImpl
{
    public Glittering() : base(SkillIds.GS_GLITTERING) { }

    private readonly Random _rng = Random.Shared;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity sd) return;
        // Rebellion's RL_RICHS_COIN auto-grants 10 coins (no gain/loss roll).
        if (sd.LearnedSkills.GetValueOrDefault(SkillIds.RL_RICHS_COIN) > 0)
        {
            ctx.Orbs?.Add(sd, Map.Server.Status.OrbKind.Spirit, 10);
        }
        else if (_rng.Next(100) < 20 + 10 * skillLevel)
        {
            ctx.Orbs?.Add(sd, Map.Server.Status.OrbKind.Spirit, 1);
        }
        else
        {
            ctx.Orbs?.Remove(sd, Map.Server.Status.OrbKind.Spirit, 1);
        }
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
