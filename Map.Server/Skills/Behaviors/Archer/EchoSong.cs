using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// MI_ECHOSONG — Minstrel Echo Song. Manual port of
/// <c>rathena-fork/src/map/skills/archer/echosong.cpp</c>.
///
/// <para>Party-wide buff. val2 = WM_LESSON. Target gets the SC, then
/// every party member on the same map gets the same SC via
/// <see cref="Party.IPartyMapService.ForEachOnSameMap"/>.</para>
/// </summary>
public sealed class EchoSong : SkillImpl
{
    public EchoSong() : base(SkillIds.MI_ECHOSONG) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var lesson = (src is PlayerEntity pc) ? (ctx.PlayerSkill?.CheckSkill(pc, SkillIds.WM_LESSON) ?? 0) : 0;
        ctx.Sc?.Start(target, StatusType.Echosong, val1: skillLevel, val2: lesson, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);

        if (src is PlayerEntity pcSrc && pcSrc.PartyId > 0 && ctx.PartyMap != null)
        {
            ctx.PartyMap.ForEachOnSameMap(pcSrc, m =>
            {
                if (m.Id.Value == target.Id.Value) return;
                ctx.Sc?.Start(m, StatusType.Echosong, val1: skillLevel, val2: lesson, 0, 0, durationMs: 60_000, src);
            }, includeSelf: false);
        }
    }
}
