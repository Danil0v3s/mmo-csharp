using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// WM_DOMINION_IMPULSE — Minstrel/Wanderer Dominion Impulse
/// (skill.cpp:WM_DOMINION_IMPULSE arm). Triggers every
/// <c>WM_REVERBERATION</c> ground unit inside a 7×7 splash centered on
/// the cast point. rAthena's <c>skill_active_reverberation</c> expires
/// the group early so its onleft hook (the actual reverb damage)
/// fires.
/// </summary>
public sealed class DominionImpulse : SkillImpl
{
    /// <summary>7×7 splash radius (skill.cpp WM_DOMINION_IMPULSE AREA_SIZE).</summary>
    private const short SplashRadius = 3;

    public DominionImpulse() : base(SkillIds.WM_DOMINION_IMPULSE) { }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
        if (ctx.Units == null) return;
        var units = ctx.Units.GetUnitsInArea(src.MapId, x, y, SplashRadius, SkillIds.WM_REVERBERATION);
        var groups = new HashSet<SkillUnitGroup>();
        foreach (var u in units) groups.Add(u.Group);
        foreach (var g in groups)
        {
            ctx.Units.DelUnitGroup(g);
        }
    }
}
