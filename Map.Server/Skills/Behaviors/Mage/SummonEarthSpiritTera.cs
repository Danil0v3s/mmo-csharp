using Map.Server.Elemental;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SO_SUMMON_TERA — Sorcerer Summon Earth Spirit (skill.cpp:SO_SUMMON_TERA).
/// Binds a Tera-tier elemental: S at lv 1, M at lv 2, L at lv 3.
/// </summary>
public sealed class SummonEarthSpiritTera : SkillImpl
{
    public SummonEarthSpiritTera() : base(SkillIds.SO_SUMMON_TERA) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity pc) return;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        var classId = skillLevel switch
        {
            1 => ElementalClassIds.TeraS,
            2 => ElementalClassIds.TeraM,
            _ => ElementalClassIds.TeraL,
        };
        ctx.Elemental?.Create(pc, classId, ElementalClassIds.DefaultLifetimeMs);
    }
}
