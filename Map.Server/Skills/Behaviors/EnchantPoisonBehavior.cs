using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors;

/// <summary>
/// AS_ENCHANTPOISON (id 138) — Assassin Enchant Poison. rAthena
/// <c>skill.cpp:case AS_ENCHANTPOISON</c>: applies
/// <see cref="StatusType.Encpoison"/> on the target — weapon attacks
/// become Poison element + roll a small Poison-proc chance per hit.
/// Duration <c>60_000 + 60_000 * lv</c> ms.
/// </summary>
public sealed class EnchantPoisonBehavior : ISkillBehavior
{
    public ushort SkillId => SkillIds.AS_ENCHANTPOISON;

    public bool Resolve(Entity source, Entity target, SkillDefinition def, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc == null) return true;
        var durationMs = 60_000 + 60_000 * skillLevel;
        ctx.Sc.Start(target, StatusType.Encpoison, val1: skillLevel, 0, 0, 0,
            durationMs, source);
        return true;
    }
}
