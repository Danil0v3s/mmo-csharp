using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// HT_SPRINGTRAP — Hunter Spring Trap (skill.cpp:HT_SPRINGTRAP arm).
/// Manually trips a previously-laid Hunter trap on the targeted cell.
/// rAthena: walk every BL_SKILL on the cell, match groups whose
/// SkillId is INF2_TRAP, schedule them for immediate expiry so the
/// onleft / onplace hooks fire (effectively detonating).
/// </summary>
public sealed class SpringTrap : SkillImpl
{
    public SpringTrap() : base(SkillIds.HT_SPRINGTRAP) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        if (ctx.Units == null) return;
        // Walk every BL_SKILL on the targeted cell. rAthena uses
        // skill_get_inf2 & INF2_TRAP to filter — our SkillUnit doesn't
        // carry the inf2 mask yet, so we match on a known set of trap
        // skill ids (HT_* trap family + RA_*) which is the canonical
        // rAthena trap roster.
        var units = ctx.Units.GetUnitsInArea(target.MapId, target.X, target.Y, radius: 0);
        foreach (var u in units)
        {
            if (!IsTrap(u.Group.SkillId)) continue;
            // Schedule the group for immediate expiry — the SkillUnit
            // tick handler's onleft / detonate runs through DelUnitGroup.
            ctx.Units.DelUnitGroup(u.Group);
        }
    }

    private static bool IsTrap(ushort skillId) => skillId is
        SkillIds.HT_SKIDTRAP or
        SkillIds.HT_LANDMINE or
        SkillIds.HT_ANKLESNARE or
        SkillIds.HT_SHOCKWAVE or
        SkillIds.HT_SANDMAN or
        SkillIds.HT_FLASHER or
        SkillIds.HT_FREEZINGTRAP or
        SkillIds.HT_BLASTMINE or
        SkillIds.HT_CLAYMORETRAP or
        SkillIds.HT_TALKIEBOX or
        SkillIds.RA_DETONATOR or
        SkillIds.RA_CLUSTERBOMB or
        SkillIds.RA_FIRINGTRAP or
        SkillIds.RA_ICEBOUNDTRAP or
        SkillIds.RA_ELECTRICSHOCKER or
        SkillIds.RA_MAGENTATRAP or
        SkillIds.RA_COBALTTRAP or
        SkillIds.RA_MAIZETRAP or
        SkillIds.RA_VERDURETRAP;
}
