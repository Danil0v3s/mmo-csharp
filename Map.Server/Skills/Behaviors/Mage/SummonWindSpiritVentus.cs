using Map.Server.Elemental;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SO_SUMMON_VENTUS — Sorcerer Summon Wind Spirit (skill.cpp:SO_SUMMON_VENTUS).
/// Binds a Ventus-tier elemental: S at lv 1, M at lv 2, L at lv 3.
/// </summary>
public sealed class SummonWindSpiritVentus : SkillImpl
{
    public SummonWindSpiritVentus() : base(SkillIds.SO_SUMMON_VENTUS) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity pc) return;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        var classId = skillLevel switch
        {
            1 => ElementalClassIds.VentusS,
            2 => ElementalClassIds.VentusM,
            _ => ElementalClassIds.VentusL,
        };
        ctx.Elemental?.Create(pc, classId, ElementalClassIds.DefaultLifetimeMs);
    }
}
