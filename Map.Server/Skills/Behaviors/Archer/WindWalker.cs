using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// SN_WINDWALK — Sniper Wind Walker. Manual port of
/// <c>rathena-fork/src/map/skills/archer/windwalker.cpp</c>.
///
/// <para>Party-wide ASPD / MOVE buff. Target gets the SC, then every
/// party member on the same map gets the same SC.</para>
/// </summary>
public sealed class WindWalker : SkillImpl
{
    public WindWalker() : base(SkillIds.SN_WINDWALK) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(target, StatusType.Windwalk, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(target, target, SkillId, skillLevel);

        if (src is PlayerEntity pcSrc && pcSrc.PartyId > 0 && ctx.PartyMap != null)
        {
            ctx.PartyMap.ForEachOnSameMap(pcSrc, m =>
            {
                if (m.Id.Value == target.Id.Value) return;
                ctx.Sc?.Start(m, StatusType.Windwalk, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
            }, includeSelf: false);
        }
    }
}
