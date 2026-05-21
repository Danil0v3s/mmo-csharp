using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// WM_SOUND_OF_DESTRUCTION — Minstrel/Wanderer Sound of Destruction.
/// Manual port of <c>rathena-fork/src/map/skills/archer/soundofdestruction.cpp</c>.
/// Splash debuff. WM_LESSON duration bonus TODO.
/// </summary>
public sealed class SoundOfDestruction : SkillImpl
{
    public SoundOfDestruction() : base(SkillIds.WM_SOUND_OF_DESTRUCTION) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(target, StatusType.Soundofdestruction, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
