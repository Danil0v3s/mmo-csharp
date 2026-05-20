using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors;

/// <summary>
/// PR_IMPOSITIO (id 66) — Priest Impositio Manus. rAthena
/// <c>skill.cpp:case PR_IMPOSITIO</c>: applies
/// <see cref="StatusType.Impositio"/> on the target with
/// <c>Val1 = lv * 5</c> → +5 ATK per level (flat WeaponAtk boost).
/// Duration <c>60_000</c> ms in renewal.
/// </summary>
public sealed class ImpositioManusBehavior : ISkillBehavior
{
    public ushort SkillId => SkillIds.PR_IMPOSITIO;

    public bool Resolve(Entity source, Entity target, SkillDefinition def, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc == null) return true;
        ctx.Sc.Start(target, StatusType.Impositio, val1: skillLevel * 5, 0, 0, 0,
            durationMs: 60_000, source);
        return true;
    }
}
