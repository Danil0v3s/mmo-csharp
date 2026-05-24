using Map.Server.Elemental;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SO_SUMMON_AQUA — Sorcerer Summon Water Spirit (skill.cpp:SO_SUMMON_AQUA).
/// Binds an Aqua-tier elemental: S at lv 1, M at lv 2, L at lv 3.
/// </summary>
public sealed class SummonWaterSpiritAqua : SkillImpl
{
    public SummonWaterSpiritAqua() : base(SkillIds.SO_SUMMON_AQUA) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity pc) return;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        var classId = skillLevel switch
        {
            1 => ElementalClassIds.AquaS,
            2 => ElementalClassIds.AquaM,
            _ => ElementalClassIds.AquaL,
        };
        ctx.Elemental?.Create(pc, classId, ElementalClassIds.DefaultLifetimeMs);
    }
}
