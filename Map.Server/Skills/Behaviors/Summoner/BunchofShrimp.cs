using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Summoner;

/// <summary>
/// SU_BUNCHOFSHRIMP — Summoner Bunch of Shrimp. Manual port of
/// <c>rathena-fork/src/map/skills/summoner/bunchofshrimp.cpp</c>.
/// Buffs target with the bunch-of-shrimp SC + fans out to same-map
/// partymates. Duration is extended when the caster has
/// <c>SU_SPIRITOFSEA</c> learned (rAthena pc_checkskill read).
/// </summary>
public sealed class BunchofShrimp : SkillImpl
{
    public BunchofShrimp() : base(SkillIds.SU_BUNCHOFSHRIMP) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var duration = 60_000 + 30_000 * skillLevel;
        // rAthena: SU_SPIRITOFSEA mastery extends the duration. The
        // mastery isn't on our SkillIds catalog yet (4th-class Doram
        // Spirit Handler); duration extension lands when the skill id
        // surfaces.

        ctx.Sc?.Start(target, StatusType.Shrimp, val1: skillLevel, 0, 0, 0, duration, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);

        if (src is PlayerEntity pcSrc2 && pcSrc2.PartyId > 0 && ctx.PartyMap != null)
        {
            ctx.PartyMap.ForEachOnSameMap(pcSrc2, m =>
            {
                if (m.Id.Value == target.Id.Value) return;
                ctx.Sc?.Start(m, StatusType.Shrimp, val1: skillLevel, 0, 0, 0, duration, src);
            }, includeSelf: false);
        }
    }
}
