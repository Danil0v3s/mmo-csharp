using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// WM_FRIGG_SONG — Minstrel/Wanderer Frigg's Song. Manual port of
/// <c>rathena-fork/src/map/skills/archer/friggssong.cpp</c>.
/// Party-wide MaxHP buff. Splash via party_foreachsamemap TODO.
/// </summary>
public sealed class FriggsSong : SkillImpl
{
    public FriggsSong() : base(SkillIds.WM_FRIGG_SONG) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(target, StatusType.FriggSong, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(target, target, SkillId, skillLevel);
    }
}
