using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// RA_WUGMASTERY — Ranger Warg Mastery. rAthena
/// (<c>skill.cpp:13129</c>): toggles <c>OPTION_WUG</c>, which spawns or
/// despawns the pet-warg sprite next to the caster. Cast on self, no
/// damage, no SC.
/// </summary>
public sealed class WargMastery : SkillImpl
{
    public WargMastery() : base(SkillIds.RA_WUGMASTERY) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity pc) return;
        var hasWug = (pc.Option & PlayerOption.Wug) != 0;
        ctx.Options?.SetWug(pc, !hasWug);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
