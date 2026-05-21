using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SA_ELEMENTWIND — Sage Elemental Change (Wind). Manual port of
/// <c>rathena-fork/src/map/skills/mage/elementalchangewind.cpp</c>.
/// Same shape as <see cref="ElementalChangeFire"/> — flips defensive
/// element to Wind.
/// </summary>
public sealed class ElementalChangeWind : SkillImpl
{
    public ElementalChangeWind() : base(SkillIds.SA_ELEMENTWIND) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is PlayerEntity)
        {
            if (target is not MobEntity) return;
            if ((target.Stats.Mode & MobMode.StatusImmune) != 0) return;
        }
        ctx.Sc?.Start(target, StatusType.Elementalchange, val1: skillLevel, val2: (int)BattleElement.Wind, 0, 0, durationMs: 30_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
