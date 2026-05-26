using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// WM_SIRCLEOFNATURE — Wanderer/Minstrel Circle of Nature's Sound.
/// Manual port of <c>rathena-fork/src/map/skills/archer/circleofnaturessound.cpp</c>.
///
/// <para>SP-regen aura. val2 carries WM_LESSON. Caster gets the SC
/// then every nearby party member on the same map gets the SC.</para>
/// </summary>
public sealed class CircleOfNaturesSound : SkillImpl
{
    public CircleOfNaturesSound() : base(SkillIds.WM_SIRCLEOFNATURE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var lesson = (src is PlayerEntity pc) ? (ctx.PlayerSkill?.CheckSkill(pc, SkillIds.WM_LESSON) ?? 0) : 0;
        ctx.Sc?.Start(src, StatusType.Sircleofnature, val1: skillLevel, val2: lesson, 0, 0, durationMs: 30_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);

        if (src is PlayerEntity pcSrc && pcSrc.PartyId > 0 && ctx.PartyMap != null)
        {
            ctx.PartyMap.ForEachOnSameMap(pcSrc, m =>
            {
                if (m.Id.Value == pcSrc.Id.Value) return;
                ctx.Sc?.Start(m, StatusType.Sircleofnature, val1: skillLevel, val2: lesson, 0, 0, durationMs: 30_000, src);
            }, includeSelf: false);
        }
    }
}
