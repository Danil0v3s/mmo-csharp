using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// HT_REMOVETRAP — Hunter Remove Trap. Manual port of
/// <c>rathena-fork/src/map/skills/archer/removetrap.cpp</c>.
///
/// <para>Picks up a trap on the targeted cell — only the caster's own
/// trap (in non-PvP zones) — and removes it. Refund mirrors rAthena
/// with <c>battle_config.skill_removetrap_type = 0</c> (default):
/// returns one generic Trap item (item id 1065). The skill_removetrap_type
/// = 1 path (refund every recipe-listed ItemConsume material) requires
/// the per-trap-skill require list from <c>skill_db.yml</c> which our
/// SkillDef doesn't surface today; the single-Trap refund is the
/// canonical default and lands here.</para>
/// </summary>
public sealed class RemoveTrap : SkillImpl
{
    public RemoveTrap() : base(SkillIds.HT_REMOVETRAP) { }

    /// <summary>rAthena <c>ITEMID_TRAP</c> — the generic "Trap" item
    /// granted on default-mode removal. id 1065 in item_db.</summary>
    private const uint ItemIdTrap = 1065;

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

            // rAthena: refund 1 Trap item back to the caster's bag
            // (battle_config.skill_removetrap_type == 0 path —
            // su->group->item_id or ITEMID_TRAP fallback).
            if (ctx.Sessions != null && ctx.Inventory != null)
            {
                var session = ctx.Sessions.TryGet(pc);
                if (session != null)
                {
                    ctx.Inventory.GiveItem(session, ItemIdTrap, 1);
                }
            }

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
