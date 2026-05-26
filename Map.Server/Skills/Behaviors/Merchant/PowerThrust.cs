using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// BS_OVERTHRUST — Blacksmith Power Thrust (Over Thrust). Manual port
/// of <c>rathena-fork/src/map/skills/merchant/powerthrust.cpp</c>.
/// Solo caster: applies SC_OVERTHRUST on the named target (Val2 = 1
/// when self-cast, 0 otherwise — drives the renewal +5% extra-vs-self
/// bonus). Partied caster: splashes to every same-map party member via
/// <see cref="Map.Server.Party.IPartyMapService"/>.
///
/// <para>Weapon-type gate (rAthena <c>skill_get_weapontype</c> +
/// <c>pc_check_weapontype</c>) — 🚩 INFRA-DEFERRED until skill-db
/// weapontype masks are surfaced on <c>SkillDefinition</c>. For now
/// every weapon class is accepted (matches the conservative
/// pass-through used by sibling skills).</para>
/// </summary>
public sealed class PowerThrust : SkillImpl
{
    public PowerThrust() : base(SkillIds.BS_OVERTHRUST) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is PlayerEntity pcSrc && pcSrc.PartyId > 0 && ctx.PartyMap != null)
        {
            ctx.PartyMap.ForEachOnSameMap(pcSrc, m =>
            {
                var selfFlag = src.Id == m.Id ? 1 : 0;
                ctx.Sc?.Start(m, StatusType.Overthrust, val1: skillLevel, val2: selfFlag, 0, 0, durationMs: 180_000, src);
                ctx.Client?.BroadcastSkillNoDamage(m, m, SkillId, skillLevel);
            }, includeSelf: true);
            return;
        }
        var self = src.Id == target.Id ? 1 : 0;
        ctx.Sc?.Start(target, StatusType.Overthrust, val1: skillLevel, val2: self, 0, 0, durationMs: 180_000, src);
        ctx.Client?.BroadcastSkillNoDamage(target, target, SkillId, skillLevel);
    }
}
