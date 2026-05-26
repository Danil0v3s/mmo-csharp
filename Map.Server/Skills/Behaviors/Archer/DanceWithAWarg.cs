using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// WM_DANCE_WITH_WUG — Minstrel/Wanderer Dance With a Warg. Manual
/// port of <c>rathena-fork/src/map/skills/archer/dancewithawarg.cpp</c>.
///
/// <para>Party-wide ASPD buff. val2 carries WM_LESSON. The caster
/// gets the SC, then every party member on the same map within the
/// splash radius gets the same SC via
/// <see cref="Party.IPartyMapService.ForEachOnSameMap"/>.</para>
/// </summary>
public sealed class DanceWithAWarg : SkillImpl
{
    public DanceWithAWarg() : base(SkillIds.WM_DANCE_WITH_WUG) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var lesson = (src is PlayerEntity pc) ? (ctx.PlayerSkill?.CheckSkill(pc, SkillIds.WM_LESSON) ?? 0) : 0;
        ctx.Sc?.Start(src, StatusType.Dancewithwug, val1: skillLevel, val2: lesson, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);

        if (src is PlayerEntity pcSrc && pcSrc.PartyId > 0 && ctx.PartyMap != null)
        {
            ctx.PartyMap.ForEachOnSameMap(pcSrc, m =>
            {
                if (m.Id.Value == pcSrc.Id.Value) return;
                ctx.Sc?.Start(m, StatusType.Dancewithwug, val1: skillLevel, val2: lesson, 0, 0, durationMs: 60_000, src);
            }, includeSelf: false);
        }
    }
}
