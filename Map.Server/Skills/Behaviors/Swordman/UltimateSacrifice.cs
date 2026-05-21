using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// IG_ULTIMATE_SACRIFICE — Imperial Guard Ultimate Sacrifice. Manual
/// port of <c>rathena-fork/src/map/skills/swordman/ultimatesacrifice.cpp</c>.
/// Sacrifices the caster (HP → 1) and splashes the heal SC to party
/// members. Party splash + SC enum (likely SC_HOLY_OIL/SACRIFICE) are
/// TODO; for now we land the animation and drop the caster to 1 HP.
/// </summary>
public sealed class UltimateSacrifice : SkillImpl
{
    public UltimateSacrifice() : base(SkillIds.IG_ULTIMATE_SACRIFICE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is PlayerEntity sd)
            sd.Hp = 1;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        // TODO: party splash with SC_ULTIMATE_SACRIFICE (enum missing).
    }
}
