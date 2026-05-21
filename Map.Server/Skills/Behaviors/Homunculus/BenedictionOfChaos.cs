using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Homunculus;

/// <summary>
/// HVAN_CHAOTIC — Vanilmirth Benediction of Chaos. Manual port of
/// <c>rathena-fork/src/map/skills/homunculus/homunculus_benedictionofchaos.cpp</c>.
/// Random-target heal (homunculus / master / enemy targeting master).
/// Heal pipeline + battle_get_master are TODO; we land the animation.
/// </summary>
public sealed class BenedictionOfChaos : SkillImpl
{
    public BenedictionOfChaos() : base(SkillIds.HVAN_CHAOTIC) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
}
