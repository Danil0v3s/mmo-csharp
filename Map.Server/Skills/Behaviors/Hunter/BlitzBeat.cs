using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Hunter;

/// <summary>
/// HT_BLITZBEAT — Hunter Blitz Beat. Mirrors
/// <c>rathena-fork/src/map/skills/hunter/blitzbeat.cpp</c>.
///
/// Falcon-driven Wind hit. Hit count = min(lv, 5). Per-hit damage
/// = (dex*int/10) + level + int.
/// </summary>
public sealed class BlitzBeat : SkillImpl
{
    public BlitzBeat() : base(SkillIds.HT_BLITZBEAT) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var hitCount = Math.Min((int)skillLevel, 5);
        var perHit = (src.Stats.Dex * src.Stats.IntStat) / 10 + src.Level + src.Stats.IntStat;
        for (var hit = 0; hit < hitCount; hit++)
        {
            ctx.Damage.ApplyDamage(target, Math.Max(1, perHit), src);
        }
    }
}
