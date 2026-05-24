using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// IG_ULTIMATE_SACRIFICE — Imperial Guard Ultimate Sacrifice. Manual
/// port of <c>rathena-fork/src/map/skills/swordman/ultimatesacrifice.cpp</c>.
/// Sacrifices the caster (HP → 1) and splashes the heal SC to party
/// members. Reuses <see cref="StatusType.Sacrifice"/> as the SC type;
/// SC_ULTIMATE_SACRIFICE has no dedicated enum slot in this port —
/// the parent Sacrifice SC carries the +HP-pool effect via Val1.
/// </summary>
public sealed class UltimateSacrifice : SkillImpl
{
    public UltimateSacrifice() : base(SkillIds.IG_ULTIMATE_SACRIFICE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: status_set_hp(src, 1, 0) on every cast.
        if (src is PlayerEntity sd) sd.Hp = 1;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);

        // Party fan-out per rAthena skill.cpp: BCT_PARTY|1 — every
        // same-map partymate gets the Sacrifice SC.
        var duration = 30_000;
        if (src is PlayerEntity pcSrc && pcSrc.PartyId > 0 && ctx.PartyMap != null)
        {
            ctx.PartyMap.ForEachOnSameMap(pcSrc, m =>
            {
                ctx.Sc?.Start(m, StatusType.Sacrifice, val1: skillLevel, 0, 0, 0, duration, src);
            }, includeSelf: true);
        }
        else
        {
            ctx.Sc?.Start(target, StatusType.Sacrifice, val1: skillLevel, 0, 0, 0, duration, src);
        }
    }
}
