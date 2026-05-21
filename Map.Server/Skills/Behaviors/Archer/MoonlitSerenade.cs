using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// WA_MOONLIT_SERENADE — Wanderer Moonlit Serenade. Manual port of
/// <c>rathena-fork/src/map/skills/archer/moonlitserenade.cpp</c>.
/// Party-wide buff. Splash via party_foreachsamemap TODO; lands on
/// the target.
/// </summary>
public sealed class MoonlitSerenade : SkillImpl
{
    public MoonlitSerenade() : base(SkillIds.WA_MOONLIT_SERENADE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(target, StatusType.Moonlitserenade, val1: skillLevel, val2: 5, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
