using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// BS_WEAPONPERFECT — Blacksmith Weapon Perfection. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/weaponperfection.cpp</c>.
/// Solo cast applies SC_WEAPONPERFECTION to the target; with a party
/// the splash version is broadcast to all party members. Weapon-type
/// equip check + party splash are TODO.
/// </summary>
public sealed class WeaponPerfection : SkillImpl
{
    public WeaponPerfection() : base(SkillIds.BS_WEAPONPERFECT) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var self = src == target ? 1 : 0;
        ctx.Sc?.Start(target, StatusType.Weaponperfection, val1: skillLevel, val2: self, 0, 0, durationMs: 60_000 * skillLevel, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        // TODO: party_foreachsamemap splash when src is in a party.
    }
}
