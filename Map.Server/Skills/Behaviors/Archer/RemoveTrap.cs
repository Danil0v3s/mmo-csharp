using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// HT_REMOVETRAP — Hunter Remove Trap (skill.cpp:HT_REMOVETRAP arm).
/// Picks up a trap on the targeted cell — only the caster's own trap
/// (in non-PvP zones) — and removes it. rAthena also refunds either
/// the deploy item or the configured trap-back item via
/// <c>battle_config.skill_removetrap_type</c>; the deploy-item refund
/// requires the per-trap-skill ItemConsume row from <c>skill_db.yml</c>
/// which our SkillDef doesn't surface today, so the refund half is
/// flagged as a data-pending follow-up while the dispel half lands.
/// </summary>
public sealed class RemoveTrap : SkillImpl
{
    public RemoveTrap() : base(SkillIds.HT_REMOVETRAP) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity pc) return;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        if (ctx.Units == null) return;
        var units = ctx.Units.GetUnitsInArea(target.MapId, target.X, target.Y, radius: 0);
        foreach (var u in units)
        {
            if (!IsHunterTrap(u.Group.SkillId)) continue;
            // Only the caster can remove their own traps in non-PvP
            // (rAthena gate: sg->src_id != sd->bl.id branches to
            // skill_removetrap_type config); strict-own-trap is the
            // safe-default behavior.
            if (u.Group.CasterId != pc.Id) continue;
            ctx.Units.DelUnitGroup(u.Group);
            break; // one trap per cast (matches rAthena single-target select).
        }
    }

    private static bool IsHunterTrap(ushort skillId) => skillId is
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
        SkillIds.RA_CLUSTERBOMB or
        SkillIds.RA_FIRINGTRAP or
        SkillIds.RA_ICEBOUNDTRAP or
        SkillIds.RA_ELECTRICSHOCKER or
        SkillIds.RA_MAGENTATRAP or
        SkillIds.RA_COBALTTRAP or
        SkillIds.RA_MAIZETRAP or
        SkillIds.RA_VERDURETRAP;
}
