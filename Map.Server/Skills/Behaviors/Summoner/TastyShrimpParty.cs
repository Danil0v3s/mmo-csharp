using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Summoner;

/// <summary>
/// SU_SHRIMPARTY — Summoner Tasty Shrimp Party. Manual port of
/// <c>rathena-fork/src/map/skills/summoner/tastyshrimpparty.cpp</c>.
/// Applies the party buff SC (SC_SHRIMPARTY enum not yet registered —
/// deferred). With SU_FRESHSHRIMP also runs that buff via SC_FRESHSHRIMP.
/// </summary>
public sealed class TastyShrimpParty : SkillImpl
{
    public TastyShrimpParty() : base(SkillIds.SU_SHRIMPARTY) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // SC_SHRIMPARTY isn't on the enum yet; use SC_FRESHSHRIMP which
        // shares the per-tick MaxHp regen consumer (rAthena
        // status.cpp: same handler routing for both names).
        ctx.Sc?.Start(target, StatusType.Freshshrimp, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
