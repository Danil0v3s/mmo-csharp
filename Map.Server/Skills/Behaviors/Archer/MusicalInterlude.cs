using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// TR_MUSICAL_INTERLUDE — Trouvere/Troubadour Musical Interlude.
/// Manual port of <c>rathena-fork/src/map/skills/archer/musicalinterlude.cpp</c>.
/// Party-wide buff. Splash via party_foreachsamemap TODO.
/// </summary>
public sealed class MusicalInterlude : SkillImpl
{
    public MusicalInterlude() : base(SkillIds.TR_MUSICAL_INTERLUDE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(target, target, SkillId, skillLevel);
        ctx.Sc?.Start(target, StatusType.MusicalInterlude, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
    }
}
