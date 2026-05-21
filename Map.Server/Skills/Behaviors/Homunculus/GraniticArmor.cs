using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Homunculus;

/// <summary>
/// MH_GRANITIC_ARMOR — Homunculus Granitic Armor. Manual port of
/// <c>rathena-fork/src/map/skills/homunculus/homunculus_graniticarmor.cpp</c>.
/// Applies SC_GRANITIC_ARMOR to both target (master) and self. Master
/// lookup is TODO; we land on the named target.
/// </summary>
public sealed class GraniticArmor : SkillImpl
{
    public GraniticArmor() : base(SkillIds.MH_GRANITIC_ARMOR) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(target, StatusType.GraniticArmor, val1: skillLevel, val2: src.Level, 0, 0, durationMs: 60_000, src);
        ctx.Sc?.Start(src, StatusType.GraniticArmor, val1: skillLevel, val2: src.Level, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
