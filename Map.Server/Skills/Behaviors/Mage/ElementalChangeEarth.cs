using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SA_ELEMENTGROUND — Sage Elemental Change (Earth). Manual port of
/// <c>rathena-fork/src/map/skills/mage/elementalchangeearth.cpp</c>.
/// Same shape as <see cref="ElementalChangeFire"/> — flips defensive
/// element to Earth.
/// </summary>
public sealed class ElementalChangeEarth : SkillImpl
{
    public ElementalChangeEarth() : base(SkillIds.SA_ELEMENTGROUND) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is PlayerEntity)
        {
            if (target is not MobEntity) return;
            if ((target.Stats.Mode & MobMode.StatusImmune) != 0) return;
        }
        ctx.Sc?.Start(target, StatusType.Elementalchange, val1: skillLevel, val2: (int)BattleElement.Earth, 0, 0, durationMs: 30_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
