using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// EM_SUMMON_ELEMENTAL_ARDOR — Elemental Master Summon Ardor.
/// Manual port of <c>rathena-fork/src/map/skills/mage/summonelementalardor.cpp</c>.
///
/// <para>Promotes a level-3 Agni elemental to its super (Ardor) form
/// and buffs the caster with SC_SUMMON_ELEMENTAL_ARDOR. The bound
/// elemental upgrade is TODO — we land the broadcast + caster buff
/// here.</para>
/// </summary>
public sealed class SummonElementalArdor : SkillImpl
{
    public SummonElementalArdor() : base(SkillIds.EM_SUMMON_ELEMENTAL_ARDOR) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        if (src is not PlayerEntity) return;
        // TODO: verify bound elemental is AGNI_L, delete + create ARDOR.
        ctx.Sc?.Start(src, StatusType.SummonElementalArdor, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
    }
}
