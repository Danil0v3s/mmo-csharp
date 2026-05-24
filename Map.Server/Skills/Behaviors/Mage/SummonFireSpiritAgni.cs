using Map.Server.Elemental;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SO_SUMMON_AGNI — Sorcerer Summon Fire Spirit (skill.cpp:SO_SUMMON_AGNI).
/// Binds an Agni-tier elemental to the caster: <c>S</c> at lv 1,
/// <c>M</c> at lv 2, <c>L</c> at lv 3. Replaces any active elemental
/// (elemental_delete-before-create pattern).
/// </summary>
public sealed class SummonFireSpiritAgni : SkillImpl
{
    public SummonFireSpiritAgni() : base(SkillIds.SO_SUMMON_AGNI) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity pc) return;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        var classId = skillLevel switch
        {
            1 => ElementalClassIds.AgniS,
            2 => ElementalClassIds.AgniM,
            _ => ElementalClassIds.AgniL,
        };
        ctx.Elemental?.Create(pc, classId, ElementalClassIds.DefaultLifetimeMs);
    }
}
