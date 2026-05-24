using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// AB_CANTO — Arch Bishop Canto Candidus. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/cantocandidus.cpp</c>.
///
/// <para>Party-wide Increase Agility. The applied SC level matches
/// the caster's learned <c>AL_INCAGI</c> level + JobLevel/10
/// (mob casters default to max-level Increase Agi + 5). The SC type
/// is <see cref="StatusType.IncreaseAgi"/> (same as AL_INCAGI),
/// so the buff stacks-by-refresh with regular Increase Agility.</para>
///
/// <para>Duration: 120 000 ms per skill_db.</para>
/// </summary>
public sealed class CantoCandidus : SkillImpl
{
    public CantoCandidus() : base(SkillIds.AB_CANTO) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: int32 agi_lv = ((sd) ? pc_checkskill(sd,AL_INCAGI) : skill_get_max(AL_INCAGI))
        //                       + (((sd) ? sd->status.job_level : 50) / 10);
        int agiLv;
        if (src is PlayerEntity caster)
        {
            var learnedAgi = ctx.PlayerSkill?.CheckSkill(caster, SkillIds.AL_INCAGI) ?? 0;
            agiLv = learnedAgi + (caster.JobLevel / 10);
        }
        else
        {
            // Mob caster: skill_get_max returns 10 for AL_INCAGI.
            agiLv = 10 + 50 / 10;
        }
        if (agiLv < 1) agiLv = 1;

        ctx.Client?.BroadcastSkillNoDamage(target, target, SkillId, skillLevel);
        ctx.Sc?.Start(target, StatusType.IncreaseAgi,
            val1: agiLv, 0, 0, 0, durationMs: 120_000, src);

        if (src is PlayerEntity pcSrc && pcSrc.PartyId > 0 && ctx.PartyMap != null)
        {
            ctx.PartyMap.ForEachOnSameMap(pcSrc, m =>
            {
                if (m.Id.Value == target.Id.Value) return;
                ctx.Sc?.Start(m, StatusType.IncreaseAgi, val1: agiLv, 0, 0, 0, durationMs: 120_000, src);
            }, includeSelf: false);
        }
    }
}
