using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// TR_GEF_NOCTURN — Troubadour Geffenia Nocturn. Manual port of
/// <c>rathena-fork/src/map/skills/archer/geffenianocturn.cpp</c>.
/// Chorus debuff (Magical Resistance down).
/// </summary>
public sealed class GeffeniaNocturn : SkillImpl
{
    public GeffeniaNocturn() : base(SkillIds.TR_GEF_NOCTURN) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(target, target, SkillId, skillLevel);
        ctx.Sc?.Start(target, StatusType.GefNocturn, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
    }
}
