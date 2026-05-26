using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// TR_JAWAII_SERENADE — Trouvere Jawaii Serenade. Manual port of
/// <c>rathena-fork/src/map/skills/archer/jawaiiserenade.cpp</c>.
///
/// <para>Party-wide chorus song. The named target gets the SC, then
/// every party member on the same map gets it. A chorus-partner
/// within AREA_SIZE flips the val3 magnitude flag.</para>
/// </summary>
public sealed class JawaiiSerenade : SkillImpl
{
    public JawaiiSerenade() : base(SkillIds.TR_JAWAII_SERENADE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var val3 = 0;
        if (src is PlayerEntity pcSrc && pcSrc.PartyId > 0 && ctx.PartyMap != null)
        {
            ctx.PartyMap.ForEachOnSameMapInRange(pcSrc, 14, m =>
            {
                if (m.Id.Value == pcSrc.Id.Value) return;
                val3 |= 2;
            }, includeSelf: false);
        }

        ctx.Client?.BroadcastSkillNoDamage(target, target, SkillId, skillLevel);
        ctx.Sc?.Start(target, StatusType.JawaiiSerenade, val1: skillLevel, val2: 0, val3: val3, val4: 0, durationMs: 60_000, src);

        if (src is PlayerEntity pc2 && pc2.PartyId > 0 && ctx.PartyMap != null)
        {
            ctx.PartyMap.ForEachOnSameMap(pc2, m =>
            {
                if (m.Id.Value == target.Id.Value) return;
                ctx.Sc?.Start(m, StatusType.JawaiiSerenade, val1: skillLevel, val2: 0, val3: val3, val4: 0, durationMs: 60_000, src);
            }, includeSelf: false);
        }
    }
}
