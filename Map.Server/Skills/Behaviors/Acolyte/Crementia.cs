using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// AB_CLEMENTIA — Arch Bishop Clementia. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/crementia.cpp</c>.
///
/// <para>Party-wide Blessing. The applied SC is
/// <see cref="StatusType.Blessing"/> with level =
/// <c>caster's learned AL_BLESSING level + JobLevel/10</c>
/// (mob casters use Blessing's max level + 5 as fallback).</para>
///
/// <para>Duration: 120 000 ms per skill_db.</para>
/// </summary>
public sealed class Crementia : SkillImpl
{
    public Crementia() : base(SkillIds.AB_CLEMENTIA) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: bless_lv = pc_checkskill(sd, AL_BLESSING) + job_lv/10;
        int blessLv;
        if (src is PlayerEntity sd)
        {
            var learned = ctx.PlayerSkill?.CheckSkill(sd, SkillIds.AL_BLESSING) ?? 0;
            blessLv = learned + sd.JobLevel / 10;
        }
        else
        {
            blessLv = 10 + 50 / 10;
        }
        if (blessLv < 1) blessLv = 1;

        ctx.Client?.BroadcastSkillNoDamage(target, target, SkillId, skillLevel);
        ctx.Sc?.Start(target, StatusType.Blessing,
            val1: blessLv, 0, 0, 0, durationMs: 120_000, src);

        if (src is PlayerEntity pcSrc && pcSrc.PartyId > 0 && ctx.PartyMap != null)
        {
            ctx.PartyMap.ForEachOnSameMap(pcSrc, m =>
            {
                if (m.Id.Value == target.Id.Value) return;
                ctx.Sc?.Start(m, StatusType.Blessing, val1: blessLv, 0, 0, 0, durationMs: 120_000, src);
            }, includeSelf: false);
        }
    }
}
