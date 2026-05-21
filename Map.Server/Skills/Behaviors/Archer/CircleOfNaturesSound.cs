using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// WM_SIRCLEOFNATURE — Wanderer/Minstrel Circle of Nature's Sound.
/// Manual port of <c>rathena-fork/src/map/skills/archer/circleofnaturessound.cpp</c>.
///
/// <para>SP-regen aura. val2 carries the caster's WM_LESSON passive
/// level (lookup TODO). Party splash via party_foreachsamemap is TODO;
/// for now we land the SC on the caster.</para>
/// </summary>
public sealed class CircleOfNaturesSound : SkillImpl
{
    public CircleOfNaturesSound() : base(SkillIds.WM_SIRCLEOFNATURE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(src, StatusType.Sircleofnature, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
