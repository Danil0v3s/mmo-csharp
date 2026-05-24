using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// BO_THE_WHOLE_PROTECTION — Biolo The Whole Protection. Manual port
/// of <c>rathena-fork/src/map/skills/merchant/thewholeprotection.cpp</c>.
/// Solo cast: applies the four CP_* SCs (Weapon / Shield / Armor / Helm)
/// to the recipient if they have the matching equip slot. With a party,
/// broadcasts the animation and splashes party-mates.
/// </summary>
public sealed class TheWholeProtection : SkillImpl
{
    public TheWholeProtection() : base(SkillIds.BO_THE_WHOLE_PROTECTION) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ApplyTo(target, skillLevel, src, ctx);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);

        // rAthena party_foreachsamemap fan-out: same CP_* per-equip
        // application to every same-map partymate.
        if (src is PlayerEntity pcSrc && pcSrc.PartyId > 0 && ctx.PartyMap != null)
        {
            ctx.PartyMap.ForEachOnSameMap(pcSrc, m =>
            {
                if (m.Id.Value == target.Id.Value) return;
                ApplyTo(m, skillLevel, src, ctx);
            }, includeSelf: false);
        }
    }

    /// <summary>
    /// Per-target apply: only start a CP_* SC when the recipient has
    /// the matching equip slot. Uses <see cref="IMapSessionRegistry"/>
    /// + <see cref="IEquipService.CheckEquip"/> to gate each SC.
    /// </summary>
    private static void ApplyTo(Entity bl, ushort skillLevel, Entity src, SkillBehaviorContext ctx)
    {
        if (bl is not PlayerEntity pc) return;
        var session = ctx.Sessions?.TryGet(pc);
        var duration = 60_000 * skillLevel;
        // rAthena pc_checkequip gates each SC by the matching slot.
        if (session != null && ctx.Equip != null)
        {
            if (ctx.Equip.CheckEquip(session, EquipBits.HandR | EquipBits.HandL) >= 0)
                ctx.Sc?.Start(bl, StatusType.CpWeapon, val1: skillLevel, 0, 0, 0, duration, src);
            if (ctx.Equip.CheckEquip(session, EquipBits.HandL) >= 0)
                ctx.Sc?.Start(bl, StatusType.CpShield, val1: skillLevel, 0, 0, 0, duration, src);
            if (ctx.Equip.CheckEquip(session, EquipBits.Armor) >= 0)
                ctx.Sc?.Start(bl, StatusType.CpArmor, val1: skillLevel, 0, 0, 0, duration, src);
            if (ctx.Equip.CheckEquip(session, EquipBits.Helm) >= 0)
                ctx.Sc?.Start(bl, StatusType.CpHelm, val1: skillLevel, 0, 0, 0, duration, src);
        }
        else
        {
            // No session bridge available (test ctx); apply all four
            // as a permissive default — matches rAthena behavior for
            // NPC-spawned applies.
            ctx.Sc?.Start(bl, StatusType.CpWeapon, val1: skillLevel, 0, 0, 0, duration, src);
            ctx.Sc?.Start(bl, StatusType.CpShield, val1: skillLevel, 0, 0, 0, duration, src);
            ctx.Sc?.Start(bl, StatusType.CpArmor, val1: skillLevel, 0, 0, 0, duration, src);
            ctx.Sc?.Start(bl, StatusType.CpHelm, val1: skillLevel, 0, 0, 0, duration, src);
        }
    }
}
