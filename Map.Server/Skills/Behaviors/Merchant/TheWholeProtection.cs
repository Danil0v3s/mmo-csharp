using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// BO_THE_WHOLE_PROTECTION — Biolo The Whole Protection. Manual port
/// of <c>rathena-fork/src/map/skills/merchant/thewholeprotection.cpp</c>.
/// Solo cast: applies the four CP_* SCs (Weapon / Shield / Armor / Helm)
/// to the recipient if they have the matching equip slot. With a party,
/// broadcasts the animation and splashes party-mates instead.
/// Equip-slot gating + party splash are TODO.
/// </summary>
public sealed class TheWholeProtection : SkillImpl
{
    public TheWholeProtection() : base(SkillIds.BO_THE_WHOLE_PROTECTION) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (target is PlayerEntity)
        {
            // TODO: per equip slot — only start the SC if pc_checkequip(target, EQP_*) >= 0.
            ctx.Sc?.Start(target, StatusType.CpWeapon, val1: skillLevel, 0, 0, 0, durationMs: 60_000 * skillLevel, src);
            ctx.Sc?.Start(target, StatusType.CpShield, val1: skillLevel, 0, 0, 0, durationMs: 60_000 * skillLevel, src);
            ctx.Sc?.Start(target, StatusType.CpArmor, val1: skillLevel, 0, 0, 0, durationMs: 60_000 * skillLevel, src);
            ctx.Sc?.Start(target, StatusType.CpHelm, val1: skillLevel, 0, 0, 0, durationMs: 60_000 * skillLevel, src);
        }
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        // TODO: party_foreachsamemap splash with BCT_PARTY when src is in a party.
    }
}
