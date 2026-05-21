using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SA_ELEMENTWATER — Sage Elemental Change (Water). Manual port of
/// <c>rathena-fork/src/map/skills/mage/elementalchangewater.cpp</c>.
/// Same shape as <see cref="ElementalChangeFire"/> — flips the target's
/// defensive element to Water.
/// </summary>
public sealed class ElementalChangeWater : SkillImpl
{
    public ElementalChangeWater() : base(SkillIds.SA_ELEMENTWATER) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is PlayerEntity)
        {
            if (target is not MobEntity) return;
            if ((target.Stats.Mode & MobMode.StatusImmune) != 0) return;
        }
        ctx.Sc?.Start(target, StatusType.Elementalchange, val1: skillLevel, val2: (int)BattleElement.Water, 0, 0, durationMs: 30_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
