using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// BS_WEAPONPERFECT — Blacksmith Weapon Perfection. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/weaponperfection.cpp</c>.
/// Solo cast applies SC_WEAPONPERFECTION to the target; partied caster
/// also splashes the buff to every same-map party member via
/// <see cref="Map.Server.Party.IPartyMapService"/>.
///
/// <para>Weapon-type gate (rAthena <c>skill_get_weapontype</c> +
/// <c>pc_check_weapontype</c>) — 🚩 INFRA-DEFERRED until skill-db
/// weapontype masks are surfaced on <c>SkillDefinition</c>. For now
/// every weapon class is accepted.</para>
/// </summary>
public sealed class WeaponPerfection : SkillImpl
{
    public WeaponPerfection() : base(SkillIds.BS_WEAPONPERFECT) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var self = src == target ? 1 : 0;
        ctx.Sc?.Start(target, StatusType.Weaponperfection, val1: skillLevel, val2: self, 0, 0, durationMs: 60_000 * skillLevel, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);

        if (src is PlayerEntity pcSrc && pcSrc.PartyId > 0 && ctx.PartyMap != null)
        {
            ctx.PartyMap.ForEachOnSameMap(pcSrc, m =>
            {
                if (m.Id.Value == target.Id.Value) return;
                ctx.Sc?.Start(m, StatusType.Weaponperfection, val1: skillLevel, val2: 0, 0, 0, durationMs: 60_000 * skillLevel, src);
            }, includeSelf: false);
        }
    }
}
