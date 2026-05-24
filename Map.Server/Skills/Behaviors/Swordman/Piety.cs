using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// LG_PIETY — Royal Guard Piety. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/piety.cpp</c>.
/// Solo cast applies Holy-element endow to the target; with a party
/// splashes to nearby party members and self. rAthena uses
/// <c>SC_BENEDICTIO</c> as the SC type for Piety (skill_db Status
/// column); C# port maps the same way via
/// <see cref="StatusType.Benedictio"/> (Mdef increase + Holy element
/// resist for the duration).
/// </summary>
public sealed class Piety : SkillImpl
{
    public Piety() : base(SkillIds.LG_PIETY) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var duration = 60_000 + 30_000 * skillLevel; // skill_db default for LG_PIETY
        ctx.Sc?.Start(target, StatusType.Benedictio, val1: skillLevel, 0, 0, 0, duration, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);

        // rAthena party_foreachsamemap fan-out per skill.cpp:12009 (BCT_PARTY|BCT_SELF|1).
        if (src is PlayerEntity pcSrc && pcSrc.PartyId > 0 && ctx.PartyMap != null)
        {
            ctx.PartyMap.ForEachOnSameMap(pcSrc, m =>
            {
                if (m.Id.Value == target.Id.Value) return;
                ctx.Sc?.Start(m, StatusType.Benedictio, val1: skillLevel, 0, 0, 0, duration, src);
            }, includeSelf: false);
        }
    }
}
