using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// RA_DETONATOR — Ranger Detonator (skill.cpp:RA_DETONATOR arm).
/// Detonates all of the caster's own detonator-class traps in a 9×9
/// splash centered on the cast point. rAthena's
/// <c>skill_detonator</c> fires the trap by expiring its group (the
/// per-trap onleft hook produces the damage burst).
/// </summary>
public sealed class Detonator : SkillImpl
{
    /// <summary>9×9 splash radius (rAthena <c>AREA_SIZE</c> equivalent for this skill).</summary>
    private const short SplashRadius = 4;

    public Detonator() : base(SkillIds.RA_DETONATOR) { }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
        if (ctx.Units == null) return;
        var units = ctx.Units.GetUnitsInArea(src.MapId, x, y, SplashRadius);
        // Group set first — multiple cells of the same trap share a
        // group; only DelUnitGroup once per group.
        var groups = new HashSet<SkillUnitGroup>();
        foreach (var u in units)
        {
            if (u.Group.CasterId != src.Id) continue;
            if (!IsDetonable(u.Group.SkillId)) continue;
            groups.Add(u.Group);
        }
        foreach (var g in groups)
        {
            ctx.Units.DelUnitGroup(g);
        }
    }

    /// <summary>
    /// rAthena's <c>skill_get_inf2(skill_id) &amp; INF2_TRAP</c> trap
    /// filter combined with the per-trap Detonator-eligibility flag
    /// (<c>INF2_NOTRAP_DETONATOR</c>): not every trap is detonatable
    /// — Ankle Snare / Talkie Box / Skid Trap etc. aren't.
    /// </summary>
    private static bool IsDetonable(ushort skillId) => skillId is
        SkillIds.HT_BLASTMINE or
        SkillIds.HT_CLAYMORETRAP or
        SkillIds.HT_LANDMINE or
        SkillIds.HT_FREEZINGTRAP or
        SkillIds.HT_FLASHER or
        SkillIds.HT_SHOCKWAVE or
        SkillIds.HT_SANDMAN or
        SkillIds.RA_CLUSTERBOMB or
        SkillIds.RA_FIRINGTRAP or
        SkillIds.RA_ICEBOUNDTRAP or
        SkillIds.RA_ELECTRICSHOCKER;
}
