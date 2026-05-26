using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// MI_RUSH_WINDMILL — Minstrel Windmill Rush Attack. Manual port of
/// <c>rathena-fork/src/map/skills/archer/windmillrushattack.cpp</c>.
///
/// <para>Party-wide buff. val2 = WM_LESSON. Target gets the SC then
/// every party member on the same map gets it.</para>
/// </summary>
public sealed class WindmillRushAttack : SkillImpl
{
    public WindmillRushAttack() : base(SkillIds.MI_RUSH_WINDMILL) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var lesson = (src is PlayerEntity pc) ? (ctx.PlayerSkill?.CheckSkill(pc, SkillIds.WM_LESSON) ?? 0) : 0;
        ctx.Sc?.Start(target, StatusType.Rushwindmill, val1: skillLevel, val2: lesson, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);

        if (src is PlayerEntity pcSrc && pcSrc.PartyId > 0 && ctx.PartyMap != null)
        {
            ctx.PartyMap.ForEachOnSameMap(pcSrc, m =>
            {
                if (m.Id.Value == target.Id.Value) return;
                ctx.Sc?.Start(m, StatusType.Rushwindmill, val1: skillLevel, val2: lesson, 0, 0, durationMs: 60_000, src);
            }, includeSelf: false);
        }
    }
}
