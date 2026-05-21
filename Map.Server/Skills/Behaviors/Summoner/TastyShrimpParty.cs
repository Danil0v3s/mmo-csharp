using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Summoner;

/// <summary>
/// SU_SHRIMPARTY — Summoner Tasty Shrimp Party. Manual port of
/// <c>rathena-fork/src/map/skills/summoner/tastyshrimpparty.cpp</c>.
/// Applies the party buff SC (TODO — SC enum missing). With
/// SU_FRESHSHRIMP also runs that buff via SC_FRESHSHRIMP.
/// </summary>
public sealed class TastyShrimpParty : SkillImpl
{
    public TastyShrimpParty() : base(SkillIds.SU_SHRIMPARTY) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // TODO: missing SC_SHRIMPARTY enum; fall back to SC_FRESHSHRIMP.
        ctx.Sc?.Start(target, StatusType.Freshshrimp, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
