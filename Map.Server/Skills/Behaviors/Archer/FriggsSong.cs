using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// WM_FRIGG_SONG — Minstrel/Wanderer Frigg's Song. Manual port of
/// <c>rathena-fork/src/map/skills/archer/friggssong.cpp</c>.
///
/// <para>Party-wide MaxHP buff. Target gets the SC, then every party
/// member on the same map gets the same SC.</para>
/// </summary>
public sealed class FriggsSong : SkillImpl
{
    public FriggsSong() : base(SkillIds.WM_FRIGG_SONG) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(target, StatusType.FriggSong, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(target, target, SkillId, skillLevel);

        if (src is PlayerEntity pcSrc && pcSrc.PartyId > 0 && ctx.PartyMap != null)
        {
            ctx.PartyMap.ForEachOnSameMap(pcSrc, m =>
            {
                if (m.Id.Value == target.Id.Value) return;
                ctx.Sc?.Start(m, StatusType.FriggSong, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
            }, includeSelf: false);
        }
    }
}
