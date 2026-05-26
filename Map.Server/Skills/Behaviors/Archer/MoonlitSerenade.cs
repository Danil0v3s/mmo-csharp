using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// WA_MOONLIT_SERENADE — Wanderer Moonlit Serenade. Manual port of
/// <c>rathena-fork/src/map/skills/archer/moonlitserenade.cpp</c>.
///
/// <para>Party-wide buff. val2 = WM_LESSON. Target gets the SC, then
/// every party member on the same map gets it too.</para>
/// </summary>
public sealed class MoonlitSerenade : SkillImpl
{
    public MoonlitSerenade() : base(SkillIds.WA_MOONLIT_SERENADE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var lesson = (src is PlayerEntity pc) ? (ctx.PlayerSkill?.CheckSkill(pc, SkillIds.WM_LESSON) ?? 0) : 0;
        ctx.Sc?.Start(target, StatusType.Moonlitserenade, val1: skillLevel, val2: lesson, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);

        if (src is PlayerEntity pcSrc && pcSrc.PartyId > 0 && ctx.PartyMap != null)
        {
            ctx.PartyMap.ForEachOnSameMap(pcSrc, m =>
            {
                if (m.Id.Value == target.Id.Value) return;
                ctx.Sc?.Start(m, StatusType.Moonlitserenade, val1: skillLevel, val2: lesson, 0, 0, durationMs: 60_000, src);
            }, includeSelf: false);
        }
    }
}
