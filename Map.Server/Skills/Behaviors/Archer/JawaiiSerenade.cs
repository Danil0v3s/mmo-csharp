using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// TR_JAWAII_SERENADE — Trouvere Jawaii Serenade. Manual port of
/// <c>rathena-fork/src/map/skills/archer/jawaiiserenade.cpp</c>.
/// Party-wide chorus song. Splash + partner detection TODO; lands
/// on target.
/// </summary>
public sealed class JawaiiSerenade : SkillImpl
{
    public JawaiiSerenade() : base(SkillIds.TR_JAWAII_SERENADE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(target, target, SkillId, skillLevel);
        ctx.Sc?.Start(target, StatusType.JawaiiSerenade, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
    }
}
