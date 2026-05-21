using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// MI_ECHOSONG — Minstrel Echo Song. Manual port of
/// <c>rathena-fork/src/map/skills/archer/echosong.cpp</c>.
/// Party-wide WM_LESSON-scaled buff; WM_LESSON passive lookup TODO.
/// Splash via party_foreachsamemap TODO; lands on target.
/// </summary>
public sealed class EchoSong : SkillImpl
{
    public EchoSong() : base(SkillIds.MI_ECHOSONG) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(target, StatusType.Echosong, val1: skillLevel, val2: 5, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
