using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SA_ELEMENTFIRE — Sage Endow / Elemental Change (Fire). Manual port of
/// <c>rathena-fork/src/map/skills/mage/elementalchangefire.cpp</c>.
///
/// <para>Forces the target's defensive element to Fire via SC_ELEMENTALCHANGE
/// (val2 = skill element). rAthena restricts the cast: a player caster
/// can only land it on monsters and never on status-immune (MVP/Boss)
/// monsters; we mirror both gates here.</para>
/// </summary>
public sealed class ElementalChangeFire : SkillImpl
{
    public ElementalChangeFire() : base(SkillIds.SA_ELEMENTFIRE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: player caster can only land on non-status-immune mobs.
        if (src is PlayerEntity)
        {
            if (target is not MobEntity)
                return;
            if ((target.Stats.Mode & MobMode.StatusImmune) != 0)
                return;
        }
        ctx.Sc?.Start(target, StatusType.Elementalchange, val1: skillLevel, val2: (int)BattleElement.Fire, 0, 0, durationMs: 30_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
