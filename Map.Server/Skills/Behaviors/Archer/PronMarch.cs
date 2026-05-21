using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// TR_PRON_MARCH — Trouvere/Troubadour Pron March. Manual port of
/// <c>rathena-fork/src/map/skills/archer/pronmarch.cpp</c>.
/// Party-wide chorus song. Splash via party_foreachsamemap TODO.
/// </summary>
public sealed class PronMarch : SkillImpl
{
    public PronMarch() : base(SkillIds.TR_PRON_MARCH) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(target, target, SkillId, skillLevel);
        ctx.Sc?.Start(target, StatusType.PronMarch, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
    }
}
