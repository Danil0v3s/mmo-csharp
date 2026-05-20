using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors;

/// <summary>
/// PR_KYRIE (id 73) — Priest Kyrie Eleison. rAthena
/// <c>skill.cpp:case PR_KYRIE</c>: applies
/// <see cref="StatusType.Kyrie"/> on the target — HP shield that
/// absorbs incoming damage up to <c>Val1 = target.MaxHp * (12 + 2*lv)/100</c>
/// hp, and lasts for <c>Val2 = 5 + lv</c> hits, whichever comes first.
/// Consumer hook is <see cref="Combat.DamageService"/>'s
/// ApplyScDamageReduction (T2.4b+).
///
/// Duration <c>120_000</c> ms in renewal (auto-expires even if shield
/// + hit counter still have headroom).
/// </summary>
public sealed class KyrieEleisonBehavior : ISkillBehavior
{
    public ushort SkillId => SkillIds.PR_KYRIE;

    public bool Resolve(Entity source, Entity target, SkillDefinition def, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc == null) return true;

        // Shield = (12 + 2*lv) % MaxHp; hit counter = 5 + lv.
        var shieldHp = target.Stats.MaxHp * (12 + 2 * skillLevel) / 100;
        var hitCount = 5 + skillLevel;
        ctx.Sc.Start(target, StatusType.Kyrie,
            val1: Math.Max(1, shieldHp), val2: hitCount, val3: 0, val4: 0,
            durationMs: 120_000, source);
        return true;
    }
}
