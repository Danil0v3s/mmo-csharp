using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Priest;

/// <summary>
/// PR_KYRIE — Priest Kyrie Eleison. Mirrors
/// <c>rathena-fork/src/map/skills/priest/kyrieeleison.cpp</c>.
///
/// Apply <see cref="StatusType.Kyrie"/> on target — HP shield
/// absorbs damage. Val1 = (12 + 2*lv)% of target.MaxHp HP shield,
/// Val2 = (5 + lv) hits. Duration 120 s.
/// </summary>
public sealed class KyrieEleison : SkillImpl
{
    public KyrieEleison() : base(SkillIds.PR_KYRIE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc == null) return;
        var shieldHp = target.Stats.MaxHp * (12 + 2 * skillLevel) / 100;
        var hitCount = 5 + skillLevel;
        ctx.Sc.Start(target, StatusType.Kyrie,
            val1: Math.Max(1, shieldHp), val2: hitCount, val3: 0, val4: 0,
            durationMs: 120_000, src);
    }
}
