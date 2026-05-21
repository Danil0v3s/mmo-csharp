using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Summoner;

/// <summary>
/// SU_TUNAPARTY — Summoner Tuna Party. Manual port of
/// <c>rathena-fork/src/map/skills/summoner/tunaparty.cpp</c>.
/// Applies SC_TUNAPARTY.
/// </summary>
public sealed class TunaParty : SkillImpl
{
    public TunaParty() : base(SkillIds.SU_TUNAPARTY) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(target, StatusType.Tunaparty, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
